using DG.Tweening;
using ExplodeIt.Bombs;
using ExplodeIt.Core;
using ExplodeIt.Progression;
using TMPro;
using UnityEngine;

namespace ExplodeIt.UI
{
    // 메인 메뉴. 책 왼쪽 페이지에 지금 장착한 무기·스킬·아티팩트 칸이 보이고, 칸을 누르면 고르기 페이지로 간다.
    // 오른쪽 페이지에서 모드를 누르면 장착한 그대로 바로 판을 시작한다.
    // 무한 모드는 스토리를 클리어해야 열리고, 열렸어도 아직 구현 전이면 "준비 중"만 알린다.
    public class MainMenuPage : MonoBehaviour, IMenuPage
    {
        // 알림 글이 떠 있는 시간(초)과 잠긴 버튼이 흔들리는 세기(UI 단위).
        private const float NoticeHold = 2f;
        private const float LockShake = 14f;

        [SerializeField] private UnlockCatalog _catalog;
        [SerializeField] private MenuPager _pager;
        [SerializeField] private RunLauncher _launcher;

        [Header("장비 칸")]
        [SerializeField] private ItemCard _weaponSlot;
        [SerializeField] private ItemCard _abilitySlot;
        // 칸 순서대로. 카탈로그의 아티팩트 칸 수보다 많은 칸은 숨긴다.
        [SerializeField] private ItemCard[] _artifactSlots;
        [SerializeField] private EquipSelectPage _select;
        [SerializeField] private CanvasGroup _selectPage;
        [SerializeField] private string _emptySlotText = "빈 칸";
        [SerializeField, TextArea] private string _noArtifactsText = "아티팩트는 준비 중이에요!";

        [Header("무한 모드")]
        [SerializeField] private RectTransform _endlessButton;
        // 스토리 클리어 전 버튼에 붙는 잠김 표시.
        [SerializeField] private GameObject _endlessLock;
        [Tooltip("무한 모드 구현이 끝나면 켠다. 끄면 해금돼도 누를 때 준비 중 알림만 뜬다")]
        [SerializeField] private bool _isEndlessReady;
        [SerializeField, TextArea] private string _endlessLockedText = "스토리 모드를 클리어하면 열려요!";
        [SerializeField, TextArea] private string _endlessNotReadyText = "무한 모드는 준비 중이에요!";

        [Header("승천")]
        // 무한 모드에 거는 저주·난이도 칸. 무한 모드처럼 스토리를 클리어해야 열린다.
        [SerializeField] private RectTransform _ascensionSlot;
        [SerializeField] private GameObject _ascensionLock;
        [SerializeField] private CanvasGroup _cursePage;
        // 장착한 저주 수. 저주가 잠겨 있으면(스토리 미클리어) 숨긴다.
        [SerializeField] private TMP_Text _curseCountText;
        [Tooltip("장착한 저주 수 표시. {0} = 장착 수, {1} = 전체 수 (??? 칸 제외)")]
        [SerializeField] private string _curseCountFormat = "{0} / {1}";
        [SerializeField, TextArea] private string _ascensionLockedText = "스토리 모드를 클리어하면 열려요!";

        [Header("알림")]
        // 잠겼거나 준비 중이라 못 누를 때 소리.
        [SerializeField] private SoundData _errorSound;
        [SerializeField] private CanvasGroup _notice;
        [SerializeField] private TMP_Text _noticeText;

        private float _noticeUntil;

        public void OnPageOpened()
        {
            _endlessLock.SetActive(!SaveSystem.Data.StoryCleared);
            _ascensionLock.SetActive(!SaveSystem.Data.StoryCleared);
            _curseCountText.gameObject.SetActive(SaveSystem.Data.StoryCleared);
            _curseCountText.SetText(_curseCountFormat, _catalog.EquippedCurseCount, _catalog.CurseCountWithoutKing);
            _notice.alpha = 0f;
            _noticeUntil = 0f;
            RefreshSlots();
        }

        public void OnPageClosed()
        {
        }

        private void Awake()
        {
            for (int i = 0; i < _artifactSlots.Length; i++)
            {
                _artifactSlots[i].gameObject.SetActive(i < _catalog.ArtifactSlots);
            }
        }

        private void Update()
        {
            if (_noticeUntil > 0f && Time.unscaledTime >= _noticeUntil)
            {
                _noticeUntil = 0f;
                _notice.DOFade(0f, 0.3f).SetUpdate(true).SetLink(gameObject);
            }
        }

