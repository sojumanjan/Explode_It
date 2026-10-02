using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Player
{
    // 주인공 그림의 좌우 방향과 트윈 동작(숨쉬기, 통통 걷기, 구르기 눌림, 던지기). 판정과 무관하고 그림(Body)만 움직인다.
    // 걷는 중에도 항상 커서(던질 곳) 쪽을 본다. 폭탄을 어디로 던질지가 이동 방향보다 중요한 정보다.
    // 그림의 크기와 위치는 Body 트랜스폼 값을 기준으로 삼으므로, 크기 조절은 Body에서 한다.
    public class PlayerVisual : MonoBehaviour
    {
        // 이보다 느리면 서 있는 것으로 본다. 멈출 때 감속 중 걷기 모션이 잠깐 남는 정도로 둔다.
        private const float MovingSpeed = 0.5f;
        private const float TurnThreshold = 0.05f;
        // 걷기와 숨쉬기가 서로 넘어가는 빠르기 (초당 비율).
        private const float HopBlendSpeed = 8f;

        [SerializeField] private SpriteRenderer _sprite;
        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private PlayerDodge _dodge;
        [SerializeField] private PlayerMotionData _motion;
        // 그림이 원래 어느 쪽을 보고 그려졌는지. 반대쪽을 볼 때 뒤집는다.
        [SerializeField] private bool _artFacesRight;

        private Camera _camera;
        private Transform _body;
        private Vector3 _baseLocalPosition;
        private Vector3 _baseLocalScale;
        private bool _facesRight = true;
        private float _time;
        private float _hopPhase;
        private float _hopWeight;
        private bool _wasDodging;
        // 구르기 눌림 연출이 시작된 뒤 지난 시간. 음수면 연출 중이 아니다.
        private float _dodgeFxTime = -1f;

        private SpringValue _scaleX = new SpringValue(1f);
        private SpringValue _scaleY = new SpringValue(1f);
        private SpringValue _lean;

        private void Awake()
        {
            _camera = Camera.main;
            _body = _sprite.transform;
            _baseLocalPosition = _body.localPosition;
            _baseLocalScale = _body.localScale;
        }

        private void OnEnable()
        {
            GameEvents.GameStateChanged += OnGameStateChanged;
            GameEvents.BombThrown += OnBombThrown;
        }

        private void OnDisable()
        {
            GameEvents.GameStateChanged -= OnGameStateChanged;
            GameEvents.BombThrown -= OnBombThrown;
        }

        private void LateUpdate()
        {
            float deltaTime = Time.deltaTime;
            _time += deltaTime;

            float faceX = _camera.ScreenToWorldPoint(_input.Aim).x - transform.position.x;
            if (Mathf.Abs(faceX) > TurnThreshold)
            {
                _facesRight = faceX > 0f;
            }

            _sprite.flipX = _facesRight != _artFacesRight;

            float frequency = _motion.SpringFrequency;
            float damping = _motion.SpringDamping;
            _scaleX.Step(1f, frequency, damping, deltaTime);
            _scaleY.Step(1f, frequency, damping, deltaTime);
            _lean.Step(0f, frequency, damping, deltaTime);

            // 구르기는 회전 없이 위아래로 눌렸다가 천천히 펴진다. 스프링은 너무 빨리 돌아와서 시간을 직접 정한다.
            // 연출 동안은 뛰기·숨쉬기를 얹지 않아, 눌린 모양이 그대로 읽히게 한다.
            bool isDodging = _dodge.IsDodging;
            if (isDodging && !_wasDodging)
            {
                _dodgeFxTime = 0f;
            }

            _wasDodging = isDodging;
            if (_dodgeFxTime >= 0f)
            {
                TickDodgeSquash(deltaTime);
                return;
            }

            Vector2 scale = new Vector2(_scaleX.Value, _scaleY.Value);
            float lean = _lean.Value;
            float offsetY = 0f;

            bool isMoving = _rigidbody.linearVelocity.sqrMagnitude > MovingSpeed * MovingSpeed;
            _hopWeight = Mathf.MoveTowards(_hopWeight, isMoving ? 1f : 0f, HopBlendSpeed * deltaTime);

            // 서 있을 때만 숨쉬고, 걸을수록 통통 뛰기로 넘어간다.
            float breath = Mathf.Sin(_time * _motion.BreathRate * 2f * Mathf.PI) * _motion.BreathAmount * (1f - _hopWeight);
            scale.y *= 1f + breath;
            scale.x *= 1f - breath * 0.5f;

            if (_hopWeight > 0f)
            {
                if (isMoving)
                {
                    _hopPhase += _motion.HopRate * deltaTime;
                }

                float wave = Mathf.Sin(Mathf.PI * _hopPhase);
                float air = Mathf.Abs(wave);
                float ground = 1f - air;
                float stretch = _hopWeight * (_motion.HopStretch * air - _motion.HopSquash * ground * ground * ground * ground);
                scale.y *= 1f + stretch;
                scale.x *= 1f - stretch * 0.5f;
                offsetY += _hopWeight * _motion.HopHeight * air;
                lean += _hopWeight * _motion.HopTilt * wave;
            }

            ApplyPose(offsetY, scale, -lean * FacingSign);
        }

        // 눌리는 구간은 빠르게 들어가고, 펴지는 구간은 끝에서 살짝 넘쳤다가 멈춘다.
        private void TickDodgeSquash(float deltaTime)
        {
            _dodgeFxTime += deltaTime;
            float squashTime = _motion.DodgeSquashTime;
            float recoverTime = _motion.DodgeRecoverTime;

            float squash;
            if (_dodgeFxTime < squashTime)
            {
                float t = _dodgeFxTime / squashTime;
                squash = 1f - (1f - t) * (1f - t);
            }
            else if (_dodgeFxTime < squashTime + recoverTime)
            {
                // easeOutBack: 1에서 시작해 0을 살짝 지나쳤다가 0으로 돌아온다.
                float t = (_dodgeFxTime - squashTime) / recoverTime - 1f;
                const float overshoot = 1.2f;
                squash = 1f - (1f + (overshoot + 1f) * t * t * t + overshoot * t * t);
            }
            else
            {
                // 연출이 끝나면 걷기·숨쉬기가 툭 끊기지 않고 바로 이어지도록 스프링을 원래 자세에 맞춰 둔다.
                _dodgeFxTime = -1f;
                _scaleX.Snap(1f);
                _scaleY.Snap(1f);
                _lean.Snap(0f);
                squash = 0f;
            }

            ApplyPose(0f, Vector2.LerpUnclamped(Vector2.one, _motion.DodgeScale, squash), 0f);
        }

        private float FacingSign => _facesRight ? 1f : -1f;

        private void ApplyPose(float offsetY, Vector2 scale, float angle)
        {
            _body.localPosition = _baseLocalPosition + new Vector3(0f, offsetY, 0f);
            _body.localScale = new Vector3(_baseLocalScale.x * scale.x, _baseLocalScale.y * scale.y, _baseLocalScale.z);
            _body.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        // 던지는 순간 자세로 순간 이동시키고, 스프링이 탄성 있게 되돌린다.
        private void OnBombThrown()
        {
            _scaleX.Snap(_motion.ThrowScale.x);
            _scaleY.Snap(_motion.ThrowScale.y);
            _lean.Snap(_motion.ThrowAngle);
        }

        // 죽은 뒤 회색으로 멈춘 모습이 "끝났다"는 신호가 되도록 바로 선 자세로 세운다. 일시정지는 timeScale이 알아서 멈춘다.
        private void OnGameStateChanged(GameState previous, GameState current)
        {
            if (current == GameState.PlayerDead)
            {
                ApplyPose(0f, Vector2.one, 0f);
            }

            enabled = current != GameState.PlayerDead;
        }
    }
}
