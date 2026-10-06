using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 정면 사각형 공격의 예고. 전체 범위를 옅게 깔고, 안쪽 채움이 몸에서 앞으로 차오르다 다 차는 순간 공격한다.
    // 보이는 사각형이 곧 판정 사각형이므로, 공격하는 쪽과 같은 길이·폭을 넘긴다.
    public class RectTelegraph : MonoBehaviour
    {
        // 중심 기준점의 사각형 스프라이트. 크기는 스프라이트 원래 크기로 나눠 맞추므로 어떤 정사각형을 써도 된다.
        [SerializeField] private SpriteRenderer _area;
        [SerializeField] private SpriteRenderer _fill;

        private void Awake()
        {
            Hide();
        }

        // progress: 0(시작) ~ 1(공격 순간).
        public void Show(Vector2 origin, Vector2 forward, float length, float width, float progress)
        {
            float angle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
            transform.SetPositionAndRotation(origin, Quaternion.Euler(0f, 0f, angle));

            Place(_area, length, width);
            Place(_fill, length * Mathf.Clamp01(progress), width);
            _area.enabled = true;
            _fill.enabled = true;
        }

        public void Hide()
        {
            _area.enabled = false;
            _fill.enabled = false;
        }

        // 몸 쪽 끝을 원점에 붙이고 정면(로컬 +x)으로 length만큼 뻗는다.
        private static void Place(SpriteRenderer renderer, float length, float width)
        {
            Vector2 size = renderer.sprite.bounds.size;
            Transform t = renderer.transform;
            t.localPosition = new Vector3(length * 0.5f, 0f, 0f);
            t.localScale = new Vector3(length / size.x, width / size.y, 1f);
        }
    }
}
