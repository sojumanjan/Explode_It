using DG.Tweening;
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.UI
{
    // 보스전 시작 연출. 웨이브 경계는 숨기지만 보스전만은 확실히 알린다.
    // 보스가 생기기 전까지는 이 연출이 웨이브 사이의 유일한 신호다.
    [RequireComponent(typeof(CanvasGroup))]
    public class BossBannerView : MonoBehaviour
    {
        [SerializeField] private RectTransform _content;
        [SerializeField, Min(0f)] private float _fadeIn = 0.15f;
        [SerializeField, Min(0f)] private float _hold = 1.4f;
        [SerializeField, Min(0f)] private float _fadeOut = 0.4f;

        private CanvasGroup _group;
        private Sequence _sequence;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
        }

        private void OnEnable()
        {
            GameEvents.BossIntroStarted += OnBossIntroStarted;
        }

        private void OnDisable()
        {
            GameEvents.BossIntroStarted -= OnBossIntroStarted;
            _sequence?.Kill();
        }

        // 일시정지 중에는 같이 멈추도록 게임 시간 기준으로 연출한다.
        private void OnBossIntroStarted(int bossNumber)
        {
            _sequence?.Kill();
            _group.alpha = 0f;
            _content.localScale = Vector3.one * 1.6f;
            _sequence = DOTween.Sequence()
                .Append(_group.DOFade(1f, _fadeIn))
                .Join(_content.DOScale(1f, _fadeIn).SetEase(Ease.OutBack))
                .AppendInterval(_hold)
                .Append(_group.DOFade(0f, _fadeOut))
                .SetLink(gameObject);
        }
    }
}
