using System;
using ExplodeIt.Core;
using ExplodeIt.Player;
using UnityEngine;

namespace ExplodeIt.Bombs
{
    public class BombLauncher : MonoBehaviour
    {
        [SerializeField] private WeaponData _weaponData;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private Bomb _bombPrefab;
        [SerializeField] private ThrowRangeIndicator _rangeIndicator;
        [SerializeField, Min(1)] private int _poolPrewarm = 16;
        [SerializeField, Min(1)] private int _poolMaxSize = 64;
        // 구조물 안에 떨어진 폭탄은 범위가 0이 되어 아무 일 없이 사라지므로, 착지점이 구조물 위면 던지지 않는다.
        [SerializeField] private LayerMask _obstacleMask;

        private static readonly Collider2D[] LandingBuffer = new Collider2D[1];

        private ComponentPool<Bomb> _pool;
        private ContactFilter2D _obstacleFilter;
        private WeaponStats _stats;
        private Camera _camera;
        private Action<Bomb> _releaseBomb;
        private int _chargesLeft;
        private float _nextThrowTime;
        private float _rechargeTimer;
        // 연발 무기에서 아직 자동으로 나갈 발 수와 다음 발 시각.
        private int _pendingShots;
        private float _nextBurstTime;
        private bool _canThrow = true;

        public WeaponStats Stats => _stats;
        public WeaponData Weapon => _weaponData;

        private void Awake()
        {
            _camera = Camera.main;
            _stats = new WeaponStats(_weaponData);
            _chargesLeft = _stats.Charges;
            _releaseBomb = ReleaseBomb;

            _obstacleFilter = new ContactFilter2D();
            _obstacleFilter.SetLayerMask(_obstacleMask);

            // 폭탄은 던진 뒤 플레이어를 따라가면 안 되므로 부모 없이 월드에 둔다.
            _pool = new ComponentPool<Bomb>(_bombPrefab, null, _poolPrewarm, _poolMaxSize);
            _pool.Prewarm(_poolPrewarm);
        }

        private void Start()
        {
            ApplyStats();
        }

        // 실행 중 수치가 바뀌면 화면 표시와 남은 개수를 새 수치에 맞춘다.
        public void ApplyStats()
        {
            _rangeIndicator.SetRadius(_stats.MaxThrowRange);
            _chargesLeft = Mathf.Min(_chargesLeft, _stats.Charges);
            RaiseChargesChanged();
        }

        private void RaiseChargesChanged()
        {
            GameEvents.RaiseBombChargesChanged(_chargesLeft, _stats.Charges);
        }

        public void ResetStats()
        {
            _stats.CopyFrom(_weaponData);
            ApplyStats();
        }

        // 무기를 갈아 끼운다. 새 무기를 바로 시험할 수 있게 가득 찬 상태로 시작한다.
        public void Equip(WeaponData weapon)
        {
            if (weapon == null)
            {
                return;
            }

            _weaponData = weapon;
            _stats.CopyFrom(weapon);
            _chargesLeft = _stats.Charges;
            _pendingShots = 0;
            ApplyStats();
        }

        private void OnEnable()
        {
            GameEvents.GameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.GameStateChanged -= OnGameStateChanged;
        }

        private void Update()
        {
            TickRecharge();

            if (!_canThrow)
            {
                return;
            }

            if (_pendingShots > 0)
            {
                TickBurst();
                return;
            }

            if (!_input.ThrowHeld || _chargesLeft <= 0 || Time.time < _nextThrowTime)
            {
                return;
            }

            Throw();
        }

        // 가득 차 있지 않으면 쿨타임마다 하나씩 찬다. 던져도 진행 중인 충전은 처음부터 다시 세지 않는다.
        // 한꺼번에 차는 방식은 다 쓸 때까지 몰아 던지고 기다리는 리듬만 남겨, 아껴 던지는 선택이 의미가 없었다.
        private void TickRecharge()
        {
            if (_chargesLeft >= _stats.Charges)
            {
                return;
            }

            _rechargeTimer -= Time.deltaTime;
            if (_rechargeTimer <= 0f)
            {
                // 연발 무기는 한 번 누를 몫(연발 수)이 한꺼번에 찬다.
                _chargesLeft = Mathf.Min(_chargesLeft + _stats.BurstCount, _stats.Charges);
                _rechargeTimer += _stats.RechargeTime;
                RaiseChargesChanged();
            }
        }

        // 막힌 곳을 겨누면 폭탄도 쓰지 않고 연사 간격도 소모하지 않는다. 커서를 벽 밖으로 옮기는 즉시 던져진다.
        private void Throw()
        {
            if (!TryLaunch())
            {
                return;
            }

            _pendingShots = Mathf.Min(_stats.BurstCount - 1, _chargesLeft);
            _nextBurstTime = Time.time + _stats.BurstInterval;
        }

        // 나머지 발은 누르고 있지 않아도 간격마다 그 순간의 커서 위치로 나간다.
        // 그 순간 커서가 막힌 곳이면 기다리지 않고 그 발만 건너뛴다. 기다리면 자동 연발의 박자가 깨진다.
        private void TickBurst()
        {
            if (Time.time < _nextBurstTime)
            {
                return;
            }

            _pendingShots--;
            _nextBurstTime = Time.time + _stats.BurstInterval;
            if (_chargesLeft > 0)
            {
                TryLaunch();
            }
        }

        private bool TryLaunch()
        {
            Vector2 target = GetThrowTarget();
            if (IsOnObstacle(target))
            {
                return false;
            }

            Bomb bomb = _pool.Get(transform.position);
            bomb.Launch(target, _stats, _releaseBomb);

            // 가득 찬 상태에서 처음 던질 때만 충전을 새로 시작한다.
            if (_chargesLeft >= _stats.Charges)
            {
                _rechargeTimer = _stats.RechargeTime;
            }

            _chargesLeft--;
            _nextThrowTime = Time.time + _stats.ThrowInterval;
            RaiseChargesChanged();
            GameEvents.RaiseBombThrown();
            return true;
        }

        private bool IsOnObstacle(Vector2 point)
        {
            return Physics2D.OverlapPoint(point, _obstacleFilter, LandingBuffer) > 0;
        }

        public Vector2 GetThrowTarget()
        {
            return GetThrowTarget(_stats.MaxThrowRange);
        }

        // 특수 폭탄은 사거리가 따로 있어서, 커서 위치 계산만 공유하고 사거리는 받아서 쓴다.
        public Vector2 GetThrowTarget(float maxRange)
        {
            Vector2 origin = transform.position;
            Vector2 aim = _camera.ScreenToWorldPoint(_input.Aim);
            return origin + Vector2.ClampMagnitude(aim - origin, maxRange);
        }

        private void ReleaseBomb(Bomb bomb)
        {
            _pool.Release(bomb);
        }

        private void OnGameStateChanged(GameState previous, GameState current)
        {
            _canThrow = current == GameState.Playing;
            // 죽거나 판이 끝나면 남은 연발은 취소한다. 일시정지는 풀리면 이어서 나가도록 남겨 둔다.
            if (!_canThrow && current != GameState.Paused)
            {
                _pendingShots = 0;
            }
        }
    }
}
