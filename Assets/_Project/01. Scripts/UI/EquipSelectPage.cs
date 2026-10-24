using System.Collections.Generic;
using ExplodeIt.Core;
using ExplodeIt.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExplodeIt.UI
{
    // 메인 메뉴의 장비 칸을 누르면 열리는 고르기 페이지. 누른 칸에 따라 무기·스킬·아티팩트 목록을 보여 준다.
    // 왼쪽 페이지에서 카드를 누르면 오른쪽 페이지에 크게 보이고, 장착을 누르면 저장한다. 메인 메뉴로는 뒤로가기로 돌아간다.
    // 잠긴 카드는 이름을 가리고 해금 조건(그 보상을 주는 업적의 조건)을 보여 준다.
    // 카드는 씬에 미리 놓아 두고(하이어라키에서 위치를 직접 고칠 수 있게), 목록 순서대로 짝지어 쓴다.
    public class EquipSelectPage : MonoBehaviour, IMenuPage
    {
        [SerializeField] private UnlockCatalog _catalog;
        [SerializeField] private TMP_Text _title;
        [SerializeField, TextArea] private string _weaponTitle = "무기 고르기";
        [SerializeField, TextArea] private string _abilityTitle = "스킬 고르기";
        [SerializeField, TextArea] private string _artifactTitle = "아티팩트 {0} 고르기";
        // 아티팩트 칸 번호 표시. 칸 순서대로.
        [SerializeField] private string[] _slotNames = { "I", "II" };

        [Header("목록 (씬에 미리 놓은 카드, 카탈로그 목록과 같은 순서)")]
        [SerializeField] private ItemCard[] _weaponCards;
        [SerializeField] private ItemCard[] _artifactCards;
        // 무기·아티팩트 카드를 담는 칸. 스킬일 때는 숨긴다.
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
        // 해금한 아티팩트는 어떻게 땄는지를 지우지 않고 남겨 두고, 그 밑에 능력을 보여 준다. 도감 대신 여기서 다시 볼 수 있게 한다.
        [SerializeField, TextArea] private string _clearedFormat = "<b>해금 조건</b>\n{0}\n\n<b>능력</b>\n{1}";
        // 해금 조건 옆에 찍는 "클리어" 도장. 해금한 아티팩트를 볼 때만 켠다.
        [SerializeField] private GameObject _clearStamp;
        [Tooltip("해금 조건 글 끝과 도장 사이 간격 (UI 단위)")]
        [SerializeField] private float _stampGap = 6f;
        [Header("스킬 (왼쪽 페이지에 스킬 3줄, 각 스킬 밑으로 강화 칸)")]
        // 스킬 줄을 담는 칸. 무기·아티팩트 목록 대신 스킬일 때만 보인다.
        [SerializeField] private RectTransform _abilityList;
        [SerializeField] private ItemCard[] _abilityCards;
        // 강화 칸. 카탈로그의 강화 목록과 같은 순서.
        [SerializeField] private ItemCard[] _upgradeCards;
        [SerializeField] private string _upgradeCellFormat = "강화 {0}";

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

            if (IsInOtherSlot(_preview))
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
            // 장착해도 페이지에 남아 다른 것과 비교해 볼 수 있게 한다. 메인으로는 뒤로가기로만 돌아간다.
            AudioManager.Play(_clickSound);
            Refresh();
        }

        private void Unequip()
        {
            SaveSystem.Data.SetArtifactInSlot(_slot, "");
            SaveSystem.Save();
            // 장착해도 페이지에 남아 다른 것과 비교해 볼 수 있게 한다. 메인으로는 뒤로가기로만 돌아간다.
            AudioManager.Play(_clickSound);
            Refresh();
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
            Pair(Kind.Weapon, _catalog.Weapons, _weaponCards);
            Pair(Kind.Artifact, _catalog.Artifacts, _artifactCards);
            Pair(Kind.Ability, _catalog.Abilities, _abilityCards);
            AbilityUpgradeData[] upgrades = _catalog.Upgrades;
            WarnIfMismatch("강화", upgrades.Length, _upgradeCards.Length);
            for (int i = 0; i < upgrades.Length && i < _upgradeCards.Length; i++)
            {
                _upgradeCells.Add(_upgradeCards[i]);
                _upgradeItems.Add(upgrades[i]);
            }
        }

        // 목록과 카드 수가 다르면 남는 쪽은 쓰지 않는다. 아이템을 추가했으면 카드를 복제해 같은 순서에 놓으면 된다.
        private void Pair(Kind kind, IUnlockable[] items, ItemCard[] cards)
        {
            WarnIfMismatch(kind.ToString(), items.Length, cards.Length);
            for (int i = 0; i < items.Length && i < cards.Length; i++)
            {
                _items[(int)kind].Add(items[i]);
                _cards[(int)kind].Add(cards[i]);
            }

            for (int i = items.Length; i < cards.Length; i++)
            {
                cards[i].gameObject.SetActive(false);
            }
        }

        private static void WarnIfMismatch(string label, int itemCount, int cardCount)
        {
            if (itemCount != cardCount)
            {
                Debug.LogWarning($"[EquipSelectPage] {label} 목록 {itemCount}개, 씬 카드 {cardCount}개. 카드를 목록 순서에 맞게 놓아 주세요.");
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
                cards[i].Setup(item, item.Icon, item.DisplayName, !item.IsUnlocked, OnCardClicked, OnCardDoubleClicked);
                // 노란 표시는 "지금 장착 중"에만 쓴다. 눌러 보기만 한 카드에 칠하면 장착된 줄 헷갈린다.
                cards[i].SetSelected(item == equipped);
                cards[i].SetNew(item.IsUnlocked && SaveSystem.Data.IsNew(item.Id));
                // 다른 칸에 끼운 아티팩트는 회색으로 칠한다(눌러서 설명은 볼 수 있다). 노란색은 지금 고르는 칸의 것에만 쓴다.
                cards[i].SetDimmed(item.IsUnlocked && item != equipped && IsInOtherSlot(item));
            }

            if (_kind == Kind.Ability)
            {
                RefreshUpgradeCells();
            }

            ShowPreview(_preview);
            bool isEquipped = _preview != null && _preview == equipped;
            _equipButton.gameObject.SetActive(_preview != null);
            // 다른 칸에 낀 아티팩트는 설명만 보여 주고 "장착 중"으로 묶어 둔다. 해제·맞바꿈은 그 칸에서만 한다.
            if (_preview != null && IsInOtherSlot(_preview))
            {
                SetEquipButton(true, false, _equipText);
                return;
            }

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
                _upgradeCells[i].Setup(upgrade, upgrade.Icon, label, !upgrade.IsUnlocked, OnUpgradeCellClicked, OnUpgradeCellDoubleClicked);
                // 강화 칸의 표시는 "보고 있는 것"이 아니라 "장착한 것"이다.
                _upgradeCells[i].SetSelected(upgrade.IsEquipped);
                _upgradeCells[i].SetNew(upgrade.IsUnlocked && SaveSystem.Data.IsNew(upgrade.Id));
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

            MarkSeen(cell);
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

        // 편의 기능: 카드를 두 번 누르면 장착 버튼을 누른 것과 같다(장착 중이면 해제). 첫 클릭에서 이미 오른쪽에 띄워져 있다.
        private void OnCardDoubleClicked(ItemCard card)
        {
            if (!card.IsLocked && ReferenceEquals(card.Item, _preview))
            {
                Equip();
            }
        }

        private void OnUpgradeCellDoubleClicked(ItemCard cell)
        {
            if (!cell.IsLocked && ReferenceEquals(cell.Item, _upgradePreview))
            {
                Equip();
            }
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

            MarkSeen(card);
            _upgradePreview = null;
            _preview = item;
            Refresh();
        }

        // 해금한 것을 한 번 눌러 보면 NEW를 끈다. 메인 메뉴 칸의 NEW도 이 기록을 읽는다.
        private static void MarkSeen(ItemCard card)
        {
            if (SaveSystem.Data.MarkSeen(((IUnlockable)card.Item).Id))
            {
                SaveSystem.Save();
                card.SetNew(false);
            }
        }

        private void ShowPreview(IUnlockable item)
        {
            Sprite icon = item != null ? item.Icon : null;
            _previewIcon.sprite = icon;
            _previewIcon.enabled = icon != null;
            _previewIcon.color = Color.white;
            _previewName.text = item != null ? item.DisplayName : "";
            _previewText.text = item != null ? item.Description : "";
            _clearStamp.SetActive(false);
            // 무기·스킬·강화·아티팩트 모두 같다. 처음부터 열린 것은 주는 업적이 없어 그대로 둔다.
            if (item != null && item.IsUnlocked)
            {
                AchievementData achievement = _catalog.FindAchievementRewarding((Object)item);
                if (achievement != null)
                {
                    // 달성한 조건은 가운데에 줄을 그어 끝냈다는 걸 한눈에 보이게 한다.
                    _previewText.text = string.Format(_clearedFormat, "<s>" + achievement.Description + "</s>", item.Description);
                    _clearStamp.SetActive(true);
                    PlaceStampAfterCondition(achievement.Description);
                }
            }
        }

        // 도장을 해금 조건 글의 오른쪽 끝에 둔다. 조건이 여러 줄이면 가장 긴 줄의 끝, 높이는 그 줄들 전체의 가운데.
        // 조건 길이·줄바꿈·자동 글자 크기가 바뀌어도 글을 따라간다.
        private void PlaceStampAfterCondition(string condition)
        {
            int start = _clearedFormat.IndexOf("{0}");
            if (start < 0 || condition.Length == 0)
            {
                return;
            }

            _previewText.ForceMeshUpdate();
            TMP_TextInfo info = _previewText.textInfo;
            int first = VisibleLength(_clearedFormat.Substring(0, start));
            int last = first + condition.Length - 1;
            if (first < 0 || last >= info.characterCount)
            {
                return;
            }

            int firstLine = info.characterInfo[first].lineNumber;
            int lastLine = info.characterInfo[last].lineNumber;
            float right = float.MinValue;
            for (int i = first; i <= last; i++)
            {
                TMP_CharacterInfo character = info.characterInfo[i];
                if (character.isVisible)
                {
                    right = Mathf.Max(right, character.topRight.x);
                }
            }

            float middle = (info.lineInfo[firstLine].ascender + info.lineInfo[lastLine].descender) * 0.5f;
            Vector3 world = _previewText.rectTransform.TransformPoint(new Vector3(right, middle, 0f));
            var stamp = (RectTransform)_clearStamp.transform;
            var parent = (RectTransform)stamp.parent;
            Vector2 local = parent.InverseTransformPoint(world);
            float gap = stamp.rect.width * 0.5f + _stampGap;
            stamp.localPosition = new Vector3(local.x + gap, local.y, stamp.localPosition.z);
        }

        // 서식 태그(<b> 등)를 뺀 글자 수. 화면 글자 번호를 셀 때 쓴다.
        private static int VisibleLength(string text)
        {
            int count = 0;
            bool inTag = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '<')
                {
                    inTag = true;
                }
                else if (c == '>')
                {
                    inTag = false;
                }
                else if (!inTag)
                {
                    count++;
                }
            }

            return count;
        }

        // 잠긴 카드는 고르지 않고 오른쪽 페이지에 실루엣과 해금 조건만 띄운다. 숨김 업적이면 조건도 가린다.
        private void ShowLocked(IUnlockable item)
        {
            AchievementData achievement = _catalog.FindAchievementRewarding((Object)item);
            _previewIcon.sprite = item.Icon;
            _previewIcon.enabled = item.Icon != null;
            _previewIcon.color = _silhouetteColor;
            _previewName.text = "???";
            _clearStamp.SetActive(false);
            _previewText.text = achievement == null ? _noUnlockText : string.Format(_unlockFormat, achievement.IsHidden ? "???" : achievement.Description);
            _equipButton.gameObject.SetActive(false);
        }
    }
}
