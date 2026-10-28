using System;
using System.Collections.Generic;
using UnityEngine;

namespace ExplodeIt.Core
{
    // 한 사람의 저장 내용 전부. JSON 한 덩어리로 저장된다.
    // 칸을 새로 늘려도 예전 저장에는 그 칸이 없을 뿐이라 초기값으로 채워진다. 칸 이름을 바꾸면 예전 값을 잃으니 바꾸지 않는다.
    [Serializable]
    public class SaveData
    {
        // 2: 첫 판 결과 창을 봤는지(_firstRunDone)를 따로 둔다.
        public const int CurrentVersion = 2;

        // 저장 형식이 크게 바뀌어 옛 값을 옮겨야 할 때 어느 형식인지 알아보는 데 쓴다.
        [SerializeField] private int _version = CurrentVersion;
        [SerializeField] private bool _introSeen;
        // 첫 판 결과 창을 봤는지. 보기 전에는 타이틀의 시작하기가 책 대신 곧장 첫 판으로 들어간다.
        // 결과 창을 보기 전에 강제로 끄면 꺼진 채로 남아 다음에도 첫 판부터 한다.
        [SerializeField] private bool _firstRunDone;
        // 스토리 클리어. 무한 모드 해금 조건이다.
        [SerializeField] private bool _storyCleared;
        // 스토리에서 가장 멀리 연 시작 지점. 1~3: 그 번째 보스를 쓰러뜨려 다음 웨이브부터, 웨이브 수(4): 최종 보스전부터. 0이면 없음.
        [SerializeField] private int _storyCheckpoint;
        // 지금까지 모든 판에서 처치한 수 (업적용).
        [SerializeField] private int _totalKills;
        [SerializeField] private int _bestEndlessKills;
        // 스토리 최단 클리어 시간 (초). 0이면 아직 기록이 없다.
        [SerializeField] private float _bestStoryTime;
        // 모드별 가장 오래 버틴 시간 (초)과 스토리 최고 처치 수. 결과 화면의 최고 기록 칸에 쓴다.
        [SerializeField] private float _bestStorySurvival;
        [SerializeField] private float _bestEndlessSurvival;
        [SerializeField] private int _bestStoryKills;
        // 업적 등으로 해금한 무기 ID. 처음부터 열린 무기는 무기 데이터 쪽 표시로 판단하므로 여기 넣지 않는다.
        [SerializeField] private List<string> _unlockedWeapons = new List<string>();
        [SerializeField] private string _selectedWeapon = "";
        [SerializeField] private List<string> _unlockedAbilities = new List<string>();
        [SerializeField] private List<string> _unlockedUpgrades = new List<string>();
        // 해금한 강화 중 장착한 것. 장착한 강화만 판에서 효과가 난다.
        [SerializeField] private List<string> _equippedUpgrades = new List<string>();
        // 이름 붙은 누적 수(스킬별 누적 처치 등). 업적 조건이 읽는다.
        [SerializeField] private List<SaveStat> _stats = new List<SaveStat>();
        [SerializeField] private string _selectedAbility = "";
        [SerializeField] private List<string> _unlockedArtifacts = new List<string>();
        // 칸 순서대로 장착한 아티팩트 ID. 빈 칸은 빈 문자열이다.
        [SerializeField] private List<string> _selectedArtifacts = new List<string>();
        [SerializeField] private List<string> _completedAchievements = new List<string>();
        // 해금했지만 아직 고르기 화면에서 눌러 보지 않은 무기·스킬·강화·아티팩트 ID. 메뉴에 NEW 표시를 띄운다.
        // 처음부터 열린 것과 이 칸이 생기기 전에 해금한 것은 들어 있지 않아 NEW가 뜨지 않는다.
        [SerializeField] private List<string> _newItems = new List<string>();
        // 승천 저주. 처음부터 열린 저주는 저주 데이터 쪽 표시로 판단하므로 해금 목록에 넣지 않는다.
        [SerializeField] private List<string> _unlockedCurses = new List<string>();
        [SerializeField] private List<string> _equippedCurses = new List<string>();
        // 모든 저주를 끼고 각성한 왕을 쓰러뜨려 진짜 엔딩을 봤는지.
        [SerializeField] private bool _trueEndingSeen;
        [SerializeField] private float _sfxVolume = 1f;
        [SerializeField] private float _bgmVolume = 1f;
        // 화면 흔들림이 어지러운 사람을 위해 끌 수 있게 한다.
        [SerializeField] private bool _shakeEnabled = true;
        // 빛에 민감한 사람을 위해 화면 번쩍임·빠른 깜빡임을 약하게 한다.
        [SerializeField] private bool _reduceFlashes;
        // 창 밖(웹 페이지의 다른 곳)을 누르면 키 입력이 끊겨 그대로 맞으므로, 기본으로 일시정지한다.
        [SerializeField] private bool _autoPauseEnabled = true;
        // 설정에서 고른 언어 코드(ko, en). 비어 있으면 아직 고르지 않은 것이라 시스템 언어를 따른다.
        [SerializeField] private string _language = "";
        // 플레이 기록 수집. 처음 켤 때 한 번 묻고(물었는지), 답(동의했는지)은 설정에서 바꿀 수 있다.
        [SerializeField] private bool _analyticsAsked;
        [SerializeField] private bool _analyticsEnabled;
        // 지금까지 시작한 판 수와 판에서 보낸 시간 (초). 플레이 기록에서 "이 사람이 얼마나 해 봤는지"를 붙이는 데 쓴다.
        [SerializeField] private int _runCount;
        [SerializeField] private float _totalPlaySeconds;

