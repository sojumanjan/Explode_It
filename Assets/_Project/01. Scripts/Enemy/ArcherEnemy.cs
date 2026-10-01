using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 고블린 궁수: 다가옴 → 멈춰서 조준 예고(조준선만 플레이어를 따라감) → 예고 끝 방향으로 화살 한 발 → 회복.
    // 예측 과제: 쏘는 순간의 방향으로 날아가므로, 예고가 끝나기 직전에 옆으로 빠지면서 멈춰 선 궁수에게 폭탄을 둔다.
    public class ArcherEnemy : Enemy
    {
        [SerializeField] private ArcherData _data;
        [SerializeField] private TelegraphLine _telegraph;

        private Vector2 _aimDirection;
        private float _shotLength;

        protected override EnemyData Data => _data;

        protected override void TickMove(float deltaTime)
        {
            MoveTowardTarget(_data.MoveSpeed, deltaTime);
        }

        protected override bool ShouldStartAttack()
        {
            return IsTargetWithin(_data.AttackTriggerRange);
        }

        // 조준 중에는 제자리에 서서, 멈춘 궁수가 폭탄을 맞힐 기회가 되게 한다.
        protected override void TickTelegraph(float deltaTime)
        {
            AimAtTarget();
        }

        // 마지막 예고 프레임에 보여준 조준선 그대로 쏜다.
        protected override bool TickAttack(float deltaTime)
        {
            EnemyProjectilePool.Current.Fire(Body.position, _aimDirection,
                _data.ProjectileSpeed, _shotLength, _data.ProjectileHitRadius);
            AudioManager.Play(_data.ShotSound);
            return true;
        }

        protected override void OnEnterState(EnemyState state)
        {
            if (state == EnemyState.Telegraph)
            {
                AimAtTarget();
                return;
            }

            _telegraph.Hide();
        }

        private void AimAtTarget()
        {
            _aimDirection = DirectionToTarget();
            // 화살은 구조물에 막힌다. 비행 거리를 막힌 지점까지로 줄여, 조준선 끝이 곧 화살이 사라지는 곳이 되게 한다.
            _shotLength = ClearDistance(_aimDirection, _data.ProjectileRange, _data.ProjectileHitRadius);
            _telegraph.Show(Body.position, _aimDirection, _shotLength);
        }
    }
}
