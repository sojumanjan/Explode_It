using ExplodeIt.Bombs;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 드릴 광부가 뚫고 나올 구멍 하나의 예고 표시. 범위 원이 차오르다 다 차는 순간 분출한다.
    // 진짜 구멍(광부가 나오는 곳)에만 표시 하나를 더 켜서, 자세히 보면 구분되게 한다.
    public class DrillHole : MonoBehaviour
    {
        [SerializeField] private ExplosionShape _range;
        [SerializeField] private ExplosionShape _fill;
        // 진짜 구멍에만 켜는 표시. 원 크기·색은 가짜와 같고 이것만 다르다.
        [SerializeField] private GameObject _realMark;

        private float _radius;

        public void Show(Vector2 position, float radius, bool isReal)
        {
            transform.position = position;
            _radius = radius;
            _range.BuildCircle(radius);
            _fill.CopyLimits(_range);
            _fill.SetRadius(0f);
            _range.Visible = true;
            _fill.Visible = true;
            if (_realMark != null)
            {
                _realMark.SetActive(isReal);
            }

            gameObject.SetActive(true);
        }

        // progress: 0(표시 순간) ~ 1(분출 순간).
        public void SetProgress(float progress)
        {
            _fill.SetRadius(_radius * Mathf.Clamp01(progress));
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
