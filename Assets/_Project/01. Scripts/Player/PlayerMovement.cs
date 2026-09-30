using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private PlayerData _data;
        [SerializeField] private PlayerInputReader _input;

        private Rigidbody2D _rigidbody;
        private bool _canMove = true;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            GameEvents.GameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.GameStateChanged -= OnGameStateChanged;
        }

        private void FixedUpdate()
        {
            // 게임패드 스틱 대각선 입력이 1을 넘어 더 빨라지지 않게 자른다.
            Vector2 input = _canMove ? Vector2.ClampMagnitude(_input.Move, 1f) : Vector2.zero;
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
