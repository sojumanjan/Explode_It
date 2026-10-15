using System.Collections.Generic;
using ExplodeIt.Bombs;
using ExplodeIt.Core;
using ExplodeIt.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExplodeIt.UI
{
    // 메인 메뉴의 장비 칸을 누르면 열리는 고르기 페이지. 무기 칸이면 무기 목록, 아티팩트 칸이면 아티팩트 목록을 보여 준다.
    // 왼쪽 페이지에서 카드를 누르면 오른쪽 페이지에 크게 보이고, 장착을 누르면 저장하고 메인 메뉴로 돌아간다.
    // 잠긴 카드는 이름을 가리고 해금 조건(그 보상을 주는 업적의 조건)을 보여 준다.
    // 카드는 처음 열릴 때 견본을 복제해 만든다(전투 중이 아니라 Instantiate를 써도 된다).
    public class EquipSelectPage : MonoBehaviour, IMenuPage
    {
        [SerializeField] private UnlockCatalog _catalog;
        [SerializeField] private MenuPager _pager;
        [SerializeField] private TMP_Text _title;
        [SerializeField, TextArea] private string _weaponTitle = "무기 고르기";
        [SerializeField, TextArea] private string _artifactTitle = "아티팩트 {0} 고르기";
        // 아티팩트 칸 번호 표시. 칸 순서대로.
        [SerializeField] private string[] _slotNames = { "I", "II" };

        [Header("목록")]
        // 복제할 견본 카드. 씬에서는 꺼 둔다.
        [SerializeField] private ItemCard _cardTemplate;
        [SerializeField] private Transform _list;
        // 아티팩트가 하나도 없을 때 대신 보이는 글.
        [SerializeField] private GameObject _empty;

        [Header("오른쪽 페이지")]
        [SerializeField] private Image _previewIcon;
        // 잠긴 것을 눌렀을 때 그림을 이 색으로 칠해 실루엣만 보여 준다.
        [SerializeField] private Color _silhouetteColor = new Color(0.12f, 0.09f, 0.07f, 0.85f);
        [SerializeField] private TMP_Text _previewName;
        [SerializeField] private TMP_Text _previewText;
        [SerializeField] private Button _equipButton;
        [SerializeField] private TMP_Text _equipLabel;
        // 아티팩트 칸에 뭔가 끼워져 있을 때만 보인다.
        [SerializeField] private GameObject _unequipButton;
        [SerializeField] private string _equipText = "장착";
        [SerializeField] private string _equippedText = "장착 중";
        [SerializeField, TextArea] private string _noUnlockText = "아직 해금 방법이 없어요.";
        // 잠긴 카드에는 업적 이름 없이 해금 조건만 보여 준다.
        [SerializeField, TextArea] private string _unlockFormat = "<b>해금 조건</b>\n{0}";

        private readonly List<ItemCard> _weaponCards = new List<ItemCard>();
        private readonly List<ItemCard> _artifactCards = new List<ItemCard>();
        // -1이면 무기 칸, 0부터는 아티팩트 칸 번호.
        private int _slot = -1;
        private Object _preview;
        private bool _hasCards;

        private bool IsWeapon => _slot < 0;

        public void PrepareWeapon()
        {
            _slot = -1;
        }

        public void PrepareArtifact(int slot)
        {
            _slot = slot;
        }

        public void OnPageOpened()
        {
            BuildCards();
            _title.text = IsWeapon ? _weaponTitle : string.Format(_artifactTitle, SlotName(_slot));
            for (int i = 0; i < _weaponCards.Count; i++)
            {
                _weaponCards[i].gameObject.SetActive(IsWeapon);
            }

            for (int i = 0; i < _artifactCards.Count; i++)
            {
                _artifactCards[i].gameObject.SetActive(!IsWeapon);
            }

            _empty.SetActive(!IsWeapon && _artifactCards.Count == 0);
            _preview = Equipped();
            Refresh();
        }

        public void OnPageClosed()
        {
        }

        // 장착 버튼. 다른 아티팩트 칸에 이미 끼운 것이면 두 칸을 맞바꾼다.
        public void Equip()
        {
            SaveData data = SaveSystem.Data;
            WeaponData weapon = _preview as WeaponData;
            if (weapon != null)
            {
                data.SelectedWeapon = weapon.Id;
            }

            ArtifactData artifact = _preview as ArtifactData;
            if (artifact != null)
            {
                string current = data.GetArtifactInSlot(_slot);
                for (int i = 0; i < _catalog.ArtifactSlots; i++)
                {
                    if (i != _slot && data.GetArtifactInSlot(i) == artifact.Id)
                    {
                        data.SetArtifactInSlot(i, current);
                    }
                }

                data.SetArtifactInSlot(_slot, artifact.Id);
            }

            SaveSystem.Save();
            _pager.Back();
        }

        // 빼기 버튼 (아티팩트 칸만).
        public void Unequip()
        {
            SaveSystem.Data.SetArtifactInSlot(_slot, "");
            SaveSystem.Save();
            _pager.Back();
        }

        private Object Equipped()
        {
            SaveData data = SaveSystem.Data;
            if (IsWeapon)
            {
                return _catalog.FindUsableWeapon(data.SelectedWeapon);
            }

            return _catalog.FindUsableArtifact(data.GetArtifactInSlot(_slot));
        }

        private string SlotName(int slot)
        {
            return slot < _slotNames.Length ? _slotNames[slot] : (slot + 1).ToString();
        }

        private void BuildCards()
        {
            if (_hasCards)
            {
                return;
            }

            _hasCards = true;
            for (int i = 0; i < _catalog.Weapons.Length; i++)
            {
                _weaponCards.Add(CreateCard());
            }

            for (int i = 0; i < _catalog.Artifacts.Length; i++)
            {
                _artifactCards.Add(CreateCard());
            }
        }

        private ItemCard CreateCard()
        {
            ItemCard card = Instantiate(_cardTemplate, _list);
            card.gameObject.SetActive(false);
            return card;
        }

        private void Refresh()
        {
            Object equipped = Equipped();
            if (IsWeapon)
            {
                WeaponData[] weapons = _catalog.Weapons;
                for (int i = 0; i < weapons.Length; i++)
                {
                    WeaponData weapon = weapons[i];
                    _weaponCards[i].Setup(weapon, weapon.Icon, weapon.DisplayName, !weapon.IsUnlocked, OnCardClicked);
                    _weaponCards[i].SetSelected(weapon == _preview);
                }
            }
            else
            {
                ArtifactData[] artifacts = _catalog.Artifacts;
                for (int i = 0; i < artifacts.Length; i++)
                {
                    ArtifactData artifact = artifacts[i];
                    _artifactCards[i].Setup(artifact, artifact.Icon, artifact.DisplayName, !artifact.IsUnlocked, OnCardClicked);
                    _artifactCards[i].SetSelected(artifact == _preview);
                }
            }

            ShowPreview(_preview);
            bool isEquipped = _preview != null && _preview == equipped;
            _equipButton.gameObject.SetActive(_preview != null);
            _equipButton.interactable = !isEquipped;
            _equipLabel.text = isEquipped ? _equippedText : _equipText;
            _unequipButton.SetActive(!IsWeapon && equipped != null);
        }

        private void OnCardClicked(ItemCard card)
        {
            if (card.IsLocked)
            {
                card.Shake();
                ShowLocked((Object)card.Item);
                return;
            }

            _preview = (Object)card.Item;
            Refresh();
        }

        private void ShowPreview(Object item)
        {
            WeaponData weapon = item as WeaponData;
            ArtifactData artifact = item as ArtifactData;
            Sprite icon = weapon != null ? weapon.Icon : artifact != null ? artifact.Icon : null;
            _previewIcon.sprite = icon;
            _previewIcon.enabled = icon != null;
            _previewIcon.color = Color.white;
            _previewName.text = weapon != null ? weapon.DisplayName : artifact != null ? artifact.DisplayName : "";
            _previewText.text = weapon != null ? weapon.Description : artifact != null ? artifact.Description : "";
        }

        // 잠긴 카드는 고르지 않고 오른쪽 페이지에 실루엣과 해금 조건만 띄운다. 숨김 업적이면 조건도 가린다.
        private void ShowLocked(Object item)
        {
            AchievementData achievement = _catalog.FindAchievementRewarding(item);
            WeaponData weapon = item as WeaponData;
            ArtifactData artifact = item as ArtifactData;
            Sprite icon = weapon != null ? weapon.Icon : artifact != null ? artifact.Icon : null;
            _previewIcon.sprite = icon;
            _previewIcon.enabled = icon != null;
            _previewIcon.color = _silhouetteColor;
            _previewName.text = "???";
            _previewText.text = achievement == null ? _noUnlockText : string.Format(_unlockFormat, achievement.IsHidden ? "???" : achievement.Description);
            _equipButton.gameObject.SetActive(false);
        }
    }
}
