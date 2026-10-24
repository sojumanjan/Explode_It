using System;
using DG.Tweening;
using ExplodeIt.Bombs;
using ExplodeIt.Core;
using ExplodeIt.Player;
using ExplodeIt.Stage;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ExplodeIt.Progression
{
    // 처치로 게이지를 채우고, 한 칸이 차면 메뉴에서 고른 스킬을 쓸 수 있다. 칸은 여러 개라 모아 둘 수 있다.
    // 스킬은 한 번에 하나만 발동한다. 칸이 남아 있어도 앞의 발동이 끝난 뒤에 다음을 쓴다.
    //   블랙홀: 커서 쪽에 떨어져 범위 안의 적을 빨아들였다가 터진다.
    //   다발폭탄: 360도를 고르게 나눠 폭탄을 뿌린다(칸마다 살짝 비틂). 벽을 무시하고 방패를 뚫는다. 강화하면 터진 자리에서 작은 폭탄이 다시 튄다.
    //   순간폭탄: 정한 시간 동안 던진 폭탄이 땅에 닿자마자 터지고 연사·충전이 빨라지며 사거리가 늘어난다.
    // 해금하고 장착한 강화만 판이 시작될 때 이 스킬에 붙는다.
    // 아티팩트: 필요 처치 수 감소(망토), 칸 증가(벨트), 가만히 서 있으면 충전(동전), 많이 잡으면 회복(반지).
    public class PlayerAbility : MonoBehaviour
    {
        [SerializeField] private UnlockCatalog _catalog;
        // 메뉴에서 고른 스킬을 찾지 못할 때(게임 씬을 바로 켰을 때 등) 쓰는 스킬.
        [SerializeField] private AbilityData _data;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private BombLauncher _launcher;
        [SerializeField] private BlackHoleBomb _blackHolePrefab;

        private BlackHoleBomb _blackHole;
        private Action _onBlackHoleFinished;
        private Action<Vector2> _onScatterExploded;
        private Action _onSkillBombFinished;
        private WeaponStats _scatterStats;
        private WeaponStats _childStats;
        private BombOptions _scatterOptions;
        private int _charge;
        private bool _isActive;
        private bool _canUse = true;
        // 아직 터지지 않은 다발폭탄 수(작은 폭탄 포함). 0이 되면 발동이 끝난다.
        private int _pendingBombs;
        private float _instantEndTime = -1f;
        // 아티팩트를 반영한 한 칸에 필요한 처치 수와 칸 수.
        private int _killsToCharge;
        private int _maxCharges;
        // 이번 발동으로 잡은 수(반지)와, 이동하지 않은 시간·다음 충전까지 남은 시간(동전).
        private int _useKills;
        private float _stillTime;
        private float _nextStillCharge;

        // 해금한 강화를 합친 값.
        private float _radiusMultiplier = 1f;
        private float _durationBonus;
        private int _countBonus;
        private bool _hasChildBombs;

        private void Awake()
        {
            AbilityData chosen = _catalog != null ? _catalog.FindUsableAbility(RunSetup.AbilityId) : null;
            if (chosen != null)
            {
                _data = chosen;
            }

            ApplyUpgrades();
            _killsToCharge = Mathf.Max(1, _data.KillsToCharge - RunModifiers.KillsToChargeReduction);
            _maxCharges = Mathf.Max(1, _data.MaxCharges + RunModifiers.ExtraAbilityCharges);
            _nextStillCharge = RunModifiers.StillChargeDelay;

            // 한 번에 하나만 존재하므로 풀 대신 하나를 미리 만들어 재사용한다. 전투 중 생성이 일어나지 않는다.
            // 던진 뒤 플레이어를 따라가면 안 되므로 부모 없이 월드에 둔다.
            _blackHole = Instantiate(_blackHolePrefab);
            _blackHole.gameObject.SetActive(false);
            _onBlackHoleFinished = Finish;
            _onScatterExploded = SpawnChildBombs;
            _onSkillBombFinished = OnSkillBombFinished;
            _scatterOptions = new BombOptions(true, true, DamageSource.ScatterBomb);
            _scatterStats = BuildScatterStats(_data.ScatterRadius, _data.ScatterFuseDelayMin, _data.ScatterFlightDuration, _data.ScatterSpriteSize);
            _childStats = BuildScatterStats(_data.ChildRadius, _data.ChildFuseDelayMin, _data.ChildFlightDuration, _data.ChildSpriteSize);

            // 첫 위기에 바로 쓸 수 있게 차 있는 채로 시작한다. 능력을 처음부터 알게 되는 효과도 있다.
            _charge = _killsToCharge * Mathf.Min(_data.StartCharges, _maxCharges);
        }

        private void ApplyUpgrades()
        {
            if (_catalog == null)
            {
                return;
            }

            AbilityUpgradeData[] upgrades = _catalog.Upgrades;
            for (int i = 0; i < upgrades.Length; i++)
            {
                AbilityUpgradeData upgrade = upgrades[i];
                if (upgrade.Ability != _data || !upgrade.IsEquipped)
                {
                    continue;
                }

                switch (upgrade.Effect)
                {
                    case AbilityUpgradeEffect.RadiusMultiplier:
                        _radiusMultiplier *= upgrade.Value;
                        break;
                    case AbilityUpgradeEffect.DurationAdd:
                        _durationBonus += upgrade.Value;
                        break;
                    case AbilityUpgradeEffect.CountAdd:
                        _countBonus += Mathf.RoundToInt(upgrade.Value);
                        break;
                    case AbilityUpgradeEffect.ChildBombs:
                        _hasChildBombs = true;
                        break;
                }
            }
        }

        private WeaponStats BuildScatterStats(float radius, float fuseDelay, float flightDuration, float spriteSize)
        {
            var stats = new WeaponStats();
            stats.ExplosionRadius = radius;
            stats.FuseDelay = fuseDelay;
            stats.FlightDuration = flightDuration;
            stats.ArcHeight = _data.ScatterArcHeight;
            stats.BombSprite = _data.ScatterSprite;
            stats.BombSize = spriteSize;
            return stats;
        }

        private void OnEnable()
        {
            GameEvents.EnemyKilled += OnEnemyKilled;
            GameEvents.EnemyKilledBy += OnEnemyKilledBy;
            GameEvents.PlayerDodged += OnPlayerDodged;
            GameEvents.GameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.EnemyKilled -= OnEnemyKilled;
            GameEvents.EnemyKilledBy -= OnEnemyKilledBy;
            GameEvents.PlayerDodged -= OnPlayerDodged;
            GameEvents.GameStateChanged -= OnGameStateChanged;
        }

        // 게이지 UI가 OnEnable에서 구독을 마친 뒤 첫 값을 받도록 Start에서 알린다.
        private void Start()
        {
            RaiseChargeChanged();
        }

        private int MaxCharge => _killsToCharge * _maxCharges;

        // 개발자 패널용. 처치 없이 바로 시험해 볼 수 있게 모든 칸을 채운다.
        public void FillCharge()
        {
            _charge = MaxCharge;
            RaiseChargeChanged();
        }

        private void RaiseChargeChanged()
        {
            GameEvents.RaiseAbilityChargeChanged(_charge, _killsToCharge, _maxCharges);
        }

        private void Update()
        {
            if (_instantEndTime >= 0f && Time.time >= _instantEndTime)
            {
                _instantEndTime = -1f;
                _launcher.EndInstantMode();
                Finish();
            }

            TickStillCharge();

            if (!_canUse || _isActive || _charge < _killsToCharge || !_input.AbilityPressed)
            {
                return;
            }

            Use();
        }

        private void Use()
        {
            // 한 칸만 쓴다. 차던 중인 다음 칸의 진행분은 그대로 남는다.
            _charge -= _killsToCharge;
            _isActive = true;
            _useKills = 0;
            RaiseChargeChanged();
            GameEvents.RaiseAbilityUsed();
            ProgressionEvents.RaiseAbilityStarted(_data);

            switch (_data.Kind)
            {
                case AbilityKind.ScatterBomb:
                    ThrowScatter();
                    break;
                case AbilityKind.InstantBomb:
                    _launcher.BeginInstantMode(_data.InstantSpeedMultiplier, _radiusMultiplier, _data.InstantRangeMultiplier);
                    _instantEndTime = Time.time + _data.InstantDuration + _durationBonus;
                    break;
                default:
                    ThrowBlackHole();
                    break;
            }
        }

        private void ThrowBlackHole()
        {
            WeaponStats weapon = _launcher.Stats;
            _blackHole.transform.position = transform.position;
            _blackHole.gameObject.SetActive(true);
            _blackHole.Launch(_launcher.GetThrowTarget(_data.MaxThrowRange), _data.BlackHoleRadius * _radiusMultiplier,
                _data.PullDuration + _durationBonus, _data.PullSpeed, weapon, _onBlackHoleFinished);
        }

        // 360도를 폭탄 수로 나눈 칸마다 하나씩, 칸 가운데에서 정한 각도 안으로 살짝 비틀어 던진다.
        // 던지는 시각·거리·폭발까지 시간을 폭탄마다 무작위로 정해, 한꺼번에 날아가 동시에 터지지 않고 우르르 이어 터지게 한다.
        private void ThrowScatter()
        {
            int total = Mathf.Max(1, _data.ScatterCount + _countBonus);
            // 한 발은 발밑에 떨구고, 나머지로 360도를 나눈다.
            ThrowScatterBomb(Vector2.zero, _data.ScatterSelfDropRadius, 0f);
            int count = total - 1;
            if (count <= 0)
            {
                return;
            }

            float step = 360f / count;
            float jitter = _data.ScatterAngleJitter;
            for (int i = 0; i < count; i++)
            {
                float angle = (step * i + Random.Range(-jitter, jitter)) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                ThrowScatterBomb(direction, _data.ScatterDistanceMin, _data.ScatterDistanceMax);
            }
        }

        // 던지는 시각·거리·폭발까지 시간을 폭탄마다 무작위로 정한다. 방향이 0이면 거리 반경 안 아무 곳(발밑)에 떨군다.
        private void ThrowScatterBomb(Vector2 direction, float distanceMin, float distanceMax)
        {
            float fuse = Random.Range(_data.ScatterFuseDelayMin, _data.ScatterFuseDelayMax);
            Vector2 offset = direction == Vector2.zero
                ? Random.insideUnitCircle * distanceMin
                : direction * Random.Range(distanceMin, distanceMax);
            LaunchLater(Random.Range(0f, _data.ScatterThrowDelayMax), () =>
            {
                // 늦게 나가는 폭탄은 그 순간의 플레이어 자리에서 출발한다.
                Vector2 origin = transform.position;
                _scatterStats.FuseDelay = fuse;
                _launcher.LaunchSkillBomb(origin, ClampToArena(origin + offset), _scatterStats, _scatterOptions,
                    _hasChildBombs ? _onScatterExploded : null, _onSkillBombFinished);
            });
        }

        // 다발폭탄 강화: 터진 자리에서 작은 폭탄이 무작위 방향으로 튀어 나가 연달아 터진다. 작은 폭탄은 다시 튀지 않는다.
        private void SpawnChildBombs(Vector2 at)
        {
            int count = Random.Range(_data.ChildCountMin, _data.ChildCountMax + 1);
            for (int i = 0; i < count; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                Vector2 target = ClampToArena(at + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(_data.ChildDistanceMin, _data.ChildDistanceMax));
                float fuse = Random.Range(_data.ChildFuseDelayMin, _data.ChildFuseDelayMax);
                LaunchLater(Random.Range(0f, _data.ChildThrowDelayMax), () =>
                {
                    _childStats.FuseDelay = fuse;
                    _launcher.LaunchSkillBomb(at, target, _childStats, _scatterOptions, null, _onSkillBombFinished);
                });
            }
        }

        // 아직 안 나간 폭탄도 발동이 끝나기 전에 셀 수 있게, 예약하는 순간 남은 수에 넣는다. 스킬은 드물어 람다 할당은 감수한다.
        private void LaunchLater(float delay, TweenCallback launch)
        {
            _pendingBombs++;
            if (delay <= 0f)
            {
                launch();
                return;
            }

            DOVirtual.DelayedCall(delay, launch).SetLink(gameObject);
        }

        private void OnSkillBombFinished()
        {
            _pendingBombs--;
            if (_pendingBombs <= 0 && _isActive)
            {
                _pendingBombs = 0;
                Finish();
            }
        }

        // 벽은 무시하지만 맵 밖에는 떨어지지 않게 한다.
        private static Vector2 ClampToArena(Vector2 point)
        {
            ArenaBounds arena = ArenaBounds.Current;
            if (arena == null)
            {
                return point;
            }

            Bounds bounds = arena.Bounds;
            point.x = Mathf.Clamp(point.x, bounds.min.x, bounds.max.x);
            point.y = Mathf.Clamp(point.y, bounds.min.y, bounds.max.y);
            return point;
        }

        // 스킬로 뭉친 적을 지우면 게이지가 곧바로 다시 차서 연달아 쓰게 되므로, 발동이 끝날 때까지는 처치를 세지 않는다.
        private void OnEnemyKilled(Vector2 position)
        {
            if (_isActive || _charge >= MaxCharge)
            {
                return;
            }

            _charge++;
            RaiseChargeChanged();
        }

        // 이동 입력이 없으면 시간을 센다. 폭탄은 던져도 되지만 움직이거나 구르면 처음부터.
        private void TickStillCharge()
        {
            if (RunModifiers.StillChargeDelay <= 0f || !_canUse)
            {
                return;
            }

            if (_input.Move.sqrMagnitude > 0.01f)
            {
                ResetStill();
                return;
            }

            _stillTime += Time.deltaTime;
            if (_stillTime < _nextStillCharge)
            {
                return;
            }

            _nextStillCharge += Mathf.Max(0.1f, RunModifiers.StillChargeInterval);
            AddCharge(1);
        }

        private void ResetStill()
        {
            _stillTime = 0f;
            _nextStillCharge = RunModifiers.StillChargeDelay;
        }

        private void OnPlayerDodged()
        {
            ResetStill();
        }

        // 발동 중에는 처치로 게이지가 차지 않으므로, 아티팩트 충전도 같은 규칙을 따른다.
        private void AddCharge(int amount)
        {
            if (_isActive || _charge >= MaxCharge)
            {
                return;
            }

            _charge = Mathf.Min(_charge + amount, MaxCharge);
            RaiseChargeChanged();
        }

        // 이번 발동의 스킬로 잡은 적(보스 제외). 반지 회복 조건에 쓴다.
        private void OnEnemyKilledBy(DamageSource source, EnemyKind kind)
        {
            if (_isActive && kind != EnemyKind.Boss && (source == DamageSource.BlackHole || source == DamageSource.ScatterBomb
                || source == DamageSource.InstantBomb))
            {
                _useKills++;
            }
        }

        private void Finish()
        {
            if (_data.Kind == AbilityKind.BlackHole)
            {
                _blackHole.gameObject.SetActive(false);
            }

            _isActive = false;
            if (RunModifiers.AbilityRefundMinKills > 0 && _useKills >= RunModifiers.AbilityRefundMinKills)
            {
                AddCharge(RunModifiers.AbilityRefundAmount);
            }

            GameEvents.RaiseAbilityFinished();
            ProgressionEvents.RaiseAbilityEnded(_data);
        }

        private void OnGameStateChanged(GameState previous, GameState current)
        {
            _canUse = current == GameState.Playing;
        }
    }
}
