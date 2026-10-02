using System;
using ExplodeIt.Core;
using ExplodeIt.Stage;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 상태 전환은 이 클래스만 한다. 하위 클래스는 공격 상태로 직접 들어갈 수 없으므로
    // 모든 공격은 반드시 예고 상태를 거친다.
    [RequireComponent(typeof(Rigidbody2D), typeof(HitReceiver))]
    public abstract class Enemy : MonoBehaviour
    {
        // 적 공격은 순차적으로 처리되므로 버퍼 하나를 모든 적이 공유한다.
        private static readonly Collider2D[] AttackBuffer = new Collider2D[8];
        private static readonly RaycastHit2D[] CastBuffer = new RaycastHit2D[4];

        // 씬에 직접 배치해 테스트할 때만 인스펙터로 넣는다. 스폰 시에는 Initialize로 주입한다.
        [SerializeField] private Transform _target;
        [SerializeField] private LayerMask _attackMask;
        [SerializeField] private LayerMask _obstacleMask;

        private HitReceiver _hitReceiver;
        private ContactFilter2D _attackFilter;
        private ContactFilter2D _obstacleFilter;
        private Action<Enemy> _release;
        private EnemyState _state;
        private float _stateTime;
        private float _arenaTime;
        private float _lastPulledTime;
        private Collider2D _collider;

        protected Rigidbody2D Body { get; private set; }

        // 끌려갈 때 벽에 몸이 파묻히지 않도록 쓰는 반경. 콜라이더 크기를 그대로 따른다.
        private float BodyRadius => Mathf.Min(_collider.bounds.extents.x, _collider.bounds.extents.y);
        protected Transform Target => _target;
        protected EnemyState State => _state;
        protected abstract EnemyData Data { get; }

        // 표시 컴포넌트가 읽는 값. 상태를 바꾸는 건 여전히 이 클래스만 한다.
        public EnemyState CurrentState => _state;
        public Vector2 TargetPosition => _target != null ? (Vector2)_target.position : Body.position;

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
        }

        protected virtual void OnEnable()
        {
            _hitReceiver.ResetHits(Data.HitsToDie);
            _hitReceiver.Died += OnDied;
            _arenaTime = 0f;
            EnterState(EnemyState.Move);
        }

        protected virtual void OnDisable()
        {
            _hitReceiver.Died -= OnDied;
        }

        public void Initialize(Transform target, Action<Enemy> release)
        {
            _target = target;
            _release = release;
        }

        // Kinematic 리지드바디를 MovePosition으로 옮기므로 물리 스텝에서 처리한다.
        private void FixedUpdate()
        {
            if (_target == null || _state == EnemyState.Dead)
            {
                return;
            }

            float deltaTime = Time.fixedDeltaTime;
            _stateTime += deltaTime;

            switch (_state)
            {
                case EnemyState.Move:
                    TickMove(deltaTime);
                    TrackArenaEntry(deltaTime);
                    // 가려진 상태에서 예고하면 벽에 대고 공격하게 되므로, 보일 때만 시작한다.
                    // 일단 시작한 예고는 도중에 가려져도 끝까지 진행한다.
                    if (HasEnteredArena() && ShouldStartAttack() && CanSeeTarget())
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
            if (_state == EnemyState.Dead)
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

        protected void MoveTowardTarget(float speed, float deltaTime)
        {
            Vector2 toTarget = (Vector2)_target.position - Body.position;
            float step = speed * deltaTime;
            if (toTarget.sqrMagnitude <= step * step)
            {
                return;
            }

            // 흐름장이 없는 씬(테스트 씬 등)에서는 예전처럼 직진한다.
            FlowField field = FlowField.Current;
            Vector2 direction = field != null ? field.GetDirection(Body.position) : toTarget.normalized;
            Body.MovePosition(Body.position + direction * step);
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

        private void EnterState(EnemyState state)
        {
            _state = state;
            _stateTime = 0f;
            OnEnterState(state);
        }

        private void OnDied(HitInfo hit)
        {
            EnterState(EnemyState.Dead);
            GameEvents.RaiseEnemyKilled(Body.position);

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
