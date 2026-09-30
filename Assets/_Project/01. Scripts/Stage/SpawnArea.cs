using UnityEngine;

namespace ExplodeIt.Stage
{
    // 씬에 보이는 스프라이트 사각형이 곧 스폰 범위다. 구역 조정은 스프라이트를 옮기고 늘리는 것으로 끝난다.
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpawnArea : MonoBehaviour
    {
        private Bounds _bounds;

        // 구역은 전투 중 움직이지 않으므로 한 번만 잰다.
        private void Awake()
        {
            _bounds = GetComponent<SpriteRenderer>().bounds;
        }

        public Vector2 GetRandomPoint()
        {
            return new Vector2(
                Random.Range(_bounds.min.x, _bounds.max.x),
                Random.Range(_bounds.min.y, _bounds.max.y));
        }
    }
}
