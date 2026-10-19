using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 야수가 할퀴며 돌진할 때 지나간 자리에 남는 발톱 자국. 몸만 미끄러지면 "할퀸다"는 느낌이 없어서 범위 위에 줄을 긋는다.
    // 돌진하는 동안 시작점에서 몸 위치까지 자국이 늘어나고, 끝나면 잠깐 남았다가 흐려져 사라진다.
    // 줄은 처음 한 번만 만들어 두고 다시 쓴다(전투 중 생성 없음). 판정과는 상관없는 그림이다.
    public class ClawSlash : MonoBehaviour
    {
        // 한 줄을 이루는 점 수. 가운데가 굵고 양 끝이 가는 모양을 그리려면 점이 여럿 있어야 한다.
        private const int PointsPerStreak = 8;

        [Tooltip("자국 줄 수")]
        [SerializeField, Min(1)] private int _streakCount = 3;

        [Tooltip("줄 사이 간격 (할퀴기 폭 대비 비율). 0.2면 폭 6일 때 줄 사이가 1.2유닛")]
        [SerializeField, Range(0f, 0.5f)] private float _spacingRatio = 0.18f;

        [Tooltip("가운데 줄보다 바깥 줄이 앞뒤로 짧아지는 길이 (돌진 길이 대비 비율). 발톱 끝처럼 가운데가 길게 보인다")]
        [SerializeField, Range(0f, 0.4f)] private float _outerShrinkRatio = 0.12f;

        [Tooltip("줄 굵기 (유닛). 가운데 기준이고 양 끝으로 갈수록 가늘어진다")]
        [SerializeField, Min(0.01f)] private float _width = 0.35f;

        [Tooltip("줄 굵기 모양. 가로축 0 = 시작점, 1 = 끝점")]
        [SerializeField] private AnimationCurve _widthCurve = new AnimationCurve(
            new Keyframe(0f, 0.1f), new Keyframe(0.35f, 1f), new Keyframe(1f, 0.05f));

        [Tooltip("자국 색. 시작점(왼쪽) → 끝점(오른쪽)")]
        [SerializeField] private Gradient _color = DefaultColor();

        [Tooltip("돌진이 끝난 뒤 자국이 그대로 남는 시간 (초)")]
        [SerializeField, Min(0f)] private float _holdDuration = 0.08f;

        [Tooltip("자국이 흐려져 사라지는 시간 (초)")]
        [SerializeField, Min(0.01f)] private float _fadeDuration = 0.25f;

        [Tooltip("줄 그림에 쓸 머티리얼 (스프라이트 기본 머티리얼)")]
        [SerializeField] private Material _material;

        [Tooltip("그리는 순서. 범위 예고(2) 위, 야수 몸(5) 아래라 바닥에 그어진 자국처럼 보인다")]
        [SerializeField] private int _sortingOrder = 3;

        private LineRenderer[] _lines;
        private GradientColorKey[] _colorKeys;
        private GradientAlphaKey[] _baseAlphaKeys;
        private GradientAlphaKey[] _alphaKeys;
        private Gradient _fadeGradient;
        private Vector2 _start;
        private Vector2 _direction;
        private Vector2 _side;
        private float _spacing;
        private float _length;
        // 돌진이 끝난 뒤 지난 시간. 음수면 돌진 중이거나 숨긴 상태다.
        private float _releasedTime = -1f;
        private bool _isVisible;

        private static Gradient DefaultColor()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.95f, 0.9f), 0f), new GradientColorKey(new Color(1f, 0.25f, 0.2f), 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }

        private void Awake()
        {
            _colorKeys = _color.colorKeys;
            _baseAlphaKeys = _color.alphaKeys;
            _alphaKeys = new GradientAlphaKey[_baseAlphaKeys.Length];
            _fadeGradient = new Gradient();
            _lines = new LineRenderer[_streakCount];
            for (int i = 0; i < _streakCount; i++)
            {
                var child = new GameObject($"Streak{i}");
                child.transform.SetParent(transform, false);
                LineRenderer line = child.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = PointsPerStreak;
                line.widthCurve = _widthCurve;
                line.widthMultiplier = _width;
                line.colorGradient = _color;
                line.sharedMaterial = _material;
                line.sortingOrder = _sortingOrder;
                line.numCapVertices = 2;
                line.enabled = false;
                _lines[i] = line;
            }
        }

        private void OnDisable()
        {
            Hide();
        }

        // 돌진을 시작하는 순간. 자국은 아직 길이 0이다.
        public void Begin(Vector2 start, Vector2 direction, float clawWidth)
        {
            _start = start;
            _direction = direction.normalized;
            _side = new Vector2(-_direction.y, _direction.x);
            _spacing = clawWidth * _spacingRatio;
            _length = 0f;
            _releasedTime = -1f;
            _isVisible = true;
            SetAlpha(1f);
            for (int i = 0; i < _lines.Length; i++)
            {
                _lines[i].enabled = true;
            }

            Rebuild();
        }

        // 돌진 중 몸이 지금 닿은 곳까지 자국을 늘린다.
        public void Reach(Vector2 position)
        {
            if (!_isVisible || _releasedTime >= 0f)
            {
                return;
            }

            _length = Mathf.Max(_length, Vector2.Dot(position - _start, _direction));
            Rebuild();
        }

        // 돌진이 끝났거나 끊겼다. 잠깐 남았다가 흐려진다.
        public void Release()
        {
            if (_isVisible && _releasedTime < 0f)
            {
                _releasedTime = 0f;
            }
        }

        public void Hide()
        {
            _isVisible = false;
            _releasedTime = -1f;
            if (_lines == null)
            {
                return;
            }

            for (int i = 0; i < _lines.Length; i++)
            {
                _lines[i].enabled = false;
            }
        }

        private void Update()
        {
            if (!_isVisible || _releasedTime < 0f)
            {
                return;
            }

            _releasedTime += Time.deltaTime;
            float fade = (_releasedTime - _holdDuration) / _fadeDuration;
            if (fade >= 1f)
            {
                Hide();
                return;
            }

            SetAlpha(1f - Mathf.Clamp01(fade));
        }

        private void Rebuild()
        {
            float center = (_lines.Length - 1) * 0.5f;
            for (int i = 0; i < _lines.Length; i++)
            {
                float offset = i - center;
                // 바깥 줄일수록 앞뒤가 조금씩 짧다.
                float shrink = Mathf.Abs(offset) * _outerShrinkRatio * _length;
                float from = Mathf.Min(shrink, _length);
                float to = Mathf.Max(from, _length - shrink);
                Vector2 lineStart = _start + _side * (offset * _spacing) + _direction * from;
                Vector2 step = _direction * ((to - from) / (PointsPerStreak - 1));
                for (int p = 0; p < PointsPerStreak; p++)
                {
                    _lines[i].SetPosition(p, lineStart + step * p);
                }
            }
        }

        // 색 그라디언트는 그대로 두고 알파만 곱한다. 키 배열은 처음에 한 번만 만든다.
        private void SetAlpha(float alpha)
        {
            for (int i = 0; i < _alphaKeys.Length; i++)
            {
                _alphaKeys[i] = new GradientAlphaKey(_baseAlphaKeys[i].alpha * alpha, _baseAlphaKeys[i].time);
            }

            _fadeGradient.SetKeys(_colorKeys, _alphaKeys);
            for (int i = 0; i < _lines.Length; i++)
            {
                _lines[i].colorGradient = _fadeGradient;
            }
        }
    }
}
