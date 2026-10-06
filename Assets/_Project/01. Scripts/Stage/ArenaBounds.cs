using UnityEngine;

namespace ExplodeIt.Stage
{
    // 플레이 영역(맵) 범위. 적 프리팹은 씬 오브젝트를 참조할 수 없어서 정적 접근을 연다.
    [RequireComponent(typeof(SpriteRenderer))]
    public class ArenaBounds : MonoBehaviour
    {
        private Bounds _bounds;

        public static ArenaBounds Current { get; private set; }

        // 맵은 전투 중 움직이지 않으므로 한 번만 잰다.
        private void Awake()
        {
            _bounds = GetComponent<SpriteRenderer>().bounds;
        }

        private void OnEnable()
        {
            Current = this;
        }

        private void OnDisable()
        {
            if (Current == this)
            {
                Current = null;
            }
        }

        // 보스 구체 반사, 보스 등장 위치 계산에 쓴다.
        public Bounds Bounds => _bounds;

        public bool Contains(Vector2 position)
        {
            return position.x >= _bounds.min.x && position.x <= _bounds.max.x
                && position.y >= _bounds.min.y && position.y <= _bounds.max.y;
        }
    }
}
