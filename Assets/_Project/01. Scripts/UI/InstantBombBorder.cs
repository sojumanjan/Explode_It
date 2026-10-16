using DG.Tweening;
using ExplodeIt.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace ExplodeIt.UI
{
    // 순간폭탄이 발동하는 동안 화면 테두리를 맥박치듯 물들여 "지금 스킬 중"임을 알린다.
    // 폭탄이 바로 터지는 것만으로는 발동 중인지 헷갈리고, 화면 가운데를 가리면 예측 플레이를 방해하므로 가장자리만 쓴다.
    // 테두리 그림은 처음 한 번만 코드로 만든다(가장자리에서 안쪽으로 옅어지는 흰 그림을 색으로 물들인다).
    public class InstantBombBorder : MonoBehaviour
    {
        private const int TextureSize = 128;

        [SerializeField] private RawImage _border;
        [Tooltip("테두리 색. 투명도는 아래 맥박 값이 정한다")]
        [SerializeField] private Color _color = new Color(1f, 0.55f, 0.1f, 1f);
        [Tooltip("테두리 두께 (화면 대비 비율, 0~0.5). 가장자리에서 이만큼 안쪽까지 옅어지며 들어온다")]
        [SerializeField, Range(0.02f, 0.5f)] private float _thickness = 0.12f;
        [Tooltip("맥박의 가장 진할 때 투명도 (0~1)")]
        [SerializeField, Range(0f, 1f)] private float _pulseMax = 0.85f;
        [Tooltip("맥박의 가장 옅을 때 투명도 (0~1)")]
        [SerializeField, Range(0f, 1f)] private float _pulseMin = 0.4f;
        [Tooltip("맥박 반 주기 (초, 실제 시간). 진해졌다 옅어지는 데 이 값의 두 배가 걸린다")]
        [SerializeField, Min(0.05f)] private float _pulseHalfPeriod = 0.35f;
        [Tooltip("켜지고 꺼지는 시간 (초, 실제 시간)")]
        [SerializeField, Min(0.01f)] private float _fadeTime = 0.2f;

        private Texture2D _texture;

        private void Awake()
        {
            _texture = BuildTexture();
            _border.texture = _texture;
            _border.raycastTarget = false;
            SetAlpha(0f);
            _border.enabled = false;
        }

        private void OnEnable()
        {
            ProgressionEvents.AbilityStarted += OnAbilityStarted;
            ProgressionEvents.AbilityEnded += OnAbilityEnded;
        }

        private void OnDisable()
        {
            ProgressionEvents.AbilityStarted -= OnAbilityStarted;
            ProgressionEvents.AbilityEnded -= OnAbilityEnded;
            _border.DOKill();
        }

        private void OnDestroy()
        {
            if (_texture != null)
            {
                Destroy(_texture);
            }
        }

        private void OnAbilityStarted(AbilityData ability)
        {
            if (ability.Kind != AbilityKind.InstantBomb)
            {
                return;
            }

            _border.DOKill();
            _border.enabled = true;
            SetAlpha(0f);
            // 시퀀스 안에서는 무한 반복을 못 쓰므로, 켜지는 트윈이 끝난 뒤 맥박 트윈을 따로 건다.
            _border.DOFade(_pulseMax, _fadeTime).SetUpdate(true).SetLink(gameObject).OnComplete(StartPulse);
        }

        private void StartPulse()
        {
            _border.DOFade(_pulseMin, _pulseHalfPeriod).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine)
                .SetUpdate(true).SetLink(gameObject);
        }

        private void OnAbilityEnded(AbilityData ability)
        {
            if (ability.Kind != AbilityKind.InstantBomb)
            {
                return;
            }

            _border.DOKill();
            _border.DOFade(0f, _fadeTime).SetUpdate(true).SetLink(gameObject).OnComplete(() => _border.enabled = false);
        }

        private void SetAlpha(float alpha)
        {
            Color color = _color;
            color.a = alpha;
            _border.color = color;
        }

        // 가장 가까운 가장자리까지의 거리로 투명도를 정한다. 가장자리에서 진하고 두께만큼 들어가면 사라진다.
        private Texture2D BuildTexture()
        {
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[TextureSize * TextureSize];
            for (int y = 0; y < TextureSize; y++)
            {
                float v = (y + 0.5f) / TextureSize;
                for (int x = 0; x < TextureSize; x++)
                {
                    float u = (x + 0.5f) / TextureSize;
                    float edge = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
                    float t = 1f - Mathf.Clamp01(edge / _thickness);
                    pixels[y * TextureSize + x] = new Color32(255, 255, 255, (byte)(t * t * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
