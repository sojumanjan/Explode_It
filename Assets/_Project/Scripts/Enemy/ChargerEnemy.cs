using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 고블린 돌격병: 다가오다 멈춤 → 예고 → 직선 돌진.
    // 예측 과제: 돌진 경로는 예고 순간 고정되므로, 경로를 벗어나면서 돌진이 끝날 지점에 폭탄을 둔다.
    public class ChargerEnemy : Enemy
    {
        [SerializeField] private ChargerData _data;
        [SerializeField] private TelegraphLine _telegraph;

        private Vector2 _chargeDirection;
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

        protected override bool TickAttack(float deltaTime)
        {
            float remaining = _data.ChargeDistance - _chargedDistance;
            float step = Mathf.Min(_data.ChargeSpeed * deltaTime, remaining);
            Vector2 next = Body.position + _chargeDirection * step;

            Body.MovePosition(next);
            _chargedDistance += step;
            HitOverlapping(next, _data.ContactRadius);

            return _chargedDistance >= _data.ChargeDistance;
        }

        protected override void OnEnterState(EnemyState state)
        {
            if (state == EnemyState.Telegraph)
            {
                // 예고 시작 시점에 방향을 고정해야 플레이어가 경로를 읽고 피할 수 있다.
                // 돌진 중 추적하면 예측이 불가능해진다.
                _chargeDirection = DirectionToTarget();
                _telegraph.Show(Body.position, _chargeDirection, _data.ChargeDistance);
                return;
            }

            if (state == EnemyState.Attack)
            {
                _chargedDistance = 0f;
            }

            _telegraph.Hide();
        }
    }
}
