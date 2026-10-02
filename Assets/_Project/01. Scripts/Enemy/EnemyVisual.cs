using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 적 그림의 방향과 상태별 트윈 동작. 판정·상태와 무관한 표시만 맡고, 그림(Body)만 움직인다.
    // 상태가 바뀔 때 자세가 툭 끊기지 않도록 크기·기울기·위치를 스프링으로 따라가게 하고, 통통 뛰기·떨림은 그 위에 더한다.
    // 그림의 크기와 위치는 프리팹의 Body 트랜스폼 값을 기준으로 삼으므로, 크기 조절은 Body에서 한다.
    public class EnemyVisual : MonoBehaviour
    {
        // 이 이하로 움직이면 방향을 바꾸지 않는다. 벽에 붙어 미세하게 흔들릴 때 좌우로 깜빡이지 않게 한다.
        private const float TurnThreshold = 0.002f;
        // 이동 상태여도 거의 안 움직이면(목표에 붙어 있음 등) 뛰지 않는다. 제자리 뛰기는 "멈췄다"는 예고를 흐린다.
        private const float HopMoveThreshold = 0.0005f;
        // 뛰기가 켜지고 꺼지는 빠르기 (초당 비율). 멈출 때 착지하듯 줄어든다.
        private const float HopBlendSpeed = 8f;

        // 개체마다 뛰기 박자를 어긋나게 주기 위한 생성 순번.
        private static int _createdCount;

        // 그림이 원래 어느 쪽을 보고 그려졌는지. 반대쪽으로 갈 때 뒤집는다.
        [SerializeField] private bool _artFacesRight;
        [SerializeField] private Enemy _enemy;
        [SerializeField] private SpriteRenderer _sprite;
        [SerializeField] private EnemyMotionData _motion;

        private Transform _body;
        private Vector3 _baseLocalPosition;
        private Vector3 _baseLocalScale;
        private Color _baseColor;

        private Vector2 _lastPosition;
        private EnemyState _lastState;
        private float _stateTime;
        private bool _facesRight;
        private float _hopPhase;
        private float _hopWeight;
        private float _spinAngle;
        private Vector2 _deathDirection;

        private SpringValue _scaleX;
        private SpringValue _scaleY;
        private SpringValue _lean;
        private SpringValue _offsetX;
        private SpringValue _offsetY;

        private void Awake()
        {
            _body = _sprite.transform;
            _baseLocalPosition = _body.localPosition;
            _baseLocalScale = _body.localScale;
            _baseColor = _sprite.color;
            // 같은 무리가 박자를 맞춰 뛰면 기계적으로 보여서 개체마다 뛰기 박자를 어긋나게 둔다. 그림에만 쓰는 값이다.
            _hopPhase = Mathf.Repeat(_createdCount++ * 0.618f, 1f);
        }

        // 풀에서 다시 나올 때 지난 사망 연출의 투명도·크기가 남지 않게 한다.
        private void OnEnable()
        {
            _lastPosition = transform.position;
            _lastState = EnemyState.Move;
            _stateTime = 0f;
            _hopWeight = 0f;
            _scaleX.Snap(1f);
            _scaleY.Snap(1f);
            _lean.Snap(0f);
            _offsetX.Snap(0f);
            _offsetY.Snap(0f);
            _sprite.color = _baseColor;
            ApplyPose(Vector2.zero, Vector2.one, 0f);
        }

        private void LateUpdate()
        {
            float deltaTime = Time.deltaTime;
            Vector2 position = transform.position;
            Vector2 delta = position - _lastPosition;
            _lastPosition = position;

            EnemyState state = _enemy.CurrentState;
            if (state != _lastState)
            {
                OnStateChanged(_lastState, state, position);
                _lastState = state;
                _stateTime = 0f;
            }

            _stateTime += deltaTime;

            if (state == EnemyState.Dead)
            {
                TickDeath();
                return;
            }

            UpdateFacing(state, position, delta.x);
            TickPose(state, delta, deltaTime);
        }

        // 걸을 때는 가는 방향을, 예고·공격 중에는 예고한 방향을, 그 밖에 멈춰 있을 때는 플레이어 쪽을 본다.
        // 예고 중 플레이어를 따라 돌아보면, 방향이 고정된 적도 따라오는 것처럼 보여 피할 방향을 잘못 읽게 된다.
        private void UpdateFacing(EnemyState state, Vector2 position, float deltaX)
        {
            float faceX;
            if (state == EnemyState.Telegraph || state == EnemyState.Attack)
            {
                faceX = _enemy.AimDirection.x;
            }
            else if (state == EnemyState.Move && Mathf.Abs(deltaX) > TurnThreshold)
            {
                faceX = deltaX;
            }
            else
            {
                faceX = _enemy.TargetPosition.x - position.x;
            }

            if (Mathf.Abs(faceX) > TurnThreshold)
            {
                _facesRight = faceX > 0f;
                _sprite.flipX = _facesRight != _artFacesRight;
            }
        }

        private void OnStateChanged(EnemyState previous, EnemyState current, Vector2 position)
        {
            if (previous == EnemyState.Pulled)
            {
                // 돌던 각도에서 이어서 바로 서도록, 회전값을 기울기로 옮겨 스프링이 이어받게 한다.
                _lean.Snap(-Mathf.DeltaAngle(0f, _spinAngle) * FacingSign);
                _spinAngle = 0f;
            }

            switch (current)
            {
                case EnemyState.Attack:
                    // 공격 순간 자세로 순간 이동시키고, 스프링이 탄성 있게 되돌린다.
                    Vector2 aim = _enemy.AimDirection;
                    _offsetX.Snap(aim.x * _motion.KickOffset);
                    _offsetY.Snap(aim.y * _motion.KickOffset);
                    _scaleX.Snap(_motion.KickScale.x);
                    _scaleY.Snap(_motion.KickScale.y);
                    _lean.Snap(_motion.KickAngle);
                    break;

                case EnemyState.Pulled:
                    _spinAngle = -_lean.Value * FacingSign;
                    break;

                case EnemyState.Dead:
                    // 터진 쪽이 아니라 플레이어 반대쪽으로 날려, 누가 잡았는지가 바로 읽히게 한다.
                    Vector2 away = position - _enemy.TargetPosition;
                    _deathDirection = away.sqrMagnitude > 0.0001f ? away.normalized : Vector2.right;
                    break;
            }
        }

        private void TickPose(EnemyState state, Vector2 delta, float deltaTime)
        {
            Vector2 targetScale = Vector2.one;
            float targetLean = 0f;
            float telegraphProgress = 0f;

            switch (state)
            {
                case EnemyState.Telegraph:
                    // 예고 시간 내내 서서히 웅크려, 가장 웅크린 순간이 곧 공격 순간이 되게 한다.
                    float t = Mathf.Clamp01(_stateTime / _enemy.TelegraphDuration);
                    telegraphProgress = t * t * (3f - 2f * t);
                    targetScale = Vector2.LerpUnclamped(Vector2.one, _motion.TelegraphScale, telegraphProgress);
                    targetLean = _motion.TelegraphLean * telegraphProgress;
                    break;

                case EnemyState.Attack:
                    targetScale = _motion.AttackHoldScale;
                    targetLean = _motion.AttackHoldLean;
                    break;

                case EnemyState.Recover:
                    targetScale = _motion.RecoverScale;
                    break;

                case EnemyState.Pulled:
                    targetScale = _motion.PulledScale;
                    _spinAngle += _motion.PulledSpinSpeed * deltaTime;
                    break;
            }

            float frequency = _motion.SpringFrequency;
            float damping = _motion.SpringDamping;
            _scaleX.Step(targetScale.x, frequency, damping, deltaTime);
            _scaleY.Step(targetScale.y, frequency, damping, deltaTime);
            _lean.Step(targetLean, frequency, damping, deltaTime);
            _offsetX.Step(0f, frequency, damping, deltaTime);
            _offsetY.Step(0f, frequency, damping, deltaTime);

            Vector2 offset = new Vector2(_offsetX.Value, _offsetY.Value);
            Vector2 scale = new Vector2(_scaleX.Value, _scaleY.Value);
            float lean = _lean.Value;

            // 통통 뛰기: 한 번 뛸 때마다 위로 튀고, 착지 순간 납작해지며, 좌우로 번갈아 기운다.
            bool isWalking = state == EnemyState.Move && delta.sqrMagnitude > HopMoveThreshold * HopMoveThreshold;
            _hopWeight = Mathf.MoveTowards(_hopWeight, isWalking ? 1f : 0f, HopBlendSpeed * deltaTime);
            if (_hopWeight > 0f)
            {
                if (isWalking)
                {
                    _hopPhase += _motion.HopRate * deltaTime;
                }

                float wave = Mathf.Sin(Mathf.PI * _hopPhase);
                float air = Mathf.Abs(wave);
                float ground = 1f - air;
                float stretch = _hopWeight * (_motion.HopStretch * air - _motion.HopSquash * ground * ground * ground * ground);
                scale.y *= 1f + stretch;
                scale.x *= 1f - stretch * 0.5f;
                offset.y += _hopWeight * _motion.HopHeight * air;
                lean += _hopWeight * _motion.HopTilt * wave;
            }

            // 떨림은 무작위 대신 사인파로 둔다. 그림 전용이지만 매 프레임 다르게 튀면 위치가 읽기 어렵다.
            if (telegraphProgress > 0f)
            {
                offset.x += Mathf.Sin(_stateTime * _motion.TelegraphShakeRate * 2f * Mathf.PI) * _motion.TelegraphShake * telegraphProgress;
            }

            float angle = state == EnemyState.Pulled ? _spinAngle : -lean * FacingSign;
            ApplyPose(offset, scale, angle);
        }

        // 흐려진 채 멀리 밀려남 → 빠르게 부풂 → 쭉 줄어 사라짐. 전체 시간은 적 데이터의 사망 연출 시간을 따른다.
        private void TickDeath()
        {
            float t = Mathf.Clamp01(_stateTime / _enemy.DeathDuration);
            float knockEnd = _motion.DeathKnockEnd;
            float popPeak = _motion.DeathPopPeak;

            float knock = Mathf.Clamp01(t / knockEnd);
            float knockEase = 1f - (1f - knock) * (1f - knock) * (1f - knock);
            Vector2 offset = _deathDirection * (_motion.DeathKnockDistance * knockEase);

            float size;
            if (t < knockEnd)
            {
                size = 1f;
            }
            else if (t < popPeak)
            {
                float pop = (t - knockEnd) / (popPeak - knockEnd);
                size = Mathf.Lerp(1f, _motion.DeathPopScale, 1f - (1f - pop) * (1f - pop));
            }
            else
            {
                float shrink = (t - popPeak) / (1f - popPeak);
                size = Mathf.Lerp(_motion.DeathPopScale, 0f, shrink * shrink);
            }

            // 죽는 순간 바로 흐려져, 밀려나는 몸이 아직 살아 있는 적으로 읽히지 않게 한다.
            Color color = _baseColor;
            color.a *= _motion.DeathAlpha;
            _sprite.color = color;

            ApplyPose(offset, new Vector2(size, size), 0f);
        }

        private float FacingSign => _facesRight ? 1f : -1f;

        private void ApplyPose(Vector2 offset, Vector2 scale, float angle)
        {
            _body.localPosition = _baseLocalPosition + (Vector3)offset;
            _body.localScale = new Vector3(_baseLocalScale.x * scale.x, _baseLocalScale.y * scale.y, _baseLocalScale.z);
            _body.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
