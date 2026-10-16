using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Progression
{
    // 판 도중 업적 조건을 세고, 달성하면 보상을 해금하고 바로 저장한다(드물게 일어나므로 그때그때 저장해도 된다).
    // 폭탄 하나로 잡은 수: 폭탄이 터질 때 맞은 적의 처치 이벤트가 같은 프레임 안에서 폭발 이벤트보다 먼저 온다.
    // 그래서 이번 프레임에 쌓인 처치 수를 폭발 이벤트가 가져간다. 같은 프레임에 블랙홀로 잡은 적이 섞이면 함께 세지는데, 드물어 그대로 둔다.
    // 개발자 패널을 쓴 판은 세지 않는다.
    public class AchievementTracker : MonoBehaviour
    {
        [SerializeField] private UnlockCatalog _catalog;

        private bool _isRunning;
        private bool _isDisabled;
        private int _runKills;
        private int _singleKillStreak;
        private int _frameKills;
        private int _frameKillsFrame = -1;

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
            GameEvents.BombExploded += OnBombExploded;
            GameEvents.PlayerDied += OnRunEnded;
            GameEvents.StageCleared += OnRunEnded;
        }

        private void OnDisable()
        {
            GameEvents.StageStarted -= OnStageStarted;
            GameEvents.EnemyKilled -= OnEnemyKilled;
            GameEvents.BombExploded -= OnBombExploded;
            GameEvents.PlayerDied -= OnRunEnded;
            GameEvents.StageCleared -= OnRunEnded;
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

        private void OnEnemyKilled(Vector2 position)
        {
            if (!IsCounting)
            {
                return;
            }

            if (_frameKillsFrame != Time.frameCount)
            {
                _frameKillsFrame = Time.frameCount;
                _frameKills = 0;
            }

            _frameKills++;
            _runKills++;
            Check(AchievementCondition.KillsInOneRun, _runKills);
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

        private void Check(AchievementCondition condition, int value)
        {
            AchievementData[] achievements = _catalog.Achievements;
            for (int i = 0; i < achievements.Length; i++)
            {
                AchievementData achievement = achievements[i];
                if (achievement.Condition == condition && value >= achievement.Target && !achievement.IsCompleted)
                {
                    Complete(achievement);
                }
            }
        }

        private static void Complete(AchievementData achievement)
        {
            SaveData data = SaveSystem.Data;
            data.CompleteAchievement(achievement.Id);
            if (achievement.RewardWeapon != null)
            {
                data.UnlockWeapon(achievement.RewardWeapon.Id);
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
