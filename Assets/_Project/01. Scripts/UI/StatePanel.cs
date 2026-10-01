using DG.Tweening;
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.UI
{
    // 지정한 게임 상태일 때만 보이는 패널. 일시정지·사망·클리어 패널이 이 하나로 동작한다.
    // 켜고 끄는 건 항상 켜져 있는 StatePanelSwitcher가 맡는다. 그래서 에디터에서 패널을 꺼 두든 켜 두든 결과가 같다.
    [RequireComponent(typeof(CanvasGroup))]
    public class StatePanel : MonoBehaviour
    {
        [SerializeField] private GameState _showOn;
        // 사망 순간을 눈으로 확인할 틈을 준다. 일시정지 패널은 0으로 둔다.
        [SerializeField, Min(0f)] private float _showDelay;
        [SerializeField, Min(0f)] private float _fadeDuration = 0.2f;

        private CanvasGroup _group;
        private Tween _tween;

        public GameState ShowOn => _showOn;

        private CanvasGroup Group => _group != null ? _group : _group = GetComponent<CanvasGroup>();

        private void OnDisable()
        {
            _tween?.Kill();
        }

        // 일시정지 중에는 timeScale이 0이므로 실제 시간으로 연출한다.
        public void Show()
        {
            _tween?.Kill();
            Group.alpha = 0f;
            gameObject.SetActive(true);
            _tween = Group.DOFade(1f, _fadeDuration)
                .SetDelay(_showDelay)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        public void Hide()
        {
            _tween?.Kill();
            gameObject.SetActive(false);
        }
    }
}
