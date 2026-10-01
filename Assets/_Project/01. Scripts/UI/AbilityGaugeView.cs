using ExplodeIt.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ExplodeIt.UI
{
    // 능력 시스템을 직접 참조하지 않고 충전 이벤트만 받아 그린다.
    public class AbilityGaugeView : MonoBehaviour
    {
        // Image Type을 Filled로 둔 이미지. fillAmount로 충전량을 보여준다.
        [SerializeField] private Image _fill;
        [SerializeField] private Color _chargingColor = new Color(0.55f, 0.45f, 0.9f, 1f);
        // 다 찼다는 것을 전투 중 곁눈질로도 알 수 있게 색을 확 바꾼다.
        [SerializeField] private Color _readyColor = new Color(1f, 0.85f, 0.3f, 1f);

        private void OnEnable()
        {
            GameEvents.AbilityChargeChanged += OnChargeChanged;
        }

        private void OnDisable()
        {
            GameEvents.AbilityChargeChanged -= OnChargeChanged;
        }

        private void OnChargeChanged(int current, int required)
        {
            _fill.fillAmount = (float)current / required;
            _fill.color = current >= required ? _readyColor : _chargingColor;
        }
    }
}
