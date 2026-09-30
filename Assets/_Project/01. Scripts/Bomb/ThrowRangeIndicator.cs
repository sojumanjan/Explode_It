using UnityEngine;

namespace ExplodeIt.Bombs
{
    // 최대 사거리를 플레이어 둘레에 원으로 그린다. 플레이어의 자식으로 두면 따라다닌다.
    [RequireComponent(typeof(LineRenderer))]
    public class ThrowRangeIndicator : MonoBehaviour
    {
        [SerializeField, Min(8)] private int _segments = 64;

        private LineRenderer _line;

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.useWorldSpace = false;
            _line.loop = true;
        }

        // 사거리가 바뀔 때만 호출한다. 매 프레임 다시 그리지 않는다.
        public void SetRadius(float radius)
        {
            _line.positionCount = _segments;
            float step = Mathf.PI * 2f / _segments;
            for (int i = 0; i < _segments; i++)
            {
                float angle = step * i;
                _line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }
        }
    }
}