        public int Version => _version;

        public bool IntroSeen
        {
            get => _introSeen;
            set => _introSeen = value;
        }

        public bool FirstRunDone
        {
            get => _firstRunDone;
            set => _firstRunDone = value;
        }

        // 옛 형식으로 저장된 값을 지금 형식으로 옮긴다. 불러온 직후 한 번 부른다. 옮겼으면 true.
        public bool UpgradeToCurrent()
        {
            if (_version >= CurrentVersion)
            {
                return false;
            }

            // 첫 판 표시가 없던 저장: 이미 인트로를 봤거나 스토리를 깬 사람은 첫 판을 끝낸 것으로 본다.
            if (_version < 2)
            {
                _firstRunDone = _introSeen || _storyCleared;
            }

            _version = CurrentVersion;
            return true;
        }

        public bool StoryCleared
        {
            get => _storyCleared;
            set => _storyCleared = value;
        }

        public int StoryCheckpoint => _storyCheckpoint;

        // 더 먼 시작 지점을 열었으면 true. 이미 연 지점이나 그보다 앞이면 그대로 둔다.
        public bool UnlockStoryCheckpoint(int checkpoint)
        {
            if (checkpoint <= _storyCheckpoint)
            {
                return false;
            }

            _storyCheckpoint = checkpoint;
            return true;
        }

        public int TotalKills
        {
            get => _totalKills;
            set => _totalKills = Mathf.Max(0, value);
        }

        public int BestEndlessKills => _bestEndlessKills;
        public float BestStoryTime => _bestStoryTime;
        public int BestStoryKills => _bestStoryKills;

        public float BestSurvival(bool isStoryMode) => isStoryMode ? _bestStorySurvival : _bestEndlessSurvival;
        public int BestKills(bool isStoryMode) => isStoryMode ? _bestStoryKills : _bestEndlessKills;

        public string SelectedWeapon
        {
            get => _selectedWeapon;
            set => _selectedWeapon = value ?? "";
        }

        public string SelectedAbility
        {
            get => _selectedAbility;
            set => _selectedAbility = value ?? "";
        }

        public float SfxVolume
        {
            get => _sfxVolume;
            set => _sfxVolume = Mathf.Clamp01(value);
        }

        public float BgmVolume
        {
            get => _bgmVolume;
            set => _bgmVolume = Mathf.Clamp01(value);
        }

        public bool ShakeEnabled
        {
            get => _shakeEnabled;
            set => _shakeEnabled = value;
        }

        public bool ReduceFlashes
        {
            get => _reduceFlashes;
            set => _reduceFlashes = value;
        }

        public bool AutoPauseEnabled
        {
            get => _autoPauseEnabled;
            set => _autoPauseEnabled = value;
        }

        public string Language
        {
            get => _language;
            set => _language = value ?? "";
        }

        public IReadOnlyList<string> SelectedArtifacts => _selectedArtifacts;
        public IReadOnlyList<string> EquippedCurses => _equippedCurses;

        public bool AnalyticsAsked
        {
            get => _analyticsAsked;
            set => _analyticsAsked = value;
        }

        public bool AnalyticsEnabled
        {
            get => _analyticsEnabled;
            set => _analyticsEnabled = value;
        }

        public int RunCount
        {
            get => _runCount;
            set => _runCount = Mathf.Max(0, value);
        }

        public float TotalPlaySeconds
        {
            get => _totalPlaySeconds;
            set => _totalPlaySeconds = Mathf.Max(0f, value);
        }

        public int EquippedUpgradeCount => _equippedUpgrades.Count;

        // 업적 등으로 해금한 무기·스킬·강화·아티팩트·저주 수. 처음부터 열린 것은 세지 않는다.
        public int UnlockedCount => _unlockedWeapons.Count + _unlockedAbilities.Count + _unlockedUpgrades.Count
            + _unlockedArtifacts.Count + _unlockedCurses.Count;

        public bool TrueEndingSeen
        {
            get => _trueEndingSeen;
            set => _trueEndingSeen = value;
        }

        // 기록을 넘었으면 갱신하고 true를 돌려준다. 결과 화면에서 "신기록"을 띄우는 데 쓴다.
        public bool TryRecordEndlessKills(int kills)
        {
            if (kills <= _bestEndlessKills)
            {
                return false;
            }

            _bestEndlessKills = kills;
            return true;
        }

        public bool TryRecordStoryKills(int kills)
        {
            if (kills <= _bestStoryKills)
            {
                return false;
            }

            _bestStoryKills = kills;
            return true;
        }

