using UnityEngine;

namespace ExplodeIt.Stage
{
    // 씬에 보이는 스프라이트 사각형이 곧 스폰 범위다. 구역 조정은 스프라이트를 옮기고 늘리는 것으로 끝난다.
    // 스테이지 진행과 흐름장이 씬에서 자동으로 찾으므로, 구역을 늘릴 때 따로 연결하지 않아도 된다.
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpawnArea : MonoBehaviour
    {
        // SO는 씬 오브젝트를 직접 가리킬 수 없으므로, 군집 데이터는 이 번호로 구역을 고른다.
        // 같은 번호를 여러 구역에 주면 그중 하나를 무작위로 고른다.
        [SerializeField, Min(0)] private int _id;

        private Bounds _bounds;
        private bool _hasBounds;

        public int Id => _id;

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

#if UNITY_EDITOR
        // 군집 데이터에 적을 번호를 씬 뷰에서 바로 읽을 수 있게 한다.
        private void OnDrawGizmos()
        {
            UnityEditor.Handles.Label(transform.position, $"Area {_id}");
        }
#endif
    }
}
