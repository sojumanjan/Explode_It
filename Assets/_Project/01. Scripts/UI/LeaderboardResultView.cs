// 무한 모드 결과 창에서 최고 기록을 순위표에 자동으로 올리고, 올렸는지(흰 글) 못 올렸는지(빨간 글)만 알린다
using System.Collections;
using ExplodeIt.Core;
using ExplodeIt.Progression;
using ExplodeIt.Stage;
using TMPro;
using UnityEngine;

namespace ExplodeIt.UI
{
    // 순위는 결과 창에 보여 주지 않는다(무한 모드 입장 창의 순위표 패널에서만 본다). 최고 기록을 새로 올렸을 때만 결과를 한 줄로 알린다.
    // 닉네임이 아직 없는데 최고 기록을 세웠으면, 결과 창이 다 나타난 뒤 닉네임 창을 띄우고(해금 창은 그동안 미룸) 정하면 바로 올린다.
    // 처음 켤 때 물으면 무슨 기록인지 몰랐고, 죽자마자 물으면 결과 창보다 먼저 떠서 어색했다.
    // 결과 창이 켜질 때 마지막 판 기록(ProgressRecorder)을 읽는다. 결과 창 기록 칸(StageResultView)과 같은 방식이다.
    public class LeaderboardResultView : MonoBehaviour
    {
        [SerializeField] private ProgressRecorder _recorder;
        [SerializeField] private StageStats _stats;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private NicknameDialog _nicknameDialog;
        // 결과 창은 켜진 뒤 잠깐 기다렸다 서서히 나타난다. 다 나타난 뒤에 닉네임 창을 띄우려고 투명도를 본다.
        [SerializeField] private CanvasGroup _resultGroup;
        // 판 끝 해금 창. 닉네임을 묻는 동안 잡아 두었다가 닉네임 창이 닫히면 띄운다(결과 창 → 닉네임 → 해금). 비우면 잡지 않는다.
        [SerializeField] private UnlockSummaryPanel _unlockSummary;

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
            if (!_recorder.LastIsRecordedEndless || !LeaderboardService.IsAvailable)
            {
                return;
            }

            SaveData save = SaveSystem.Data;
            if (!LeaderboardService.HasName)
            {
                if (_recorder.LastIsNewKills && save.BestEndlessKills > 0 && _nicknameDialog != null)
                {
                    SetSummaryHeld(true);
                    StartCoroutine(AskNameThenSubmit(_showId));
                }

                return;
            }

            if (save.BestEndlessKills > save.LeaderboardSubmittedKills)
            {
                Submit(_showId);
            }
        }

        // 다시하기 등으로 결과 창이 닫히면 잡아 둔 해금 창을 놓아 준다.
        private void OnDisable()
        {
            SetSummaryHeld(false);
        }

        private void SetSummaryHeld(bool isHeld)
        {
            if (_unlockSummary != null)
            {
                _unlockSummary.SetHeld(isHeld);
            }
        }

        private IEnumerator AskNameThenSubmit(int showId)
        {
            while (_resultGroup != null && _resultGroup.alpha < 0.99f)
            {
                yield return null;
            }

            if (showId != _showId)
            {
                yield break;
            }

            _nicknameDialog.ShowFirst(() =>
            {
                SetSummaryHeld(false);
                if (this != null && isActiveAndEnabled && showId == _showId && LeaderboardService.HasName)
                {
                    Submit(showId);
                }
            });
        }

        private async void Submit(int showId)
        {
            // 에디터에서는 설정을 켰을 때만 올린다(개발 중 기록이 섞이지 않게). 닉네임은 그대로 정해 둔다.
            if (LeaderboardService.IsBlockedInEditor)
            {
                return;
            }

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
