using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 고블린 돌격병: 다가옴 → 멈춰서 예고(예고 시작 순간의 방향으로 경로선 고정) → 그 경로선 그대로 직선 돌진.
    // 예측 과제: 경로가 예고 시작에 정해지므로, 선 밖으로 빠지면서 돌진이 끝날 지점에 폭탄을 둔다.
    public class ChargerEnemy : Enemy
    {
        [SerializeField] private ChargerData _data;
        [SerializeField] private TelegraphLine _telegraph;

        private Vector2 _chargeDirection;
        private float _chargeLength;
        private float _chargedDistance;

        protected override EnemyData Data => _data;
        public override Vector2 AimDirection => _chargeDirection;

        protected override void TickMove(float deltaTime)
        {
            MoveTowardTarget(_data.MoveSpeed, deltaTime);
        }

        protected override bool ShouldStartAttack()
        {
            return IsTargetWithin(_data.AttackTriggerRange);
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
            // 예고 중에는 제자리에 서고 경로선도 다시 재지 않아, 보이는 선이 곧 돌진 경로가 된다.
            if (state == EnemyState.Telegraph)
            {
                AimAtTarget();
                return;
            }

            // 예고 때 보여준 방향과 길이를 그대로 쓴다.
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
