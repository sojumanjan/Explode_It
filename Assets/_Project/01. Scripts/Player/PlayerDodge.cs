using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Player
{
    // 누른 순간의 이동 방향으로 일정 거리를 구른다. 이동 입력이 없으면 마지막으로 움직인 방향으로 구른다.
    // 속도로 밀기 때문에 벽은 물리가 막아 준다.
    [RequireComponent(typeof(Rigidbody2D), typeof(HitReceiver))]
    public class PlayerDodge : MonoBehaviour
    {
        [SerializeField] private DodgeData _data;
        [SerializeField] private PlayerInputReader _input;

        private Rigidbody2D _rigidbody;
        private HitReceiver _hitReceiver;
        private Vector2 _direction;
        private Vector2 _lastMoveDirection = Vector2.right;
        private float _dodgeEndTime;
        private float _readyTime;
        private bool _canDodge = true;
        // 이번 구르기의 무적이 끝나는 시각과, 그동안 공격을 흘려 낸 연출을 이미 냈는지.
        private float _evadeUntil;
        private bool _hasEvaded;

        public bool IsDodging => Time.time < _dodgeEndTime;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _hitReceiver = GetComponent<HitReceiver>();
        }

        private void OnEnable()
        {
            GameEvents.GameStateChanged += OnGameStateChanged;
            _hitReceiver.Blocked += OnBlocked;
        }

        private void OnDisable()
        {
            GameEvents.GameStateChanged -= OnGameStateChanged;
            _hitReceiver.Blocked -= OnBlocked;
        }

        private void Update()
        {
            Vector2 move = _input.Move;
            if (move.sqrMagnitude > 0.01f)
            {
                _lastMoveDirection = move.normalized;
            }

            if (_canDodge && Time.time >= _readyTime && _input.DodgePressed)
            {
                StartDodge(move);
            }
        }

        // 이동 컴포넌트는 구르는 동안 속도를 건드리지 않으므로 여기서만 속도를 정한다.
        private void FixedUpdate()
        {
            if (_canDodge && IsDodging)
            {
                _rigidbody.linearVelocity = _direction * (_data.Distance / _data.Duration);
            }
        }

        private void StartDodge(Vector2 move)
        {
            _direction = move.sqrMagnitude > 0.01f ? move.normalized : _lastMoveDirection;
            _dodgeEndTime = Time.time + _data.Duration;
            _readyTime = Time.time + _data.Cooldown;
            _hitReceiver.GrantInvulnerability(_data.InvulnerableDuration);
            _evadeUntil = Time.time + _data.InvulnerableDuration;
            _hasEvaded = false;
            AudioManager.Play(_data.Sound);
            GameEvents.RaisePlayerDodged();
        }

        // 구르기 무적이 공격을 막아 냈다. 치트 무적이나 보스 실드로 막힌 것은 빼고, 구르기 한 번에 한 번만 잠깐 느려진다.
        // 아슬아슬하게 피했다는 손맛을 주기 위해서다.
        private void OnBlocked(HitInfo hit)
        {
            if (_hasEvaded || Time.time >= _evadeUntil || _hitReceiver.CheatInvulnerable)
            {
                return;
            }

            _hasEvaded = true;
            FeedbackPlayer.Play(_data.EvadeFeedback, transform.position);
        }

        private void OnGameStateChanged(GameState previous, GameState current)
        {
            _canDodge = current == GameState.Playing;
        }
    }
}
