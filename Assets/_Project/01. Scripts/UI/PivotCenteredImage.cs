using UnityEngine;
using UnityEngine.UI;

namespace ExplodeIt.UI
{
    // UI 그림은 스프라이트의 기준점(pivot)을 무시하고 사각형 가운데에 맞춰 그린다.
    // 폭탄 그림처럼 심지가 한쪽으로 뻗은 그림은 몸통이 한쪽으로 치우쳐 보이므로, 기준점(몸통 가운데로 잡아 둠)이 칸 가운데에 오도록 그림을 옮긴다.
    // 게임 안 폭탄도 같은 기준점으로 돌고 착지하므로, 기준점 하나로 두 곳을 함께 맞춘다.
    // 바닥 맞춤을 켜면 세로는 그림의 실제 바닥(투명한 여백을 뺀 맨 아래)을 칸 바닥에 붙인다.
    // 크기가 다른 폭탄들이 같은 바닥선 위에 놓여 보이게 하려는 것이다. 바닥은 스프라이트 외곽선(Tight 메시)에서 읽는다.
    [RequireComponent(typeof(Image))]
    public class PivotCenteredImage : MonoBehaviour
    {
        [Tooltip("켜면 세로는 그림의 실제 바닥을 칸 바닥에 맞춘다. 끄면 기준점을 칸 가운데에 맞춘다")]
        [SerializeField] private bool _alignBottom;

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
            Vector2 offset = Vector2.Scale(new Vector2(0.5f, 0.5f) - pivot, drawn);
            if (_alignBottom)
            {
                // 그림 아래 끝에서 실제 바닥까지의 비율만큼 내려 바닥을 칸 아래 끝에 붙인다.
                offset.y = -(_rect.rect.height - drawn.y) * 0.5f - ContentBottom(sprite) * drawn.y;
            }

            return offset;
        }

        // 그림 사각형 아래 끝에서 불투명한 부분 맨 아래까지의 비율 (0~1).
        private static float ContentBottom(Sprite sprite)
        {
            Vector2[] vertices = sprite.vertices;
            if (vertices.Length == 0)
            {
                return 0f;
            }

            float minY = vertices[0].y;
            for (int i = 1; i < vertices.Length; i++)
            {
                minY = Mathf.Min(minY, vertices[i].y);
            }

            // 꼭짓점은 기준점 기준 유닛이라 픽셀로 바꿔 그림 아래 끝 기준으로 옮긴다.
            float bottomPixels = sprite.pivot.y + minY * sprite.pixelsPerUnit;
            return Mathf.Clamp01(bottomPixels / sprite.rect.height);
        }
    }
}
