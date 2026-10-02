using ExplodeIt.Bombs;
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 전사: 다가옴 → 공격 거리에 들어온 순간 그 방향으로 고정하고 멈춰서 예고 → 고정된 방향으로 부채꼴 휘두르기 → 회복.
    // 범위가 좁아 바닥 예고는 그리지 않고, 멈춰 서는 모습 자체가 예고다(이후 애니메이션이 맡는다).
    // 예측 과제: 멈춘 순간 방향이 정해지므로 옆이나 뒤로 빠지고, 휘두른 뒤 회복 동안 서 있는 전사에게 폭탄을 둔다.
    public class WarriorEnemy : Enemy
    {
        [SerializeField] private WarriorData _data;
        // 휘두르는 순간 판정과 같은 부채꼴을 잠깐 그려, 맞은 이유를 바로 알 수 있게 한다.
        [SerializeField] private ExplosionShape _swingShape;
        [SerializeField, Min(0f)] private float _swingFxDuration = 0.18f;

        private Vector2 _aimDirection = Vector2.right;
        private float _swingFxTime = -1f;

        protected override EnemyData Data => _data;
        public override Vector2 AimDirection => _aimDirection;

        protected override void OnEnable()
        {
            base.OnEnable();
            _swingShape.Visible = false;
            _swingFxTime = -1f;
        }

        protected override void TickMove(float deltaTime)
        {
            MoveTowardTarget(_data.MoveSpeed, deltaTime);
        }

        protected override bool ShouldStartAttack()
        {
            return IsTargetWithin(_data.AttackTriggerRange);
        }

        // 한 번 휘두르고 끝난다.
        protected override bool TickAttack(float deltaTime)
        {
            HitInSector(_aimDirection, _data.AttackRadius, _data.AttackAngle);
            ShowSwing();
            AudioManager.Play(_data.SwingSound);
            return true;
        }

        protected override void OnEnterState(EnemyState state)
        {
            // 근접이라 끝까지 따라가면 피할 길이 없다. 예고가 시작되는 순간의 방향으로 고정한다.
            if (state == EnemyState.Telegraph)
            {
                _aimDirection = DirectionToTarget();
            }
        }

        // 트윈 대신 시간을 직접 세서, 적이 수십 마리여도 매번 트윈을 만들지 않는다.
        private void Update()
        {
            if (_swingFxTime < 0f)
            {
                return;
            }

            _swingFxTime += Time.deltaTime;
            float t = _swingFxTime / _swingFxDuration;
            if (t >= 1f)
            {
                _swingShape.Visible = false;
                _swingFxTime = -1f;
                return;
            }

            // 앞 절반 동안 반경이 휙 퍼지고, 전체에 걸쳐 흐려진다.
            _swingShape.SetRadius(_data.AttackRadius * Mathf.Min(1f, t * 2f));
            _swingShape.SetOpacity(1f - t);
        }

        private void ShowSwing()
        {
            _swingShape.BuildSector(_aimDirection,_data.AttackRadius, _data.AttackAngle);
            _swingShape.SetRadius(0f);
            _swingShape.ResetColor();
            _swingShape.Visible = true;
            _swingFxTime = 0f;
        }
    }
}
