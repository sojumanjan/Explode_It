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
        // 잠긴 카드는 그림을 거의 검게 칠해 모양만 알아보게 한다.
        private static readonly Color LockedTint = new Color(0.08f, 0.06f, 0.05f, 0.92f);
        // 다른 칸에 이미 끼운 카드는 살짝 회색으로 눌러 둔다.
        private static readonly Color DimmedTint = new Color(0.55f, 0.55f, 0.55f, 1f);

        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private GameObject _lockMark;
        [SerializeField] private GameObject _selectedMark;

        private Action<ItemCard> _onClick;
        private Color _nameColor;

        public object Item { get; private set; }
        public bool IsLocked { get; private set; }

        private void Awake()
        {
            _button.onClick.AddListener(OnClick);
            _nameColor = _name.color;
        }

        public void Setup(object item, Sprite icon, string displayName, bool isLocked, Action<ItemCard> onClick)
        {
            Item = item;
            IsLocked = isLocked;
            _onClick = onClick;
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _icon.color = isLocked ? LockedTint : Color.white;
            _name.text = isLocked ? "???" : displayName;
            _lockMark.SetActive(isLocked);
            _name.color = _nameColor;
            SetSelected(false);
        }

        public void SetDimmed(bool isDimmed)
        {
            if (IsLocked)
            {
                return;
            }

            _icon.color = isDimmed ? DimmedTint : Color.white;
            _name.color = isDimmed ? _nameColor * DimmedTint : _nameColor;
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
        }
    }
}
