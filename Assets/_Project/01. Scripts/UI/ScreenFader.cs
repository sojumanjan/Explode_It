using System;
using DG.Tweening;
using UnityEngine;

namespace ExplodeIt.UI
{
    // 화면 전체를 덮는 검은 막. 씬을 넘길 때 어둡게 덮었다가, 다음 씬이 시작되면 걷어 낸다.
    // 메뉴 → 게임이 툭 끊기지 않고 어두워졌다 밝아지며 이어지게 한다.
    public class ScreenFader : MonoBehaviour
    {
        // 밝아질 때 한 프레임에 반영하는 최대 시간(초). 씬을 막 불러온 직후 프레임은 로딩 시간이 통째로 들어와
        // 트윈이면 첫 프레임에 끝나 버려 밝아지는 게 보이지 않는다.
        private const float MaxFadeStep = 1f / 30f;

        [SerializeField] private CanvasGroup _group;
        [Tooltip("씬이 시작될 때 검은 화면에서 밝아질지")]
        [SerializeField] private bool _fadeInOnStart = true;
        [Tooltip("어두워지고 밝아지는 시간 (초, 실제 시간)")]
        [SerializeField, Min(0.05f)] private float _fadeTime = 0.4f;

        private bool _isFadingIn;
        // 첫 프레임을 건너뛰고 밝아지기 시작한다. 그 프레임까지 씬의 Start들이 돌며 끊긴다.
        private bool _hasSkippedFirstFrame;

        private void Start()
        {
            if (!_fadeInOnStart)
            {
                Hide();
                return;
            }

            _group.gameObject.SetActive(true);
            _group.alpha = 1f;
            _group.blocksRaycasts = true;
            _isFadingIn = true;
        }

        private void Update()
        {
            if (!_isFadingIn)
            {
                return;
            }

            if (!_hasSkippedFirstFrame)
            {
                _hasSkippedFirstFrame = true;
                return;
            }

            _group.alpha -= Mathf.Min(Time.unscaledDeltaTime, MaxFadeStep) / _fadeTime;
            if (_group.alpha <= 0f)
            {
                _isFadingIn = false;
                Hide();
            }
        }

        // 다 덮으면 onCovered를 부른다. 덮는 동안은 클릭이 아래로 새지 않는다.
        public void FadeOut(Action onCovered)
        {
            _isFadingIn = false;
            _group.DOKill();
            _group.gameObject.SetActive(true);
            _group.blocksRaycasts = true;
            _group.DOFade(1f, _fadeTime).SetUpdate(true).SetLink(gameObject).OnComplete(() => onCovered?.Invoke());
        }

        // 검게 덮고, 다 덮이면 onCovered로 화면을 바꾼 뒤 다시 걷어 낸다. 같은 씬 안에서 배경이 통째로 바뀔 때 쓴다.
        public void FadeThrough(float duration, Action onCovered)
        {
            FadeThrough(duration, duration, onCovered);
        }

        // 덮는 시간과 걷어 내는 시간을 따로 준다. 천천히 밝아지며 다른 장면이 드러나게 할 때 쓴다.
        public void FadeThrough(float coverTime, float revealTime, Action onCovered)
        {
            _isFadingIn = false;
            _group.DOKill();
            _group.gameObject.SetActive(true);
            _group.blocksRaycasts = true;
            DOTween.Sequence()
                .Append(_group.DOFade(1f, coverTime))
                .AppendCallback(() => onCovered?.Invoke())
                .Append(_group.DOFade(0f, revealTime))
                .OnComplete(Hide)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void Hide()
        {
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.gameObject.SetActive(false);
        }
    }
}
