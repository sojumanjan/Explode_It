using ExplodeIt.Core;
using ExplodeIt.Stage;
using TMPro;
using UnityEngine;

namespace ExplodeIt.UI
{
    // 판이 끝난 순간의 기록을 채운다. 클리어 패널과 사망 패널에 함께 쓴다.
    public class StageResultView : MonoBehaviour
    {
        [SerializeField] private GameState _fillOn;
        [SerializeField] private StageStats _stats;
        [SerializeField] private TMP_Text _timeText;
        [SerializeField] private TMP_Text _killsText;
        [SerializeField] private TMP_Text _bestMultiKillText;

        private readonly char[] _buffer = new char[5];

        private void OnEnable()
        {
            GameEvents.GameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.GameStateChanged -= OnGameStateChanged;
        }

        private void OnGameStateChanged(GameState previous, GameState current)
        {
            if (current != _fillOn)
            {
                return;
            }

            TimeText.Write(_timeText, _buffer, (int)_stats.Elapsed);
            _killsText.SetText("{0}", _stats.Kills);
            if (_bestMultiKillText != null)
            {
                _bestMultiKillText.SetText("{0}", _stats.BestMultiKill);
            }
        }
    }
}
