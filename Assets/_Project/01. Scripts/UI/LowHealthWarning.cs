using DG.Tweening;
using ExplodeIt.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ExplodeIt.UI
{
    // 체력이 한 칸만 남는 순간 화면 가장자리를 붉게 깜빡여 "위험하다"를 알린다.
    // 계속 켜 두면 시야를 가리고 금방 무뎌지므로, 몇 초만 깜빡이고 사라진다.
    public class LowHealthWarning : MonoBehaviour
    {
        [SerializeField] private Image _vignette;
        [Tooltip("깜빡이는 시간 (초). 지나면 서서히 사라진다")]
        [SerializeField, Min(0.1f)] private float _duration = 5f;
        [Tooltip("한 번 진해졌다 옅어지는 데 걸리는 시간 (초)")]
        [SerializeField, Min(0.1f)] private float _blinkPeriod = 0.8f;
        [Tooltip("가장 진할 때 불투명도 (0~1)")]
        [SerializeField, Range(0f, 1f)] private float _maxAlpha = 0.5f;
        [Tooltip("가장 옅을 때 불투명도 (0~1)")]
        [SerializeField, Range(0f, 1f)] private float _minAlpha = 0.1f;
        [Tooltip("번쩍임 줄이기를 켰을 때: 깜빡이지 않고 이 불투명도로 은은하게 켜 둔다 (0~1)")]
        [SerializeField, Range(0f, 1f)] private float _reducedAlpha = 0.2f;
        [Tooltip("다 깜빡인 뒤 사라지는 시간 (초)")]
        [SerializeField, Min(0.05f)] private float _fadeOutTime = 0.8f;

        private int _lastHealth = -1;
        private Sequence _sequence;

        private void Awake()
        {
            SetAlpha(0f);
        }

        private void OnEnable()
        {
            GameEvents.PlayerHealthChanged += OnHealthChanged;
            GameEvents.PlayerDied += Stop;
        }

        private void OnDisable()
        {
            GameEvents.PlayerHealthChanged -= OnHealthChanged;
            GameEvents.PlayerDied -= Stop;
            Stop();
        }

        // 깎여서 한 칸이 된 순간에만 띄운다. 처음부터 한 칸인 판(저주 "한 방")은 내내 위험하니 띄우지 않는다.
        private void OnHealthChanged(int current, int max)
        {
            if (current == 1 && _lastHealth > 1)
            {
                Play();
            }
            else if (current != 1)
            {
                Stop();
            }

            _lastHealth = current;
        }

        // 일시정지 동안 같이 멈추도록 게임 시간으로 돈다.
        private void Play()
        {
            Stop();
            _sequence = DOTween.Sequence().SetLink(gameObject);
            if (FlashSafety.IsReduced)
            {
                _sequence.Append(_vignette.DOFade(_reducedAlpha, _fadeOutTime));
                _sequence.AppendInterval(Mathf.Max(0f, _duration - _fadeOutTime));
                _sequence.Append(_vignette.DOFade(0f, _fadeOutTime));
                return;
            }

            int blinks = Mathf.Max(1, Mathf.RoundToInt(_duration / _blinkPeriod));
            float half = _blinkPeriod * 0.5f;
            for (int i = 0; i < blinks; i++)
            {
                _sequence.Append(_vignette.DOFade(_maxAlpha, half).SetEase(Ease.OutQuad));
                _sequence.Append(_vignette.DOFade(_minAlpha, half).SetEase(Ease.InQuad));
            }

            _sequence.Append(_vignette.DOFade(0f, _fadeOutTime));
        }

        private void Stop()
        {
            _sequence?.Kill();
            _sequence = null;
            SetAlpha(0f);
        }

        private void SetAlpha(float alpha)
        {
            Color color = _vignette.color;
            color.a = alpha;
            _vignette.color = color;
        }
    }
}
