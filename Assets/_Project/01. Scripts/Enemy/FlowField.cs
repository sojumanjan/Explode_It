using ExplodeIt.Stage;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 모든 칸에 "플레이어까지 걸어가는 거리"를 적어 두고, 적은 거리가 줄어드는 쪽으로 내려간다.
    // 계산은 플레이어가 칸을 옮길 때 한 번만 하고, 적은 배열만 읽으므로 적 수가 늘어도 비용이 거의 같다.
    public class FlowField : MonoBehaviour
    {
        private const float DiagonalCost = 1.41421356f;
        private const float Epsilon = 0.000001f;

        private static readonly Collider2D[] OverlapBuffer = new Collider2D[1];
        // 앞 4개는 상하좌우, 뒤 4개는 대각선.
        private static readonly int[] NeighborX = { 1, -1, 0, 0, 1, 1, -1, -1 };
        private static readonly int[] NeighborY = { 0, 0, 1, -1, 1, -1, 1, -1 };

        [SerializeField] private Transform _target;
        [Tooltip("격자 범위의 기준이 되는 맵 스프라이트. 씬의 스폰 구역은 자동으로 포함된다")]
        [SerializeField] private SpriteRenderer _mapArea;
        [SerializeField] private LayerMask _obstacleMask;
        [Tooltip("칸 한 변의 길이 (유닛). 작을수록 좁은 통로를 인식하지만 칸 수가 제곱으로 늘어난다")]
        [SerializeField, Min(0.1f)] private float _cellSize = 0.5f;
        [Tooltip("적 몸 반경 (유닛). 벽을 이만큼 부풀려 검사해 적이 벽 모서리에 몸을 걸치지 않게 한다")]
        [SerializeField, Min(0f)] private float _agentRadius = 0.4f;

        private Vector2 _origin;
        private int _width;
        private int _height;
        private bool[] _blocked;
        private float[] _distance;
        private Vector2[] _flow;
        private CellMinHeap _heap;

        // 시야는 적이 실제로 있는 칸만 필요하므로, 조회될 때 계산해 재계산 전까지 기억한다.
        private int[] _sightStamp;
        private bool[] _sightValue;
        private int _stamp;

        private int _targetCell = -1;
        private Vector2 _targetPosition;

        public static FlowField Current { get; private set; }

        private void Awake()
        {
            BuildGrid();
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

        private void FixedUpdate()
        {
            int cell = CellIndexAt(_target.position);
            if (cell == _targetCell)
            {
                return;
            }

            _targetCell = cell;
            _targetPosition = _target.position;
            if (cell >= 0)
            {
                Recompute();
            }
        }

        // 적이 이번 스텝에 움직일 방향. 플레이어가 보이면 직진, 가려져 있으면 흐름을 따른다.
        public Vector2 GetDirection(Vector2 position)
        {
            Vector2 toTarget = (Vector2)_target.position - position;
            Vector2 direct = toTarget.sqrMagnitude > Epsilon ? toTarget.normalized : Vector2.zero;
            if (_targetCell < 0)
            {
                return direct;
            }

            int cell = CellIndexAt(position);
            if (cell < 0)
            {
                return direct;
            }

            if (!_blocked[cell] && HasLineOfSight(cell))
            {
                return direct;
            }

            Vector2 flow = SampleFlow(position);
            return flow.sqrMagnitude > Epsilon ? flow.normalized : direct;
        }

        private void BuildGrid()
        {
            // 적이 맵 밖 스폰 구역에서 걸어 들어오므로 구역까지 격자에 포함한다.
            Bounds bounds = _mapArea.bounds;
            SpawnArea[] areas = FindObjectsByType<SpawnArea>();
            for (int i = 0; i < areas.Length; i++)
            {
                bounds.Encapsulate(areas[i].Bounds);
            }

            // 가장자리 칸도 이웃을 가지도록 한 칸씩 여유를 둔다.
            _origin = (Vector2)bounds.min - Vector2.one * _cellSize;
            _width = Mathf.CeilToInt(bounds.size.x / _cellSize) + 2;
            _height = Mathf.CeilToInt(bounds.size.y / _cellSize) + 2;

            int count = _width * _height;
            _blocked = new bool[count];
            _distance = new float[count];
            _flow = new Vector2[count];
            _sightStamp = new int[count];
            _sightValue = new bool[count];
            // 칸마다 이웃 8칸에서 한 번씩 갱신될 수 있으므로 최대 삽입 수는 칸 수의 8배다.
            _heap = new CellMinHeap(count * 8 + 8);

            var filter = new ContactFilter2D();
            filter.SetLayerMask(_obstacleMask);
            var boxSize = Vector2.one * (_cellSize + _agentRadius * 2f);

            // 씬 로드 직후 Awake에서는 콜라이더 위치가 물리 쪽에 아직 반영되지 않았을 수 있어 먼저 맞춘다.
            Physics2D.SyncTransforms();

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    _blocked[y * _width + x] = Physics2D.OverlapBox(CellCenter(x, y), boxSize, 0f, filter, OverlapBuffer) > 0;
                }
            }
        }

        private void Recompute()
        {
            for (int i = 0; i < _distance.Length; i++)
            {
                _distance[i] = float.PositiveInfinity;
            }

            _stamp++;
            _heap.Clear();
            SeedTarget();

            while (_heap.Count > 0)
            {
                int cell = _heap.Pop(out float distance);
                if (distance > _distance[cell])
                {
                    continue;
                }

                int cx = cell % _width;
                int cy = cell / _width;
                for (int k = 0; k < 8; k++)
                {
                    int nx = cx + NeighborX[k];
                    int ny = cy + NeighborY[k];
                    if (!IsOpen(nx, ny))
                    {
                        continue;
                    }

                    bool isDiagonal = k >= 4;
                    // 대각선 양옆 중 하나라도 벽이면 모서리를 파고드는 경로가 되므로 막는다.
                    if (isDiagonal && (!IsOpen(nx, cy) || !IsOpen(cx, ny)))
                    {
                        continue;
                    }

                    int neighbor = ny * _width + nx;
                    float next = distance + (isDiagonal ? DiagonalCost : 1f);
                    if (next < _distance[neighbor])
                    {
                        _distance[neighbor] = next;
                        _heap.Push(neighbor, next);
                    }
                }
            }

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    _flow[y * _width + x] = ComputeFlow(x, y);
                }
            }
        }

        // 벽에 붙어 있으면 부풀린 벽 때문에 플레이어 칸이 막힌 칸일 수 있다. 그때는 주변의 열린 칸에서 시작한다.
        private void SeedTarget()
        {
            int tx = _targetCell % _width;
            int ty = _targetCell / _width;
            if (!_blocked[_targetCell])
            {
                _distance[_targetCell] = 0f;
                _heap.Push(_targetCell, 0f);
                return;
            }

            for (int k = 0; k < 8; k++)
            {
                int nx = tx + NeighborX[k];
                int ny = ty + NeighborY[k];
                if (!IsOpen(nx, ny))
                {
                    continue;
                }

                int neighbor = ny * _width + nx;
                float cost = k >= 4 ? DiagonalCost : 1f;
                _distance[neighbor] = cost;
                _heap.Push(neighbor, cost);
            }
        }

        // 좌우·상하 거리 차이로 연속적인 방향을 만든다.
        private Vector2 ComputeFlow(int x, int y)
        {
            int cell = y * _width + x;
            float self = _distance[cell];
            if (_blocked[cell] || float.IsInfinity(self))
            {
                return Vector2.zero;
            }

            float left = DistanceOr(x - 1, y, self);
            float right = DistanceOr(x + 1, y, self);
            float down = DistanceOr(x, y - 1, self);
            float up = DistanceOr(x, y + 1, self);
            var gradient = new Vector2(left - right, down - up);

            // 막힌 쪽으로 향하는 성분은 지운다. 그대로 두면 벽 쪽으로 밀려 붙는다.
            if ((gradient.x < 0f && !IsOpen(x - 1, y)) || (gradient.x > 0f && !IsOpen(x + 1, y)))
            {
                gradient.x = 0f;
            }

            if ((gradient.y < 0f && !IsOpen(x, y - 1)) || (gradient.y > 0f && !IsOpen(x, y + 1)))
            {
                gradient.y = 0f;
            }

            if (gradient.sqrMagnitude > Epsilon)
            {
                return gradient.normalized;
            }

            // 벽 정중앙 뒤처럼 양쪽 길이 똑같으면 기울기가 0이 된다.
            // 이웃 순서로 항상 같은 쪽을 골라 멈추지 않게 하고, 결과도 매번 같게 한다.
            return LowestNeighborDirection(x, y, self);
        }

        private Vector2 LowestNeighborDirection(int x, int y, float self)
        {
            float lowest = self;
            Vector2 direction = Vector2.zero;
            for (int k = 0; k < 8; k++)
            {
                int nx = x + NeighborX[k];
                int ny = y + NeighborY[k];
                if (!IsOpen(nx, ny))
                {
                    continue;
                }

                if (k >= 4 && (!IsOpen(nx, y) || !IsOpen(x, ny)))
                {
                    continue;
                }

                float distance = _distance[ny * _width + nx];
                if (distance < lowest)
                {
                    lowest = distance;
                    direction = new Vector2(NeighborX[k], NeighborY[k]).normalized;
                }
            }

            return direction;
        }

        // 적을 둘러싼 칸 중심 4개의 방향을 가까운 비율로 섞어, 칸 경계를 넘어도 방향이 끊기지 않게 한다.
        private Vector2 SampleFlow(Vector2 position)
        {
            float gx = (position.x - _origin.x) / _cellSize - 0.5f;
            float gy = (position.y - _origin.y) / _cellSize - 0.5f;
            int x0 = Mathf.FloorToInt(gx);
            int y0 = Mathf.FloorToInt(gy);
            float tx = gx - x0;
            float ty = gy - y0;

            Vector2 sum = Vector2.zero;
            AddCorner(x0, y0, (1f - tx) * (1f - ty), ref sum);
            AddCorner(x0 + 1, y0, tx * (1f - ty), ref sum);
            AddCorner(x0, y0 + 1, (1f - tx) * ty, ref sum);
            AddCorner(x0 + 1, y0 + 1, tx * ty, ref sum);
            return sum;
        }

        // 벽 칸이나 도달할 수 없는 칸은 방향이 없으므로 섞지 않는다. 합은 호출 쪽에서 정규화한다.
        private void AddCorner(int x, int y, float weight, ref Vector2 sum)
        {
            if (!IsOpen(x, y))
            {
                return;
            }

            sum += _flow[y * _width + x] * weight;
        }

        // 부풀린 벽 기준의 격자 위에서만 본다. 물리 검사를 하지 않아 적이 많아도 가볍다.
        private bool HasLineOfSight(int cell)
        {
            if (_sightStamp[cell] == _stamp)
            {
                return _sightValue[cell];
            }

            int x = cell % _width;
            int y = cell / _width;
            Vector2 from = CellCenter(x, y);
            Vector2 to = _targetPosition;
            int steps = Mathf.CeilToInt(Vector2.Distance(from, to) / (_cellSize * 0.5f));

            bool visible = true;
            for (int i = 1; i < steps; i++)
            {
                int sample = CellIndexAt(Vector2.Lerp(from, to, (float)i / steps));
                // 플레이어가 벽에 붙어 있으면 플레이어 칸 자체가 막힌 칸일 수 있어 제외한다.
                if (sample < 0 || sample == _targetCell)
                {
                    continue;
                }

                if (_blocked[sample])
                {
                    visible = false;
                    break;
                }
            }

            _sightStamp[cell] = _stamp;
            _sightValue[cell] = visible;
            return visible;
        }

        private float DistanceOr(int x, int y, float fallback)
        {
            if (!IsOpen(x, y))
            {
                return fallback;
            }

            float distance = _distance[y * _width + x];
            return float.IsInfinity(distance) ? fallback : distance;
        }

        private bool IsOpen(int x, int y)
        {
            return x >= 0 && x < _width && y >= 0 && y < _height && !_blocked[y * _width + x];
        }

        private int CellIndexAt(Vector2 position)
        {
            int x = Mathf.FloorToInt((position.x - _origin.x) / _cellSize);
            int y = Mathf.FloorToInt((position.y - _origin.y) / _cellSize);
            if (x < 0 || x >= _width || y < 0 || y >= _height)
            {
                return -1;
            }

            return y * _width + x;
        }

        private Vector2 CellCenter(int x, int y)
        {
            return _origin + new Vector2((x + 0.5f) * _cellSize, (y + 0.5f) * _cellSize);
        }

        // 흐름이 의도대로 벽을 돌아가는지 씬 뷰에서 눈으로 확인하기 위한 표시.
        private void OnDrawGizmosSelected()
        {
            if (_blocked == null)
            {
                return;
            }

            var cubeSize = new Vector3(_cellSize * 0.9f, _cellSize * 0.9f, 0f);
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    int cell = y * _width + x;
                    Vector2 center = CellCenter(x, y);
                    if (_blocked[cell])
                    {
                        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
                        Gizmos.DrawCube(center, cubeSize);
                    }
                    else if (_flow[cell] != Vector2.zero)
                    {
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawLine(center, center + _flow[cell] * (_cellSize * 0.45f));
                    }
                }
            }
        }
    }
}
