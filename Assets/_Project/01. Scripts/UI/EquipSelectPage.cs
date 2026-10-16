using System.Collections.Generic;
using ExplodeIt.Core;
using ExplodeIt.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExplodeIt.UI
{
    // 메인 메뉴의 장비 칸을 누르면 열리는 고르기 페이지. 누른 칸에 따라 무기·스킬·아티팩트 목록을 보여 준다.
    // 왼쪽 페이지에서 카드를 누르면 오른쪽 페이지에 크게 보이고, 장착을 누르면 저장하고 메인 메뉴로 돌아간다.
    // 잠긴 카드는 이름을 가리고 해금 조건(그 보상을 주는 업적의 조건)을 보여 준다.
    // 카드는 처음 열릴 때 견본을 복제해 만든다(전투 중이 아니라 Instantiate를 써도 된다).
    public class EquipSelectPage : MonoBehaviour, IMenuPage
    {
        [SerializeField] private UnlockCatalog _catalog;
        [SerializeField] private MenuPager _pager;
        [SerializeField] private TMP_Text _title;
        [SerializeField, TextArea] private string _weaponTitle = "무기 고르기";
        [SerializeField, TextArea] private string _abilityTitle = "스킬 고르기";
        [SerializeField, TextArea] private string _artifactTitle = "아티팩트 {0} 고르기";
        // 아티팩트 칸 번호 표시. 칸 순서대로.
        [SerializeField] private string[] _slotNames = { "I", "II" };

        [Header("목록")]
        // 복제할 견본 카드. 씬에서는 꺼 둔다.
        [SerializeField] private ItemCard _cardTemplate;
        [SerializeField] private Transform _list;
        // 고를 것이 하나도 없을 때(아직 아티팩트가 없는 등) 대신 보이는 글.
        [SerializeField] private GameObject _empty;

        [Header("오른쪽 페이지")]
        [SerializeField] private Image _previewIcon;
        // 카드를 누를 때 소리. 잠긴 카드도 같은 소리다(못 쓰는 기능이 아니라 아직 안 모은 것이라서).
        [SerializeField] private SoundData _clickSound;
        // 잠긴 것을 눌렀을 때 그림을 이 색으로 칠해 실루엣만 보여 준다.
        [SerializeField] private Color _silhouetteColor = new Color(0.12f, 0.09f, 0.07f, 0.85f);
        [SerializeField] private TMP_Text _previewName;
        [SerializeField] private TMP_Text _previewText;
        [SerializeField] private Button _equipButton;
        // "장착 중"에 마우스를 올리면 "장착 해제"로 바꿔 보인다. 장착 중을 다시 누르면 해제된다.
        [SerializeField] private HoverLabel _equipHover;
        [SerializeField] private string _unequipText = "장착 해제";
        [SerializeField] private string _equipText = "장착";
        [SerializeField] private string _equippedText = "장착 중";
        [SerializeField] private string _upgradeEquipText = "강화 장착";
        [SerializeField, TextArea] private string _noUnlockText = "아직 해금 방법이 없어요.";
        // 잠긴 카드에는 업적 이름 없이 해금 조건만 보여 준다.
        [SerializeField, TextArea] private string _unlockFormat = "<b>해금 조건</b>\n{0}";
        [Header("스킬 (왼쪽 페이지에 스킬 3개, 각 스킬 밑으로 강화 칸이 세로로 이어진다)")]
        // 스킬 줄을 담는 칸(가로 배치). 무기·아티팩트 목록 대신 스킬일 때만 보인다.
        [SerializeField] private RectTransform _abilityList;
        // 강화 칸 견본(스킬 카드보다 조금 작은 카드). 씬에서는 꺼 둔다.
        [SerializeField] private ItemCard _upgradeCellTemplate;
        [SerializeField] private string _upgradeCellFormat = "강화 {0}";
        // 스킬 한 줄의 너비와 칸 사이 간격 (UI 단위). 높이는 스킬 카드와 강화 칸 수로 정해진다.
        [SerializeField] private float _abilityColumnWidth = 180f;
        [SerializeField] private float _upgradeCellSpacing = 8f;

        private enum Kind
        {
            Weapon,
            Ability,
            Artifact
        }

        // 종류마다 카드와 그 카드에 담긴 것. 카드는 처음 한 번만 만들고 종류가 바뀌면 켜고 끈다.
        private readonly List<ItemCard>[] _cards = { new List<ItemCard>(), new List<ItemCard>(), new List<ItemCard>() };
        private readonly List<IUnlockable>[] _items = { new List<IUnlockable>(), new List<IUnlockable>(), new List<IUnlockable>() };
        private Kind _kind;
        // 아티팩트 칸 번호. 무기·스킬에서는 쓰지 않는다.
        private int _slot;
        private IUnlockable _preview;
        private bool _hasCards;
        // 스킬 줄마다의 강화 칸과 그 강화.
        private readonly List<ItemCard> _upgradeCells = new List<ItemCard>();
        private readonly List<AbilityUpgradeData> _upgradeItems = new List<AbilityUpgradeData>();
        // 오른쪽 페이지에 띄운 강화. 있으면 장착·빼기 버튼이 스킬이 아니라 이 강화를 다룬다.
        private AbilityUpgradeData _upgradePreview;

        public void PrepareWeapon()
        {
            _kind = Kind.Weapon;
        }

        public void PrepareAbility()
        {
            _kind = Kind.Ability;
        }

        public void PrepareArtifact(int slot)
        {
            _kind = Kind.Artifact;
            _slot = slot;
        }

        public void OnPageOpened()
        {
            BuildCards();
            _title.text = _kind == Kind.Weapon ? _weaponTitle
                : _kind == Kind.Ability ? _abilityTitle
                : string.Format(_artifactTitle, SlotName(_slot));
            bool isAbility = _kind == Kind.Ability;
            _list.gameObject.SetActive(!isAbility);
            _abilityList.gameObject.SetActive(isAbility);
            for (int k = 0; k < _cards.Length; k++)
            {
                for (int i = 0; i < _cards[k].Count; i++)
                {
                    _cards[k][i].gameObject.SetActive(k == (int)_kind);
                }
            }

            _empty.SetActive(_items[(int)_kind].Count == 0);
            _upgradePreview = null;
            _preview = Equipped();
            Refresh();
        }

        public void OnPageClosed()
        {
        }

        // 장착 버튼. 다른 아티팩트 칸에 이미 끼운 것이면 두 칸을 맞바꾼다. 이미 장착 중인 것을 누르면 해제한다.
        public void Equip()
        {
            // 강화는 같은 버튼으로 끼웠다 뺐다 한다.
            if (_upgradePreview != null)
            {
                SetUpgradeEquipped(!_upgradePreview.IsEquipped);
                return;
            }

            if (_preview == null)
            {
                return;
            }

            SaveData data = SaveSystem.Data;
            if (_preview == Equipped())
            {
                if (CanUnequip())
                {
                    Unequip();
                }

                return;
            }

            switch (_kind)
            {
                case Kind.Weapon:
                    data.SelectedWeapon = _preview.Id;
                    break;

                case Kind.Ability:
                    data.SelectedAbility = _preview.Id;
                    break;

                case Kind.Artifact:
                    string current = data.GetArtifactInSlot(_slot);
                    for (int i = 0; i < _catalog.ArtifactSlots; i++)
                    {
                        if (i != _slot && data.GetArtifactInSlot(i) == _preview.Id)
                        {
                            data.SetArtifactInSlot(i, current);
                        }
                    }

                    data.SetArtifactInSlot(_slot, _preview.Id);
                    break;
            }

            SaveSystem.Save();
            _pager.Back();
        }

        private void Unequip()
        {
            SaveSystem.Data.SetArtifactInSlot(_slot, "");
            SaveSystem.Save();
            _pager.Back();
        }

        // 무기·스킬은 늘 하나를 끼고 있어야 하므로 해제하지 않고 다른 것으로 바꿔 끼기만 한다. 아티팩트만 칸을 비울 수 있다.
        private bool CanUnequip()
        {
            return _kind == Kind.Artifact;
        }

        // 다른 아티팩트 칸에 끼워 둔 것인지. 고르면 두 칸이 맞바뀐다.
        private bool IsInOtherSlot(IUnlockable item)
        {
            if (_kind != Kind.Artifact)
            {
                return false;
            }

            SaveData data = SaveSystem.Data;
            for (int i = 0; i < _catalog.ArtifactSlots; i++)
            {
                if (i != _slot && data.GetArtifactInSlot(i) == item.Id)
                {
                    return true;
                }
            }

            return false;
        }

        private void SetEquipButton(bool isEquipped, bool canUnequip, string equipText)
        {
            _equipButton.interactable = !isEquipped || canUnequip;
            _equipHover.SetTexts(isEquipped ? _equippedText : equipText, isEquipped && canUnequip ? _unequipText : "");
        }

        // 강화는 여러 개를 함께 끼울 수 있어 페이지를 떠나지 않고 바로 반영한다.
        private void SetUpgradeEquipped(bool isEquipped)
        {
            SaveSystem.Data.SetUpgradeEquipped(_upgradePreview.Id, isEquipped);
            SaveSystem.Save();
            AudioManager.Play(_clickSound);
            RefreshUpgradeCells();
            ShowUpgradeButtons();
        }

        private IUnlockable Equipped()
        {
            SaveData data = SaveSystem.Data;
            switch (_kind)
            {
                case Kind.Weapon:
                    return _catalog.FindUsableWeapon(data.SelectedWeapon);
                case Kind.Ability:
                    return _catalog.FindUsableAbility(data.SelectedAbility);
                default:
                    return _catalog.FindUsableArtifact(data.GetArtifactInSlot(_slot));
            }
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
            AddItems(Kind.Weapon, _catalog.Weapons);
            AddItems(Kind.Artifact, _catalog.Artifacts);
            AbilityData[] abilities = _catalog.Abilities;
            for (int i = 0; i < abilities.Length; i++)
            {
                AddAbilityColumn(abilities[i]);
            }
        }

        // 스킬 한 줄: 위에 스킬 카드, 그 밑으로 강화 칸이 세로로 이어진다.
        private void AddAbilityColumn(AbilityData ability)
        {
            var column = new GameObject(ability.Id, typeof(RectTransform));
            var columnRect = (RectTransform)column.transform;
            columnRect.SetParent(_abilityList, false);
            var vertical = column.AddComponent<VerticalLayoutGroup>();
            vertical.childAlignment = TextAnchor.UpperCenter;
            vertical.spacing = _upgradeCellSpacing;
            vertical.childControlWidth = false;
            vertical.childControlHeight = false;
            vertical.childForceExpandWidth = false;
            vertical.childForceExpandHeight = false;

            _items[(int)Kind.Ability].Add(ability);
            ItemCard card = Instantiate(_cardTemplate, columnRect);
            card.gameObject.SetActive(true);
            _cards[(int)Kind.Ability].Add(card);
            float height = ((RectTransform)_cardTemplate.transform).sizeDelta.y;
            float cellHeight = ((RectTransform)_upgradeCellTemplate.transform).sizeDelta.y;

            AbilityUpgradeData[] upgrades = _catalog.Upgrades;
            for (int i = 0; i < upgrades.Length; i++)
            {
                if (upgrades[i].Ability != ability)
                {
                    continue;
                }

                ItemCard cell = Instantiate(_upgradeCellTemplate, columnRect);
                cell.gameObject.SetActive(true);
                _upgradeCells.Add(cell);
                _upgradeItems.Add(upgrades[i]);
                height += _upgradeCellSpacing + cellHeight;
            }

            columnRect.sizeDelta = new Vector2(_abilityColumnWidth, height);
        }

        private void AddItems(Kind kind, IUnlockable[] items)
        {
            for (int i = 0; i < items.Length; i++)
            {
                _items[(int)kind].Add(items[i]);
                ItemCard card = Instantiate(_cardTemplate, _list);
                card.gameObject.SetActive(false);
                _cards[(int)kind].Add(card);
            }
        }

        private void Refresh()
        {
            IUnlockable equipped = Equipped();
            List<IUnlockable> items = _items[(int)_kind];
            List<ItemCard> cards = _cards[(int)_kind];
            for (int i = 0; i < items.Count; i++)
            {
                IUnlockable item = items[i];
                cards[i].Setup(item, item.Icon, item.DisplayName, !item.IsUnlocked, OnCardClicked);
                // 노란 표시는 "지금 장착 중"에만 쓴다. 눌러 보기만 한 카드에 칠하면 장착된 줄 헷갈린다.
                cards[i].SetSelected(item == equipped);
                // 다른 칸에 끼운 아티팩트는 회색으로 칠하고 누를 수 없게 한다. 노란색은 지금 고르는 칸의 것에만 쓴다.
                cards[i].SetDimmed(item.IsUnlocked && item != equipped && IsInOtherSlot(item));
            }

            if (_kind == Kind.Ability)
            {
                RefreshUpgradeCells();
            }

            ShowPreview(_preview);
            bool isEquipped = _preview != null && _preview == equipped;
            _equipButton.gameObject.SetActive(_preview != null);
            SetEquipButton(isEquipped, CanUnequip(), _equipText);
        }

        // 같은 스킬의 강화는 목록 순서대로 I, II로 부른다. 잠긴 칸은 이름 대신 ???가 보인다.
        private void RefreshUpgradeCells()
        {
            AbilityData current = null;
            int number = 0;
            for (int i = 0; i < _upgradeCells.Count; i++)
            {
                AbilityUpgradeData upgrade = _upgradeItems[i];
                number = upgrade.Ability == current ? number + 1 : 0;
                current = upgrade.Ability;
                string label = string.Format(_upgradeCellFormat, SlotName(number));
                _upgradeCells[i].Setup(upgrade, upgrade.Icon, label, !upgrade.IsUnlocked, OnUpgradeCellClicked);
                // 강화 칸의 표시는 "보고 있는 것"이 아니라 "장착한 것"이다.
                _upgradeCells[i].SetSelected(upgrade.IsEquipped);
            }
        }

        // 잠겼으면 해금 조건만 보여 준다(강화 내용은 해금 전까지 숨긴다). 해금했으면 내용과 장착·빼기 버튼을 보여 준다.
        private void OnUpgradeCellClicked(ItemCard cell)
        {
            AudioManager.Play(_clickSound);
            var upgrade = (AbilityUpgradeData)cell.Item;
            if (cell.IsLocked)
            {
                _upgradePreview = null;
                cell.Shake();
                ShowLocked(upgrade);
                return;
            }

            _upgradePreview = upgrade;
            ShowPreview(upgrade);
            ShowUpgradeButtons();
        }

        private void ShowUpgradeButtons()
        {
            bool isEquipped = _upgradePreview.IsEquipped;
            _equipButton.gameObject.SetActive(true);
            SetEquipButton(isEquipped, true, _upgradeEquipText);
        }

        private void OnCardClicked(ItemCard card)
        {
            AudioManager.Play(_clickSound);
            IUnlockable item = (IUnlockable)card.Item;
            if (card.IsLocked)
            {
                if (_upgradePreview != null)
                {
                    // 강화를 보던 중이었으면 빼기 버튼이 스킬 쪽으로 남지 않게 원래 상태로 되돌린다.
                    _upgradePreview = null;
                    Refresh();
                }

                card.Shake();
                ShowLocked(item);
                return;
            }

            _upgradePreview = null;
            _preview = item;
            Refresh();
        }

        private void ShowPreview(IUnlockable item)
        {
            Sprite icon = item != null ? item.Icon : null;
            _previewIcon.sprite = icon;
            _previewIcon.enabled = icon != null;
            _previewIcon.color = Color.white;
            _previewName.text = item != null ? item.DisplayName : "";
            _previewText.text = item != null ? item.Description : "";
        }

        // 잠긴 카드는 고르지 않고 오른쪽 페이지에 실루엣과 해금 조건만 띄운다. 숨김 업적이면 조건도 가린다.
        private void ShowLocked(IUnlockable item)
        {
            AchievementData achievement = _catalog.FindAchievementRewarding((Object)item);
            _previewIcon.sprite = item.Icon;
            _previewIcon.enabled = item.Icon != null;
            _previewIcon.color = _silhouetteColor;
            _previewName.text = "???";
            _previewText.text = achievement == null ? _noUnlockText : string.Format(_unlockFormat, achievement.IsHidden ? "???" : achievement.Description);
            _equipButton.gameObject.SetActive(false);
        }
    }
}
