using System;
using ExplodeIt.Core;
using ExplodeIt.Stage;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 상태 전환은 이 클래스만 한다. 하위 클래스는 공격 상태로 직접 들어갈 수 없으므로
    // 모든 공격은 반드시 예고 상태를 거친다.
    [RequireComponent(typeof(Rigidbody2D), typeof(HitReceiver))]
    public abstract class Enemy : MonoBehaviour, IPullable
    {
        // 적 공격은 순차적으로 처리되므로 버퍼 하나를 모든 적이 공유한다.
        private static readonly Collider2D[] AttackBuffer = new Collider2D[8];
        private static readonly RaycastHit2D[] CastBuffer = new RaycastHit2D[4];
        private static readonly Collider2D[] SeparationBuffer = new Collider2D[8];

        // 씬에 직접 배치해 테스트할 때만 인스펙터로 넣는다. 스폰 시에는 Initialize로 주입한다.
        [SerializeField] private Transform _target;
        private Collider2D _targetCollider;
        [SerializeField] private LayerMask _attackMask;
        [SerializeField] private LayerMask _obstacleMask;
        // 벽·구조물에 부딪히는 발 판정. 꺼 둔 원 콜라이더로 크기(반경)만 읽고 물리에는 참여하지 않는다.
        // 몸에 붙은 콜라이더는 폭탄에 맞는 판정이라 그림보다 크게 잡혀 있어, 그대로 벽 검사에 쓰면 좁은 틈을 못 지나간다.
        // 비워 두면 피격 콜라이더 크기를 쓴다(보스 등).
        [SerializeField] private CircleCollider2D _moveCollider;

        private HitReceiver _hitReceiver;
        private ContactFilter2D _attackFilter;
        private ContactFilter2D _obstacleFilter;
        private ContactFilter2D _separationFilter;
        private Action<Enemy> _died;
        private Action<Enemy> _release;
        private EnemyState _state;
        private float _stateTime;
        private float _arenaTime;
        // 맵 안에 들어와 싸우는 적 수(보스 제외). 판 전체에서 하나라 정적으로 센다.
        private static int _arenaCount;
        private bool _isCountedInArena;
        private float _lastPulledTime;
        private float _stunDuration;
        // 공격 거리 안에서 플레이어가 계속 보인 시간 (초).
        private float _sightTime;
        private Collider2D _collider;
        // 맵 입구로 먼저 걸어가는 중인지. 다 걸어가면 평소 이동(플레이어 추적)으로 넘어간다.
        private bool _hasEntryPoint;
        private float[] _telegraphOverrides;
        private float _recoverOverride;
        private Vector2 _entryPoint;
        private float _entryTime;

        // 입구 점에 이만큼 가까워지면 지나간 것으로 본다 (유닛).
        private const float EntryReachDistance = 0.3f;
        // 다른 적에 밀리거나 끼어 입구 점에 끝내 닿지 못해도 이 시간이 지나면 추적으로 넘어간다 (초). 안전장치라 데이터로 빼지 않는다.
        private const float EntryGiveUpTime = 15f;

        protected Rigidbody2D Body { get; private set; }
        protected HitReceiver HitReceiver => _hitReceiver;
        protected LayerMask ObstacleMask => _obstacleMask;
        // 공격을 시작하려면 플레이어가 보여야 하는지. 땅속에서 나오는 적처럼 벽과 상관없이 공격하는 적은 끈다.
        protected virtual bool RequiresLineOfSight => true;
        // 블랙홀 같은 외부 힘에 끌려가는지. 보스처럼 자리를 지켜야 하는 적은 끈다.
        protected virtual bool CanBePulled => true;
        // 죽은 뒤에도 쓰러진 몸을 남겨 둘지. 처치 대화가 이어지는 보스(최종 보스)가 대화 동안 쓰러진 모습을 보여 줄 때 쓴다.
        protected virtual bool HoldsBodyAfterDeath => false;

        // 벽·구조물 검사에 쓰는 이동 반경. 이동용 콜라이더가 있으면 그 크기, 없으면 피격 콜라이더 크기.
        protected float BodyRadius => _moveCollider != null ? MoveColliderRadius() : HitRadius;
        // 폭탄에 맞는 몸 크기. 적끼리 간격을 벌릴 때도 이 크기로 잰다.
        protected float HitRadius => Mathf.Min(_collider.bounds.extents.x, _collider.bounds.extents.y);
        protected Transform Target => _target;
        protected EnemyState State => _state;
        // 지금 상태에 들어온 뒤 지난 시간 (초, 물리 스텝 기준).
        protected float StateTime => _stateTime;
        protected abstract EnemyData Data { get; }

        // 표시 컴포넌트가 읽는 값. 상태를 바꾸는 건 여전히 이 클래스만 한다.
        public EnemyState CurrentState => _state;
        // 플레이어 오브젝트 위치가 아니라 실제 피격 판정(콜라이더) 한가운데를 노린다. 판정이 그림 아래쪽에 있어
        // 오브젝트 위치를 노리면 조준·추적이 몸통보다 위로 치우쳐, 위로 피할 때만 불리해졌다.
        public Vector2 TargetPosition => _target == null ? Body.position
            : _targetCollider != null && _targetCollider.enabled ? (Vector2)_targetCollider.bounds.center : (Vector2)_target.position;
        // 예고 길이. 저주 "광폭"이 덮어쓰면 그 값을, 아니면 적마다 정한 값을 쓴다. 예고 표시도 이 값을 읽는다.
        public float TelegraphDuration => _telegraphOverrides != null && _telegraphOverrides.Length > 0
            ? _telegraphOverrides[Mathf.Clamp(AttackStageIndex, 0, _telegraphOverrides.Length - 1)]
            : BaseTelegraphDuration;
        // 패턴마다 예고 길이가 다른 적(여러 기술을 쓰는 보스)은 지금 쓰는 패턴의 길이로 바꾼다.
        protected virtual float BaseTelegraphDuration => Data.TelegraphDuration;
        // 공격 뒤 쉬는 시간. 저주 "광폭"이 덮어쓰면 그 값을 쓴다.
        protected float RecoverDuration => _recoverOverride > 0f ? _recoverOverride : BaseRecoverDuration;
        // 무한 모드 보스는 바퀴마다 줄인다.
        protected virtual float BaseRecoverDuration => Data.RecoverDuration;
        // 단계마다 예고 길이가 다른 적(골렘)의 지금 단계. 광폭 예고 배열에서 몇 번째 값을 쓸지 정한다.
        protected virtual int AttackStageIndex => 0;
        // 예고·공격 중 그림이 바라볼 방향. 예고한 방향과 그림이 어긋나면 어디로 칠지 읽기 어렵다.
        public virtual Vector2 AimDirection => DirectionToTarget();
        // 움직이는 중에도 그림이 플레이어 쪽을 보게 할지. 뒷걸음질·뒷도약처럼 가는 방향과 보는 방향이 반대일 때 켠다.
        public virtual bool FacesTargetWhileMoving => false;
        public float DeathDuration => Data.DeathDuration;
        // 공격에 들어간 횟수. 공격 상태는 물리 한 스텝만에 끝나기도 해서, 그림 쪽이 상태만 보고는 공격 순간을 놓칠 수 있다.
        public int AttackCount { get; private set; }

        protected virtual void Awake()
        {
            Body = GetComponent<Rigidbody2D>();
            _hitReceiver = GetComponent<HitReceiver>();
            _collider = GetComponent<Collider2D>();

            _attackFilter = new ContactFilter2D();
            _attackFilter.SetLayerMask(_attackMask);
            _attackFilter.useTriggers = true;

            _obstacleFilter = new ContactFilter2D();
            _obstacleFilter.SetLayerMask(_obstacleMask);

            // 적끼리만 밀어낸다. 적 콜라이더는 트리거라 트리거도 검사한다.
            _separationFilter = new ContactFilter2D();
            _separationFilter.SetLayerMask(1 << gameObject.layer);
            _separationFilter.useTriggers = true;
        }

        protected virtual void OnEnable()
        {
            _hitReceiver.ResetHits(Data.HitsToDie);
            _hitReceiver.Died += OnDied;
            SpeedOverride = 0f;
            _telegraphOverrides = null;
            _recoverOverride = 0f;
            _arenaTime = 0f;
            _hasEntryPoint = false;
            _collider.enabled = true;
            EnterState(EnemyState.Move);
        }

        protected virtual void OnDisable()
        {
            _hitReceiver.Died -= OnDied;
            LeaveArenaCount();
        }

        // 도메인 리로드를 끈 채 플레이를 다시 눌러도 지난 판 수가 남지 않게 한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetArenaCount()
        {
            _arenaCount = 0;
        }

        // 진입 유예까지 끝내 실제로 싸움에 들어온 적만 센다. 보스는 따로 나오므로 뺀다.
        private void JoinArenaCount()
        {
            if (_isCountedInArena || Kind == EnemyKind.Boss)
            {
                return;
            }

            _isCountedInArena = true;
            _arenaCount++;
            GameEvents.RaiseEnemiesInArenaChanged(_arenaCount);
        }

        private void LeaveArenaCount()
        {
            if (!_isCountedInArena)
            {
                return;
            }

            _isCountedInArena = false;
            _arenaCount = Mathf.Max(0, _arenaCount - 1);
            GameEvents.RaiseEnemiesInArenaChanged(_arenaCount);
        }

        // died: 죽는 순간(살아 있는 적 목록에서 빼기), release: 사망 연출이 끝난 뒤(풀로 돌려보내기).
        public void Initialize(Transform target, Action<Enemy> died, Action<Enemy> release)
        {
            _target = target;
            _targetCollider = target != null ? target.GetComponent<Collider2D>() : null;
            _died = died;
            _release = release;
        }

        // 맵 안 한가운데에 바로 나온 적(저주 "기습")은 맵 밖에서 걸어 들어오지 않으므로 진입 유예 없이 곧바로 싸움에 든다.
        // 나오기 전 깃발 표시가 예고 역할을 한다. 스폰 직후 호출한다.
        public void SkipArenaEntryDelay()
        {
            _arenaTime = Data.ArenaEntryDelay;
            JoinArenaCount();
        }

        // 스폰 직후 호출된다. 맵 밖 구역에서 바로 플레이어를 쫓으면 맵 바깥을 돌아 아무 틈으로나 들어오므로,
        // 이 점(맵 입구)까지 곧장 걸어간 뒤 평소 이동을 시작한다. 걸어가는 동안은 공격하지 않는다.
        public void SetEntryPoint(Vector2 point)
        {
            _entryPoint = point;
            _entryTime = 0f;
            _hasEntryPoint = true;
        }

        // Kinematic 리지드바디를 MovePosition으로 옮기므로 물리 스텝에서 처리한다.
        private void FixedUpdate()
        {
            float deltaTime = Time.fixedDeltaTime;
            if (_state == EnemyState.Dead)
            {
                // 몸을 남겨 두는 동안은 사망 시간을 세지 않는다. 놓아주면 그때부터 사망 연출 시간을 다 채운 뒤 사라진다.
                if (HoldsBodyAfterDeath)
                {
                    return;
                }

                _stateTime += deltaTime;
                if (_stateTime >= Data.DeathDuration)
                {
                    Release();
                }

                return;
            }

            if (_target == null)
            {
                return;
            }

            _stateTime += deltaTime;

            switch (_state)
            {
                case EnemyState.Move:
                    TickMove(deltaTime);
                    UpdateEntry(deltaTime);
                    TrackArenaEntry(deltaTime);
                    // 가려진 상태에서 예고하면 벽에 대고 공격하게 되므로, 보일 때만 시작한다.
                    // 일단 시작한 예고는 도중에 가려져도 끝까지 진행한다. 입구로 들어오는 중에는 공격하지 않는다.
                    // 처음 본 뒤 확인 시간 동안은 계속 걸어 나오며 시야가 이어지는지 본다. 가려지면 처음부터 다시 센다.
                    // 시야 검사는 공격 거리 안일 때만 한다. 적 100마리가 매 스텝 검사하지 않게 한다.
                    bool isReady = !_hasEntryPoint && HasEnteredArena() && ShouldStartAttack();
                    bool canSee = isReady && (!RequiresLineOfSight || CanSeeTarget());
                    _sightTime = canSee ? _sightTime + deltaTime : 0f;
                    if (canSee && _sightTime >= Data.SightConfirmTime)
                    {
                        EnterState(EnemyState.Telegraph);
                    }
                    break;

                case EnemyState.Telegraph:
                    TickTelegraph(deltaTime);
                    if (_stateTime >= TelegraphDuration)
                    {
                        EnterState(EnemyState.Attack);
                    }
                    break;

                case EnemyState.Attack:
                    if (TickAttack(deltaTime))
                    {
                        EnterState(EnemyState.Recover);
                    }
                    break;

                case EnemyState.Recover:
                    if (_stateTime >= RecoverDuration)
                    {
                        EnterState(EnemyState.Move);
                    }
                    break;

                case EnemyState.Stunned:
                    if (_stateTime >= _stunDuration)
                    {
                        EnterState(EnemyState.Move);
                    }
                    break;

                case EnemyState.Pulled:
                    // 끌어당기는 쪽과 이 컴포넌트의 FixedUpdate 순서는 정해져 있지 않으므로 한 스텝 여유를 둔다.
                    if (Time.fixedTime - _lastPulledTime > deltaTime * 1.5f)
                    {
                        EnterState(EnemyState.Move);
                    }
                    break;
            }
        }

        // 외부 힘으로 center 쪽으로 최대 step만큼 끌려간다. 구조물에 닿으면 그 앞에서 멈춘다.
        // 끌려가는 동안 예고·공격이 끊기고, 풀려나면 이동부터 다시 하므로 예고 없이 공격하는 일은 없다.
        public void PullToward(Vector2 center, float step)
        {
            if (_state == EnemyState.Dead || !CanBePulled)
            {
                return;
            }

            _lastPulledTime = Time.fixedTime;
            // 끌려가 위치가 바뀌었으니 입구로 되돌아가지 않고 풀려난 자리에서 바로 쫓는다.
            _hasEntryPoint = false;
            if (_state != EnemyState.Pulled)
            {
                EnterState(EnemyState.Pulled);
            }

            // 끌려온 적끼리 흡입보다 세게 밀어내, 한 점이 아니라 여러 마리가 엉긴 덩어리로 모이게 한다.
            Vector2 move = SeparationStep(step * Data.PulledSeparationRatio);
            Vector2 toCenter = center - Body.position;
            float distance = toCenter.magnitude;
            if (distance >= 0.0001f)
            {
                // 중심을 지나쳐 앞뒤로 떨리지 않게 남은 거리까지만 움직인다.
                Vector2 direction = toCenter / distance;
                float pull = Mathf.Min(step, distance);
                move += direction * Mathf.Min(pull, ClearDistance(direction, pull, BodyRadius));
            }

            if (move.sqrMagnitude > 0f)
            {
                Body.MovePosition(Body.position + move);
            }
        }

        // 예고·공격 중이어도 끊고 기절시킨다. 풀려나면 이동부터 다시 하므로 예고 없이 공격하는 일은 없다.
        protected void Stun(float duration)
        {
            if (_state == EnemyState.Dead)
            {
                return;
            }

            _stunDuration = duration;
            EnterState(EnemyState.Stunned);
        }

        // 회복(노출) 시간을 다 채우지 않고 이동으로 돌아간다. 맞는 순간 도망가는 적처럼 회복을 일찍 끝낼 때 쓴다.
        protected void EndRecoverEarly()
        {
            if (_state == EnemyState.Recover)
            {
                EnterState(EnemyState.Move);
            }
        }

        protected abstract void TickMove(float deltaTime);
        protected abstract bool ShouldStartAttack();

        // 예고 중 동작. 예고 표시는 공격 직전까지 실제 공격과 같은 값을 보여줘야 한다.
        protected virtual void TickTelegraph(float deltaTime)
        {
        }

        // 공격이 끝나면 true를 돌려준다.
        protected abstract bool TickAttack(float deltaTime);

        protected virtual void OnEnterState(EnemyState state)
        {
        }

        // 플레이어가 아닌 지점으로 곧장 간다. 구조물에 막히면 흐름장을 따라 플레이어 쪽으로 돌아가며 길을 찾는다.
        protected void MoveTowardPoint(Vector2 point, float speed, float deltaTime)
        {
            Vector2 toPoint = point - Body.position;
            float distance = toPoint.magnitude;
            if (distance < 0.0001f)
            {
                return;
            }

            Vector2 direction = toPoint / distance;
            float step = Mathf.Min(speed * deltaTime, distance);
            float clear = ClearDistance(direction, step, BodyRadius);
            if (clear < step * 0.5f)
            {
                MoveTowardTarget(speed, deltaTime);
                return;
            }

            Body.MovePosition(Body.position + direction * clear);
        }

        // 입구 점에 닿았는지 본다. 걷는 방식은 적마다 그대로이고, 가는 곳(MoveGoal)만 입구로 바뀌어 있다.
        private void UpdateEntry(float deltaTime)
        {
            if (!_hasEntryPoint)
            {
                return;
            }

            _entryTime += deltaTime;
            if ((_entryPoint - Body.position).sqrMagnitude <= EntryReachDistance * EntryReachDistance || _entryTime >= EntryGiveUpTime)
            {
                _hasEntryPoint = false;
            }
        }

        // 입구로 들어오는 중인지. 이동 방식이 따로인 적(돌진 등)은 이 동안 공격 거리 계산 없이 입구까지 간다.
        protected bool IsEntering => _hasEntryPoint;

        // 지금 걸어가야 할 곳. 입구로 들어오는 중이면 입구 점, 아니면 플레이어.
        protected Vector2 MoveGoal => _hasEntryPoint ? _entryPoint : TargetPosition;

        // 이번 걸음의 방향. 입구로 갈 때는 입구를, 입구를 지난 뒤에는 플레이어를 목적지로 흐름장을 따라 벽을 돌아간다.
        // 입구까지 곧장 걸으면 스폰 구역과 입구 사이에 놓인 소품에 막혀 오래 멈췄다.
        protected Vector2 MoveDirection()
        {
            Vector2 toGoal = MoveGoal - Body.position;
            Vector2 direct = toGoal.sqrMagnitude > 0.0001f ? toGoal.normalized : Vector2.zero;
            // 흐름장이 없는 씬(테스트 씬 등)에서는 예전처럼 직진한다.
            FlowField field = FlowField.Current;
            if (field == null)
            {
                return direct;
            }

            return _hasEntryPoint ? field.GetDirectionTo(_entryPoint, Body.position) : field.GetDirection(Body.position);
        }

        // 플레이어를 쫓아 걷는 속도. 무한 모드에서 웨이브가 쌓이면 판 전체 배율만큼 빨라진다.
        protected virtual float ChaseSpeed => Data.MoveSpeed * EnemyTempo.MoveSpeedMultiplier;

        // 0보다 크면 적 종류와 상관없이 이 속도를 기준으로 쫓는다(저주 "광폭"). 웨이브 배율은 똑같이 곱해져 함께 빨라진다.
        // 풀에서 다시 나올 때 지운다.
        public float SpeedOverride { get; set; }

        // 실제로 걷는 속도. 하위 클래스는 ChaseSpeed 대신 이것으로 걷는다.
        protected float CurrentChaseSpeed => SpeedOverride > 0f ? SpeedOverride * EnemyTempo.MoveSpeedMultiplier : ChaseSpeed;

        // 걸음 대신 멈춤·돌진으로 움직이는 적(사마귀)이 쓰는 빠르기 배율. 광폭이면 원래 속도 대비 배율만큼 빨라진다.
        protected float MoveTempo => SpeedOverride > 0f
            ? SpeedOverride / Mathf.Max(Data.MoveSpeed, 0.01f) * EnemyTempo.MoveSpeedMultiplier
            : EnemyTempo.MoveSpeedMultiplier;

        // 저주 "광폭"이 공격 박자를 당긴다. telegraphs는 단계별 예고 길이(단계가 없으면 하나), recover는 쉬는 시간(초).
        // 예고 자체는 남기므로 모든 공격에 예고가 있다는 규칙은 그대로다. 풀에서 다시 나올 때 지운다.
        public void SetAttackOverride(float[] telegraphs, float recover)
        {
            _telegraphOverrides = telegraphs;
            _recoverOverride = recover;
        }

        // 처치 수에 들어가는 일반 적인지. 보스는 저주(광폭 등) 대상에서 뺀다.
        public bool CountsAsKill => Data.CountsAsKill;
        public EnemyData EnemyData => Data;

        // 이름은 그대로지만 입구로 들어오는 중에는 입구 점으로 간다. 걷는 속도·밀어내기는 같다.
        protected void MoveTowardTarget(float speed, float deltaTime)
        {
            Vector2 toTarget = MoveGoal - Body.position;
            float step = speed * deltaTime;
            Vector2 move = SeparationStep(Data.SeparationSpeed * deltaTime);
            if (toTarget.sqrMagnitude > step * step)
            {
                Vector2 direction = MoveDirection();
                // 입구까지는 흐름장이 소품을 돌아가게 하지만, 흐름 방향이 벽 모서리를 스칠 수 있어 구조물에 파묻히지 않게 직접 멈춘다.
                float distance = _hasEntryPoint ? ClearDistance(direction, step, BodyRadius) : step;
                move += direction * distance;
            }

            if (move.sqrMagnitude > 0f)
            {
                Body.MovePosition(Body.position + move);
            }
        }

        // 흐름장을 따라 같은 길로 오면 한 점에 포개지므로, 너무 가까운 다른 적에게서 조금씩 떨어진다.
        // 이동 중과 블랙홀에 끌려갈 때만 적용한다. 예고·공격 중인 적은 제자리를 지켜야 한다.
        // 서 있는 적은 밀리지 않으므로, 걸어오는 쪽이 비켜 간다.
        // maxPush: 이번 스텝에 밀려날 수 있는 최대 거리.
        private Vector2 SeparationStep(float maxPush)
        {
            if (maxPush <= 0f)
            {
                return Vector2.zero;
            }

            float radius = HitRadius;
            Vector2 position = Body.position;
            // 내 원과 겹치는 콜라이더만 모은다. 겹치지 않는 적은 밀어낼 대상이 아니다.
            int count = Physics2D.OverlapCircle(position, radius, _separationFilter, SeparationBuffer);
            Vector2 push = Vector2.zero;
            for (int i = 0; i < count; i++)
            {
                Collider2D other = SeparationBuffer[i];
                if (other == _collider)
                {
                    continue;
                }

                Bounds bounds = other.bounds;
                float desired = (radius + Mathf.Min(bounds.extents.x, bounds.extents.y)) * Data.SeparationDistanceRatio;
                Vector2 away = position - (Vector2)bounds.center;
                float distance = away.magnitude;
                if (distance >= desired)
                {
                    continue;
                }

                // 완전히 같은 자리면 방향이 없으므로, 두 적이 서로 반대쪽으로 갈리도록 생성 순서로 정한다.
                Vector2 direction = distance > 0.0001f
                    ? away / distance
                    : (_collider.GetHashCode() > other.GetHashCode() ? Vector2.right : Vector2.left);
                push += direction * (1f - distance / desired);
            }

            float strength = push.magnitude;
            if (strength < 0.0001f)
            {
                return Vector2.zero;
            }

            // 여럿에게 둘러싸여도 정해진 속도보다 빨리 밀리지 않게 한다.
            Vector2 pushDirection = push / strength;
            float pushDistance = Mathf.Min(strength, 1f) * maxPush;
            // 키네마틱이라 물리가 막아 주지 않으므로, 밀려서 구조물에 파묻히지 않게 직접 멈춘다.
            pushDistance = ClearDistance(pushDirection, pushDistance, BodyRadius);
            return pushDirection * pushDistance;
        }

        // 맵 밖은 화면 밖이라, 보이지 않는 적에게 맞는 일이 없도록 맵 안에서 걸은 시간을 센다.
        // 한 번 유예를 채우면 이후에는 맵 밖으로 밀려나도 다시 세지 않는다.
        private void TrackArenaEntry(float deltaTime)
        {
            if (HasEnteredArena())
            {
                return;
            }

            // 맵 범위가 없는 테스트 씬에서는 처음부터 맵 안으로 본다.
            ArenaBounds arena = ArenaBounds.Current;
            if (arena == null || arena.Contains(Body.position))
            {
                _arenaTime += deltaTime;
                if (HasEnteredArena())
                {
                    JoinArenaCount();
                }
            }
        }

        private bool HasEnteredArena()
        {
            return _arenaTime >= Data.ArenaEntryDelay;
        }

        // 폭탄·화살과 같은 물리 기준으로 판단한다. 사거리 안일 때만 불리므로 호출 수가 적다.
        private bool CanSeeTarget()
        {
            return Physics2D.Linecast(Body.position, TargetPosition, _obstacleFilter, CastBuffer) == 0;
        }

        // 목표와 겹쳐 방향이 없을 때도 공격이 멈추지 않도록 기본 방향을 준다.
        protected Vector2 DirectionToTarget()
        {
            Vector2 toTarget = TargetPosition - Body.position;
            return toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector2.right;
        }

        // 꺼 둔 콜라이더는 bounds가 비므로 반경에 크기 배율을 직접 곱한다.
        private float MoveColliderRadius()
        {
            Vector3 scale = _moveCollider.transform.lossyScale;
            return _moveCollider.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        }

        // 주어진 방향으로 반경만 한 원을 밀었을 때 구조물에 닿기 전까지 갈 수 있는 거리.
        // 구조물은 움직이지 않으므로 예고 시점에 한 번 재면 공격이 끝날 때까지 유효하다.
        // 예고선과 실제 공격이 같은 값을 쓰므로 보이는 길이가 곧 공격 거리다.
        protected float ClearDistance(Vector2 direction, float maxDistance, float radius)
        {
            int count = Physics2D.CircleCast(Body.position, radius, direction, _obstacleFilter, CastBuffer, maxDistance);
            float closest = maxDistance;
            for (int i = 0; i < count; i++)
            {
                if (CastBuffer[i].distance < closest)
                {
                    closest = CastBuffer[i].distance;
                }
            }

            return closest;
        }

        protected bool IsTargetWithin(float range)
        {
            return (TargetPosition - Body.position).sqrMagnitude <= range * range;
        }

        // 공격 상태에서만 호출한다. 이동 중 몸통 접촉은 예고 없는 공격이 되므로 피해를 주지 않는다.
        protected void HitOverlapping(Vector2 center, float radius)
        {
            int count = Physics2D.OverlapCircle(center, radius, _attackFilter, AttackBuffer);
            for (int i = 0; i < count; i++)
            {
                if (AttackBuffer[i].TryGetComponent(out IHittable hittable))
                {
                    hittable.ReceiveHit(HitInfo.FromEnemy(center, Kind, Data));
                }
            }
        }

        // 맵 위 정해진 직사각형(축 정렬) 안을 친다. 구조물과 상관없다. 몸 중심이 칸 안이어야 맞는다(폭탄과 같은 규칙).
        protected void HitInWorldRect(Vector2 center, Vector2 size)
        {
            int count = Physics2D.OverlapBox(center, size, 0f, _attackFilter, AttackBuffer);
            Vector2 half = size * 0.5f;
            for (int i = 0; i < count; i++)
            {
                Vector2 target = AttackBuffer[i].bounds.center;
                if (Mathf.Abs(target.x - center.x) > half.x || Mathf.Abs(target.y - center.y) > half.y)
                {
                    continue;
                }

                if (AttackBuffer[i].TryGetComponent(out IHittable hittable))
                {
                    hittable.ReceiveHit(HitInfo.FromEnemy(center, Kind, Data));
                }
            }
        }

        // 맵 전체를 한 번 치되, 안전지대(원) 안에 중심이 있는 대상은 뺀다. 화면 전체 공격(최종 보스 충격파)에 쓴다.
        // 한 순간만 판정하므로, 그 순간 구르기 무적이면 피한다.
        protected void HitAllOutside(Vector2[] safeCenters, int safeCount, float safeRadius)
        {
            int count = Physics2D.OverlapCircle(Body.position, 500f, _attackFilter, AttackBuffer);
            float safeSqr = safeRadius * safeRadius;
            for (int i = 0; i < count; i++)
            {
                Vector2 target = AttackBuffer[i].bounds.center;
                bool isSafe = false;
                for (int j = 0; j < safeCount; j++)
                {
                    if ((target - safeCenters[j]).sqrMagnitude <= safeSqr)
                    {
                        isSafe = true;
                        break;
                    }
                }

                if (!isSafe && AttackBuffer[i].TryGetComponent(out IHittable hittable))
                {
                    hittable.ReceiveHit(HitInfo.FromEnemy(Body.position, Kind, Data));
                }
            }
        }

        // 부채꼴 근접 공격. 대상의 중심이 반경 안, 정면 기준 각도 안에 있고, 사이에 구조물이 없어야 맞는다.
        // 폭탄과 같은 규칙(중심 한 점, 구조물에 막힘)이라 플레이어가 결과를 같은 방식으로 읽는다.
        protected void HitInSector(Vector2 forward, float radius, float angle)
        {
            Vector2 center = Body.position;
            float cosHalf = Mathf.Cos(angle * 0.5f * Mathf.Deg2Rad);
            int count = Physics2D.OverlapCircle(center, radius, _attackFilter, AttackBuffer);
            for (int i = 0; i < count; i++)
            {
                Vector2 target = AttackBuffer[i].bounds.center;
                Vector2 toTarget = target - center;
                if (toTarget.sqrMagnitude > radius * radius)
                {
                    continue;
                }

                // 정면과의 각도는 내적으로 비교한다. 대상이 몸 안에 겹쳐 있으면 방향과 무관하게 맞는다.
                if (toTarget.sqrMagnitude > 0.0001f && Vector2.Dot(toTarget.normalized, forward) < cosHalf)
                {
                    continue;
                }

                if (Physics2D.Linecast(center, target, _obstacleFilter, CastBuffer) > 0)
                {
                    continue;
                }

                if (AttackBuffer[i].TryGetComponent(out IHittable hittable))
                {
                    hittable.ReceiveHit(HitInfo.FromEnemy(center, Kind, Data));
                }
            }
        }

        // 정면 사각형 근접 공격. 몸 중심에서 정면으로 length, 좌우 폭 width. 대상 중심이 사각형 안에 있고 사이에 구조물이 없어야 맞는다.
        // ignoreObstacles: 구조물을 뚫고 돌진하는 공격처럼, 보이는 사각형 안이면 벽 너머라도 맞힐 때 켠다.
        protected void HitInBox(Vector2 forward, float length, float width, bool ignoreObstacles = false)
        {
            Vector2 origin = Body.position;
            Vector2 center = origin + forward * (length * 0.5f);
            float angle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
            int count = Physics2D.OverlapBox(center, new Vector2(length, width), angle, _attackFilter, AttackBuffer);
            for (int i = 0; i < count; i++)
            {
                Vector2 target = AttackBuffer[i].bounds.center;
                // 겹침 검사는 몸 일부만 걸쳐도 잡히므로, 폭탄과 같이 중심 한 점이 사각형 안인지 다시 본다.
                Vector2 local = target - origin;
                float along = Vector2.Dot(local, forward);
                float side = Mathf.Abs(local.x * forward.y - local.y * forward.x);
                if (along < 0f || along > length || side > width * 0.5f)
                {
                    continue;
                }

                if (!ignoreObstacles && Physics2D.Linecast(origin, target, _obstacleFilter, CastBuffer) > 0)
                {
                    continue;
                }

                if (AttackBuffer[i].TryGetComponent(out IHittable hittable))
                {
                    hittable.ReceiveHit(HitInfo.FromEnemy(origin, Kind, Data));
                }
            }
        }

        private void EnterState(EnemyState state)
        {
            if (state == EnemyState.Attack)
            {
                AttackCount++;
            }

            _state = state;
            _stateTime = 0f;
            // 공격·기절 뒤 다시 걸을 때는 처음부터 다시 확인한다.
            _sightTime = 0f;
            OnEnterState(state);
        }

        // 업적이 따로 세는 적 부류. 특정 적을 세야 하면 그 적이 덮어쓴다.
        protected virtual EnemyKind Kind => this is IBoss ? EnemyKind.Boss : EnemyKind.Normal;

        // 판정과 처치 집계는 죽는 순간 끝내고, 몸만 사망 연출 동안 남겨 둔다.
        // 콜라이더를 꺼서 남은 몸이 폭탄 범위·블랙홀·길막에 끼어들지 않게 한다.
        private void OnDied(HitInfo hit)
        {
            LeaveArenaCount();
            bool wasPulled = _state == EnemyState.Pulled;
            EnterState(EnemyState.Dead);
            _collider.enabled = false;
            if (Data.CountsAsKill)
            {
                GameEvents.RaiseEnemyKilled(Body.position);
                GameEvents.RaiseCountedEnemyKilled(hit.Source, wasPulled);
            }

            GameEvents.RaiseEnemyKilledBy(hit.Source, Kind);

            _died?.Invoke(this);
        }

        private void Release()
        {
            if (_release != null)
            {
                _release(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
