using DG.Tweening;
using ExplodeIt.Progression;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ExplodeIt.UI
{
    // 도감의 스티커 한 장. 자리는 씬에 미리 놓아 두고(기울기·크기도 트랜스폼으로 직접 잡는다), 업적만 연결한다.
    // 딴 업적은 스티커를, 못 딴 업적은 흰 실루엣을 보여 준다. 빈자리가 보여야 채우고 싶어진다.
    // 마우스를 올리면 살짝 커지고 도감 페이지가 옆에 설명 창을 띄운다.
    public class StickerSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private AchievementData _achievement;
        [SerializeField] private Image _image;
        [Tooltip("마우스를 올렸을 때 커지는 배율")]
        [SerializeField, Min(1f)] private float _hoverScale = 1.12f;
        [Tooltip("커지고 줄어드는 시간 (초)")]
        [SerializeField, Min(0f)] private float _hoverDuration = 0.12f;
        [Tooltip("실루엣이 없을 때 스티커를 대신 보여 줄 색")]
        [SerializeField] private Color _lockedFallbackColor = new Color(1f, 1f, 1f, 0.25f);

        private StickerBookPage _page;
        private Vector3 _baseScale;
        private bool _hasBaseScale;

        public AchievementData Achievement => _achievement;
        public bool IsCompleted => _achievement != null && _achievement.IsCompleted;

        public void Bind(StickerBookPage page)
        {
            _page = page;
            if (!_hasBaseScale)
            {
                _baseScale = transform.localScale;
                _hasBaseScale = true;
            }
        }

        public void Refresh()
        {
            if (_achievement == null)
            {
                _image.enabled = false;
                return;
            }

            _image.enabled = true;
            bool isCompleted = _achievement.IsCompleted;
            Sprite silhouette = _achievement.StickerSilhouette;
            if (isCompleted || silhouette == null)
            {
                _image.sprite = _achievement.Sticker;
                _image.color = isCompleted ? Color.white : _lockedFallbackColor;
            }
            else
            {
                _image.sprite = silhouette;
                _image.color = Color.white;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_achievement == null)
            {
                return;
            }

            transform.DOKill();
            transform.DOScale(_baseScale * _hoverScale, _hoverDuration).SetUpdate(true).SetLink(gameObject);
            // 커진 스티커가 옆 스티커 밑에 깔리지 않게 맨 위로 올린다.
            transform.SetAsLastSibling();
            if (_page != null)
            {
                _page.ShowTooltip(this);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.DOKill();
            transform.DOScale(_baseScale, _hoverDuration).SetUpdate(true).SetLink(gameObject);
            if (_page != null)
            {
                _page.HideTooltip(this);
            }
        }

        // 페이지를 넘기거나 닫을 때 커진 채로 남지 않게 한다.
        private void OnDisable()
        {
            transform.DOKill();
            if (_hasBaseScale)
            {
                transform.localScale = _baseScale;
            }
        }
    }
}
