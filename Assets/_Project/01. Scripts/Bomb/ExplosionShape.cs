using UnityEngine;

namespace ExplodeIt.Bombs
{
    // 폭발 범위를 구조물에 잘린 모양 그대로 그린다.
    // 판정(적 중심까지 Linecast)과 같은 규칙으로 보여야 플레이어가 결과를 예측할 수 있다.
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class ExplosionShape : MonoBehaviour
    {
        private static readonly RaycastHit2D[] RayBuffer = new RaycastHit2D[4];

        [SerializeField, Min(8)] private int _segments = 64;
        [SerializeField] private Color _color = Color.white;
        [SerializeField] private int _sortingOrder;

        private Mesh _mesh;
        private Vector3[] _vertices;
        private Color32[] _colors;
        private Vector2[] _directions;
        // 방향마다 구조물에 막히기 전까지의 거리. 반경이 커지는 도중에도 이 값을 넘지 않는다.
        private float[] _limits;
        private MeshRenderer _renderer;

        private void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();
            _renderer.sortingOrder = _sortingOrder;

            // 정점 수, 방향, 삼각형 구성은 바뀌지 않으므로 한 번만 만들고, 이후엔 정점 위치와 색만 덮어쓴다.
            _vertices = new Vector3[_segments + 1];
            _colors = new Color32[_segments + 1];
            _directions = new Vector2[_segments];
            _limits = new float[_segments];
            var triangles = new int[_segments * 3];

            float step = Mathf.PI * 2f / _segments;
            for (int i = 0; i < _segments; i++)
            {
                float angle = step * i;
                _directions[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = (i + 1) % _segments + 1;
            }

            _mesh = new Mesh();
            _mesh.MarkDynamic();
            _mesh.vertices = _vertices;
            _mesh.triangles = triangles;
            GetComponent<MeshFilter>().sharedMesh = _mesh;

            SetColor(_color);
        }

        private void OnDestroy()
        {
            Destroy(_mesh);
        }

        public bool Visible
        {
            set => _renderer.enabled = value;
        }

        // 중심에서 반경까지 광선을 부채꼴로 쏴서, 방향마다 막히는 거리를 기록하고 최대 반경으로 그린다.
        public void Build(Vector2 center, float radius, ContactFilter2D obstacleFilter)
        {
            for (int i = 0; i < _segments; i++)
            {
                _limits[i] = ClosestHit(center, _directions[i], radius, obstacleFilter);
            }

            SetRadius(radius);
        }

        // 구조물을 무시하는 범위용. 잘리지 않은 원이라 판정도 벽과 상관없다는 것이 바로 읽힌다.
        public void BuildCircle(float radius)
        {
            for (int i = 0; i < _segments; i++)
            {
                _limits[i] = radius;
            }

            SetRadius(radius);
        }

        // 바깥 모양과 채움 모양은 같은 구조물 조건을 쓰므로 광선을 두 번 쏘지 않는다.
        public void CopyLimits(ExplosionShape source)
        {
            System.Array.Copy(source._limits, _limits, _limits.Length);
        }

        // 원이 반경 radius로 퍼진 상태를 그린다. 벽에 닿은 방향만 그 자리에서 멈춘다.
        public void SetRadius(float radius)
        {
            _vertices[0] = Vector3.zero;
            for (int i = 0; i < _segments; i++)
            {
                _vertices[i + 1] = _directions[i] * Mathf.Min(radius, _limits[i]);
            }

            _mesh.vertices = _vertices;
            _mesh.RecalculateBounds();
        }

        public void SetColor(Color color)
        {
            Color32 color32 = color;
            for (int i = 0; i < _colors.Length; i++)
            {
                _colors[i] = color32;
            }

            _mesh.colors32 = _colors;
        }

        public void ResetColor()
        {
            SetColor(_color);
        }

        // 폭발 후 사라지는 연출용. 기본 색의 알파에 비율을 곱한다.
        public void SetOpacity(float opacity)
        {
            Color color = _color;
            color.a *= opacity;
            SetColor(color);
        }

        private static float ClosestHit(Vector2 origin, Vector2 direction, float maxDistance, ContactFilter2D filter)
        {
            int count = Physics2D.Raycast(origin, direction, filter, RayBuffer, maxDistance);
            float closest = maxDistance;
            for (int i = 0; i < count; i++)
            {
                if (RayBuffer[i].distance < closest)
                {
                    closest = RayBuffer[i].distance;
                }
            }

            return closest;
        }
    }
}
