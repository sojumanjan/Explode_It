using System.Collections.Generic;
using DG.Tweening;
using ExplodeIt.Bombs;
using ExplodeIt.Core;
using ExplodeIt.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExplodeIt.UI
{
    // 판이 끝나면(사망·클리어) 결과 창 위에 "이번 판에 새로 해금한 것" 창을 겹쳐 띄운다. 판 도중 알림을 놓쳐도 잊지 않게 한다.
    // 여러 개를 해금했으면 한 장에 하나씩 크게 보여 주고, 버튼을 누를 때마다 다음 장으로 넘긴다. 마지막 장에서 누르면 닫힌다.
    // 판 도중 해금한 저주도 여기서 함께 보여 준다(저주는 판 도중 알림을 띄우지 않는다).
    // 스토리를 처음 깬 판에는 이 모든 것보다 먼저 "무한 모드 해금" 특수 화면을 띄운다.
    // 이번 판에 해금한 게 없으면 뜨지 않는다. 이 컴포넌트는 늘 켜 두고 이벤트를 들으며, 창(_window)만 켜고 끈다.
    public class UnlockSummaryPanel : MonoBehaviour
    {
        private const float PopTime = 0.25f;

        [Tooltip("결과 창이 먼저 뜨고 나서 겹쳐 뜨도록 기다리는 시간 (초, 실제 시간)")]
        [SerializeField, Min(0f)] private float _showDelay = 0.8f;
        [Tooltip("다음 장이 옆에서 밀려 들어오는 시간 (초, 실제 시간)")]
        [SerializeField, Min(0.01f)] private float _slideTime = 0.25f;
        [Tooltip("다음 장이 밀려 들어오기 시작하는 거리 (UI 단위)")]
        [SerializeField] private float _slideOffset = 260f;
        [SerializeField] private GameObject _window;
        [SerializeField] private RectTransform _box;
        // 한 장의 내용(그림·종류·이름). 넘길 때 이것만 밀려 들어온다.
        [SerializeField] private RectTransform _slide;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _kindText;
        [SerializeField] private TMP_Text _nameText;
        // 해금한 것의 설명(예: 블랙홀 범위 +30%).
        [UnityEngine.Serialization.FormerlySerializedAs("_pageText")]
        [SerializeField] private TMP_Text _descriptionText;
        [Header("획득 경로 (어느 업적으로 얻었는지)")]
        [SerializeField] private Image _sourceSticker;
        [SerializeField] private TMP_Text _sourceText;
        // 업적에 스티커 그림이 아직 없을 때 대신 보이는 공용 빈 스티커.
        [SerializeField] private Sprite _fallbackSticker;
        // 판이 끝난 순간부터 이 창이 뜰 때까지 결과 창 버튼을 막는 투명한 막. 먼저 눌러 해금 창을 건너뛰지 않게 한다.
        [SerializeField] private GameObject _inputBlocker;
        [SerializeField] private TMP_Text _buttonLabel;
        [SerializeField] private string _nextText = "다음";
        [SerializeField] private string _closeText = "확인";
        [SerializeField] private SoundData _sound;
        // 장을 넘길 때 소리. 비워 두면 해금 소리를 다시 낸다.
        [SerializeField] private SoundData _pageSound;
        [SerializeField] private string _weaponKind = "새 무기";
        [SerializeField] private string _abilityKind = "새 스킬";
        [SerializeField] private string _upgradeKind = "스킬 강화";
        [SerializeField] private string _artifactKind = "새 아티팩트";
        [SerializeField] private string _curseKind = "새 저주";
        [Tooltip("저주의 획득 경로 문구. {0} = 장착한 저주 수 기준, {1} = 처치 수")]
        [SerializeField] private string _curseSourceFormat = "저주 {0}개 이상을 장착하고 한 판에 {1}마리 처치";

        [Header("무한 모드 해금 (스토리 첫 클리어, 다른 해금보다 먼저)")]
        [SerializeField] private GameObject _modeWindow;
        [SerializeField] private RectTransform _modeBox;
        [SerializeField] private RectTransform _modeTitle;
        [SerializeField] private Image _modeSticker;
        [SerializeField] private TMP_Text _modeAchievementText;
        [Tooltip("스토리 클리어 업적을 땄을 때 업적 줄 문구. {0} = 업적 이름")]
        [SerializeField] private string _modeAchievementFormat = "업적 달성 - {0}";
        [Tooltip("처음 클리어했는데 스토리 클리어 업적이 이번 판에 들어오지 않았을 때(예: 업적만 이미 딴 저장) 업적 줄 대신 보이는 문구")]
        [SerializeField] private string _modeFallbackText = "스토리 모드 클리어";
        // 비워 두면 해금 소리를 낸다.
        [SerializeField] private SoundData _modeSound;

        private readonly List<AchievementData> _completed = new List<AchievementData>();
        private readonly List<CurseData> _curses = new List<CurseData>();
        private readonly List<IUnlockable> _rewards = new List<IUnlockable>();
        // 보상마다 그 보상을 준 업적. _rewards와 같은 순서다. 저주는 업적 없이 열리므로 null이다.
        private readonly List<AchievementData> _sources = new List<AchievementData>();
        private float _showAt = -1f;
        private bool _hasShown;
        private int _index;
        private float _slideBaseX;
        // 이 판을 시작할 때 이미 스토리를 깬 상태였는지. 이번 판이 첫 클리어일 때만 무한 모드 해금 화면을 띄운다.
        private bool _wasStoryCleared;

        private void Awake()
        {
            _window.SetActive(false);
            _modeWindow.SetActive(false);
            _inputBlocker.SetActive(false);
            _slideBaseX = _slide.anchoredPosition.x;
        }

        private void Start()
        {
            _wasStoryCleared = SaveSystem.Data.StoryCleared;
        }

        private void OnEnable()
        {
            ProgressionEvents.AchievementCompleted += OnAchievementCompleted;
            ProgressionEvents.CurseUnlocked += OnCurseUnlocked;
            GameEvents.GameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            ProgressionEvents.AchievementCompleted -= OnAchievementCompleted;
            ProgressionEvents.CurseUnlocked -= OnCurseUnlocked;
            GameEvents.GameStateChanged -= OnGameStateChanged;
            _box.DOKill();
            _slide.DOKill();
        }

        private void OnAchievementCompleted(AchievementData achievement)
        {
            _completed.Add(achievement);
        }

        private void OnCurseUnlocked(CurseData curse)
        {
            _curses.Add(curse);
        }

        private void OnGameStateChanged(GameState previous, GameState current)
        {
            bool isOver = current == GameState.PlayerDead || current == GameState.StageClear;
            // 첫 클리어 여부는 저장이 끝난 뒤(Show)에 다시 확인한다. 여기서는 그럴 가능성만 보고 막을 친다.
            bool mayUnlockMode = current == GameState.StageClear && !_wasStoryCleared;
            if (isOver && !_hasShown && (_completed.Count > 0 || _curses.Count > 0 || mayUnlockMode))
            {
                _showAt = Time.unscaledTime + _showDelay;
                _inputBlocker.SetActive(true);
            }
        }

        private void Update()
        {
            if (_showAt >= 0f && Time.unscaledTime >= _showAt)
            {
                _showAt = -1f;
                Show();
            }
        }

        // 버튼. 다음 장이 있으면 넘기고, 마지막 장이면 닫는다.
        public void Next()
        {
            if (_index + 1 >= _rewards.Count)
            {
                Close();
                return;
            }

            _index++;
            ShowSlide();
            AudioManager.Play(_pageSound != null ? _pageSound : _sound);
            _slide.DOKill();
            _slide.anchoredPosition = new Vector2(_slideBaseX + _slideOffset, _slide.anchoredPosition.y);
            _slide.DOAnchorPosX(_slideBaseX, _slideTime).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(gameObject);
        }

        public void Close()
        {
            _box.DOKill();
            _slide.DOKill();
            _modeBox.DOKill();
            _modeTitle.DOKill();
            _window.SetActive(false);
            _modeWindow.SetActive(false);
            _inputBlocker.SetActive(false);
        }

        // 무한 모드 해금 화면의 버튼. 다른 해금이 있으면 이어서 보여 주고, 없으면 닫는다.
        public void CloseModeWindow()
        {
            _modeBox.DOKill();
            _modeTitle.DOKill();
            _modeWindow.SetActive(false);
            if (_rewards.Count == 0)
            {
                Close();
                return;
            }

            ShowRewards();
        }

        // 여러 업적을 한꺼번에 따면 달성 순서대로 쌓여 종류가 섞인다. 무기 → 스킬 → 스킬 강화 → 아티팩트 순으로 보여 준다.
        // 같은 종류끼리는 달성 순서를 지키고(삽입 정렬), 보상마다 붙은 획득 경로도 같이 옮긴다. 많아야 몇 장이라 단순하게 둔다.
        private void SortByKind()
        {
            for (int i = 1; i < _rewards.Count; i++)
            {
                IUnlockable reward = _rewards[i];
                AchievementData source = _sources[i];
                int rank = KindRank(reward);
                int j = i - 1;
                while (j >= 0 && KindRank(_rewards[j]) > rank)
                {
                    _rewards[j + 1] = _rewards[j];
                    _sources[j + 1] = _sources[j];
                    j--;
                }

                _rewards[j + 1] = reward;
                _sources[j + 1] = source;
            }
        }

        private static int KindRank(IUnlockable reward)
        {
            if (reward is CurseData)
            {
                return 4;
            }

            if (reward is WeaponData)
            {
                return 0;
            }

            if (reward is AbilityData)
            {
                return 1;
            }

            return reward is AbilityUpgradeData ? 2 : 3;
        }

        private void Show()
        {
            _hasShown = true;
            _rewards.Clear();
            _sources.Clear();
            for (int i = 0; i < _completed.Count; i++)
            {
                _completed[i].CollectRewards(_rewards);
                while (_sources.Count < _rewards.Count)
                {
                    _sources.Add(_completed[i]);
                }
            }

            for (int i = 0; i < _curses.Count; i++)
            {
                _rewards.Add(_curses[i]);
                _sources.Add(null);
            }

            bool unlocksMode = !_wasStoryCleared && SaveSystem.Data.StoryCleared;
            if (_rewards.Count == 0 && !unlocksMode)
            {
                _inputBlocker.SetActive(false);
                return;
            }

            SortByKind();
            if (unlocksMode)
            {
                ShowModeWindow();
                return;
            }

            ShowRewards();
        }

        private void ShowRewards()
        {
            _window.SetActive(true);
            _index = 0;
            ShowSlide();
            AudioManager.Play(_sound);
            _box.DOKill();
            _box.localScale = Vector3.one * 0.8f;
            _box.DOScale(1f, PopTime).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
        }

        // 스토리를 처음 깬 판에는 무엇보다 먼저 무한 모드가 열렸다는 걸 크게 알린다. 엔딩 편지의 "갚으러 오든지"에 바로 답하는 자리다.
        private void ShowModeWindow()
        {
            AchievementData story = null;
            for (int i = 0; i < _completed.Count; i++)
            {
                if (_completed[i].Condition == AchievementCondition.StoryCleared)
                {
                    story = _completed[i];
                }
            }

            _modeSticker.sprite = story != null && story.Sticker != null ? story.Sticker : _fallbackSticker;
            _modeSticker.enabled = _modeSticker.sprite != null;
            _modeAchievementText.text = story != null ? string.Format(_modeAchievementFormat, story.DisplayName) : _modeFallbackText;

            _modeWindow.SetActive(true);
            AudioManager.Play(_modeSound != null ? _modeSound : _sound);
            _modeBox.DOKill();
            _modeBox.localScale = Vector3.one * 0.6f;
            _modeBox.DOScale(1f, PopTime * 1.6f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
            // 제목은 상자가 다 커진 뒤 한 번 더 쿵 튀어 "특별한 해금"인 걸 드러낸다.
            _modeTitle.DOKill();
            _modeTitle.localScale = Vector3.one;
            _modeTitle.DOPunchScale(Vector3.one * 0.15f, 0.4f, 5).SetDelay(PopTime * 1.6f).SetUpdate(true).SetLink(gameObject);
        }

        private void ShowSlide()
        {
            IUnlockable reward = _rewards[_index];
            _icon.sprite = reward.Icon;
            _icon.enabled = reward.Icon != null;
            _kindText.text = reward is WeaponData ? _weaponKind : reward is AbilityData ? _abilityKind
                : reward is AbilityUpgradeData ? _upgradeKind : reward is CurseData ? _curseKind : _artifactKind;
            // 강화는 이름("블랙홀 범위 +30%")이 설명과 겹치므로 이름 줄을 빼고 설명만 보여 준다.
            bool showsName = !(reward is AbilityUpgradeData);
            _nameText.gameObject.SetActive(showsName);
            _nameText.text = reward.DisplayName;
            _descriptionText.text = reward.Description;
            AchievementData source = _sources[_index];
            if (reward is CurseData curse)
            {
                // 저주는 업적 스티커가 없으므로 경로 그림을 숨기고 해금 조건만 적는다.
                _sourceSticker.enabled = false;
                _sourceText.text = string.Format(_curseSourceFormat, curse.RequiredCurses, curse.UnlockKills);
            }
            else
            {
                _sourceSticker.sprite = source.Sticker != null ? source.Sticker : _fallbackSticker;
                _sourceSticker.enabled = _sourceSticker.sprite != null;
                _sourceText.text = source.Description;
            }
            _buttonLabel.text = _index + 1 < _rewards.Count ? _nextText : _closeText;
        }
    }
}
