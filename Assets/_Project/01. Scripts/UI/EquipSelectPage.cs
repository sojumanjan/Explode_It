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
        [SerializeField] private TMP_Text _equipLabel;
        // 아티팩트 칸에 뭔가 끼워져 있을 때만 보인다.
        [SerializeField] private GameObject _unequipButton;
        [SerializeField] private string _equipText = "장착";
        [SerializeField] private string _equippedText = "장착 중";
        [SerializeField, TextArea] private string _noUnlockText = "아직 해금 방법이 없어요.";
        // 잠긴 카드에는 업적 이름 없이 해금 조건만 보여 준다.
        [SerializeField, TextArea] private string _unlockFormat = "<b>해금 조건</b>\n{0}";

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
            for (int k = 0; k < _cards.Length; k++)
            {
                for (int i = 0; i < _cards[k].Count; i++)
                {
                    _cards[k][i].gameObject.SetActive(k == (int)_kind);
                }
            }

            _empty.SetActive(_items[(int)_kind].Count == 0);
            _preview = Equipped();
            Refresh();
        }

        public void OnPageClosed()
        {
        }

        // 장착 버튼. 다른 아티팩트 칸에 이미 끼운 것이면 두 칸을 맞바꾼다.
        public void Equip()
        {
            if (_preview == null)
            {
                return;
            }

            SaveData data = SaveSystem.Data;
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

        // 빼기 버튼 (아티팩트 칸만).
        public void Unequip()
        {
            SaveSystem.Data.SetArtifactInSlot(_slot, "");
            SaveSystem.Save();
            _pager.Back();
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
            AddItems(Kind.Ability, _catalog.Abilities);
            AddItems(Kind.Artifact, _catalog.Artifacts);
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
                cards[i].SetSelected(item == _preview);
            }

            ShowPreview(_preview);
            bool isEquipped = _preview != null && _preview == equipped;
            _equipButton.gameObject.SetActive(_preview != null);
            _equipButton.interactable = !isEquipped;
            _equipLabel.text = isEquipped ? _equippedText : _equipText;
            _unequipButton.SetActive(_kind == Kind.Artifact && equipped != null);
        }

        private void OnCardClicked(ItemCard card)
        {
            AudioManager.Play(_clickSound);
            IUnlockable item = (IUnlockable)card.Item;
            if (card.IsLocked)
            {
                card.Shake();
                ShowLocked(item);
                return;
            }

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
