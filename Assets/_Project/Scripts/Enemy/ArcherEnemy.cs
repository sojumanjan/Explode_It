using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 고블린 궁수: 다가오다 사거리에서 멈춤 → 조준 예고 → 화살 한 발 → 회복.
    // 예측 과제: 조준 방향은 예고 순간 고정되므로, 예고를 보고 옆으로 빠지면서 멈춰 선 궁수에게 폭탄을 둔다.
    public class ArcherEnemy : Enemy
    {
        [SerializeField] private ArcherData _data;
        [SerializeField] private TelegraphLine _telegraph;

        private Vector2 _aimDirection;

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
            EnemyProjectilePool.Current.Fire(Body.position, _aimDirection,
                _data.ProjectileSpeed, _data.ProjectileRange, _data.ProjectileHitRadius);
            return true;
        }

        protected override void OnEnterState(EnemyState state)
        {
            if (state == EnemyState.Telegraph)
            {
                // 예고 중 조준을 따라가게 하면 피할 방법이 없어진다.
                _aimDirection = DirectionToTarget();
                _telegraph.Show(Body.position, _aimDirection, _data.ProjectileRange);
                return;
            }

            _telegraph.Hide();
        }
    }
}
