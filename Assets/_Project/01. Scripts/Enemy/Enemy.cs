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
        [SerializeField] private LayerMask _attackMask;
        [SerializeField] private LayerMask _obstacleMask;

        private HitReceiver _hitReceiver;
        private ContactFilter2D _attackFilter;
        private ContactFilter2D _obstacleFilter;
        private ContactFilter2D _separationFilter;
        private Action<Enemy> _died;
        private Action<Enemy> _release;
        private EnemyState _state;
        private float _stateTime;
        private float _arenaTime;
        private float _lastPulledTime;
        private float _stunDuration;
        private Collider2D _collider;

        protected Rigidbody2D Body { get; private set; }
        protected HitReceiver HitReceiver => _hitReceiver;
        protected LayerMask ObstacleMask => _obstacleMask;
        // 공격을 시작하려면 플레이어가 보여야 하는지. 땅속에서 나오는 적처럼 벽과 상관없이 공격하는 적은 끈다.
        protected virtual bool RequiresLineOfSight => true;
        // 블랙홀 같은 외부 힘에 끌려가는지. 보스처럼 자리를 지켜야 하는 적은 끈다.
        protected virtual bool CanBePulled => true;

        // 끌려갈 때 벽에 몸이 파묻히지 않도록 쓰는 반경. 콜라이더 크기를 그대로 따른다.
        protected float BodyRadius => Mathf.Min(_collider.bounds.extents.x, _collider.bounds.extents.y);
        protected Transform Target => _target;
        protected EnemyState State => _state;
        // 지금 상태에 들어온 뒤 지난 시간 (초, 물리 스텝 기준).
        protected float StateTime => _stateTime;
        protected abstract EnemyData Data { get; }

        // 표시 컴포넌트가 읽는 값. 상태를 바꾸는 건 여전히 이 클래스만 한다.
        public EnemyState CurrentState => _state;
        public Vector2 TargetPosition => _target != null ? (Vector2)_target.position : Body.position;
        public float TelegraphDuration => Data.TelegraphDuration;
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
            _arenaTime = 0f;
            _collider.enabled = true;
            EnterState(EnemyState.Move);
        }

        protected virtual void OnDisable()
        {
            _hitReceiver.Died -= OnDied;
        }

        // died: 죽는 순간(살아 있는 적 목록에서 빼기), release: 사망 연출이 끝난 뒤(풀로 돌려보내기).
        public void Initialize(Transform target, Action<Enemy> died, Action<Enemy> release)
        {
            _target = target;
            _died = died;
            _release = release;
        }

        // Kinematic 리지드바디를 MovePosition으로 옮기므로 물리 스텝에서 처리한다.
        private void FixedUpdate()
        {
            float deltaTime = Time.fixedDeltaTime;
            if (_state == EnemyState.Dead)
            {
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
                    TrackArenaEntry(deltaTime);
                    // 가려진 상태에서 예고하면 벽에 대고 공격하게 되므로, 보일 때만 시작한다.
                    // 일단 시작한 예고는 도중에 가려져도 끝까지 진행한다.
                    if (HasEnteredArena() && ShouldStartAttack() && (!RequiresLineOfSight || CanSeeTarget()))
                    {
                        EnterState(EnemyState.Telegraph);
                    }
                    break;

                case EnemyState.Telegraph:
                    TickTelegraph(deltaTime);
                    if (_stateTime >= Data.TelegraphDuration)
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
                    if (_stateTime >= Data.RecoverDuration)
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
            if (_state != EnemyState.Pulled)
            {
                EnterState(EnemyState.Pulled);
            }

            Vector2 toCenter = center - Body.position;
            float distance = toCenter.magnitude;
            if (distance < 0.0001f)
            {
                return;
            }

            // 중심을 지나쳐 앞뒤로 떨리지 않게 남은 거리까지만 움직인다.
            Vector2 direction = toCenter / distance;
            float move = Mathf.Min(step, distance);
            move = Mathf.Min(move, ClearDistance(direction, move, BodyRadius));
            Body.MovePosition(Body.position + direction * move);
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

        protected void MoveTowardTarget(float speed, float deltaTime)
        {
            Vector2 toTarget = (Vector2)_target.position - Body.position;
            float step = speed * deltaTime;
            Vector2 move = SeparationStep(deltaTime);
            if (toTarget.sqrMagnitude > step * step)
            {
                // 흐름장이 없는 씬(테스트 씬 등)에서는 예전처럼 직진한다.
                FlowField field = FlowField.Current;
                Vector2 direction = field != null ? field.GetDirection(Body.position) : toTarget.normalized;
                move += direction * step;
            }

            if (move.sqrMagnitude > 0f)
            {
                Body.MovePosition(Body.position + move);
            }
        }

        // 흐름장을 따라 같은 길로 오면 한 점에 포개지므로, 너무 가까운 다른 적에게서 조금씩 떨어진다.
        // 이동 중에만 적용한다. 예고·공격 중인 적은 제자리를 지켜야 하고, 블랙홀은 일부러 뭉치게 하는 기술이다.
        // 서 있는 적은 밀리지 않으므로, 걸어오는 쪽이 비켜 간다.
        private Vector2 SeparationStep(float deltaTime)
        {
            if (Data.SeparationSpeed <= 0f)
            {
                return Vector2.zero;
            }

            float radius = BodyRadius;
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
            float pushDistance = Mathf.Min(strength, 1f) * Data.SeparationSpeed * deltaTime;
            // 키네마틱이라 물리가 막아 주지 않으므로, 밀려서 구조물에 파묻히지 않게 직접 멈춘다.
            pushDistance = ClearDistance(pushDirection, pushDistance, radius);
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
            }
        }

        private bool HasEnteredArena()
        {
            return _arenaTime >= Data.ArenaEntryDelay;
        }

        // 폭탄·화살과 같은 물리 기준으로 판단한다. 사거리 안일 때만 불리므로 호출 수가 적다.
        private bool CanSeeTarget()
        {
            return Physics2D.Linecast(Body.position, _target.position, _obstacleFilter, CastBuffer) == 0;
        }

        // 목표와 겹쳐 방향이 없을 때도 공격이 멈추지 않도록 기본 방향을 준다.
        protected Vector2 DirectionToTarget()
        {
            Vector2 toTarget = (Vector2)_target.position - Body.position;
            return toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector2.right;
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
            return ((Vector2)_target.position - Body.position).sqrMagnitude <= range * range;
        }

        // 공격 상태에서만 호출한다. 이동 중 몸통 접촉은 예고 없는 공격이 되므로 피해를 주지 않는다.
        protected void HitOverlapping(Vector2 center, float radius)
        {
            int count = Physics2D.OverlapCircle(center, radius, _attackFilter, AttackBuffer);
            for (int i = 0; i < count; i++)
            {
                if (AttackBuffer[i].TryGetComponent(out IHittable hittable))
                {
                    hittable.ReceiveHit(new HitInfo(center));
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
                    hittable.ReceiveHit(new HitInfo(center));
                }
            }
        }

        // 정면 사각형 근접 공격. 몸 중심에서 정면으로 length, 좌우 폭 width. 대상 중심이 사각형 안에 있고 사이에 구조물이 없어야 맞는다.
        protected void HitInBox(Vector2 forward, float length, float width)
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

                if (Physics2D.Linecast(origin, target, _obstacleFilter, CastBuffer) > 0)
                {
                    continue;
                }

                if (AttackBuffer[i].TryGetComponent(out IHittable hittable))
                {
                    hittable.ReceiveHit(new HitInfo(origin));
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
            OnEnterState(state);
        }

        // 판정과 처치 집계는 죽는 순간 끝내고, 몸만 사망 연출 동안 남겨 둔다.
        // 콜라이더를 꺼서 남은 몸이 폭탄 범위·블랙홀·길막에 끼어들지 않게 한다.
        private void OnDied(HitInfo hit)
        {
            EnterState(EnemyState.Dead);
            _collider.enabled = false;
            if (Data.CountsAsKill)
            {
                GameEvents.RaiseEnemyKilled(Body.position);
            }

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
