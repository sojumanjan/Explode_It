using ExplodeIt.Bombs;
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 방패병: 다가옴 → 멈춰서 앞쪽 원 범위 예고(차오름) → 메이스 내려치기 → 회복.
    // 몸과 따로 도는 방패를 들고 있어, 폭발 순간 폭탄 중심이 방패 정면 각도 안이면 죽지 않는다. 블랙홀은 방패를 관통한다.
    // 방패는 이동 방향을 향해 일정한 속도로만 돌기 때문에, 지금 어디를 막고 있고 곧 어디로 돌지가 눈에 보인다.
    // 예측 과제: 다가오는 길에 폭탄을 미리 깔아, 1초 뒤 방패병이 그 폭탄을 지나쳐 등 뒤에 두게 만든다. 또는 옆으로 돌아 측면을 노린다.
    public class ShieldBearerEnemy : Enemy, IHitGuard
    {
        // 이 이하로 움직이면 이동 방향으로 치지 않는다. 벽에 붙어 미세하게 흔들릴 때 방패가 떨지 않게 한다.
        private const float MoveThreshold = 0.002f;

        [SerializeField] private ShieldBearerData _data;
        [SerializeField] private Transform _shield;
        [SerializeField] private ExplosionShape _slamRange;
        [SerializeField] private ExplosionShape _slamFill;
        [SerializeField] private EnemyVisual _visual;

        private float _shieldAngle;
        private float _targetAngle;
        private Vector2 _lastPosition;
        private Vector2 _slamDirection = Vector2.right;
        private Vector2 _slamCenter;

        protected override EnemyData Data => _data;
        public override Vector2 AimDirection => _slamDirection;

        private Vector2 BodyCenter => (Vector2)transform.position + _data.BodyCenterOffset;
        private Vector2 ShieldDirection => new Vector2(Mathf.Cos(_shieldAngle * Mathf.Deg2Rad), Mathf.Sin(_shieldAngle * Mathf.Deg2Rad));

        protected override void OnEnable()
        {
            base.OnEnable();
            HideSlam();
            HitReceiver.Blocked += OnHitBlocked;
            _shield.gameObject.SetActive(true);
            _lastPosition = transform.position;
            // 나오자마자 플레이어 쪽을 막고 있게 한다. 이후엔 이동 방향을 따라 돈다.
            Vector2 toTarget = TargetPosition - (Vector2)transform.position;
            _shieldAngle = toTarget.sqrMagnitude > 0.0001f ? Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg : 0f;
            _targetAngle = _shieldAngle;
            PlaceShield();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            HitReceiver.Blocked -= OnHitBlocked;
        }

        // 방패 각도는 판정에도 쓰이므로, 그림과 판정이 같은 값을 보도록 매 프레임 한 곳에서만 바꾼다.
        private void Update()
        {
            if (CurrentState == EnemyState.Dead)
            {
                return;
            }

            Vector2 position = transform.position;
            Vector2 delta = position - _lastPosition;
            _lastPosition = position;
            if (delta.sqrMagnitude > MoveThreshold * MoveThreshold)
            {
                _targetAngle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            }

            _shieldAngle = Mathf.MoveTowardsAngle(_shieldAngle, _targetAngle, _data.ShieldTurnSpeed * Time.deltaTime);
            PlaceShield();
        }

        private void PlaceShield()
        {
            Vector2 direction = ShieldDirection;
            _shield.position = BodyCenter + direction * _data.ShieldOrbitRadius;
            // 방패 그림은 볼록한 쪽이 위(+y)를 보고 그려져 있다.
            _shield.rotation = Quaternion.Euler(0f, 0f, _shieldAngle - 90f);
        }

        // 폭탄 중심이 방패 정면 각도 안이면 막는다. 폭탄이 몸 한가운데 있으면 방향이 없으므로 막지 않는다.
        public bool Blocks(in HitInfo hit)
        {
            Vector2 toSource = hit.SourcePosition - BodyCenter;
            if (toSource.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            return Vector2.Angle(ShieldDirection, toSource) <= _data.ShieldAngle * 0.5f;
        }

        protected override void TickMove(float deltaTime)
        {
            MoveTowardTarget(_data.MoveSpeed, deltaTime);
        }

        protected override bool ShouldStartAttack()
        {
            return IsTargetWithin(_data.AttackTriggerRange);
        }

        // 범위가 다 차는 순간 내려친다. 보이는 원이 곧 맞는 원이다.
        protected override void TickTelegraph(float deltaTime)
        {
            float progress = Mathf.Clamp01(StateTime / _data.TelegraphDuration);
            _slamFill.SetRadius(_data.SlamRadius * progress);
        }

        protected override bool TickAttack(float deltaTime)
        {
            HitOverlapping(_slamCenter, _data.SlamRadius);
            AudioManager.Play(_data.SlamSound);
            return true;
        }

        protected override void OnEnterState(EnemyState state)
        {
            switch (state)
            {
                case EnemyState.Telegraph:
                    // 근접이라 끝까지 따라가면 피할 길이 없으므로, 예고가 시작되는 순간의 자리로 고정한다.
                    _slamDirection = DirectionToTarget();
                    _slamCenter = (Vector2)transform.position + _slamDirection * _data.SlamForward;
                    ShowSlam();
                    return;

                case EnemyState.Dead:
                    _shield.gameObject.SetActive(false);
                    break;
            }

            HideSlam();
        }

        private void ShowSlam()
        {
            _slamRange.transform.position = _slamCenter;
            _slamFill.transform.position = _slamCenter;
            _slamRange.BuildCircle(_data.SlamRadius);
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

        // 방패에 막힌 폭탄에 "깡" 하는 반응을 준다. 반응이 없으면 빗나간 건지 막힌 건지 구분되지 않는다.
        private void OnHitBlocked(HitInfo hit)
        {
            FeedbackPlayer.Play(_data.BlockFeedback, _shield.position);
            AudioManager.Play(_data.BlockSound);
            if (_visual != null)
            {
                _visual.Flash(_data.BlockFlashColor, _data.BlockFlashDuration);
            }
        }
    }
}
