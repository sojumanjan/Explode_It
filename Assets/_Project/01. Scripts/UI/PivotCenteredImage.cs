using UnityEngine;
using UnityEngine.UI;

namespace ExplodeIt.UI
{
    // UI 그림은 스프라이트의 기준점(pivot)을 무시하고 사각형 가운데에 맞춰 그린다.
    // 폭탄 그림처럼 심지가 한쪽으로 뻗은 그림은 몸통이 한쪽으로 치우쳐 보이므로, 기준점(몸통 가운데로 잡아 둠)이 칸 가운데에 오도록 그림을 옮긴다.
    // 게임 안 폭탄도 같은 기준점으로 돌고 착지하므로, 기준점 하나로 두 곳을 함께 맞춘다.
    [RequireComponent(typeof(Image))]
    public class PivotCenteredImage : MonoBehaviour
    {
        private Image _image;
        private RectTransform _rect;
        private Vector2 _home;
        private Sprite _appliedSprite;
        private bool _hasApplied;

        private void Awake()
        {
            _image = GetComponent<Image>();
            _rect = (RectTransform)transform;
            _home = _rect.anchoredPosition;
        }

        // 그림이 바뀐 프레임에만 다시 계산한다.
        private void LateUpdate()
        {
            Sprite sprite = _image.sprite;
            if (_hasApplied && sprite == _appliedSprite)
            {
                return;
            }

            _hasApplied = true;
            _appliedSprite = sprite;
            _rect.anchoredPosition = _home + PivotOffset(sprite);
        }

        private Vector2 PivotOffset(Sprite sprite)
        {
            if (sprite == null)
            {
                return Vector2.zero;
            }

            Vector2 spriteSize = sprite.rect.size;
            Vector2 drawn = _rect.rect.size;
            // 비율 유지면 칸 안에 맞춰 줄어든 크기로 그려진다.
            if (_image.preserveAspect && spriteSize.y > 0f)
            {
                float aspect = spriteSize.x / spriteSize.y;
                drawn = drawn.x / drawn.y > aspect ? new Vector2(drawn.y * aspect, drawn.y) : new Vector2(drawn.x, drawn.x / aspect);
            }

            Vector2 pivot = new Vector2(sprite.pivot.x / spriteSize.x, sprite.pivot.y / spriteSize.y);
            return Vector2.Scale(new Vector2(0.5f, 0.5f) - pivot, drawn);
        }
    }
}
