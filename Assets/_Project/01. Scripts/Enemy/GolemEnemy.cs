using ExplodeIt.Bombs;
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 골렘: 여러 번 맞아야 쓰러진다. 다가옴 → 멈춰서 둘레 원 범위 예고(차오름) → 내려치기 → 회복.
    // 맞을 때마다 몸이 작아지고 빨라지며, 내려치기는 좁아지고 원이 빨리 찬다. 클 때는 길을 막는 느린 벽, 작을 때는 재빠른 적이다.
    // 예측 과제: 맞힐 때마다 움직임이 바뀌므로 같은 적을 세 번 다르게 읽어야 한다. 단계마다 속도는 고정이라 바뀐 뒤엔 예측할 수 있다.
    public class GolemEnemy : Enemy
    {
        [SerializeField] private GolemData _data;
        [SerializeField] private EnemyVisual _visual;
        // 폭탄에 맞는 판정. 몸 크기에 맞춰 단계마다 반경을 바꾼다.
        [SerializeField] private CircleCollider2D _hitCollider;
        [SerializeField] private ExplosionShape _slamRange;
        [SerializeField] private ExplosionShape _slamFill;

        private Vector2 _slamDirection = Vector2.right;
        private Vector2 _slamCenter;
        // 예고가 시작될 때의 단계 값. 예고 도중 단계가 바뀌면 멈칫으로 취소되지만, 보이는 원과 맞는 원이 어긋나지 않게 묶어 둔다.
        private float _slamRadius;
        private float _slamTelegraph = 1f;
        // 마지막으로 몸에 반영한 남은 맷집. 그림 쪽 초기화가 끝난 뒤 반영하려고 Update에서 맞춘다.
        private int _appliedHits = -1;

        protected override EnemyData Data => _data;
        public override Vector2 AimDirection => _slamDirection;
        public override float TelegraphDuration => _slamTelegraph;
        protected override float ChaseSpeed => CurrentStage.MoveSpeed * EnemyTempo.MoveSpeedMultiplier;

        private GolemData.Stage CurrentStage => _data.GetStage(HitReceiver.RemainingHits);

        protected override void OnEnable()
        {
            base.OnEnable();
            HideSlam();
            HitReceiver.Damaged += OnDamaged;
            _appliedHits = -1;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            HitReceiver.Damaged -= OnDamaged;
        }

        private void Update()
        {
            if (_appliedHits != HitReceiver.RemainingHits && CurrentState != EnemyState.Dead)
            {
                ApplyStage();
            }
        }

        private void ApplyStage()
        {
            _appliedHits = HitReceiver.RemainingHits;
            GolemData.Stage stage = CurrentStage;
            _visual.PoseSprite = stage.Sprite;
            _visual.ExtraScale = new Vector2(stage.VisualScale, stage.VisualScale);
            _hitCollider.radius = stage.HitRadius;
            _hitCollider.offset = stage.HitOffset;
        }

        // 버티고 맞으면 몸을 바로 바꾸고 멈칫한다. 예고 중이었으면 내려치기가 취소된다.
        private void OnDamaged(HitInfo hit)
        {
            ApplyStage();
            FeedbackPlayer.Play(_data.HurtFeedback, transform.position);
            AudioManager.Play(_data.HurtSound);
            _visual.Flash(_data.HurtFlashColor, _data.HurtFlashDuration);
            Stun(_data.HurtStun);
        }

        protected override void TickMove(float deltaTime)
        {
            MoveTowardTarget(ChaseSpeed, deltaTime);
        }

        protected override bool ShouldStartAttack()
        {
            return IsTargetWithin(CurrentStage.AttackTriggerRange);
        }

        // 범위가 다 차는 순간 내려친다. 보이는 원이 곧 맞는 원이다.
        protected override void TickTelegraph(float deltaTime)
        {
            float progress = Mathf.Clamp01(StateTime / _slamTelegraph);
            _slamFill.SetRadius(_slamRadius * progress);
        }

        protected override bool TickAttack(float deltaTime)
        {
            HitOverlapping(_slamCenter, _slamRadius);
            AudioManager.Play(_data.SlamSound);
            return true;
        }

        protected override void OnEnterState(EnemyState state)
        {
            if (state == EnemyState.Telegraph)
            {
                // 끝까지 따라가면 피할 길이 없으므로, 예고가 시작되는 순간의 자리와 크기로 고정한다.
                GolemData.Stage stage = CurrentStage;
                _slamRadius = stage.SlamRadius;
                _slamTelegraph = stage.TelegraphDuration;
                _slamDirection = DirectionToTarget();
                _slamCenter = (Vector2)transform.position + _slamDirection * stage.SlamForward;
                ShowSlam();
                return;
            }

            HideSlam();
        }

        private void ShowSlam()
        {
            _slamRange.transform.position = _slamCenter;
            _slamFill.transform.position = _slamCenter;
            _slamRange.BuildCircle(_slamRadius);
            _slamFill.CopyLimits(_slamRange);
            _slamFill.SetRadius(0f);
            _slamRange.Visible = true;
            _slamFill.Visible = true;
        }

        private void HideSlam()
        {
            _slamRange.Visible = false;
            _slamFill.Visible = false;
        }
    }
}
