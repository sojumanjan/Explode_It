using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 고블린 궁수·저격수: 다가옴 → 멈춰서 조준 예고(조준 방향만 플레이어를 따라감) → 예고 끝 방향으로 한 발 → 회복.
    // 두 적은 코드가 같고 데이터(사거리, 탄속, 조준선 표시, 소리)만 다르다.
    // 예측 과제: 쏘는 순간의 방향으로 날아가므로, 예고가 끝나기 직전에 옆으로 빠지면서 멈춰 선 적에게 폭탄을 둔다.
    public class ArcherEnemy : Enemy
    {
        [SerializeField] private ArcherData _data;
        [SerializeField] private TelegraphLine _telegraph;

        private Vector2 _aimDirection;
        private float _shotLength;
        private SoundHandle _aimSoundHandle = SoundHandle.None;

        protected override EnemyData Data => _data;

        protected override void TickMove(float deltaTime)
        {
            MoveTowardTarget(_data.MoveSpeed, deltaTime);
        }

        protected override bool ShouldStartAttack()
        {
            return IsTargetWithin(_data.AttackTriggerRange);
        }

        // 조준 중에는 제자리에 서서, 멈춘 적이 폭탄을 맞힐 기회가 되게 한다.
        protected override void TickTelegraph(float deltaTime)
        {
            AimAtTarget();
        }

        // 마지막 예고 프레임의 조준 그대로 쏜다.
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
                _aimSoundHandle = AudioManager.Play(_data.AimSound);
                return;
            }

            // 발사, 블랙홀에 끌려감, 사망 등 예고를 벗어나는 모든 경우에 조준 표시를 거둔다.
            _telegraph.Hide();
            StopAimSound();
        }

        // 풀로 돌아가거나 재시작으로 꺼질 때도 씬을 넘어 살아 있는 오디오 매니저에서 소리가 남지 않게 한다.
        protected override void OnDisable()
        {
            base.OnDisable();
            StopAimSound();
        }

        private void StopAimSound()
        {
            AudioManager.Stop(_aimSoundHandle);
            _aimSoundHandle = SoundHandle.None;
        }

        private void AimAtTarget()
        {
            _aimDirection = DirectionToTarget();
            // 탄은 구조물에 막힌다. 비행 거리를 막힌 지점까지로 줄여, 조준선 끝이 곧 탄이 사라지는 곳이 되게 한다.
            _shotLength = ClearDistance(_aimDirection, _data.ProjectileRange, _data.ProjectileHitRadius);
            if (_data.ShowAimLine)
            {
                _telegraph.Show(Body.position, _aimDirection, _shotLength);
            }
        }
    }
}
