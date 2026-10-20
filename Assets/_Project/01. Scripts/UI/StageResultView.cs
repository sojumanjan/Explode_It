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

        private readonly char[] _buffer = new char[5];

        private void OnEnable()
        {
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
