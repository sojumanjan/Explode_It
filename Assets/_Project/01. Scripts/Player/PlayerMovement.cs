using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private PlayerData _data;
        [SerializeField] private PlayerInputReader _input;
        // 구르는 동안은 구르기가 속도를 정한다. 비워 두면 구르기 없이 이동만 한다.
        [SerializeField] private PlayerDodge _dodge;

        // 연출이 시킨 대로 저절로 걸어갈 자리에 닿았다고 보는 거리 (유닛).
        private const float WalkArriveDistance = 0.15f;

        private Rigidbody2D _rigidbody;
        private bool _canMove = true;
        private bool _isAutoWalking;
        private Vector2 _walkTarget;
        // 아티팩트 이속 보너스 조건: 폭탄이 가득 장전됐는지, 스킬 직후 보너스가 끝나는 시각.
        private bool _areBombsFull = true;
        private float _abilityBonusUntil = -1f;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            GameEvents.GameStateChanged += OnGameStateChanged;
            GameEvents.PlayerWalkRequested += OnWalkRequested;
            GameEvents.PlayerWalkCancelled += OnWalkCancelled;
            GameEvents.BombChargesChanged += OnBombChargesChanged;
            GameEvents.AbilityUsed += OnAbilityUsed;
        }

        private void OnDisable()
        {
            GameEvents.GameStateChanged -= OnGameStateChanged;
            GameEvents.PlayerWalkRequested -= OnWalkRequested;
            GameEvents.PlayerWalkCancelled -= OnWalkCancelled;
            GameEvents.BombChargesChanged -= OnBombChargesChanged;
            GameEvents.AbilityUsed -= OnAbilityUsed;
            _isAutoWalking = false;
        }

        private void OnWalkRequested(Vector2 target)
        {
            _walkTarget = target;
            _isAutoWalking = true;
        }

        private void OnBombChargesChanged(int left, int max)
        {
            _areBombsFull = left >= max;
        }

        private void OnAbilityUsed()
        {
            if (RunModifiers.MoveSpeedBonusAfterAbility > 0f)
            {
                _abilityBonusUntil = Time.time + RunModifiers.MoveSpeedBonusAfterAbilityDuration;
            }
        }

        // 아티팩트 보너스를 더한 이동속도. 보너스끼리는 더한다(+10%와 +20%면 +30%).
        private float CurrentMoveSpeed()
        {
            float bonus = 0f;
            if (_areBombsFull)
            {
                bonus += RunModifiers.MoveSpeedBonusWhenBombsFull;
            }

            if (Time.time < _abilityBonusUntil)
            {
                bonus += RunModifiers.MoveSpeedBonusAfterAbility;
            }

            return _data.MoveSpeed * (1f + bonus);
        }

        private void OnWalkCancelled()
        {
            _isAutoWalking = false;
        }

        // 연출 중에는 조작 대신 정한 자리를 향해 걷는다. 이동 속도·가감속은 평소와 같아 걷는 모습이 자연스럽다.
        private Vector2 AutoWalkInput()
        {
            Vector2 toTarget = _walkTarget - _rigidbody.position;
            if (toTarget.magnitude <= WalkArriveDistance)
            {
                _isAutoWalking = false;
                return Vector2.zero;
            }

            return toTarget.normalized;
        }

        private void FixedUpdate()
        {
            if (_dodge != null && _dodge.IsDodging)
            {
                return;
            }

            // 게임패드 스틱 대각선 입력이 1을 넘어 더 빨라지지 않게 자른다.
            Vector2 input = _canMove ? Vector2.ClampMagnitude(_input.Move, 1f) : Vector2.zero;
            if (_isAutoWalking && _canMove)
            {
                input = AutoWalkInput();
            }
            Vector2 targetVelocity = input * CurrentMoveSpeed();
            float rate = input.sqrMagnitude > 0f ? _data.Acceleration : _data.Deceleration;

            _rigidbody.linearVelocity = Vector2.MoveTowards(
                _rigidbody.linearVelocity, targetVelocity, rate * Time.fixedDeltaTime);
        }

        private void OnGameStateChanged(GameState previous, GameState current)
        {
            _canMove = current == GameState.Playing;
        }
    }
}
