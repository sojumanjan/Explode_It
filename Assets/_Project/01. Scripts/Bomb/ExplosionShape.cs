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

        // 적·저주 공격 범위는 테두리를 둘러 플레이어 폭탄 범위와 한눈에 구분되게 한다. 0이면 테두리 없음(플레이어 폭탄).
        [Tooltip("테두리 굵기 (유닛). 0이면 테두리를 그리지 않는다. 적·저주 공격 범위에만 켠다")]
        [SerializeField, Min(0f)] private float _outlineWidth;
        [Tooltip("테두리 색 = 채움 색에 이 배율을 곱한 색. 작을수록 진하고 어둡다 (하늘색 채움 → 진한 파랑 테두리)")]
        [SerializeField, Range(0f, 1f)] private float _outlineDarken = 0.5f;
        [Tooltip("테두리 투명도 = 채움 투명도에 이 배율을 곱한 값. 채움이 옅어도 테두리는 또렷하게 보인다")]
        [SerializeField, Min(1f)] private float _outlineAlphaRatio = 4f;

        private Mesh _mesh;
        private Vector3[] _vertices;
        private Color32[] _colors;
        private Vector2[] _directions;
        // 방향마다 구조물에 막히기 전까지의 거리. 반경이 커지는 도중에도 이 값을 넘지 않는다.
        private float[] _limits;
        private MeshRenderer _renderer;
        private Material _material;
        private bool _isInitialized;
        private bool _hasOutline;

        private void Awake()
        {
            EnsureInitialized();
        }

        // 적의 자식으로 붙으면, 풀에서 처음 만들어질 때 부모의 OnEnable이 이 Awake보다 먼저 불려 여기를 건드린다.
        // 그래서 Awake를 기다리지 않고 처음 쓰이는 순간 스스로 준비한다.
        private void EnsureInitialized()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;
            _renderer = GetComponent<MeshRenderer>();
            _renderer.sortingOrder = _sortingOrder;
            // 같은 재질(Sprite-Unlit-Default)을 쓰는 다른 그림(던질 거리 원 등)과 SRP 배처에 함께 묶이면
            // 화면 속 위치에 따라 이 메쉬가 통째로 빠지는 일이 있었다. 모양마다 재질 사본을 써서 묶이지 않게 한다.
            _material = new Material(_renderer.sharedMaterial);
            _renderer.sharedMaterial = _material;

            // 정점 수, 방향, 삼각형 구성은 바뀌지 않으므로 한 번만 만들고, 이후엔 정점 위치와 색만 덮어쓴다.
            // 테두리가 있으면 채움 뒤에 바깥 고리·안쪽 고리 정점을 붙인다. 같은 메쉬 안에서 뒤에 그려 채움 위에 얹힌다.
            _hasOutline = _outlineWidth > 0f;
            int vertexCount = _segments + 1 + (_hasOutline ? _segments * 2 : 0);
            _vertices = new Vector3[vertexCount];
            _colors = new Color32[vertexCount];
            _directions = new Vector2[_segments];
            _limits = new float[_segments];
            var triangles = new int[_segments * 3 + (_hasOutline ? _segments * 6 : 0)];

            float step = Mathf.PI * 2f / _segments;
            for (int i = 0; i < _segments; i++)
            {
                float angle = step * i;
                _directions[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = (i + 1) % _segments + 1;
                if (_hasOutline)
                {
                    int next = (i + 1) % _segments;
                    int outer = OuterIndex(i);
                    int outerNext = OuterIndex(next);
                    int inner = InnerIndex(i);
                    int innerNext = InnerIndex(next);
                    int t = _segments * 3 + i * 6;
                    triangles[t] = inner;
                    triangles[t + 1] = outer;
                    triangles[t + 2] = outerNext;
                    triangles[t + 3] = inner;
                    triangles[t + 4] = outerNext;
                    triangles[t + 5] = innerNext;
                }
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
            Destroy(_material);
        }

        public bool Visible
        {
            set
            {
                EnsureInitialized();
                _renderer.enabled = value;
            }
        }

        // 중심에서 반경까지 광선을 부채꼴로 쏴서, 방향마다 막히는 거리를 기록하고 최대 반경으로 그린다.
        public void Build(Vector2 center, float radius, ContactFilter2D obstacleFilter)
        {
            EnsureInitialized();
            for (int i = 0; i < _segments; i++)
            {
                _limits[i] = ClosestHit(center, _directions[i], radius, obstacleFilter);
            }

            SetRadius(radius);
        }

        // 구조물을 무시하는 범위용. 잘리지 않은 원이라 판정도 벽과 상관없다는 것이 바로 읽힌다.
        public void BuildCircle(float radius)
        {
            EnsureInitialized();
            for (int i = 0; i < _segments; i++)
            {
                _limits[i] = radius;
            }

            SetRadius(radius);
        }

        // 정면 기준 각도 안의 방향만 반경까지 그리고 나머지는 접어 부채꼴을 만든다. 근접 공격 표시에 쓴다.
        // 정점 방향은 월드 기준이므로 이 오브젝트는 회전시키지 않는다.
        public void BuildSector(Vector2 forward, float radius, float angle)
        {
            EnsureInitialized();
            float cosHalf = Mathf.Cos(angle * 0.5f * Mathf.Deg2Rad);
            for (int i = 0; i < _segments; i++)
            {
                _limits[i] = Vector2.Dot(_directions[i], forward) >= cosHalf ? radius : 0f;
            }

            SetRadius(radius);
        }

        // 바깥 모양과 채움 모양은 같은 구조물 조건을 쓰므로 광선을 두 번 쏘지 않는다.
        public void CopyLimits(ExplosionShape source)
        {
            EnsureInitialized();
            source.EnsureInitialized();
            System.Array.Copy(source._limits, _limits, _limits.Length);
        }

        // 원이 반경 radius로 퍼진 상태를 그린다. 벽에 닿은 방향만 그 자리에서 멈춘다.
        public void SetRadius(float radius)
        {
            EnsureInitialized();
            // 반경은 월드 단위인데 정점은 이 오브젝트 기준이다. 크기를 키운 적(전사 등)의 자식이면
            // 그 배율만큼 크게 그려져 실제 판정보다 넓어 보이므로 배율을 되돌린다.
            float scale = Mathf.Abs(transform.lossyScale.x);
            float toLocal = scale > 0.0001f ? 1f / scale : 1f;
            _vertices[0] = Vector3.zero;
            float width = _outlineWidth;
            for (int i = 0; i < _segments; i++)
            {
                float edge = Mathf.Min(radius, _limits[i]);
                _vertices[i + 1] = _directions[i] * (edge * toLocal);
                if (_hasOutline)
                {
                    // 벽에 잘린 방향도 잘린 자리를 따라 테두리를 그린다. 원이 테두리보다 작으면 가운데까지만.
                    _vertices[OuterIndex(i)] = _vertices[i + 1];
                    _vertices[InnerIndex(i)] = _directions[i] * (Mathf.Max(0f, edge - width) * toLocal);
                }
            }

            _mesh.vertices = _vertices;
            _mesh.RecalculateBounds();
        }

        public void SetColor(Color color)
        {
            EnsureInitialized();
            Color32 color32 = color;
            int fillCount = _segments + 1;
            for (int i = 0; i < fillCount; i++)
            {
                _colors[i] = color32;
            }

            if (_hasOutline)
            {
                Color32 outline = OutlineColor(color);
                for (int i = fillCount; i < _colors.Length; i++)
                {
                    _colors[i] = outline;
                }
            }

            _mesh.colors32 = _colors;
        }

        public void ResetColor()
        {
            EnsureInitialized();
            SetColor(_color);
        }

        // 채움 색을 어둡게 한 색. 채움이 사라지면(투명도 0) 테두리도 함께 사라진다.
        private Color OutlineColor(Color fill)
        {
            return new Color(fill.r * _outlineDarken, fill.g * _outlineDarken, fill.b * _outlineDarken,
                Mathf.Clamp01(fill.a * _outlineAlphaRatio));
        }

        private int OuterIndex(int segment)
        {
            return _segments + 1 + segment;
        }

        private int InnerIndex(int segment)
        {
            return _segments * 2 + 1 + segment;
        }

        // 폭발 후 사라지는 연출용. 기본 색의 알파에 비율을 곱한다.
        public void SetOpacity(float opacity)
        {
            EnsureInitialized();
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