        public void PlayStory()
        {
            BeginRun(RunMode.Story);
        }

        public void PlayEndless()
        {
            if (!SaveSystem.Data.StoryCleared)
            {
                ShowNotice(_endlessLockedText);
                _endlessButton.DOKill(true);
                _endlessButton.DOShakeAnchorPos(0.35f, new Vector2(LockShake, 0f), 20).SetUpdate(true).SetLink(gameObject);
                return;
            }

            if (!_isEndlessReady)
            {
                ShowNotice(_endlessNotReadyText);
                return;
            }

            BeginRun(RunMode.Endless);
        }

        // 승천 칸 버튼. 스토리를 클리어했으면 저주 고르기 페이지로 간다.
        public void OpenAscension()
        {
            if (!SaveSystem.Data.StoryCleared)
            {
                ShowNotice(_ascensionLockedText);
                _ascensionSlot.DOKill(true);
                _ascensionSlot.DOShakeAnchorPos(0.35f, new Vector2(LockShake, 0f), 20).SetUpdate(true).SetLink(gameObject);
                return;
            }

            _pager.Open(_cursePage);
        }

        private void BeginRun(RunMode mode)
        {
            WeaponData weapon = _catalog.FindUsableWeapon(SaveSystem.Data.SelectedWeapon);
            AbilityData ability = _catalog.FindUsableAbility(SaveSystem.Data.SelectedAbility);
            _launcher.Launch(mode, weapon != null ? weapon.Id : "", ability != null ? ability.Id : "", SaveSystem.Data.SelectedArtifacts,
                SaveSystem.Data.EquippedCurses);
        }

        private void RefreshSlots()
        {
            SaveData data = SaveSystem.Data;
            WeaponData weapon = _catalog.FindUsableWeapon(data.SelectedWeapon);
            _weaponSlot.Setup(weapon, weapon != null ? weapon.Icon : null, weapon != null ? weapon.DisplayName : _emptySlotText, false, OnWeaponSlotClicked);
            AbilityData ability = _catalog.FindUsableAbility(data.SelectedAbility);
            _abilitySlot.Setup(ability, ability != null ? ability.Icon : null, ability != null ? ability.DisplayName : _emptySlotText, false, OnAbilitySlotClicked);
            // 칸 안의 것이 아니라 그 칸에서 고를 수 있는 것 중 새것이 있으면 NEW. 첫 판 뒤 어디를 눌러 볼지 알려 준다.
            _weaponSlot.SetNew(HasNew(_catalog.Weapons));
            _abilitySlot.SetNew(HasNew(_catalog.Abilities) || HasNew(_catalog.Upgrades));

            bool hasNewArtifact = HasNew(_catalog.Artifacts);
            for (int i = 0; i < _artifactSlots.Length && i < _catalog.ArtifactSlots; i++)
            {
                ArtifactData artifact = _catalog.FindUsableArtifact(data.GetArtifactInSlot(i));
                _artifactSlots[i].Setup(artifact, artifact != null ? artifact.Icon : null, artifact != null ? artifact.DisplayName : _emptySlotText, false, OnArtifactSlotClicked);
                _artifactSlots[i].SetNew(hasNewArtifact);
            }
        }

        private static bool HasNew(IUnlockable[] items)
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null && items[i].IsUnlocked && SaveSystem.Data.IsNew(items[i].Id))
                {
                    return true;
                }
            }

            return false;
        }

        private void OnWeaponSlotClicked(ItemCard slot)
        {
            _select.PrepareWeapon();
            _pager.Open(_selectPage);
        }

        private void OnAbilitySlotClicked(ItemCard slot)
        {
            _select.PrepareAbility();
            _pager.Open(_selectPage);
        }

        private void OnArtifactSlotClicked(ItemCard slot)
        {
            if (_catalog.Artifacts.Length == 0)
            {
                slot.Shake();
                ShowNotice(_noArtifactsText);
                return;
            }

            _select.PrepareArtifact(System.Array.IndexOf(_artifactSlots, slot));
            _pager.Open(_selectPage);
        }

        private void ShowNotice(string text)
        {
            AudioManager.Play(_errorSound);
            _noticeText.text = text;
            _notice.DOKill();
            _notice.alpha = 1f;
            _notice.transform.localScale = Vector3.one;
            _notice.transform.DOKill();
            _notice.transform.DOPunchScale(Vector3.one * 0.1f, 0.25f, 6).SetUpdate(true).SetLink(gameObject);
            _noticeUntil = Time.unscaledTime + NoticeHold;
        }
    }
}
