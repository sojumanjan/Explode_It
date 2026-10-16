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

        [SerializeField] private UnlockCatalog _catalog;

        private bool _isRunning;
        private bool _isDisabled;
        private int _runKills;
        private int _singleKillStreak;
        private int _frameKills;
        private int _frameKillsFrame = -1;
        // 발동 중인 스킬과 그 발동 동안 센 처치 수.
        private AbilityData _activeAbility;
        private int _useKills;

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
            GameEvents.PlayerDied += OnRunEnded;
            GameEvents.StageCleared += OnRunEnded;
            ProgressionEvents.AbilityStarted += OnAbilityStarted;
            ProgressionEvents.AbilityEnded += OnAbilityEnded;
        }

        private void OnDisable()
        {
            GameEvents.StageStarted -= OnStageStarted;
            GameEvents.EnemyKilled -= OnEnemyKilled;
            GameEvents.EnemyKilledBy -= OnEnemyKilledBy;
            GameEvents.BombExploded -= OnBombExploded;
            GameEvents.PlayerDied -= OnRunEnded;
            GameEvents.StageCleared -= OnRunEnded;
            ProgressionEvents.AbilityStarted -= OnAbilityStarted;
            ProgressionEvents.AbilityEnded -= OnAbilityEnded;
        }

        private void OnStageStarted(bool isStoryMode)
        {
            _isRunning = true;
            _runKills = 0;
            _singleKillStreak = 0;
        }

        private void OnRunEnded()
        {
            _isRunning = false;
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
        }

        // 모든 처치(보스 포함)와 그 출처.
        private void OnEnemyKilledBy(DamageSource source, bool isBoss)
        {
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

            if (_activeAbility != null && !isBoss && IsUseKill(_activeAbility, source))
            {
                _useKills++;
                Check(AchievementCondition.AbilityKillsInOneUse, _useKills, _activeAbility);
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
            int total = SaveSystem.Data.GetStat(stat);
            for (int i = 0; i < achievements.Length; i++)
            {
                AchievementData achievement = achievements[i];
                if (achievement.Condition == AchievementCondition.AbilityKillsTotal && achievement.Ability != null
                    && SourceOf(achievement.Ability.Kind) == source)
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

        // ability를 주면 그 스킬을 기준으로 한 업적만 본다.
        private void Check(AchievementCondition condition, int value, AbilityData ability = null)
        {
            AchievementData[] achievements = _catalog.Achievements;
            for (int i = 0; i < achievements.Length; i++)
            {
                AchievementData achievement = achievements[i];
                if (achievement.Condition == condition && (ability == null || achievement.Ability == ability))
                {
                    TryComplete(achievement, value);
                }
            }
        }

        private static void TryComplete(AchievementData achievement, int value)
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
        }
    }
}
