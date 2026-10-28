using ExplodeIt.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExplodeIt.UI
{
    // 마우스를 올린 항목 바로 아래에 뜨는 설명 창. 여러 항목이 한 창을 같이 쓴다.
    // 창이 마우스를 가로채면 항목에서 벗어난 것으로 처리돼 깜빡이므로, 창 안의 모든 그림은 레이캐스트를 끈다.
    // 씬에서는 꺼 둔다(Awake에서 끄면 처음 Show로 켠 직후 다시 꺼진다).
    public class TooltipPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        [Tooltip("항목 아래 가장자리와 창 사이 간격 (UI 단위)")]
        [SerializeField, Min(0f)] private float _gap = 8f;

        public void Show(string text, RectTransform anchor)
        {
            var panel = (RectTransform)transform;
            _text.text = L10n.Tr(text);
            gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);

            // 항목 왼쪽 아래 모서리에 창 왼쪽 위 모서리를 붙인다.
            var parent = (RectTransform)panel.parent;
            Vector3 corner = parent.InverseTransformPoint(anchor.TransformPoint(new Vector3(anchor.rect.xMin, anchor.rect.yMin, 0f)));
            panel.pivot = new Vector2(0f, 1f);
            panel.localPosition = new Vector3(corner.x, corner.y - _gap, 0f);
            panel.SetAsLastSibling();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
