using ExplodeIt.Stage;
using TMPro;
using UnityEngine;

namespace ExplodeIt.UI
{
    // 판이 끝난 순간의 기록을 채운다. 클리어 패널과 사망 패널에 함께 쓴다.
    // 패널은 판이 끝났을 때만 켜지므로, 켜지는 순간이 곧 기록을 채울 때다.
    public class StageResultView : MonoBehaviour
    {
        [SerializeField] private StageStats _stats;
        [SerializeField] private TMP_Text _timeText;
        [SerializeField] private TMP_Text _killsText;
        [SerializeField] private TMP_Text _bestMultiKillText;

        private readonly char[] _buffer = new char[5];

        private void OnEnable()
        {
            TimeText.Write(_timeText, _buffer, (int)_stats.Elapsed);
            _killsText.SetText("{0}", _stats.Kills);
            if (_bestMultiKillText != null)
            {
                _bestMultiKillText.SetText("{0}", _stats.BestMultiKill);
            }
        }
    }
}
