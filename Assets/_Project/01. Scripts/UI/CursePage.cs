using ExplodeIt.Core;
using ExplodeIt.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExplodeIt.UI
{
    // 메인 메뉴 승천 칸을 누르면 열리는 저주 고르기 페이지. 왼쪽 페이지에 저주 칸이 늘어서 있고, 누르면 오른쪽에 설명이 뜬다.
    // 무기와 달리 여러 개를 함께 걸 수 있다. 건 저주는 무한 모드에만 적용된다.
    // 저주는 목록 순서대로 하나씩 열린다. 잠긴 칸은 다음 해금 조건(몇 개를 걸고 몇 마리)을 보여 준다.
    // 마지막 칸(왕)은 다른 저주를 모두 걸어야 걸 수 있고, 저주를 하나라도 풀면 함께 풀린다.
    public class CursePage : MonoBehaviour, IMenuPage
    {
        [SerializeField] private UnlockCatalog _catalog;

        [Header("왼쪽 페이지 (씬에 미리 놓은 칸, 카탈로그 저주 목록과 같은 순서)")]
        [SerializeField] private ItemCard[] _cards;

        [Header("오른쪽 페이지")]
        [SerializeField] private Image _previewIcon;
        [SerializeField] private TMP_Text _previewName;
        [SerializeField] private TMP_Text _previewText;
        [SerializeField] private Button _equipButton;
        // 장비 고르기 페이지와 같은 말을 쓴다. "장착 중"에 마우스를 올리면 "장착 해제"로 바꿔 보인다.
        [SerializeField] private HoverLabel _equipHover;
        [SerializeField] private string _equipText = "장착";
        [SerializeField] private string _equippedText = "장착 중";
        [SerializeField] private string _unequipText = "장착 해제";
        [SerializeField] private string _kingBlockedText = "모든 저주를 장착해야 해요";
        // 잠긴 칸은 이름을 가리고 해금 조건만 보여 준다. 바로 다음 차례가 아닌 칸은 조건도 가린다.
        [Tooltip("다음 차례 저주의 해금 조건. {0} = 걸어야 하는 저주 수, {1} = 처치 수")]
        [SerializeField, TextArea] private string _unlockFormat = "<b>해금 조건</b>\n저주 {0}개 이상을 장착하고 한 판에 {1}마리 처치";
        [SerializeField, TextArea] private string _laterUnlockText = "<b>해금 조건</b>\n앞의 저주를 먼저 열어야 해요.";
        // 마지막 칸(왕)은 순서가 아니라 "모든 저주"가 조건이라, 차례와 상관없이 따로 암시만 준다.
        [SerializeField, TextArea] private string _kingUnlockText = "<b>해금 조건</b>\n모든 저주를 짊어진 자에게 자격이 주어진다.";
        [SerializeField] private Color _silhouetteColor = new Color(0.12f, 0.09f, 0.07f, 0.85f);
        [SerializeField] private SoundData _clickSound;

        private CurseData _preview;

        public void OnPageOpened()
        {
            if (_cards.Length != _catalog.Curses.Length)
            {
                Debug.LogWarning($"[CursePage] 저주 목록 {_catalog.Curses.Length}개, 씬 칸 {_cards.Length}개. 칸을 목록 순서에 맞게 놓아 주세요.");
            }

            _preview = FirstUnlocked();
            Refresh();
        }

        public void OnPageClosed()
        {
        }

        // 걸기 버튼. 걸려 있으면 푼다.
        public void ToggleEquip()
        {
            if (_preview == null || !_preview.IsUnlocked)
            {
                return;
            }

            bool isEquipped = !_preview.IsEquipped;
            if (isEquipped && _preview.Kind == CurseKind.King && !_catalog.CanEquipKing)
            {
                return;
            }

            SaveData data = SaveSystem.Data;
            data.SetCurseEquipped(_preview.Id, isEquipped);
            if (!isEquipped && _preview.Kind != CurseKind.King)
            {
                UnequipKing(data);
            }

            SaveSystem.Save();
            AudioManager.Play(_clickSound);
            Refresh();
        }

        private void UnequipKing(SaveData data)
        {
            CurseData[] curses = _catalog.Curses;
            for (int i = 0; i < curses.Length; i++)
            {
                if (curses[i].Kind == CurseKind.King)
                {
                    data.SetCurseEquipped(curses[i].Id, false);
                }
            }
        }

        private CurseData FirstUnlocked()
        {
            CurseData[] curses = _catalog.Curses;
            for (int i = 0; i < curses.Length; i++)
            {
                if (curses[i].IsUnlocked)
                {
                    return curses[i];
                }
            }

            return null;
        }

        private void Refresh()
        {
            CurseData[] curses = _catalog.Curses;
            for (int i = 0; i < _cards.Length; i++)
            {
                bool hasCurse = i < curses.Length;
                _cards[i].gameObject.SetActive(hasCurse);
                if (!hasCurse)
                {
                    continue;
                }

                CurseData curse = curses[i];
                _cards[i].Setup(curse, curse.Icon, curse.DisplayName, !curse.IsUnlocked, OnCardClicked, OnCardDoubleClicked);
                _cards[i].SetSelected(curse.IsEquipped);
            }

            ShowPreview(_preview);
        }

        private void ShowPreview(CurseData curse)
        {
            Sprite icon = curse != null ? curse.Icon : null;
            _previewIcon.sprite = icon;
            _previewIcon.enabled = icon != null;
            _previewIcon.color = Color.white;
            _previewName.text = curse != null ? curse.DisplayName : "";
            _previewText.text = curse != null ? curse.Description : "";
            _equipButton.gameObject.SetActive(curse != null);
            if (curse == null)
            {
                return;
            }

            bool isEquipped = curse.IsEquipped;
            bool isBlocked = !isEquipped && curse.Kind == CurseKind.King && !_catalog.CanEquipKing;
            _equipButton.interactable = !isBlocked;
            _equipHover.SetTexts(isEquipped ? _equippedText : isBlocked ? _kingBlockedText : _equipText, isEquipped ? _unequipText : "");
        }

        private void ShowLocked(CurseData curse)
        {
            _previewIcon.sprite = curse.Icon;
            _previewIcon.enabled = curse.Icon != null;
            _previewIcon.color = _silhouetteColor;
            _previewName.text = "???";
            _previewText.text = curse.Kind == CurseKind.King ? _kingUnlockText
                : curse == _catalog.NextLockedCurse ? string.Format(_unlockFormat, curse.RequiredCurses, curse.UnlockKills)
                : _laterUnlockText;
            _equipButton.gameObject.SetActive(false);
        }

        private void OnCardClicked(ItemCard card)
        {
            AudioManager.Play(_clickSound);
            var curse = (CurseData)card.Item;
            if (card.IsLocked)
            {
                card.Shake();
                ShowLocked(curse);
                return;
            }

            _preview = curse;
            ShowPreview(curse);
        }

        // 편의 기능: 같은 칸을 두 번 누르면 걸기 버튼을 누른 것과 같다.
        private void OnCardDoubleClicked(ItemCard card)
        {
            if (!card.IsLocked && ReferenceEquals(card.Item, _preview))
            {
                ToggleEquip();
            }
        }
    }
}
