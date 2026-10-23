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

        // 방금 끝난 판의 기록과 그 모드의 최고 기록(이번 판 반영 후). 결과 화면이 켜질 때 읽는다.
        // 결과 화면은 판이 끝난 뒤에 켜지므로 기록을 저장하기 전 값을 따로 들고 있어야 "신기록"을 알 수 있다.
        public bool LastIsStoryMode { get; private set; }
        // 클리어면 클리어 시간과 최단 클리어 시간, 아니면 버틴 시간과 최장 생존 시간.
        public float LastBestTime { get; private set; }
        public bool LastIsNewTime { get; private set; }
        public int LastBestKills { get; private set; }
        public bool LastIsNewKills { get; private set; }

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
            LastIsStoryMode = _isStoryMode;
            // 찐엔딩 판(왕관)은 웨이브 없이 왕과만 싸워 처치·생존 기록이 무한 모드와 견줄 수 없으므로 남기지 않는다.
            if (!_isStoryMode && RunCurses.Has(CurseKind.King))
            {
                SaveSystem.Save();
                return;
            }

            if (_isStoryMode)
            {
                LastIsNewKills = data.TryRecordStoryKills(_stats.Kills);
                if (isCleared)
                {
                    data.StoryCleared = true;
                    LastIsNewTime = data.TryRecordStoryTime(_stats.Elapsed);
                    LastBestTime = data.BestStoryTime;
                }
            }
            else
            {
                // 저주를 몇 개 걸었든 무한 기록은 하나로 겨룬다. 건 저주는 결과 창에 늘어놓아 보여 준다.
                LastIsNewKills = data.TryRecordEndlessKills(_stats.Kills);
            }

            // 클리어한 판은 "버틴 시간"이 아니므로 생존 기록에 넣지 않는다.
            if (!isCleared)
            {
                LastIsNewTime = data.TryRecordSurvival(_isStoryMode, _stats.Elapsed);
                LastBestTime = data.BestSurvival(_isStoryMode);
            }

            LastBestKills = data.BestKills(_isStoryMode);

            SaveSystem.Save();
        }
    }
}
