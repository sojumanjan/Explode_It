using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExplodeIt.UI
{
    // 출전 준비 화면의 무기·아티팩트 카드 한 장. 잠긴 카드는 회색 실루엣으로 보이고, 눌러도 고르지 않고 해금 조건만 보여 준다.
    public class ItemCard : MonoBehaviour
    {
        // 잠긴 카드는 그림을 거의 검게 칠해 모양만 알아보게 한다. 결과 화면 할 일 칸도 같은 실루엣을 쓴다.
        public static readonly Color LockedTint = new Color(0.08f, 0.06f, 0.05f, 0.92f);
        // 다른 칸에 이미 끼운 카드는 칸째 회색으로 칠한다. 눌러서 설명은 볼 수 있다.
        private static readonly Color DimmedTint = new Color(0.55f, 0.55f, 0.55f, 1f);

        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private GameObject _lockMark;
        [SerializeField] private GameObject _selectedMark;
        // 새로 해금하고 아직 눌러 보지 않았다는 NEW 도장. 비워 두면(저주 카드 등) NEW를 쓰지 않는다.
        [SerializeField] private RectTransform _newMark;

        // NEW 도장은 가만히 있다가 이 간격(초)마다 한 번 피융: 커졌다(NewPop) → 원래보다 작아졌다(NewPopUnder) → 제자리.
        // 계속 움직이면 거슬려서 가끔만 눈길을 끈다. NewPopTime은 세 단계를 합친 시간(초)이다.
        private const float NewPopInterval = 2.5f;
        private const float NewPop = 0.18f;
        private const float NewPopUnder = 0.1f;
        private const float NewPopTime = 0.5f;
        // 처음 보일 때 도장을 쾅 찍듯 이 배율에서 줄어들며 나타난다.
        private const float NewStampScale = 1.5f;
        private const float NewStampTime = 0.2f;

        // 두 번 누른 것으로 보는 간격 (초, 실제 시간). 운영체제 기본 더블클릭 간격과 비슷하게 둔다.
        private const float DoubleClickTime = 0.3f;

        private Action<ItemCard> _onClick;
        private Action<ItemCard> _onDoubleClick;
        // 목록을 다시 그려도(Setup) 지워지지 않아야 두 번째 클릭을 알아본다.
        private float _lastClickTime = -1f;
        private Color _nameColor;
        private Image _background;
        private Color _backgroundColor;

        public object Item { get; private set; }
        public bool IsLocked { get; private set; }

        private bool _isInitialized;

        private void Awake()
        {
            Initialize();
        }

        // 꺼진 채 복제된 카드는 Awake 전에 Setup이 불릴 수 있어, 원래 색을 먼저 기억해 둔다.
        private void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;
            _button.onClick.AddListener(OnClick);
            _nameColor = _name.color;
            _background = _button.targetGraphic as Image;
            if (_background != null)
            {
                _backgroundColor = _background.color;
            }
        }

        // onDoubleClick은 같은 카드를 빠르게 두 번 눌렀을 때 한 번 더 불린다(첫 클릭의 onClick 다음).
        public void Setup(object item, Sprite icon, string displayName, bool isLocked, Action<ItemCard> onClick,
            Action<ItemCard> onDoubleClick = null)
        {
            Initialize();
            _onDoubleClick = onDoubleClick;
            Item = item;
            IsLocked = isLocked;
            _onClick = onClick;
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _icon.color = isLocked ? LockedTint : Color.white;
            _name.text = isLocked ? "???" : displayName;
            _lockMark.SetActive(isLocked);
            SetDimmed(false);
            SetSelected(false);
        }

        // Setup이 끄지 않는다. 목록을 다시 그릴 때마다 꺼졌다 켜지면 찍히는 연출이 매번 다시 나와 튀어 보인다.
        public void SetNew(bool isNew)
        {
            if (_newMark == null || _newMark.gameObject.activeSelf == isNew)
            {
                return;
            }

            _newMark.DOKill();
            _newMark.localScale = Vector3.one;
            _newMark.gameObject.SetActive(isNew);
            if (!isNew)
            {
                return;
            }

            // 무한 반복은 시퀀스 안에 넣을 수 없어서, 찍히는 연출이 끝난 뒤 피융을 따로 시작한다.
            _newMark.localScale = Vector3.one * NewStampScale;
            _newMark.DOScale(1f, NewStampTime).SetEase(Ease.InQuad).SetUpdate(true).SetLink(_newMark.gameObject)
                .OnComplete(StartNewPop);
        }

        private void StartNewPop()
        {
            DOTween.Sequence()
                .AppendInterval(NewPopInterval)
                .Append(_newMark.DOScale(1f + NewPop, NewPopTime * 0.3f).SetEase(Ease.OutQuad))
                .Append(_newMark.DOScale(1f - NewPopUnder, NewPopTime * 0.3f).SetEase(Ease.InOutQuad))
                .Append(_newMark.DOScale(1f, NewPopTime * 0.4f).SetEase(Ease.OutQuad))
                .SetLoops(-1).SetTarget(_newMark).SetUpdate(true).SetLink(_newMark.gameObject);
        }

        public void SetDimmed(bool isDimmed)
        {
            _name.color = isDimmed ? _nameColor * DimmedTint : _nameColor;
            if (_background != null)
            {
                _background.color = isDimmed ? _backgroundColor * DimmedTint : _backgroundColor;
            }

            if (!IsLocked)
            {
                _icon.color = isDimmed ? DimmedTint : Color.white;
            }
        }

        public void SetSelected(bool isSelected)
        {
            _selectedMark.SetActive(isSelected);
        }

        // 잠긴 카드를 눌렀을 때 고를 수 없다는 걸 손맛으로 알린다.
        public void Shake()
        {
            transform.DOKill(true);
            transform.DOShakePosition(0.3f, new Vector3(10f, 0f, 0f), 20).SetUpdate(true).SetLink(gameObject);
        }

        private void OnClick()
        {
            transform.DOKill(true);
            transform.DOPunchScale(Vector3.one * 0.08f, 0.2f, 6).SetUpdate(true).SetLink(gameObject);
            _onClick?.Invoke(this);

            float now = Time.unscaledTime;
            if (_lastClickTime >= 0f && now - _lastClickTime <= DoubleClickTime)
            {
                _lastClickTime = -1f;
                _onDoubleClick?.Invoke(this);
                return;
            }

            _lastClickTime = now;
        }
    }
}
