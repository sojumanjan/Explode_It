using System;
using ExplodeIt.Core;
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

        // 씬에 직접 배치해 테스트할 때만 인스펙터로 넣는다. 스폰 시에는 Initialize로 주입한다.
        [SerializeField] private Transform _target;
        [SerializeField] private LayerMask _attackMask;

        private HitReceiver _hitReceiver;
        private ContactFilter2D _attackFilter;
        private Action<Enemy> _release;
        private EnemyState _state;
        private float _stateTime;

        protected Rigidbody2D Body { get; private set; }
        protected Transform Target => _target;
        protected EnemyState State => _state;
        protected abstract EnemyData Data { get; }

        protected virtual void Awake()
        {
            Body = GetComponent<Rigidbody2D>();
            _hitReceiver = GetComponent<HitReceiver>();

            _attackFilter = new ContactFilter2D();
            _attackFilter.SetLayerMask(_attackMask);
            _attackFilter.useTriggers = true;
        }

        protected virtual void OnEnable()
        {
            _hitReceiver.ResetHits(Data.HitsToDie);
            _hitReceiver.Died += OnDied;
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
                    if (ShouldStartAttack())
                    {
                        EnterState(EnemyState.Telegraph);
                    }
                    break;

                case EnemyState.Telegraph:
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
            }
        }

        protected abstract void TickMove(float deltaTime);
        protected abstract bool ShouldStartAttack();

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

            Body.MovePosition(Body.position + toTarget.normalized * step);
        }

        // 목표와 겹쳐 방향이 없을 때도 공격이 멈추지 않도록 기본 방향을 준다.
        protected Vector2 DirectionToTarget()
        {
            Vector2 toTarget = (Vector2)_target.position - Body.position;
            return toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector2.right;
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
