using ExplodeIt.Stage;
using TMPro;
using UnityEngine;

namespace ExplodeIt.UI
{
    public class TimerView : MonoBehaviour
    {
        [SerializeField] private StageStats _stats;
        [SerializeField] private TMP_Text _text;

        private readonly char[] _buffer = new char[5];
        private int _shownSeconds = -1;

        // 초가 바뀔 때만 다시 쓴다.
        private void Update()
        {
            int seconds = (int)_stats.Elapsed;
            if (seconds == _shownSeconds)
            {
                return;
            }

            _shownSeconds = seconds;
            TimeText.Write(_text, _buffer, seconds);
        }
    }
}
