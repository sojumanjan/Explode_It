using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ExplodeIt.UI
{
    // 마우스를 올리면 버튼이 살짝 커지고, 내리면 원래 크기로 돌아온다. 누를 수 있는 곳이라는 걸 손에 닿기 전에 알린다.
    // 일시정지·사망 화면에서도 움직이도록 실제 시간으로 트윈한다.
    public class HoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const float HoverScaleRatio = 1.1f;
        private const float TweenTime = 0.12f;

        // 크기를 바꿀 대상. 비워 두면 이 오브젝트. 그림자와 앞면이 따로인 버튼은 둘을 묶은 부모를 넣는다.
        [SerializeField] private Transform _target;

        private Vector3 _baseScale;

        private Transform Target => _target != null ? _target : transform;

        private void Awake()
        {
            _baseScale = Target.localScale;
        }

        // 화면이 꺼질 때 커진 채로 남지 않게 한다.
        private void OnDisable()
        {
            Target.DOKill();
            Target.localScale = _baseScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ScaleTo(_baseScale * HoverScaleRatio);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ScaleTo(_baseScale);
        }

        private void ScaleTo(Vector3 scale)
        {
            Target.DOKill();
            Target.DOScale(scale, TweenTime).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject);
        }
    }
}
