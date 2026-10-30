using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 고블린 궁수·저격수: 다가옴 → 멈춰서 조준 예고(조준 방향이 플레이어 쪽으로 돌며 따라가다 발사 직전에 고정) → 고정된 방향으로 한 발 → 회복.
    // 두 적은 코드가 같고 데이터(사거리, 탄속, 조준선 표시, 조준 회전, 소리)만 다르다.
    // 예측 과제: 쏘는 순간의 방향으로 날아가므로, 예고가 끝나기 직전에 옆으로 빠지면서 멈춰 선 적에게 폭탄을 둔다.
    public class ArcherEnemy : Enemy
    {
        [SerializeField] private ArcherData _data;
        [SerializeField] private TelegraphLine _telegraph;

        private Vector2 _aimDirection;
        private float _shotLength;
        private SoundHandle _aimSoundHandle = SoundHandle.None;

        protected override EnemyData Data => _data;
        public override Vector2 AimDirection => _aimDirection;

        protected override void TickMove(float deltaTime)
        {
            MoveTowardTarget(CurrentChaseSpeed, deltaTime);
        }

        protected override bool ShouldStartAttack()
        {
            return IsTargetWithin(_data.AttackTriggerRange);
        }

        // 조준 중에는 제자리에 서서, 멈춘 적이 폭탄을 맞힐 기회가 되게 한다.
        // 발사 직전 고정 시간에 들어가면 조준을 멈추고, 그때의 궤적으로 쏜다.
        protected override void TickTelegraph(float deltaTime)
        {
            if (StateTime < _data.TelegraphDuration - _data.AimLockTime)
            {
                TurnAimToward(DirectionToTarget(), deltaTime);
                UpdateAimLine();
            }
        }

        // 마지막으로 잰 조준 그대로 쏜다.
        protected override bool TickAttack(float deltaTime)
        {
            EnemyProjectilePool.Current.Fire(Body.position, _aimDirection,
                _data.ProjectileSpeed, _shotLength, _data.ProjectileHitRadius, _data);
            AudioManager.Play(_data.ShotSound);
            return true;
        }

        protected override void OnEnterState(EnemyState state)
        {
            if (state == EnemyState.Telegraph)
            {
                // 예고를 시작하는 순간에는 플레이어를 바로 겨눈다. 따라가는 속도는 그 뒤부터 적용한다.
                _aimDirection = DirectionToTarget();
                UpdateAimLine();
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

        // 남은 각도에 비례하는 속도로 돈다. 크게 벗어나면 빨리 넘어오고, 가까워질수록 느려져 마지막에 살짝 비킬 틈이 남는다.
        private void TurnAimToward(Vector2 desired, float deltaTime)
        {
            if (_data.AimTurnRate <= 0f)
            {
                _aimDirection = desired;
                return;
            }

            float angle = Vector2.SignedAngle(_aimDirection, desired);
            float remaining = Mathf.Abs(angle);
            float speed = Mathf.Clamp(remaining * _data.AimTurnRate, _data.AimMinTurnSpeed, _data.AimMaxTurnSpeed);
            float step = Mathf.Min(remaining, speed * deltaTime);
            _aimDirection = Quaternion.Euler(0f, 0f, Mathf.Sign(angle) * step) * _aimDirection;
        }

        private void UpdateAimLine()
        {
            // 탄은 구조물에 막힌다. 비행 거리를 막힌 지점까지로 줄여, 조준선 끝이 곧 탄이 사라지는 곳이 되게 한다.
            _shotLength = ClearDistance(_aimDirection, _data.ProjectileRange, _data.ProjectileHitRadius);
            if (_data.ShowAimLine)
            {
                _telegraph.Show(Body.position, _aimDirection, _shotLength);
            }
        }
    }
}
