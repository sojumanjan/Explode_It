using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 고블린 돌격병: 다가옴 → 멈춰서 예고(경로선만 플레이어를 따라감) → 예고 끝 방향으로 직선 돌진.
    // 예측 과제: 돌진 방향은 예고가 끝나는 순간 정해지므로, 그 직전에 경로를 벗어나면서 돌진이 끝날 지점에 폭탄을 둔다.
    public class ChargerEnemy : Enemy
    {
        [SerializeField] private ChargerData _data;
        [SerializeField] private TelegraphLine _telegraph;

        private Vector2 _chargeDirection;
        private float _chargeLength;
        private float _chargedDistance;

        protected override EnemyData Data => _data;

        protected override void TickMove(float deltaTime)
        {
            MoveTowardTarget(_data.MoveSpeed, deltaTime);
        }

        protected override bool ShouldStartAttack()
        {
            return IsTargetWithin(_data.AttackTriggerRange);
        }

        // 예고 중에는 제자리에 서서, 돌진 시작점이 흔들리지 않게 하고 멈춘 순간이 폭탄 표적이 되게 한다.
        protected override void TickTelegraph(float deltaTime)
        {
            AimAtTarget();
        }

        protected override bool TickAttack(float deltaTime)
        {
            float remaining = _chargeLength - _chargedDistance;
            float step = Mathf.Min(_data.ChargeSpeed * deltaTime, remaining);
            Vector2 next = Body.position + _chargeDirection * step;

            Body.MovePosition(next);
            _chargedDistance += step;
            HitOverlapping(next, _data.ContactRadius);

            return _chargedDistance >= _chargeLength;
        }

        protected override void OnEnterState(EnemyState state)
        {
            if (state == EnemyState.Telegraph)
            {
                AimAtTarget();
                return;
            }

            // 마지막 예고 프레임에 보여준 방향과 길이를 그대로 쓴다. 돌진 중에는 다시 추적하지 않는다.
            if (state == EnemyState.Attack)
            {
                _chargedDistance = 0f;
            }

            _telegraph.Hide();
        }

        private void AimAtTarget()
        {
            _chargeDirection = DirectionToTarget();
            // 벽이 있으면 벽 앞에서 멈춘다.
            _chargeLength = ClearDistance(_chargeDirection, _data.ChargeDistance, _data.ContactRadius);
            _telegraph.Show(Body.position, _chargeDirection, _chargeLength, _data.ContactRadius * 2f);
        }
    }
}
