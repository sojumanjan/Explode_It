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
        // 떨림 빠르기 (회/초). 눈으로 좌우가 구분되지 않을 만큼 빠르게 떨어야 진동처럼 보인다.
        private const float JitterRate = 25f;
        // 사망 연출 중 사라지는 순간 연출을 터트리는 지점 (전체 시간 대비 비율).
        private const float DeathEndPoint = 0.9f;

        // 개체마다 뛰기 박자를 어긋나게 주기 위한 생성 순번.
        private static int _createdCount;

        // 그림이 원래 어느 쪽을 보고 그려졌는지. 반대쪽으로 갈 때 뒤집는다.
        [SerializeField] private bool _artFacesRight;
        [SerializeField] private Enemy _enemy;
        [SerializeField] private SpriteRenderer _sprite;
        [SerializeField] private EnemyMotionData _motion;
        // 공격 순간에만 잠깐 바꿔 보여줄 그림. 비워 두면 그림은 그대로 두고 자세만 바꾼다.
        [SerializeField] private Sprite _attackSprite;
        // 기절(그로기) 동안 보여줄 그림. 비워 두면 그림은 그대로 두고 자세만 바꾼다.
        [SerializeField] private Sprite _stunnedSprite;
        // 죽는 순간(폭탄이 터진 순간)부터 사망 연출 동안 보여줄 그림. 비워 두면 죽기 직전 그림 그대로 간다.
        [SerializeField] private Sprite _deathSprite;

        private Transform _body;
        private Vector3 _baseLocalPosition;
        private Vector3 _baseLocalScale;
        private Color _baseColor;
        private Sprite _baseSprite;
        private int _lastAttackCount;
        // 공격 그림을 보여준 뒤 지난 시간. 음수면 보여주는 중이 아니다.
        private float _attackSpriteTime = -1f;
        private bool _hasPlayedDeathEnd;
        private Color _flashColor;
        private float _flashDuration;
        // 번쩍임이 시작된 뒤 지난 시간. 음수면 번쩍이는 중이 아니다.
        private float _flashTime = -1f;
        private Color _tint = Color.white;
        private float _alpha = 1f;

        private Vector2 _lastPosition;
        private EnemyState _lastState;
        private float _stateTime;
        private bool _facesRight;
        private float _hopPhase;
        private float _hopWeight;
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
            _baseSprite = _sprite.sprite;
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
            _sprite.sprite = _baseSprite;
            _attackSpriteTime = -1f;
            _hasPlayedDeathEnd = false;
            _flashTime = -1f;
            _tint = Color.white;
            _alpha = 1f;
            Lift = 0f;
            ExtraLean = 0f;
            ExtraScale = Vector2.one;
            Jitter = 0f;
            _lastAttackCount = _enemy.AttackCount;
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

            if (_enemy.AttackCount != _lastAttackCount)
            {
                _lastAttackCount = _enemy.AttackCount;
                OnAttacked();
            }

            TickSprite(state, deltaTime);

            if (state == EnemyState.Dead)
            {
                TickDeath();
                return;
            }

            TickFlash(deltaTime);
            UpdateFacing(state, position, delta.x);
            TickPose(state, delta, deltaTime);
        }

        // 걸을 때(블랙홀에 끌려갈 때 포함)는 가는 방향을, 예고·공격 중에는 예고한 방향을, 그 밖에 멈춰 있을 때는 플레이어 쪽을 본다.
        // 예고 중 플레이어를 따라 돌아보면, 방향이 고정된 적도 따라오는 것처럼 보여 피할 방향을 잘못 읽게 된다.
        private void UpdateFacing(EnemyState state, Vector2 position, float deltaX)
        {
            // 기절(그로기)은 쓰러진 상태라 플레이어를 따라 돌아보면 어색하다. 쓰러진 방향 그대로 둔다.
            if (state == EnemyState.Stunned)
            {
                return;
            }

            float faceX;
            if (_enemy.FacesTargetWhileMoving)
            {
                faceX = _enemy.TargetPosition.x - position.x;
            }
            else if (state == EnemyState.Telegraph || state == EnemyState.Attack)
            {
                faceX = _enemy.AimDirection.x;
            }
            else if (IsWalkingState(state) && Mathf.Abs(deltaX) > TurnThreshold)
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
            switch (current)
            {
                case EnemyState.Dead:
                    // 터진 쪽이 아니라 플레이어 반대쪽으로 날려, 누가 잡았는지가 바로 읽히게 한다.
                    Vector2 away = position - _enemy.TargetPosition;
                    _deathDirection = away.sqrMagnitude > 0.0001f ? away.normalized : Vector2.right;
                    if (_deathSprite != null)
                    {
                        _sprite.sprite = _deathSprite;
                    }
                    break;

                case EnemyState.Stunned:
                    // 철퍼덕 주저앉는 순간 납작하게 눌렸다가 스프링으로 돌아온다.
                    _scaleX.Snap(_motion.StunnedImpactScale.x);
                    _scaleY.Snap(_motion.StunnedImpactScale.y);
                    break;
            }
        }

        // 계속 유지되는 색(과열로 점점 붉어짐 등). 기본 색에 곱한다. 번쩍임이 끝나면 이 색으로 돌아온다.
        public Color Tint
        {
            set => _tint = value;
        }

        // 계속 유지되는 투명도 (0~1). 땅속으로 파고들어 사라질 때처럼 몸 전체를 흐리게 할 때 쓴다.
        public float Alpha
        {
            set => _alpha = Mathf.Clamp01(value);
        }

        // 그림을 위로 띄우는 높이 (유닛). 도약처럼 판정 위치는 그대로 두고 그림만 들어 올릴 때 쓴다. 음수면 가라앉는다.
        public float Lift { get; set; }

        // 상태별 자세에 더하는 기울기 (도, + = 바라보는 쪽으로 숙임, - = 뒤로 젖힘). 뒷도약처럼 잠깐 몸을 젖힐 때 쓴다.
        public float ExtraLean { get; set; }

        // 상태별 자세에 곱하는 크기 배율 (가로, 세로). 드릴로 파고들 때처럼 잠깐 몸을 늘이거나 누를 때 쓴다.
        public Vector2 ExtraScale { get; set; } = Vector2.one;

        // 기본 그림 대신 보여줄 자세 그림. 여러 동작 그림을 가진 보스가 패턴마다 바꾼다. null이면 기본 그림.
        // 기절·공격 그림이 있으면 그쪽이 먼저다.
        public Sprite PoseSprite { get; set; }

        // 좌우로 빠르게 떠는 폭 (유닛). 드릴 진동처럼 그림만 떨게 할 때 쓴다. 0이면 떨지 않는다.
        public float Jitter { get; set; }

        private Color RestingColor
        {
            get
            {
                Color color = _baseColor * _tint;
                color.a = _baseColor.a * _alpha;
                return color;
            }
        }

        // 무적에 막혔을 때처럼 "맞았지만 안 먹혔다"를 보여준다. 색이 번쩍였다 돌아오고 몸이 움찔한다.
        public void Flash(Color color, float duration)
        {
            _flashColor = color;
            _flashDuration = Mathf.Max(duration, 0.01f);
            _flashTime = 0f;
            _scaleX.Snap(1.1f);
            _scaleY.Snap(0.9f);
        }

        // 번쩍임이 없을 때도 매 프레임 유지 색을 적용한다. 과열 색·투명도가 바뀌면 바로 반영되게 한다.
        private void TickFlash(float deltaTime)
        {
            Color resting = RestingColor;
            if (_flashTime < 0f)
            {
                _sprite.color = resting;
                return;
            }

            _flashTime += deltaTime;
            float t = _flashTime / _flashDuration;
            if (t >= 1f)
            {
                _flashTime = -1f;
                _sprite.color = resting;
                return;
            }

            Color flash = _flashColor;
            flash.a = resting.a;
            _sprite.color = Color.Lerp(flash, resting, t);
        }

        // 공격 순간 자세로 순간 이동시키고 스프링이 탄성 있게 되돌린다. 공격 그림이 있으면 잠깐 바꿔 보여준다.
        private void OnAttacked()
        {
            Vector2 aim = _enemy.AimDirection;
            _offsetX.Snap(aim.x * _motion.KickOffset);
            _offsetY.Snap(aim.y * _motion.KickOffset);
            _scaleX.Snap(_motion.KickScale.x);
            _scaleY.Snap(_motion.KickScale.y);
            _lean.Snap(_motion.KickAngle);

            if (_attackSprite != null)
            {
                _attackSpriteTime = 0f;
            }
        }

        // 기절 그림 > 공격 그림 > 자세 그림 > 기본 그림 순으로 고른다. 사망 그림은 죽는 순간 한 번 바꾸고 그대로 둔다.
        private void TickSprite(EnemyState state, float deltaTime)
        {
            // 죽은 뒤에는 그림을 고르지 않는다. 사망 그림이 없으면 죽는 순간의 그림 그대로 사망 연출을 한다.
            if (state == EnemyState.Dead)
            {
                return;
            }

            if (_attackSpriteTime >= 0f)
            {
                _attackSpriteTime += deltaTime;
                if (_attackSpriteTime >= _motion.AttackSpriteDuration)
                {
                    _attackSpriteTime = -1f;
                }
            }

            Sprite sprite = PoseSprite != null ? PoseSprite : _baseSprite;
            if (state == EnemyState.Stunned && _stunnedSprite != null)
            {
                sprite = _stunnedSprite;
            }
            else if (_attackSpriteTime >= 0f)
            {
                sprite = _attackSprite;
            }

            if (_sprite.sprite != sprite)
            {
                _sprite.sprite = sprite;
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

                // 그로기는 회복보다 더 크게 무너진 자세로, 지금이 때릴 때라는 걸 멀리서도 읽히게 한다.
                case EnemyState.Stunned:
                    targetScale = _motion.StunnedScale;
                    targetLean = _motion.StunnedLean;
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
            bool isWalking = IsWalkingState(state) && delta.sqrMagnitude > HopMoveThreshold * HopMoveThreshold;
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

            // 기절: 느리게 숨을 몰아쉬듯 오르내리고, 어지러운 듯 좌우로 천천히 흔들린다.
            if (state == EnemyState.Stunned)
            {
                float breath = Mathf.Sin(_stateTime * _motion.StunnedBreathRate * 2f * Mathf.PI) * _motion.StunnedBreath;
                scale.y *= 1f + breath;
                scale.x *= 1f - breath * 0.5f;
                lean += Mathf.Sin(_stateTime * _motion.StunnedSwayRate * 2f * Mathf.PI) * _motion.StunnedSway;
            }

            // 떨림은 무작위 대신 사인파로 둔다. 그림 전용이지만 매 프레임 다르게 튀면 위치가 읽기 어렵다.
            if (telegraphProgress > 0f)
            {
                offset.x += Mathf.Sin(_stateTime * _motion.TelegraphShakeRate * 2f * Mathf.PI) * _motion.TelegraphShake * telegraphProgress;
            }

            if (Jitter > 0f)
            {
                offset.x += Mathf.Sin(_stateTime * JitterRate * 2f * Mathf.PI) * Jitter;
            }

            scale = Vector2.Scale(scale, ExtraScale);
            ApplyPose(offset, scale, -(lean + ExtraLean) * FacingSign);
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

            // 뽀잉: 가로·세로가 번갈아 늘었다 줄며 출렁이다 끝으로 갈수록 잦아든다. 0이면 일반 적처럼 출렁이지 않는다.
            Vector2 scale = new Vector2(size, size);
            if (_motion.DeathWobble > 0f)
            {
                float wobble = Mathf.Sin(_stateTime * _motion.DeathWobbleRate * 2f * Mathf.PI) * _motion.DeathWobble * (1f - t);
                scale.x *= 1f + wobble;
                scale.y *= 1f - wobble;
            }

            ApplyPose(offset, scale, 0f);

            // 사라지기 직전에 터트린다. 풀 반환과 같은 프레임에 겹치면 놓칠 수 있어 끝을 조금 남겨 둔다.
            if (!_hasPlayedDeathEnd && t >= DeathEndPoint)
            {
                _hasPlayedDeathEnd = true;
                FeedbackPlayer.Play(_motion.DeathEndFeedback, _body.position);
            }
        }

        private float FacingSign => _facesRight ? 1f : -1f;

        // 블랙홀에 끌려가는 동안에도 따로 연출하지 않고 걷는 모습 그대로 둔다.
        private static bool IsWalkingState(EnemyState state)
        {
            return state == EnemyState.Move || state == EnemyState.Pulled;
        }

        private void ApplyPose(Vector2 offset, Vector2 scale, float angle)
        {
            _body.localPosition = _baseLocalPosition + (Vector3)offset + new Vector3(0f, Lift, 0f);
            _body.localScale = new Vector3(_baseLocalScale.x * scale.x, _baseLocalScale.y * scale.y, _baseLocalScale.z);
            _body.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
