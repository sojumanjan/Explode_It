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

        private ComponentPool<Bomb> _pool;
        private WeaponStats _stats;
        private Camera _camera;
        private Action<Bomb> _releaseBomb;
        private int _chargesLeft;
        private float _nextThrowTime;
        private float _rechargeTimer;
        private bool _canThrow = true;

        public WeaponStats Stats => _stats;

        private void Awake()
        {
            _camera = Camera.main;
            _stats = new WeaponStats(_weaponData);
            _chargesLeft = _stats.Charges;
            _releaseBomb = ReleaseBomb;

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

            if (!_canThrow || !_input.ThrowHeld)
            {
                return;
            }

            if (_chargesLeft <= 0 || Time.time < _nextThrowTime)
            {
                return;
            }

            Throw();
        }

        // 마지막 투척 후 쿨타임이 지나면 한 번에 가득 찬다.
        // 다 쓰기 전에도 차므로, 계속 던질지 잠깐 멈추고 채울지를 플레이어가 고르게 된다.
        private void TickRecharge()
        {
            if (_chargesLeft >= _stats.Charges)
            {
                return;
            }

            _rechargeTimer -= Time.deltaTime;
            if (_rechargeTimer <= 0f)
            {
                _chargesLeft = _stats.Charges;
                RaiseChargesChanged();
            }
        }

        private void Throw()
        {
            Bomb bomb = _pool.Get(transform.position);
            bomb.Launch(GetThrowTarget(), _stats, _releaseBomb);

            _chargesLeft--;
            _nextThrowTime = Time.time + _stats.ThrowInterval;
            _rechargeTimer = _stats.RechargeTime;
            RaiseChargesChanged();
        }

        // 특수 폭탄도 같은 사거리 원 안으로 던지도록 착지점 계산을 공유한다.
        public Vector2 GetThrowTarget()
        {
            Vector2 origin = transform.position;
            Vector2 aim = _camera.ScreenToWorldPoint(_input.Aim);
            return origin + Vector2.ClampMagnitude(aim - origin, _stats.MaxThrowRange);
        }

        private void ReleaseBomb(Bomb bomb)
        {
            _pool.Release(bomb);
        }

        private void OnGameStateChanged(GameState previous, GameState current)
        {
            _canThrow = current == GameState.Playing;
        }
    }
}
