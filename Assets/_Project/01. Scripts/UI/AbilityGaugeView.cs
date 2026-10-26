using ExplodeIt.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ExplodeIt.UI
{
    // 능력 시스템을 직접 참조하지 않고 충전 이벤트만 받아 그린다.
    // 칸마다 게이지를 하나씩 두고, 앞 칸부터 차례로 채운다.
    public class AbilityGaugeView : MonoBehaviour
    {
        // Image Type을 Filled로 둔 이미지들. 1번 칸부터 순서대로 넣는다. fillAmount로 각 칸의 충전량을 보여준다.
        // 칸 수가 늘어나는 경우(아티팩트 벨트)까지 넉넉히 넣어 두면, 쓰지 않는 칸은 숨긴다.
        [SerializeField] private Image[] _fills;
        [SerializeField] private Color _chargingColor = new Color(0.55f, 0.45f, 0.9f, 1f);
        // 다 찼다는 것을 전투 중 곁눈질로도 알 수 있게 색을 확 바꾼다.
        [SerializeField] private Color _readyColor = new Color(1f, 0.85f, 0.3f, 1f);

        // 처음 배치(1·2번 칸)의 왼쪽 끝·오른쪽 끝과 칸 사이 간격. 칸 수가 바뀌면 이 폭을 나눠 다시 놓는다.
        private float _left;
        private float _right;
        private float _gap;
        private int _laidOutCount;

        // 오프닝 동안 HUD를 꺼 두므로, 꺼져 있는 사이 오는 판 시작 알림(처음 값)을 놓치지 않게 켜고 끄는 것과 상관없이 듣는다.
        private void Awake()
        {
            GameEvents.AbilityChargeChanged += OnChargeChanged;
            RectTransform first = SlotRect(0);
            RectTransform last = SlotRect(Mathf.Min(1, _fills.Length - 1));
            _left = first.anchoredPosition.x - HalfWidth(first);
            _right = last.anchoredPosition.x + HalfWidth(last);
            _gap = _fills.Length > 1 ? (last.anchoredPosition.x - HalfWidth(last)) - (first.anchoredPosition.x + HalfWidth(first)) : 0f;
            _laidOutCount = Mathf.Min(2, _fills.Length);
        }

        private RectTransform SlotRect(int index)
        {
            return (RectTransform)_fills[index].transform.parent;
        }

        private static float HalfWidth(RectTransform rect)
        {
            return rect.sizeDelta.x * rect.localScale.x * 0.5f;
        }

        // 보이는 칸 수에 맞춰 원래 두 칸이 차지하던 폭을 똑같이 나눈다. 칸 수가 바뀔 때만 한다.
        private void LayOut(int count)
        {
            _laidOutCount = count;
            float width = (_right - _left - _gap * (count - 1)) / count;
            for (int i = 0; i < count; i++)
            {
                RectTransform slot = SlotRect(i);
                Vector3 scale = slot.localScale;
                scale.x = width / slot.sizeDelta.x;
                slot.localScale = scale;
                slot.anchoredPosition = new Vector2(_left + width * 0.5f + i * (width + _gap), slot.anchoredPosition.y);
            }
        }

        private void OnDestroy()
        {
            GameEvents.AbilityChargeChanged -= OnChargeChanged;
        }

        // 칸 틀(채움 이미지의 부모)째 숨긴다. 부모가 이 컴포넌트 자신이면 채움만 숨긴다.
        private void SetSlotVisible(int index, bool isVisible)
        {
            Transform fill = _fills[index].transform;
            GameObject slot = fill.parent != null && fill.parent != transform ? fill.parent.gameObject : fill.gameObject;
            if (slot.activeSelf != isVisible)
            {
                slot.SetActive(isVisible);
            }
        }

        private void OnChargeChanged(int current, int perCharge, int maxCharges)
        {
            int visible = Mathf.Min(maxCharges, _fills.Length);
            if (visible != _laidOutCount && visible > 0)
            {
                LayOut(visible);
            }

            for (int i = 0; i < _fills.Length; i++)
            {
                SetSlotVisible(i, i < maxCharges);

                float amount = Mathf.Clamp01((float)(current - i * perCharge) / perCharge);
                _fills[i].fillAmount = amount;
                _fills[i].color = amount >= 1f ? _readyColor : _chargingColor;
            }
        }
    }
}
