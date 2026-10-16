using DG.Tweening;
using ExplodeIt.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ExplodeIt.UI
{
    // 좌측 상단 하트. 최대 체력만큼 하트를 두고, 남은 만큼만 빨갛게 칠한다. 한 칸 잃으면 그 하트가 퐁 튀며 회색이 된다.
    // 하트는 판이 시작될 때 한 번만 견본을 복제해 만든다(전투 중 생성은 없다).
    public class HealthView : MonoBehaviour
    {
        private const float LosePunch = 0.35f;

        // 견본 하트. 씬에서는 꺼 둔다.
        [SerializeField] private Image _heartTemplate;
        [SerializeField] private Color _fullColor = new Color(0.95f, 0.2f, 0.25f, 1f);
        [SerializeField] private Color _emptyColor = new Color(0.25f, 0.22f, 0.22f, 0.7f);

        private Image[] _hearts = new Image[0];
        private int _current = -1;

        private void Awake()
        {
            _heartTemplate.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.PlayerHealthChanged += OnHealthChanged;
        }

        private void OnDisable()
        {
            GameEvents.PlayerHealthChanged -= OnHealthChanged;
        }

        private void OnHealthChanged(int current, int max)
        {
            if (_hearts.Length != max)
            {
                Build(max);
            }

            for (int i = 0; i < _hearts.Length; i++)
            {
                bool isFull = i < current;
                _hearts[i].color = isFull ? _fullColor : _emptyColor;
                // 방금 잃은 칸만 튄다.
                if (!isFull && _current >= 0 && i < _current)
                {
                    RectTransform heart = _hearts[i].rectTransform;
                    heart.DOKill(true);
                    heart.DOPunchScale(Vector3.one * LosePunch, 0.35f, 6).SetUpdate(true).SetLink(heart.gameObject);
                }
            }

            _current = current;
        }

        private void Build(int max)
        {
            for (int i = 0; i < _hearts.Length; i++)
            {
                Destroy(_hearts[i].gameObject);
            }

            _hearts = new Image[max];
            for (int i = 0; i < max; i++)
            {
                _hearts[i] = Instantiate(_heartTemplate, _heartTemplate.transform.parent);
                _hearts[i].gameObject.SetActive(true);
            }
        }
    }
}