        public bool TryRecordSurvival(bool isStoryMode, float seconds)
        {
            float best = BestSurvival(isStoryMode);
            if (seconds <= best)
            {
                return false;
            }

            if (isStoryMode)
            {
                _bestStorySurvival = seconds;
            }
            else
            {
                _bestEndlessSurvival = seconds;
            }

            return true;
        }

        public bool TryRecordStoryTime(float seconds)
        {
            if (seconds <= 0f || (_bestStoryTime > 0f && seconds >= _bestStoryTime))
            {
                return false;
            }

            _bestStoryTime = seconds;
            return true;
        }

        public bool IsWeaponUnlocked(string id) => Has(_unlockedWeapons, id);
        public bool IsArtifactUnlocked(string id) => Has(_unlockedArtifacts, id);
        public bool IsAbilityUnlocked(string id) => Has(_unlockedAbilities, id);
        public bool IsUpgradeUnlocked(string id) => Has(_unlockedUpgrades, id);
        public bool IsUpgradeEquipped(string id) => Has(_equippedUpgrades, id);
        public bool IsAchievementCompleted(string id) => Has(_completedAchievements, id);
        public bool IsCurseUnlocked(string id) => Has(_unlockedCurses, id);
        public bool IsCurseEquipped(string id) => Has(_equippedCurses, id);

        // 새로 해금·달성했으면 true. 이미 있으면 false라 알림을 두 번 띄우지 않는다.
        public bool UnlockWeapon(string id) => UnlockNew(_unlockedWeapons, id);
        public bool UnlockArtifact(string id) => UnlockNew(_unlockedArtifacts, id);
        public bool UnlockAbility(string id) => UnlockNew(_unlockedAbilities, id);
        public bool UnlockUpgrade(string id) => UnlockNew(_unlockedUpgrades, id);

        public bool IsNew(string id) => Has(_newItems, id);

        // 고르기 화면에서 눌러 봤으면 NEW를 끈다. 꺼졌으면 true라 그때만 저장하면 된다.
        public bool MarkSeen(string id) => _newItems.Remove(id);
        public bool UnlockCurse(string id) => TryAdd(_unlockedCurses, id);

        public void SetCurseEquipped(string id, bool isEquipped)
        {
            if (isEquipped)
            {
                TryAdd(_equippedCurses, id);
                return;
            }

            _equippedCurses.Remove(id);
        }

        public void SetUpgradeEquipped(string id, bool isEquipped)
        {
            if (isEquipped)
            {
                TryAdd(_equippedUpgrades, id);
                return;
            }

            _equippedUpgrades.Remove(id);
        }

        public int GetStat(string id)
        {
            for (int i = 0; i < _stats.Count; i++)
            {
                if (_stats[i].Id == id)
                {
                    return _stats[i].Value;
                }
            }

            return 0;
        }

        public void AddStat(string id, int amount)
        {
            for (int i = 0; i < _stats.Count; i++)
            {
                if (_stats[i].Id == id)
                {
                    _stats[i].Value += amount;
                    return;
                }
            }

            var stat = new SaveStat(id);
            stat.Value = amount;
            _stats.Add(stat);
        }
        public bool CompleteAchievement(string id) => TryAdd(_completedAchievements, id);

        // 업적 달성과 그 보상 해금만 지운다(개발자 패널). 오프닝·기록·설정은 남긴다.
        public void ResetAchievements()
        {
            _completedAchievements.Clear();
            _newItems.Clear();
            _unlockedWeapons.Clear();
            _unlockedArtifacts.Clear();
            _unlockedAbilities.Clear();
            _unlockedUpgrades.Clear();
            _equippedUpgrades.Clear();
            _stats.Clear();
            _selectedWeapon = "";
            _selectedAbility = "";
            _selectedArtifacts.Clear();
            _unlockedCurses.Clear();
            _equippedCurses.Clear();
            _trueEndingSeen = false;
        }

        public string GetArtifactInSlot(int slot)
        {
            return slot >= 0 && slot < _selectedArtifacts.Count ? _selectedArtifacts[slot] : "";
        }

        // 칸 자리를 지킨다. 2번 칸에만 끼워도 1번 칸으로 당겨지지 않는다.
        public void SetArtifactInSlot(int slot, string id)
        {
            while (_selectedArtifacts.Count <= slot)
            {
                _selectedArtifacts.Add("");
            }

            _selectedArtifacts[slot] = id ?? "";
        }

        private static bool Has(List<string> list, string id)
        {
            return !string.IsNullOrEmpty(id) && list.Contains(id);
        }

        private bool UnlockNew(List<string> list, string id)
        {
            if (!TryAdd(list, id))
            {
                return false;
            }

            TryAdd(_newItems, id);
            return true;
        }

        private static bool TryAdd(List<string> list, string id)
        {
            if (string.IsNullOrEmpty(id) || list.Contains(id))
            {
                return false;
            }

            list.Add(id);
            return true;
        }
    }
}
