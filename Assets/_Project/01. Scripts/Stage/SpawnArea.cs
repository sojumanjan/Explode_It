using UnityEngine;

namespace ExplodeIt.Stage
{
    // 씬에 보이는 스프라이트 사각형이 곧 스폰 범위다. 구역 조정은 스프라이트를 옮기고 늘리는 것으로 끝난다.
    // 웨이브와 흐름장이 씬에서 자동으로 찾으므로, 구역을 늘릴 때 따로 연결하지 않아도 된다.
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpawnArea : MonoBehaviour
    {
        private Bounds _bounds;
        private bool _hasBounds;

        // 다른 컴포넌트의 Awake에서도 읽히므로 이 컴포넌트의 Awake 순서에 기대지 않고 처음 읽을 때 잰다.
        // 구역은 전투 중 움직이지 않으므로 한 번만 잰다.
        public Bounds Bounds
        {
            get
            {
                if (!_hasBounds)
                {
                    _bounds = GetComponent<SpriteRenderer>().bounds;
                    _hasBounds = true;
                }

                return _bounds;
            }
        }

        public Vector2 GetRandomPoint()
        {
            Bounds bounds = Bounds;
            return new Vector2(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y));
        }
    }
}
