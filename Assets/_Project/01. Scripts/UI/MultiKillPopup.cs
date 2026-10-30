using DG.Tweening;
using ExplodeIt.Core;
using TMPro;
using UnityEngine;

namespace ExplodeIt.UI
{
    // 폭탄 하나로 여러 마리를 잡으면 그 자리에서 "x3!"을 위로 툭 던졌다 떨어뜨린다. 예측이 맞았다는 걸 숫자로 보여 주는 보상이다.
    // 처치 알림은 폭발 알림보다 먼저(같은 프레임, 판정 도중) 오므로, 그 사이 처치 수를 모아 두었다가 폭발 알림에서 꺼낸다.
    // 블랙홀로 모아 터뜨린 것도 보상이라 플레이어 폭발 전체(폭탄, 다발, 순간, 블랙홀) 알림을 듣는다.
    // BombExploded는 처치 수, 정확도 집계용이라 블랙홀이 내지 않는다.
    // 글씨는 월드 공간 TMP라 카메라가 움직여도 터진 자리에 붙어 있다.
    public class MultiKillPopup : MonoBehaviour
    {
        private const float PopTime = 0.12f;
        private const float PopOvershoot = 1.3f;

        [SerializeField] private TextMeshPro _prefab;
        [Tooltip("한 폭탄에 이 수 이상 잡으면 띄운다 (마리)")]
        [SerializeField, Min(2)] private int _threshold = 3;
        [Tooltip("이 수 이상이면 가장 크고 진하게 보인다 (마리)")]
        [SerializeField, Min(3)] private int _maxEmphasisCount = 10;
        [Tooltip("띄울 글. {0}에 처치 수가 들어간다")]
        [SerializeField] private string _format = "x{0}!";
        [Tooltip("문턱 수일 때 색")]
        [SerializeField] private Color _lowColor = new Color(1f, 0.86f, 0.3f, 1f);
        [Tooltip("최대 강조 수 이상일 때 색")]
        [SerializeField] private Color _highColor = new Color(1f, 0.3f, 0.2f, 1f);
        [Tooltip("문턱 수일 때 글씨 크기 배율")]
        [SerializeField, Min(0.1f)] private float _minScale = 1f;
        [Tooltip("최대 강조 수 이상일 때 글씨 크기 배율")]
        [SerializeField, Min(0.1f)] private float _maxScale = 1.7f;
        [Tooltip("터진 자리에서 얼마나 위에서 시작하나 (월드 단위)")]
        [SerializeField] private float _startOffset = 0.4f;
        [Tooltip("위로 던져지는 높이 (월드 단위)")]
        [SerializeField, Min(0f)] private float _jumpHeight = 1.2f;
        [Tooltip("떨어져 멈추는 곳이 시작 자리보다 얼마나 아래인가 (월드 단위)")]
        [SerializeField, Min(0f)] private float _fallDistance = 0.7f;
        [Tooltip("옆으로 흘러가는 거리 (월드 단위). 띄울 때마다 좌우를 번갈아 간다")]
        [SerializeField, Min(0f)] private float _sideDrift = 0.5f;
        [Tooltip("던졌다 떨어지는 전체 시간 (초, 실제 시간). 히트스톱 중에도 바로 보이게 실제 시간으로 돈다")]
        [SerializeField, Min(0.1f)] private float _duration = 0.85f;
        [Tooltip("끝에서 이 시간 동안 흐려지며 사라진다 (초)")]
        [SerializeField, Min(0f)] private float _fadeTime = 0.3f;
        [Tooltip("동시에 떠 있을 수 있는 글씨 수. 미리 만들어 둔다")]
        [SerializeField, Min(1)] private int _poolSize = 8;

        private ComponentPool<TextMeshPro> _pool;
        private int _pendingKills;
        private int _pendingFrame = -1;
        private float _nextSide = 1f;

        private void Awake()
        {
            _pool = new ComponentPool<TextMeshPro>(_prefab, transform, _poolSize, _poolSize * 2);
            _pool.Prewarm(_poolSize);
        }

        private void OnEnable()
        {
            GameEvents.EnemyKilled += OnEnemyKilled;
            GameEvents.PlayerBlastExploded += OnBlastExploded;
        }

        private void OnDisable()
        {
            GameEvents.EnemyKilled -= OnEnemyKilled;
            GameEvents.PlayerBlastExploded -= OnBlastExploded;
        }

        // 프레임이 바뀌면 지난 처치는 이 폭발과 상관없다(저주 낙뢰 등).
        private void OnEnemyKilled(Vector2 position)
        {
            if (_pendingFrame != Time.frameCount)
            {
                _pendingFrame = Time.frameCount;
                _pendingKills = 0;
            }

            _pendingKills++;
        }

        private void OnBlastExploded(Vector2 position, float radius)
        {
            int kills = _pendingFrame == Time.frameCount ? _pendingKills : 0;
            _pendingKills = 0;
            if (kills >= _threshold)
            {
                Show(position, kills);
            }
        }

        private void Show(Vector2 position, int kills)
        {
            float emphasis = Mathf.InverseLerp(_threshold, _maxEmphasisCount, kills);
            float scale = Mathf.Lerp(_minScale, _maxScale, emphasis);
            float side = _nextSide;
            _nextSide = -_nextSide;

            Vector3 start = new Vector3(position.x, position.y + _startOffset, 0f);
            Vector3 end = start + new Vector3(side * _sideDrift, -_fallDistance, 0f);

            TextMeshPro text = _pool.Get(start);
            Transform t = text.transform;
            text.SetText(_format, kills);
            text.color = Color.Lerp(_lowColor, _highColor, emphasis);
            text.alpha = 1f;
            t.localScale = Vector3.zero;
            // 던지는 쪽으로 살짝 기울었다가 떨어지며 바로 선다.
            t.localRotation = Quaternion.Euler(0f, 0f, -side * 12f);

            Sequence sequence = DOTween.Sequence().SetUpdate(true).SetLink(text.gameObject);
            sequence.Append(t.DOJump(end, _jumpHeight, 1, _duration).SetEase(Ease.Linear));
            sequence.Insert(0f, t.DOScale(scale * PopOvershoot, PopTime).SetEase(Ease.OutQuad));
            sequence.Insert(PopTime, t.DOScale(scale, PopTime).SetEase(Ease.OutQuad));
            sequence.Insert(0f, t.DOLocalRotate(Vector3.zero, _duration * 0.5f).SetEase(Ease.OutQuad));
            sequence.Insert(Mathf.Max(0f, _duration - _fadeTime),
                DOTween.To(() => text.alpha, a => text.alpha = a, 0f, _fadeTime));
            sequence.OnComplete(() => _pool.Release(text));
        }
    }
}
