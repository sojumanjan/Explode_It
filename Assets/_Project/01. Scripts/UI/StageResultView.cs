using ExplodeIt.Core;
using ExplodeIt.Progression;
using ExplodeIt.Stage;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExplodeIt.UI
{
    // 판이 끝난 순간의 기록을 채운다. 클리어 패널과 사망 패널에 함께 쓴다.
    // 패널은 판이 끝났을 때만 켜지므로, 켜지는 순간이 곧 기록을 채울 때다.
    public class StageResultView : MonoBehaviour
    {
        [SerializeField] private StageStats _stats;
        [SerializeField] private ProgressRecorder _recorder;

        [Header("이번 판 / 최고 기록")]
        [SerializeField] private TMP_Text _timeText;
        [SerializeField] private TMP_Text _bestTimeText;
        [SerializeField] private GameObject _newTimeMark;
        [SerializeField] private TMP_Text _killsText;
        [SerializeField] private TMP_Text _bestKillsText;
        [SerializeField] private GameObject _newKillsMark;
        [SerializeField] private TMP_Text _accuracyText;

        [Header("카드 (사망: 나를 쓰러뜨린 적, 클리어: 고정 그림)")]
        [SerializeField] private Image _portrait;
        [SerializeField] private TMP_Text _caption;
        [Tooltip("켜면 쓰러뜨린 적의 그림·이름을 보여 준다(사망 패널). 끄면 아래 기본 그림과 씬에 적힌 문구를 그대로 둔다(클리어 패널)")]
        [SerializeField] private bool _showsKiller = true;
        [Tooltip("누구에게 당했는지 모를 때나 클리어 패널에 보이는 그림")]
        [SerializeField] private Sprite _fallbackPortrait;
        [Tooltip("적 이름 뒤에 붙는 문구. {0}에 이름이 들어간다")]
        [SerializeField] private string _killerCaptionFormat = "{0}에게 당함";
        [Tooltip("누구에게 당했는지 모를 때의 문구")]
        [SerializeField] private string _unknownKillerCaption = "쓰러짐";

        [Header("이번 판에 건 저주 (무한 모드)")]
        // 어떤 저주를 짊어지고 이 기록을 냈는지 한눈에 보여 줘, 결과 화면을 찍어 자랑하게 한다. 비워 두면 보이지 않는다.
        [SerializeField] private UnlockCatalog _catalog;
        [SerializeField] private GameObject _curseRow;
        // 저주 칸 수만큼 미리 놓아 둔다. 남는 칸은 숨긴다.
        [SerializeField] private Image[] _curseIcons;

        private readonly char[] _buffer = new char[5];

        private void OnEnable()
        {
            // 결과 창을 처음 본 순간부터 첫 판이 끝난 것으로 친다. 이때부터 타이틀에서 책(메인 메뉴)으로 들어간다.
            if (!SaveSystem.Data.FirstRunDone)
            {
                SaveSystem.Data.FirstRunDone = true;
                SaveSystem.Save();
            }

            TimeText.Write(_timeText, _buffer, (int)_stats.Elapsed);
            _killsText.SetText("{0}", _stats.Kills);
            _accuracyText.SetText("{0:1}%", _stats.Accuracy * 100f);

            if (_recorder.LastBestTime > 0f)
            {
                TimeText.Write(_bestTimeText, _buffer, (int)_recorder.LastBestTime);
            }
            else
            {
                _bestTimeText.SetText("-");
            }

            _bestKillsText.SetText("{0}", _recorder.LastBestKills);
            _newTimeMark.SetActive(_recorder.LastIsNewTime);
            _newKillsMark.SetActive(_recorder.LastIsNewKills);
            ShowCard();
            ShowCurses();
        }

        // 저주 고르기 페이지 순서대로, 이번 판에 걸린 저주만 앞에서부터 채운다. 하나도 없으면(스토리 포함) 줄째 숨긴다.
        private void ShowCurses()
        {
            if (_curseRow == null)
            {
                return;
            }

            CurseData[] curses = _catalog.Curses;
            int shown = 0;
            for (int i = 0; i < curses.Length && shown < _curseIcons.Length; i++)
            {
                if (!RunCurses.Has(curses[i].Kind))
                {
                    continue;
                }

                _curseIcons[shown].sprite = curses[i].Icon;
                _curseIcons[shown].gameObject.SetActive(true);
                shown++;
            }

            for (int i = shown; i < _curseIcons.Length; i++)
            {
                _curseIcons[i].gameObject.SetActive(false);
            }

            _curseRow.SetActive(shown > 0);
        }

        private void ShowCard()
        {
            IKillerProfile killer = _showsKiller ? _stats.Killer : null;
            Sprite portrait = killer != null && killer.KillerPortrait != null ? killer.KillerPortrait : _fallbackPortrait;
            _portrait.sprite = portrait;
            _portrait.enabled = portrait != null;
            _portrait.preserveAspect = true;

            if (!_showsKiller)
            {
                return;
            }

            bool hasName = killer != null && !string.IsNullOrEmpty(killer.KillerName);
            _caption.text = hasName ? string.Format(_killerCaptionFormat, killer.KillerName) : _unknownKillerCaption;
        }
    }
}
