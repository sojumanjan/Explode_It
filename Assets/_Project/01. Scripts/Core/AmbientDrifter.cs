// 나뭇잎, 구름 그림자처럼 맵 위를 바람 따라 떠다니는 장식을 띄우는 컴포넌트
using UnityEngine;

namespace ExplodeIt.Core
{
    // 장식은 시작할 때 자식으로 정해진 개수만 만들고, 범위 밖으로 나가면 반대편으로 되돌려 계속 쓴다(전투 중 생성, 삭제 없음).
    // 맵이 살아 있는 느낌만 주면 되므로 판정, 충돌은 없다. 일시 정지 중에는 함께 멈추도록 Time.deltaTime을 쓴다.
    public class AmbientDrifter : MonoBehaviour
    {
        [SerializeField] private AmbientDrifterData _data;
        [Tooltip("떠다닐 범위. 이 콜라이더의 상자 안에서 돌고, 밖으로 나가면 반대편에서 다시 들어온다(맵의 카메라 범위)")]
        [SerializeField] private Collider2D _area;

        private Transform[] _items;
        private SpriteRenderer[] _renderers;
        private float[] _speeds;
        private float[] _phases;
        private float[] _frameTimes;
        private int[] _frameIndices;
        private Vector2[] _bases;
        private Vector2 _wind;
        private Vector2 _side;
        private Vector2 _min;
        private Vector2 _max;

        private void Awake()
        {
            if (_data == null || _area == null || _data.Frames == null || _data.Frames.Length == 0)
            {
                enabled = false;
                return;
            }

            _wind = _data.WindDirection.sqrMagnitude > 0f ? _data.WindDirection.normalized : Vector2.right;
            _side = new Vector2(-_wind.y, _wind.x);
            // 그림이 반쯤 걸친 채 사라지거나 튀어나오지 않게, 그림 크기와 흔들림만큼 범위를 넓힌다.
            Bounds bounds = _area.bounds;
            Vector2 size = (Vector2)_data.Frames[0].bounds.size * _data.ScaleRange.y;
            Vector2 pad = size * 0.5f + Vector2.one * _data.WobbleAmplitude;
            _min = (Vector2)bounds.min - pad;
            _max = (Vector2)bounds.max + pad;

            int count = _data.Count;
            _items = new Transform[count];
            _renderers = new SpriteRenderer[count];
            _speeds = new float[count];
            _phases = new float[count];
            _frameTimes = new float[count];
            _frameIndices = new int[count];
            _bases = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("장식");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.color = _data.Color;
                sr.sortingOrder = _data.SortingOrder;
                _frameIndices[i] = Random.Range(0, _data.Frames.Length);
                sr.sprite = _data.Frames[_frameIndices[i]];
                sr.flipX = Random.value < 0.5f;
                float scale = Random.Range(_data.ScaleRange.x, _data.ScaleRange.y);
                go.transform.localScale = new Vector3(scale, scale, 1f);
                _items[i] = go.transform;
                _renderers[i] = sr;
                _speeds[i] = Random.Range(_data.SpeedRange.x, _data.SpeedRange.y);
                _phases[i] = Random.value * Mathf.PI * 2f;
                _frameTimes[i] = Random.value * _data.FrameTime;
                // 처음부터 맵 곳곳에 흩어 둔다(한쪽 끝에서 몰려 들어오지 않게).
                _bases[i] = new Vector2(Random.Range(_min.x, _max.x), Random.Range(_min.y, _max.y));
            }

            Place(0f);
        }

        private void Update()
        {
            Place(Time.deltaTime);
        }

        private void Place(float dt)
        {
            Sprite[] frames = _data.Frames;
            float omega = _data.WobbleFrequency * Mathf.PI * 2f;
            Vector2 span = _max - _min;
            for (int i = 0; i < _items.Length; i++)
            {
                Vector2 p = _bases[i] + _wind * (_speeds[i] * dt);
                // 범위 밖으로 나가면 반대편으로 옮긴다.
                if (p.x > _max.x) p.x -= span.x;
                else if (p.x < _min.x) p.x += span.x;
                if (p.y > _max.y) p.y -= span.y;
                else if (p.y < _min.y) p.y += span.y;
                _bases[i] = p;

                _phases[i] += omega * dt;
                Vector2 pos = p + _side * (Mathf.Sin(_phases[i]) * _data.WobbleAmplitude);
                _items[i].position = new Vector3(pos.x, pos.y, 0f);

                if (frames.Length > 1 && !_data.FramesAreVariants)
                {
                    _frameTimes[i] += dt;
                    if (_frameTimes[i] >= _data.FrameTime)
                    {
                        _frameTimes[i] -= _data.FrameTime;
                        _frameIndices[i] = (_frameIndices[i] + 1) % frames.Length;
                        _renderers[i].sprite = frames[_frameIndices[i]];
                    }
                }
            }
        }
    }
}
