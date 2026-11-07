// 무한 모드 결과 창에서 최고 기록을 순위표에 자동으로 올리고, 올렸는지(흰 글) 못 올렸는지(빨간 글)만 알린다
using ExplodeIt.Core;
using ExplodeIt.Progression;
using ExplodeIt.Stage;
using TMPro;
using UnityEngine;

namespace ExplodeIt.UI
{
    // 순위는 결과 창에 보여 주지 않는다(무한 모드 입장 창의 순위표 패널에서만 본다). 최고 기록을 새로 올렸을 때만 결과를 한 줄로 알린다.
    // 결과 창이 켜질 때 마지막 판 기록(ProgressRecorder)을 읽는다. 결과 창 기록 칸(StageResultView)과 같은 방식이다.
    public class LeaderboardResultView : MonoBehaviour
    {
        [SerializeField] private ProgressRecorder _recorder;
        [SerializeField] private StageStats _stats;
        [SerializeField] private TMP_Text _label;

        [Header("문구 (한국어 원문, 번역 표의 키)")]
        [SerializeField] private string _failedText = "네트워크가 연결되지 않아 순위에 기록되지 않았습니다.";
        [SerializeField] private string _successText = "순위표에 기록되었습니다.";
        [SerializeField] private Color _failedColor = new Color(0.95f, 0.35f, 0.3f, 1f);
        [SerializeField] private Color _successColor = Color.white;

        // 결과 창이 다시 켜지기 전에 응답이 늦게 오면 지난 판 결과로 덮어쓰지 않게 켤 때마다 번호를 바꾼다.
        private int _showId;

        private void OnEnable()
        {
            _showId++;
            _label.text = "";
            // 스토리, 찐엔딩 판, 판이 끝나기 전에 켜진 경우는 순위와 관계없다.
            if (!_recorder.LastIsRecordedEndless || !LeaderboardService.IsAvailable || !LeaderboardService.HasName
                || LeaderboardService.IsBlockedInEditor)
            {
                return;
            }

            SaveData save = SaveSystem.Data;
            if (save.BestEndlessKills > save.LeaderboardSubmittedKills)
            {
                Submit(_showId);
            }
        }

        private async void Submit(int showId)
        {
            SaveData save = SaveSystem.Data;
            // 이번 판이 최고 기록이면 이번 판의 시간과 저주를 곁들이고, 지난 판에 못 올린 기록이면 최장 생존 시간으로 대신하고 저주는 비운다.
            bool isThisRun = _recorder.LastIsNewKills;
            float seconds = isThisRun ? _stats.Elapsed : save.BestSurvival(false);
            string curses = isThisRun ? string.Join(",", save.EquippedCurses) : "";
            int result = await LeaderboardService.SubmitAsync(save.BestEndlessKills, seconds, curses);
            if (this == null || showId != _showId || !isActiveAndEnabled)
            {
                return;
            }

            if (result == LeaderboardService.NetworkFailed)
            {
                _label.color = _failedColor;
                _label.text = L10n.Tr(_failedText);
            }
            else if (result > 0)
            {
                _label.color = _successColor;
                _label.text = L10n.Tr(_successText);
            }
        }
    }
}
