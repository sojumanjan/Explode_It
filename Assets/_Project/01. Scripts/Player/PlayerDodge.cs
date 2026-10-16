using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Player
{
    // 누른 순간의 이동 방향으로 일정 거리를 구른다. 이동 입력이 없으면 마지막으로 움직인 방향으로 구른다.
    // 속도로 밀기 때문에 벽은 물리가 막아 준다.
    // 아티팩트: 연달아 쓸 수 있는 횟수(신발), 대시 중 부딪힌 적 처치와 쿨타임 증가(헬름).
    [RequireComponent(typeof(Rigidbody2D), typeof(HitReceiver))]
    public class PlayerDodge : MonoBehaviour
    {
        [SerializeField] private DodgeData _data;
        [SerializeField] private PlayerInputReader _input;
        // 대시로 적을 처치하는 아티팩트를 꼈을 때 닿았는지 검사할 적 레이어.
        [SerializeField] private LayerMask _enemyMask;

        // 대시 한 번에 검사하는 적 수 상한. 할당 없이 재사용한다.
        private static readonly Collider2D[] KillBuffer = new Collider2D[16];

        private Rigidbody2D _rigidbody;
        private HitReceiver _hitReceiver;
        private Vector2 _direction;
        private Vector2 _lastMoveDirection = Vector2.right;
        private float _dodgeEndTime;
        // 남은 대시 횟수와 다음 한 번이 차는 시각. 기본은 1회라 예전처럼 "쓰면 쿨타임"과 같다.
        private int _maxCharges = 1;
        private int _charges = 1;
        private float _rechargeAt;
        private float _cooldown;
        private ContactFilter2D _enemyFilter;
        // 이번 대시에서 이미 친 적. 방패에 막힌 적을 매 프레임 다시 치지 않게 한다.
        private readonly HitReceiver[] _struck = new HitReceiver[16];
        private int _struckCount;
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

        // 아티팩트 적용기가 Awake에서 판 보정값을 채운 뒤에 읽는다.
        private void Start()
        {
            _maxCharges = 1 + RunModifiers.ExtraDodges;
            _charges = _maxCharges;
            _cooldown = _data.Cooldown * RunModifiers.DodgeCooldownMultiplier;
            _enemyFilter = new ContactFilter2D();
            _enemyFilter.SetLayerMask(_enemyMask);
            _enemyFilter.useTriggers = true;
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

            TickRecharge();
            if (_canDodge && _charges > 0 && !IsDodging && _input.DodgePressed)
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
                if (RunModifiers.DodgeKillRadius > 0f)
                {
                    StrikeEnemies();
                }
            }
        }

        // 다 쓰지 않았으면 쿨타임마다 한 번씩 찬다(폭탄 장전과 같은 방식).
        private void TickRecharge()
        {
            if (_charges >= _maxCharges || Time.time < _rechargeAt)
            {
                return;
            }

            _charges++;
            _rechargeAt = Time.time + _cooldown;
        }

        // 몸보다 작은 원으로 검사해, 스치기만 한 적은 죽지 않고 정말 부딪힌 적만 죽는다.
        // 여러 대 맞아야 하는 적(보스)은 대시로 잡지 않는다. 방패병 정면은 방패가 막는다.
        private void StrikeEnemies()
        {
            Vector2 center = _rigidbody.position;
            int count = Physics2D.OverlapCircle(center, RunModifiers.DodgeKillRadius, _enemyFilter, KillBuffer);
            var hit = new HitInfo(center, false, DamageSource.Dodge);
            for (int i = 0; i < count; i++)
            {
                HitReceiver receiver = KillBuffer[i].GetComponentInParent<HitReceiver>();
                if (receiver == null || receiver.IsDead || receiver.RemainingHits > 1 || HasStruck(receiver))
                {
                    continue;
                }

                if (_struckCount < _struck.Length)
                {
                    _struck[_struckCount++] = receiver;
                }

                receiver.ReceiveHit(hit);
            }
        }

        private bool HasStruck(HitReceiver receiver)
        {
            for (int i = 0; i < _struckCount; i++)
            {
                if (_struck[i] == receiver)
                {
                    return true;
                }
            }

            return false;
        }

        private void StartDodge(Vector2 move)
        {
            _direction = move.sqrMagnitude > 0.01f ? move.normalized : _lastMoveDirection;
            _dodgeEndTime = Time.time + _data.Duration;
            // 가득 찬 상태에서 처음 쓸 때만 충전을 새로 시작한다.
            if (_charges >= _maxCharges)
            {
                _rechargeAt = Time.time + _cooldown;
            }

            _charges--;
            _struckCount = 0;
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
            GameEvents.RaisePlayerEvaded();
        }

        private void OnGameStateChanged(GameState previous, GameState current)
        {
            _canDodge = current == GameState.Playing;
        }
    }
}
