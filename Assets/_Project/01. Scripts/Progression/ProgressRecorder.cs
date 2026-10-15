using ExplodeIt.Core;
using ExplodeIt.Stage;
using UnityEngine;

namespace ExplodeIt.Progression
{
    // 판이 끝날 때 이번 판 기록을 저장에 옮긴다. 판 도중에는 저장하지 않는다(웹은 저장마다 디스크에 써서 처치마다 쓰면 끊긴다).
    // 끝나는 경우: 스토리 클리어, 사망, 판 도중 재시작·나가기(씬이 내려가며 꺼질 때). 어느 쪽이든 한 판에 한 번만 기록한다.
    public class ProgressRecorder : MonoBehaviour
    {
        // 이번 판 처치 수·시간을 센다. 같은 값을 따로 세지 않고 HUD와 같은 기록을 읽는다.
        [SerializeField] private StageStats _stats;

        private bool _isRunning;
        private bool _isStoryMode;

        private void OnEnable()
        {
            GameEvents.StageStarted += OnStageStarted;
            GameEvents.StageCleared += OnStageCleared;
            GameEvents.PlayerDied += OnPlayerDied;
        }

        private void OnDisable()
        {
            GameEvents.StageStarted -= OnStageStarted;
            GameEvents.StageCleared -= OnStageCleared;
            GameEvents.PlayerDied -= OnPlayerDied;
            // 재시작·타이틀로 나가기도 처치 수는 누적에 남긴다.
            EndRun(false);
        }

        private void OnStageStarted(bool isStoryMode)
        {
            _isRunning = true;
            _isStoryMode = isStoryMode;
        }

        private void OnStageCleared()
        {
            EndRun(true);
        }

        private void OnPlayerDied()
        {
            EndRun(false);
        }

        private void EndRun(bool isCleared)
        {
            if (!_isRunning)
            {
                return;
            }

            _isRunning = false;
            SaveData data = SaveSystem.Data;
            data.TotalKills += _stats.Kills;
            if (_isStoryMode)
            {
                if (isCleared)
                {
                    data.StoryCleared = true;
                    data.TryRecordStoryTime(_stats.Elapsed);
                }
            }
            else
            {
                data.TryRecordEndlessKills(_stats.Kills);
            }

            SaveSystem.Save();
        }
    }
}
