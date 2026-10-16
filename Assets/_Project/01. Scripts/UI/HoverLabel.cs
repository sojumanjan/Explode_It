using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ExplodeIt.UI
{
    // 마우스를 올린 동안만 버튼 글자를 바꾼다. "장착 중" 버튼에 올리면 "장착 해제"로 보여, 누르면 무엇이 되는지 미리 알린다.
    // 바꿀 글자가 비어 있으면 그대로 둔다.
    public class HoverLabel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TMP_Text _label;

        private string _normalText = "";
        private string _hoverText = "";
        private bool _isHovered;

        // 평소 글자와 올렸을 때 글자를 정한다. hoverText가 비어 있으면 올려도 바뀌지 않는다.
        public void SetTexts(string normalText, string hoverText)
        {
            _normalText = normalText ?? "";
            _hoverText = hoverText ?? "";
            Apply();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            Apply();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            Apply();
        }

        // 화면이 꺼질 때 올린 상태가 남지 않게 한다.
        private void OnDisable()
        {
            _isHovered = false;
            Apply();
        }

        private void Apply()
        {
            _label.text = _isHovered && _hoverText.Length > 0 ? _hoverText : _normalText;
        }
    }
}
