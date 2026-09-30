using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 돌진 경로, 조준선 등 직선형 예고를 공통으로 그린다.
    [RequireComponent(typeof(LineRenderer))]
    public class TelegraphLine : MonoBehaviour
    {
        private LineRenderer _line;

        // 자식 오브젝트라 부모 적의 OnEnable이 이 Awake보다 먼저 불릴 수 있어 지연 초기화한다.
        private LineRenderer Line
        {
            get
            {
                if (_line == null)
                {
                    _line = GetComponent<LineRenderer>();
                    _line.useWorldSpace = true;
                    _line.positionCount = 2;
                }

                return _line;
            }
        }

        public void Show(Vector2 start, Vector2 direction, float length)
        {
            Line.SetPosition(0, start);
            Line.SetPosition(1, start + direction * length);
            Line.enabled = true;
        }

        public void Hide()
        {
            Line.enabled = false;
        }
    }
}
