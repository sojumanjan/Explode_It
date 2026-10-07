using ExplodeIt.Bombs;
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 사마귀: 가만히 멈춰 있다가 → 다음 지점으로 순식간에 돌진 → 다시 멈춤을 반복하며 다가온다.
    // 공격 거리 안에서 멈춰 있으면 그 방향으로 고정하고 짧게 예고한 뒤 부채꼴로 벤다(전사와 같은 공격, 더 짧고 더 빠름).
    // 예측 과제: 위치가 끊겨서 움직이므로, 멈춘 자리와 다음 돌진이 끝날 자리를 읽고 폭탄을 둔다.
    public class MantisEnemy : Enemy
    {
        // 돌진 시간 중 눌린 모양까지 들어가는 데 쓰는 비율. 출발하자마자 납작해져야 "슥" 하는 느낌이 난다.
        private const float SquashInPortion = 0.3f;
        // 남은 거리가 이보다 짧으면 돌진하지 않고 계속 멈춰 있는다. 제자리에서 움찔대지 않게 한다.
        private const float MinDashDistance = 0.05f;
        // 공격 시작 거리 대비 돌진을 멈출 거리. 시작 거리 바로 안쪽에 서야 멈춘 직후 공격이 시작된다.
        private const float StopRangeRatio = 0.8f;

        [SerializeField] private MantisData _data;
        [SerializeField] private EnemyVisual _visual;
        // 휘두르는 순간 판정과 같은 부채꼴을 잠깐 그려, 맞은 이유를 바로 알 수 있게 한다.
        [SerializeField] private ExplosionShape _swingShape;
        [SerializeField, Min(0f)] private float _swingFxDuration = 0.18f;

        private Vector2 _aimDirection = Vector2.right;
        private float _swingFxTime = -1f;
        private bool _isDashing;
        private float _moveTime;
        private Vector2 _dashEnd;
        private float _dashSpeed;
        // 돌진이 끝난 뒤 지난 시간. 음수면 눌린 모양에서 돌아오는 중이 아니다.
        private float _squashRecoverTime = -1f;

        protected override EnemyData Data => _data;
        public override Vector2 AimDirection => _aimDirection;

        protected override void OnEnable()
        {
            _isDashing = false;
            base.OnEnable();
            // 풀에서 다시 나올 때 지난 삶의 눌림 연출이 이어지지 않게 한다.
            _squashRecoverTime = -1f;
            _swingShape.Visible = false;
            _swingFxTime = -1f;
        }

        protected override void TickMove(float deltaTime)
        {
            _moveTime += deltaTime;
            if (!_isDashing)
            {
                if (_moveTime >= _data.PauseDuration)
                {
                    BeginDash();
                }

                return;
            }

            MoveTowardPoint(_dashEnd, _dashSpeed, deltaTime);
            if (_moveTime >= _data.DashDuration)
            {
                EndDash();
            }
        }

        // 다음 지점은 출발하는 순간 정한다. 돌진 중에는 플레이어를 따라 꺾지 않아 끝날 자리를 읽을 수 있다.
        private void BeginDash()
        {
            Vector2 position = Body.position;
            // 입구로 들어오는 중이면 입구 점까지, 아니면 플레이어 공격 거리 바로 앞까지만 돌진한다.
            float stopRange = IsEntering ? 0f : _data.AttackTriggerRange * StopRangeRatio;
            float distance = Mathf.Min(_data.DashDistance, (MoveGoal - position).magnitude - stopRange);
            _moveTime = 0f;
            if (distance < MinDashDistance)
            {
                return;
            }

            // 다른 적과 같은 길찾기 방향을 따른다(입구로 갈 때는 곧장, 쫓을 때는 구조물을 돌아서).
            _dashEnd = position + MoveDirection() * distance;
            _dashSpeed = distance / _data.DashDuration;
            _isDashing = true;
            _squashRecoverTime = -1f;
        }

        private void EndDash()
        {
            _isDashing = false;
            _moveTime = 0f;
            _squashRecoverTime = 0f;
        }

        // 멈춰 있을 때만 공격을 시작한다. 돌진 도중 꺾여 베면 예고를 읽을 틈이 없다.
        protected override bool ShouldStartAttack()
        {
            return !_isDashing && IsTargetWithin(_data.AttackTriggerRange);
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
            // 돌진 도중 끊겼으면(블랙홀·사망) 눌린 모양에서 튕겨 돌아오게 한다. 이미 돌아오는 중이면 그대로 둔다.
            if (_isDashing)
            {
                _isDashing = false;
                _squashRecoverTime = 0f;
            }

            // 이동으로 돌아올 때마다 멈춤부터 다시 센다. 회복·블랙홀에서 풀린 직후 바로 튀어나가지 않게 한다.
            _moveTime = 0f;

            // 근접이라 끝까지 따라가면 피할 길이 없다. 예고가 시작되는 순간의 방향으로 고정한다.
            if (state == EnemyState.Telegraph)
            {
                _aimDirection = DirectionToTarget();
            }
        }

        // 트윈 대신 시간을 직접 세서, 적이 수십 마리여도 매번 트윈을 만들지 않는다.
        private void Update()
        {
            TickSquash(Time.deltaTime);
            TickSwingFx(Time.deltaTime);
        }

        // 돌진하자마자 납작하게 눌리고, 멈추면 살짝 넘쳤다가 원래 모양으로 튕겨 돌아온다.
        private void TickSquash(float deltaTime)
        {
            if (_visual == null)
            {
                return;
            }

            if (_isDashing)
            {
                float t = Mathf.Clamp01(_moveTime / (_data.DashDuration * SquashInPortion));
                _visual.ExtraScale = Vector2.LerpUnclamped(Vector2.one, _data.DashSquash, t);
                return;
            }

            if (_squashRecoverTime < 0f)
            {
                return;
            }

            _squashRecoverTime += deltaTime;
            float r = Mathf.Clamp01(_squashRecoverTime / _data.SquashRecoverTime);
            // 끝에서 살짝 넘치는 감속 곡선(back ease out).
            const float overshoot = 1.70158f;
            float u = r - 1f;
            float ease = 1f + (overshoot + 1f) * u * u * u + overshoot * u * u;
            _visual.ExtraScale = Vector2.LerpUnclamped(_data.DashSquash, Vector2.one, ease);
            if (r >= 1f)
            {
                _visual.ExtraScale = Vector2.one;
                _squashRecoverTime = -1f;
            }
        }

        private void TickSwingFx(float deltaTime)
        {
            if (_swingFxTime < 0f)
            {
                return;
            }

            _swingFxTime += deltaTime;
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
            _swingShape.BuildSector(_aimDirection, _data.AttackRadius, _data.AttackAngle);
            _swingShape.SetRadius(0f);
            _swingShape.ResetColor();
            _swingShape.Visible = true;
            _swingFxTime = 0f;
        }
    }
}
