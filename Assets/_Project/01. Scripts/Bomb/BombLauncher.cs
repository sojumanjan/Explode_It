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
        // 구르는 동안 던지기를 막는 데 쓴다. 비워 두면 막지 않는다.
        [SerializeField] private PlayerDodge _dodge;
        [SerializeField] private Bomb _bombPrefab;
        [SerializeField] private ThrowRangeIndicator _rangeIndicator;
        [SerializeField, Min(1)] private int _poolPrewarm = 16;
        [SerializeField, Min(1)] private int _poolMaxSize = 64;
        // 구조물 안에 떨어진 폭탄은 범위가 0이 되어 아무 일 없이 사라지므로, 착지점이 구조물 위면 가장 가까운 바닥으로 옮긴다.
        [SerializeField] private LayerMask _obstacleMask;
        // 던지는 길을 막는 것(구조물, 맵 테두리). 커서가 그 너머에 있으면 막힌 지점 바로 앞까지만 던진다.
        // 플레이어가 갈 수 없는 곳(맵 밖 적 등장 구역 등)에 폭탄을 떨어뜨려 나오자마자 잡는 것을 막는다.
        [SerializeField] private LayerMask _throwBlockMask;

        // 막힌 지점에서 이만큼(유닛) 앞에 떨어뜨려 착지점이 벽 속으로 잡히지 않게 한다.
        private const float BlockedLandingGap = 0.15f;

        // 구조물 위를 조준했을 때 가까운 바닥을 찾는 범위. 원을 조금씩 넓히며 방향마다 확인한다(유닛, 개수).
        // 그림 크기에 맞춘 탐색 값이라 데이터로 빼지 않는다. 이 안에 바닥이 없으면 예전처럼 던지지 않는다.
        private const float GroundSearchStep = 0.1f;
        private const float GroundSearchRange = 3f;
        private const int GroundSearchDirections = 24;
        // 옮긴 착지점이 구조물 가장자리에 딱 붙지 않게 두는 여유 (유닛).
        private const float GroundClearance = 0.1f;

        private static readonly Collider2D[] LandingBuffer = new Collider2D[1];
        private static readonly Vector2[] SearchDirections = BuildSearchDirections();
        private static readonly RaycastHit2D[] ThrowHits = new RaycastHit2D[8];

        private ComponentPool<Bomb> _pool;
        private ContactFilter2D _obstacleFilter;
        private ContactFilter2D _throwBlockFilter;
        private WeaponStats _stats;
        private Camera _camera;
        private Action<Bomb> _releaseBomb;
        private int _chargesLeft;
        private float _nextThrowTime;
        // 던지기 간격·대시가 끝나기 직전에 누른 클릭을 잠깐 기억해 두는 시각. 빠르게 연타한 클릭이 씹히지 않게 한다.
        private float _throwQueuedUntil = -1f;
        private const float ThrowBufferTime = 0.15f;
        private float _rechargeTimer;
        // 연발 무기에서 아직 자동으로 나갈 발 수와 다음 발 시각.
        private int _pendingShots;
        private float _nextBurstTime;
        private bool _canThrow = true;
        // 일반 투척의 처치 출처. 순간폭탄 스킬이 켜져 있는 동안은 그 스킬로 센다.
        private DamageSource _throwSource = DamageSource.Bomb;
        // 순간폭탄 동안 바꾸기 전 수치.
        private bool _isInstantMode;
        private float _savedFuseDelay;
        private float _savedThrowInterval;
        private float _savedRechargeTime;
        private float _savedExplosionRadius;
        private float _savedMaxThrowRange;

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
            _throwBlockFilter = new ContactFilter2D();
            _throwBlockFilter.useTriggers = false;
            _throwBlockFilter.SetLayerMask(_throwBlockMask);

            // 폭탄은 던진 뒤 플레이어를 따라가면 안 되므로 부모 없이 월드에 둔다.
            _pool = new ComponentPool<Bomb>(_bombPrefab, null, _poolPrewarm, _poolMaxSize);
            _pool.Prewarm(_poolPrewarm);
        }

        private void Start()
        {
            ApplyStats();
            RaiseWeaponSound();
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
            RaiseWeaponSound();
        }

        private void RaiseWeaponSound()
        {
            GameEvents.RaiseExplosionPitchChanged(_weaponData.ExplosionPitchMin, _weaponData.ExplosionPitchMax);
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

            // 폭탄이 없을 때 누른 것은 기억하지 않는다. 다시 차는 순간 저절로 나가면 의도와 다르기 때문이다.
            // 순간폭탄 동안은 꾹 누르고 있으면 연사 간격·충전마다 저절로 나간다. 짧은 지속 시간에 연타하느라 조준이 흔들리지 않게 한다.
            bool wantsThrow = _input.ThrowPressed || (_isInstantMode && _input.ThrowHeld);
            if (wantsThrow && _chargesLeft > 0)
            {
                _throwQueuedUntil = Time.time + ThrowBufferTime;
            }

            if (Time.time > _throwQueuedUntil || _chargesLeft <= 0 || Time.time < _nextThrowTime || IsBlockedByDodge())
            {
                return;
            }

            _throwQueuedUntil = -1f;
            Throw();
        }

        // 구르는 동안은 던지지 못한다. 회피와 공격을 한 번에 하지 못하게 해 대시를 쓰는 순간에 대가를 둔다.
        // 아티팩트(빨간 리본)를 끼면 구르면서도 던진다.
        private bool IsBlockedByDodge()
        {
            return _dodge != null && _dodge.IsDodging && !RunModifiers.CanThrowWhileDodging;
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
            // 스킬은 모두 방패를 뚫는다. 순간폭탄 발동 중에 던진 폭탄도 스킬 폭탄이다.
            bool ignoresGuard = _throwSource == DamageSource.InstantBomb;
            bomb.Launch(target, _stats, new BombOptions(false, ignoresGuard, _throwSource), null, _releaseBomb);

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
        // 가는 길에 벽이나 맵 테두리가 있으면 그 바로 앞까지만 던진다.
        public Vector2 GetThrowTarget(float maxRange)
        {
            Vector2 origin = transform.position;
            Vector2 aim = _camera.ScreenToWorldPoint(_input.Aim);
            Vector2 offset = Vector2.ClampMagnitude(aim - origin, maxRange);
            float distance = offset.magnitude;
            if (distance <= 0.0001f)
            {
                return origin;
            }

            Vector2 direction = offset / distance;
            int count = Physics2D.Raycast(origin, direction, _throwBlockFilter, ThrowHits, distance);
            for (int i = 0; i < count; i++)
            {
                if (ThrowHits[i].distance < distance)
                {
                    distance = ThrowHits[i].distance;
                }
            }

            if (count > 0)
            {
                distance = Mathf.Max(0f, distance - BlockedLandingGap);
            }

            return SnapToGround(origin + direction * distance, origin, maxRange);
        }

        // 구조물(울타리·건물 등) 위를 조준하면 던지지 않는 대신, 조준한 점에서 가장 가까운 바닥으로 옮겨 그곳을 착지점으로 삼는다.
        // 맵 밖을 조준하면 테두리 앞에 떨어지는 것과 같은 감각이다. 같은 거리면 플레이어에게 가까운 쪽을 고른다.
        // 옮긴 점도 사거리 안이어야 하고, 플레이어에서 그 점까지 던지는 길이 막히면 안 된다.
        private Vector2 SnapToGround(Vector2 target, Vector2 origin, float maxRange)
        {
            if (!IsOnObstacle(target))
            {
                return target;
            }

            float maxRangeSqr = maxRange * maxRange;
            for (float radius = GroundSearchStep; radius <= GroundSearchRange; radius += GroundSearchStep)
            {
                bool isFound = false;
                Vector2 best = target;
                float bestSqr = float.MaxValue;
                for (int i = 0; i < SearchDirections.Length; i++)
                {
                    Vector2 point = target + SearchDirections[i] * radius;
                    float toOriginSqr = (point - origin).sqrMagnitude;
                    if (toOriginSqr > maxRangeSqr || toOriginSqr >= bestSqr)
                    {
                        continue;
                    }

                    if (Physics2D.OverlapCircle(point, GroundClearance, _obstacleFilter, LandingBuffer) > 0 || IsThrowBlocked(origin, point))
                    {
                        continue;
                    }

                    isFound = true;
                    best = point;
                    bestSqr = toOriginSqr;
                }

                if (isFound)
                {
                    return best;
                }
            }

            return target;
        }

        private bool IsThrowBlocked(Vector2 origin, Vector2 point)
        {
            Vector2 toPoint = point - origin;
            float distance = toPoint.magnitude;
            return distance > 0.0001f && Physics2D.Raycast(origin, toPoint / distance, _throwBlockFilter, ThrowHits, distance) > 0;
        }

        private static Vector2[] BuildSearchDirections()
        {
            var directions = new Vector2[GroundSearchDirections];
            for (int i = 0; i < directions.Length; i++)
            {
                float angle = Mathf.PI * 2f * i / directions.Length;
                directions[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }

            return directions;
        }

        private void ReleaseBomb(Bomb bomb)
        {
            _pool.Release(bomb);
        }

        // 스킬이 쓰는 폭탄. 일반 폭탄과 같은 풀을 쓰고, 보유 개수·연사 간격과는 상관없다.
        // 스킬은 드물게 쓰이므로 끝 알림을 위한 람다 할당은 감수한다.
        public void LaunchSkillBomb(Vector2 from, Vector2 target, WeaponStats stats, BombOptions options,
            Action<Vector2> onExploded, Action onFinished)
        {
            Bomb bomb = _pool.Get(from);
            bomb.Launch(target, stats, options, onExploded, finished =>
            {
                _pool.Release(finished);
                onFinished?.Invoke();
            });
        }

        // 순간폭탄 스킬: 던진 폭탄이 땅에 닿자마자 터지고 연사·충전이 빨라지며 사거리가 늘어난다. 범위 배율은 강화로 커질 수 있다.
        public void BeginInstantMode(float speedMultiplier, float radiusMultiplier, float rangeMultiplier)
        {
            if (_isInstantMode)
            {
                return;
            }

            _isInstantMode = true;
            _savedFuseDelay = _stats.FuseDelay;
            _savedThrowInterval = _stats.ThrowInterval;
            _savedRechargeTime = _stats.RechargeTime;
            _savedExplosionRadius = _stats.ExplosionRadius;
            _savedMaxThrowRange = _stats.MaxThrowRange;
            float speed = Mathf.Max(0.01f, speedMultiplier);
            _stats.FuseDelay = 0f;
            _stats.ThrowInterval /= speed;
            _stats.RechargeTime /= speed;
            _stats.ExplosionRadius *= radiusMultiplier;
            _stats.MaxThrowRange *= rangeMultiplier;
            _rechargeTimer = Mathf.Min(_rechargeTimer, _stats.RechargeTime);
            _throwSource = DamageSource.InstantBomb;
            ApplyStats();
        }

        public void EndInstantMode()
        {
            if (!_isInstantMode)
            {
                return;
            }

            _isInstantMode = false;
            _stats.FuseDelay = _savedFuseDelay;
            _stats.ThrowInterval = _savedThrowInterval;
            _stats.RechargeTime = _savedRechargeTime;
            _stats.ExplosionRadius = _savedExplosionRadius;
            _stats.MaxThrowRange = _savedMaxThrowRange;
            _throwSource = DamageSource.Bomb;
            // 꾹 누른 채로 끝나면 기억해 둔 입력 때문에 일반 폭탄이 한 발 더 나가지 않게 지운다.
            _throwQueuedUntil = -1f;
            ApplyStats();
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
