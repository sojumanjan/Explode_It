using System;
using DG.Tweening;
using UnityEngine;

namespace ExplodeIt.UI
{
    // 화면 전체를 덮는 검은 막. 씬을 넘길 때 어둡게 덮었다가, 다음 씬이 시작되면 걷어 낸다.
    // 메뉴 → 게임이 툭 끊기지 않고 어두워졌다 밝아지며 이어지게 한다.
    public class ScreenFader : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [Tooltip("씬이 시작될 때 검은 화면에서 밝아질지")]
        [SerializeField] private bool _fadeInOnStart = true;
        [Tooltip("어두워지고 밝아지는 시간 (초, 실제 시간)")]
        [SerializeField, Min(0.05f)] private float _fadeTime = 0.4f;

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
            _group.DOFade(0f, _fadeTime).SetUpdate(true).SetLink(gameObject).OnComplete(Hide);
        }

        // 다 덮으면 onCovered를 부른다. 덮는 동안은 클릭이 아래로 새지 않는다.
        public void FadeOut(Action onCovered)
        {
            _group.DOKill();
            _group.gameObject.SetActive(true);
            _group.blocksRaycasts = true;
            _group.DOFade(1f, _fadeTime).SetUpdate(true).SetLink(gameObject).OnComplete(() => onCovered?.Invoke());
        }

        private void Hide()
        {
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.gameObject.SetActive(false);
        }
    }
}
