using DG.Tweening;
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.UI
{
    // 지정한 게임 상태일 때만 보이는 패널. 일시정지·사망·클리어 패널이 이 하나로 동작한다.
    [RequireComponent(typeof(CanvasGroup))]
    public class StatePanel : MonoBehaviour
    {
        [SerializeField] private GameState _showOn;
        // 사망 순간을 눈으로 확인할 틈을 준다. 일시정지 패널은 0으로 둔다.
        [SerializeField, Min(0f)] private float _showDelay;
        [SerializeField, Min(0f)] private float _fadeDuration = 0.2f;

        private CanvasGroup _group;
        private Tween _tween;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            SetVisible(false);
        }

        private void OnEnable()
        {
            GameEvents.GameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.GameStateChanged -= OnGameStateChanged;
            _tween?.Kill();
        }

        private void OnGameStateChanged(GameState previous, GameState current)
        {
            _tween?.Kill();
            if (current != _showOn)
            {
                SetVisible(false);
                return;
            }

            // 일시정지 중에는 timeScale이 0이므로 실제 시간으로 연출한다.
            _group.blocksRaycasts = true;
            _group.interactable = true;
            _tween = _group.DOFade(1f, _fadeDuration)
                .SetDelay(_showDelay)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void SetVisible(bool visible)
        {
            _group.alpha = visible ? 1f : 0f;
            _group.blocksRaycasts = visible;
            _group.interactable = visible;
        }
    }
}
