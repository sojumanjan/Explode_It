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

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            GameEvents.GameStateChanged += OnGameStateChanged;
            GameEvents.PlayerWalkRequested += OnWalkRequested;
            GameEvents.PlayerWalkCancelled += OnWalkCancelled;
        }

        private void OnDisable()
        {
            GameEvents.GameStateChanged -= OnGameStateChanged;
            GameEvents.PlayerWalkRequested -= OnWalkRequested;
            GameEvents.PlayerWalkCancelled -= OnWalkCancelled;
            _isAutoWalking = false;
        }

        private void OnWalkRequested(Vector2 target)
        {
            _walkTarget = target;
            _isAutoWalking = true;
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
            Vector2 targetVelocity = input * _data.MoveSpeed;
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
