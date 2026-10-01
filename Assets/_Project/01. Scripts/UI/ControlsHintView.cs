using DG.Tweening;
using UnityEngine;

namespace ExplodeIt.UI
{
    // 처음 하는 사람이 조작을 몰라 헤매지 않도록, 판이 시작될 때마다 잠깐 보여 주고 사라진다.
    [RequireComponent(typeof(CanvasGroup))]
    public class ControlsHintView : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _visibleDuration = 4f;
        [SerializeField, Min(0f)] private float _fadeDuration = 1f;

        private void Start()
        {
            var group = GetComponent<CanvasGroup>();
            group.alpha = 1f;
            group.blocksRaycasts = false;

            // 일시정지 중에는 같이 멈추도록 게임 시간 기준으로 센다.
            group.DOFade(0f, _fadeDuration)
                .SetDelay(_visibleDuration)
                .SetLink(gameObject);
        }
    }
}
