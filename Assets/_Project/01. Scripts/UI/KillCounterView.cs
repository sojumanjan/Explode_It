using ExplodeIt.Stage;
using TMPro;
using UnityEngine;

namespace ExplodeIt.UI
{
    public class KillCounterView : MonoBehaviour
    {
        [SerializeField] private StageStats _stats;
        [SerializeField] private TMP_Text _text;

        private void OnEnable()
        {
            _stats.KillsChanged += OnKillsChanged;
        }

        private void OnDisable()
        {
            _stats.KillsChanged -= OnKillsChanged;
        }

        private void Start()
        {
            OnKillsChanged(_stats.Kills);
        }

        // SetText의 숫자 인자 버전은 문자열을 새로 만들지 않는다.
        private void OnKillsChanged(int kills)
        {
            _text.SetText("{0}", kills);
        }
    }
}
