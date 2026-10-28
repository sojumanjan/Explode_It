using ExplodeIt.Core;
using TMPro;
using UnityEngine;

namespace ExplodeIt.UI
{
    // 기록 창의 한 줄: 아이콘 · 항목 이름 · 값 · (달성했으면) 도장. 아이콘은 씬에서 줄마다 정해 둔다.
    public class RecordRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;
        [SerializeField] private TMP_Text _value;
        // 비워 두면 도장을 찍지 않는 줄이다.
        [SerializeField] private GameObject _stamp;

        public void Set(string label, string value, bool isStamped)
        {
            gameObject.SetActive(true);
            _label.text = L10n.Tr(label);
            _value.text = value;
            if (_stamp != null)
            {
                _stamp.SetActive(isStamped);
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
