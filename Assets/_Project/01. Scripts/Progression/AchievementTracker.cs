using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Progression
{
    // 판 도중 업적 조건을 세고, 달성하면 보상을 해금하고 바로 저장한다(드물게 일어나므로 그때그때 저장해도 된다).
    // 폭탄 하나로 잡은 수: 폭탄이 터질 때 맞은 적의 처치 이벤트가 같은 프레임 안에서 폭발 이벤트보다 먼저 온다.
    // 그래서 이번 프레임에 쌓인 "일반 폭탄으로 잡은" 처치 수를 폭발 이벤트가 가져간다. 스킬 폭탄 처치는 섞이지 않는다.
    // 스킬 조건은 발동 시작~끝 사이의 처치와, 처치 출처(블랙홀·다발폭탄·순간폭탄)로 센다.
    // 개발자 패널을 쓴 판은 세지 않는다.
    public class AchievementTracker : MonoBehaviour
    {
        // 스킬별 누적 처치를 저장하는 이름 앞부분. 뒤에 처치 출처 이름이 붙는다.
        private const string AbilityKillsStatPrefix = "kills_";
        // 종류와 상관없이 스킬로 잡은 누적 수, 회피 누적 수.
        private const string AnyAbilityKillsStat = "kills_ability";
        private const string EvadesStat = "evades";

        [SerializeField] private UnlockCatalog _catalog;

        private bool _isRunning;
        private bool _isDisabled;
        private bool _isStoryMode;
        private int _runKills;
        private int _singleKillStreak;
        private int _frameKills;
        private int _frameKillsFrame = -1;
        // 발동 중인 스킬과 그 발동 동안 센 처치 수.
        private AbilityData _activeAbility;
        private int _useKills;
        private int _useThrows;
        private int _useShieldKills;
        // 이번 판에 스킬을 썼는지, 빗나간 폭탄이 있었는지.
        private bool _hasUsedAbility;
        private bool _hasMissed;

        // 이번 판에 업적을 세는 중인지. 개발자 패널이 상태 줄에 보여 준다.
        public bool IsCounting => _isRunning && !_isDisabled;

        public void DisableForThisRun()
        {
            if (_isDisabled)
            {
                return;
            }

            _isDisabled = true;
            Debug.Log("[AchievementTracker] 개발자 패널을 써서 이번 판은 업적을 세지 않습니다.");
        }

        private void OnEnable()
        {
            GameEvents.StageStarted += OnStageStarted;
            GameEvents.EnemyKilled += OnEnemyKilled;
            GameEvents.EnemyKilledBy += OnEnemyKilledBy;
            GameEvents.BombExploded += OnBombExploded;
            GameEvents.BombThrown += OnBombThrown;
            GameEvents.ThrownBombExploded += OnThrownBombExploded;
            GameEvents.PlayerEvaded += OnPlayerEvaded;
            GameEvents.PlayerDied += OnRunEnded;
            GameEvents.StageCleared += OnStageCleared;
            GameEvents.PlayerKilledBy += OnPlayerKilledBy;
            GameEvents.EnemiesInArenaChanged += OnEnemiesInArenaChanged;
            GameEvents.GolemLastStand += OnGolemLastStand;
            ProgressionEvents.AbilityStarted += OnAbilityStarted;
            ProgressionEvents.AbilityEnded += OnAbilityEnded;
        }

        private void OnDisable()
        {
            GameEvents.StageStarted -= OnStageStarted;
            GameEvents.EnemyKilled -= OnEnemyKilled;
            GameEvents.EnemyKilledBy -= OnEnemyKilledBy;
            GameEvents.BombExploded -= OnBombExploded;
            GameEvents.BombThrown -= OnBombThrown;
            GameEvents.ThrownBombExploded -= OnThrownBombExploded;
            GameEvents.PlayerEvaded -= OnPlayerEvaded;
            GameEvents.PlayerDied -= OnRunEnded;
            GameEvents.StageCleared -= OnStageCleared;
            GameEvents.PlayerKilledBy -= OnPlayerKilledBy;
            GameEvents.EnemiesInArenaChanged -= OnEnemiesInArenaChanged;
            GameEvents.GolemLastStand -= OnGolemLastStand;
            ProgressionEvents.AbilityStarted -= OnAbilityStarted;
            ProgressionEvents.AbilityEnded -= OnAbilityEnded;
        }

        private void OnStageStarted(bool isStoryMode)
        {
            _isRunning = true;
            _isStoryMode = isStoryMode;
            _runKills = 0;
            _singleKillStreak = 0;
            _hasUsedAbility = false;
            _hasMissed = false;
            // 예전 판에서 이미 다 해금했으면 여기서 챙긴다.
            CheckAllUnlocked();
        }

        private void OnRunEnded()
        {
            _isRunning = false;
        }

        private void OnStageCleared()
        {
            if (IsCounting && _isStoryMode)
            {
                Check(AchievementCondition.StoryCleared, 1);
            }

            OnRunEnded();
        }

        // 사망 이벤트보다 먼저 오므로 아직 세는 중이다.
        private void OnPlayerKilledBy(EnemyKind kind)
        {
            if (!IsCounting)
            {
                return;
            }

            AchievementData[] achievements = _catalog.Achievements;
            for (int i = 0; i < achievements.Length; i++)
            {
                if (achievements[i].Condition == AchievementCondition.KilledByEnemy && achievements[i].EnemyKind == kind)
                {
                    TryComplete(achievements[i], achievements[i].Target);
                }
            }
        }

        private void OnEnemiesInArenaChanged(int count)
        {
            if (IsCounting)
            {
                Check(AchievementCondition.EnemiesInArenaAtOnce, count);
            }
        }

        private void OnGolemLastStand(int seconds)
        {
            if (IsCounting)
            {
                Check(AchievementCondition.GolemLastStandSeconds, seconds);
            }
        }

        // 보상으로 해금이 생길 때마다 본다. 업적이 하나도 없는 종류(빈 목록)는 다 해금한 것으로 본다.
        private void CheckAllUnlocked()
        {
            if (!IsCounting || !AreAllUnlocked(_catalog.Weapons) || !AreAllUnlocked(_catalog.Abilities)
                || !AreAllUnlocked(_catalog.Upgrades) || !AreAllUnlocked(_catalog.Artifacts))
            {
                return;
            }

            Check(AchievementCondition.AllUnlocked, 1);
        }

        private static bool AreAllUnlocked(IUnlockable[] items)
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null && !items[i].IsUnlocked)
                {
                    return false;
                }
            }

            return true;
        }

        // 처치 수에 드는 처치(보스 제외). 한 판·누적 처치를 센다.
        private void OnEnemyKilled(Vector2 position)
        {
            if (!IsCounting)
            {
                return;
            }

            _runKills++;
            Check(AchievementCondition.KillsInOneRun, _runKills);
            // 누적은 판이 끝날 때 저장에 더해지므로, 그 전까지는 이번 판 수를 더해 본다.
            Check(AchievementCondition.TotalKills, SaveSystem.Data.TotalKills + _runKills);
            if (!_hasUsedAbility)
            {
                Check(AchievementCondition.KillsInOneRunWithoutAbility, _runKills);
            }

            if (!_hasMissed)
            {
                Check(AchievementCondition.NoMissKillsInOneRun, _runKills);
            }
        }

        // 폭발 직전까지 이번 프레임에 쌓인 처치가 그 폭탄의 처치다. 하나도 없으면 빗나감이다.
        private void OnThrownBombExploded()
        {
            if (!IsCounting)
            {
                return;
            }

            int kills = _frameKillsFrame == Time.frameCount ? _frameKills : 0;
            if (kills == 0)
            {
                _hasMissed = true;
            }
        }

        private void OnPlayerEvaded()
        {
            if (!IsCounting)
            {
                return;
            }

            SaveSystem.Data.AddStat(EvadesStat, 1);
            Check(AchievementCondition.EvadesTotal, SaveSystem.Data.GetStat(EvadesStat));
        }

        // 모든 처치(보스 포함)와 그 출처.
        private void OnEnemyKilledBy(DamageSource source, EnemyKind kind)
        {
            bool isBoss = kind == EnemyKind.Boss;
            if (!IsCounting)
            {
                return;
            }

            if (!isBoss && (source == DamageSource.Bomb || source == DamageSource.InstantBomb))
            {
                CountFrameKill();
            }

            if (IsAbilitySource(source))
            {
                CountAbilityKill(source, isBoss);
            }

            if (isBoss && source == DamageSource.Dodge)
            {
                Check(AchievementCondition.BossKilledByDash, 1);
            }

            if (_activeAbility != null && !isBoss && IsUseKill(_activeAbility, source))
            {
                _useKills++;
                Check(AchievementCondition.AbilityKillsInOneUse, _useKills, _activeAbility);
                if (kind == EnemyKind.ShieldBearer)
                {
                    _useShieldKills++;
                    Check(AchievementCondition.AbilityShieldKillsInOneUse, _useShieldKills, _activeAbility);
                }
            }
        }

        private void CountFrameKill()
        {
            if (_frameKillsFrame != Time.frameCount)
            {
                _frameKillsFrame = Time.frameCount;
                _frameKills = 0;
            }

            _frameKills++;
        }

        // 스킬로 잡은 적: 보스면 막타 조건을, 아니면 스킬별 누적을 본다.
        private void CountAbilityKill(DamageSource source, bool isBoss)
        {
            AchievementData[] achievements = _catalog.Achievements;
            if (isBoss)
            {
                for (int i = 0; i < achievements.Length; i++)
                {
                    AchievementData achievement = achievements[i];
                    if (achievement.Condition == AchievementCondition.BossKilledByAbility && achievement.Ability != null
                        && SourceOf(achievement.Ability.Kind) == source)
                    {
                        TryComplete(achievement, achievement.Target);
                    }
                }

                return;
            }

            string stat = AbilityKillsStatPrefix + source;
            SaveSystem.Data.AddStat(stat, 1);
            SaveSystem.Data.AddStat(AnyAbilityKillsStat, 1);
            int total = SaveSystem.Data.GetStat(stat);
            int anyTotal = SaveSystem.Data.GetStat(AnyAbilityKillsStat);
            for (int i = 0; i < achievements.Length; i++)
            {
                AchievementData achievement = achievements[i];
                if (achievement.Condition != AchievementCondition.AbilityKillsTotal)
                {
                    continue;
                }

                // 스킬을 정하지 않은 업적은 모든 스킬의 처치를 합쳐 센다.
                if (achievement.Ability == null)
                {
                    TryComplete(achievement, anyTotal);
                }
                else if (SourceOf(achievement.Ability.Kind) == source)
                {
                    TryComplete(achievement, total);
                }
            }
        }

        private void OnBombExploded(Vector2 position, float radius, int hitCount)
        {
            int kills = _frameKillsFrame == Time.frameCount ? _frameKills : 0;
            _frameKills = 0;
            if (!IsCounting || kills == 0)
            {
                return;
            }

            _singleKillStreak = kills == 1 ? _singleKillStreak + 1 : 0;
            Check(AchievementCondition.SingleKillStreak, _singleKillStreak);
            Check(AchievementCondition.KillsInOneBomb, kills);
        }

        private void OnAbilityStarted(AbilityData ability)
        {
            _activeAbility = ability;
            _useKills = 0;
            _useThrows = 0;
            _useShieldKills = 0;
            _hasUsedAbility = true;
        }

        // 일반 폭탄 던지기만 온다(스킬 폭탄은 이 이벤트를 내지 않는다).
        private void OnBombThrown()
        {
            if (!IsCounting || _activeAbility == null)
            {
                return;
            }

            _useThrows++;
            Check(AchievementCondition.AbilityThrowsInOneUse, _useThrows, _activeAbility);
        }

        private void OnAbilityEnded(AbilityData ability)
        {
            if (_activeAbility != ability)
            {
                return;
            }

            _activeAbility = null;
            if (IsCounting && _useKills == 0)
            {
                Check(AchievementCondition.AbilityUseWithoutKills, 1, ability);
            }
        }

        // 발동 동안 셀 처치인지. 순간폭탄은 발동 동안 잡은 모든 적, 나머지는 그 스킬로 잡은 적만.
        private static bool IsUseKill(AbilityData ability, DamageSource source)
        {
            return ability.Kind == AbilityKind.InstantBomb || source == SourceOf(ability.Kind);
        }

        private static bool IsAbilitySource(DamageSource source)
        {
            return source == DamageSource.BlackHole || source == DamageSource.ScatterBomb || source == DamageSource.InstantBomb;
        }

        private static DamageSource SourceOf(AbilityKind kind)
        {
            switch (kind)
            {
                case AbilityKind.ScatterBomb:
                    return DamageSource.ScatterBomb;
                case AbilityKind.InstantBomb:
                    return DamageSource.InstantBomb;
                default:
                    return DamageSource.BlackHole;
            }
        }

        // ability를 주면 그 스킬을 정한 업적과, 스킬을 정하지 않은(모든 스킬) 업적을 본다.
        private void Check(AchievementCondition condition, int value, AbilityData ability = null)
        {
            AchievementData[] achievements = _catalog.Achievements;
            for (int i = 0; i < achievements.Length; i++)
            {
                AchievementData achievement = achievements[i];
                if (achievement.Condition == condition && (ability == null || achievement.Ability == null || achievement.Ability == ability))
                {
                    TryComplete(achievement, value);
                }
            }
        }

        private void TryComplete(AchievementData achievement, int value)
        {
            if (value < achievement.Target || achievement.IsCompleted)
            {
                return;
            }

            SaveData data = SaveSystem.Data;
            data.CompleteAchievement(achievement.Id);
            if (achievement.RewardWeapon != null)
            {
                data.UnlockWeapon(achievement.RewardWeapon.Id);
            }

            if (achievement.RewardAbility != null)
            {
                data.UnlockAbility(achievement.RewardAbility.Id);
            }

            if (achievement.RewardUpgrade != null)
            {
                data.UnlockUpgrade(achievement.RewardUpgrade.Id);
            }

            if (achievement.RewardArtifact != null)
            {
                data.UnlockArtifact(achievement.RewardArtifact.Id);
            }

            SaveSystem.Save();
            ProgressionEvents.RaiseAchievementCompleted(achievement);
            if (achievement.RewardWeapon != null || achievement.RewardAbility != null
                || achievement.RewardUpgrade != null || achievement.RewardArtifact != null)
            {
                CheckAllUnlocked();
            }
        }
    }
}
