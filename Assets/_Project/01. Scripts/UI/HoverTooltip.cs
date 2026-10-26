using UnityEngine;
using UnityEngine.EventSystems;

namespace ExplodeIt.UI
{
    // 마우스를 올린 동안 설명 창을 띄운다. 설정 체크박스처럼 이름만으로 뜻이 애매한 항목에 붙인다.
    // 이름 글자와 체크박스 양쪽에 붙여 두면 어느 쪽에 올려도 같은 설명이 뜬다.
    public class HoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TooltipPanel _panel;
        [Tooltip("창을 이 칸 바로 아래에 붙인다. 비우면 자기 자신 아래")]
        [SerializeField] private RectTransform _anchor;
        [SerializeField, TextArea(2, 5)] private string _text;

        public void OnPointerEnter(PointerEventData eventData)
        {
            _panel.Show(_text, _anchor != null ? _anchor : (RectTransform)transform);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _panel.Hide();
        }

        // 페이지가 닫힐 때 창이 남지 않게 한다.
        private void OnDisable()
        {
            if (_panel != null)
            {
                _panel.Hide();
            }
        }
    }
}
