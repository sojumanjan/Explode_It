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
        public const int CurrentVersion = 1;

        // 저장 형식이 크게 바뀌어 옛 값을 옮겨야 할 때 어느 형식인지 알아보는 데 쓴다.
        [SerializeField] private int _version = CurrentVersion;
        [SerializeField] private bool _introSeen;
        // 스토리 클리어. 무한 모드 해금 조건이다.
        [SerializeField] private bool _storyCleared;
        // 지금까지 모든 판에서 처치한 수 (업적용).
        [SerializeField] private int _totalKills;
        [SerializeField] private int _bestEndlessKills;
        // 스토리 최단 클리어 시간 (초). 0이면 아직 기록이 없다.
        [SerializeField] private float _bestStoryTime;
        // 업적 등으로 해금한 무기 ID. 처음부터 열린 무기는 무기 데이터 쪽 표시로 판단하므로 여기 넣지 않는다.
        [SerializeField] private List<string> _unlockedWeapons = new List<string>();
        [SerializeField] private string _selectedWeapon = "";
        [SerializeField] private float _sfxVolume = 1f;
        [SerializeField] private float _bgmVolume = 1f;

        public int Version => _version;

        public bool IntroSeen
        {
            get => _introSeen;
            set => _introSeen = value;
        }

        public bool StoryCleared
        {
            get => _storyCleared;
            set => _storyCleared = value;
        }

        public int TotalKills
        {
            get => _totalKills;
            set => _totalKills = Mathf.Max(0, value);
        }

        public int BestEndlessKills => _bestEndlessKills;
        public float BestStoryTime => _bestStoryTime;

        public string SelectedWeapon
        {
            get => _selectedWeapon;
            set => _selectedWeapon = value ?? "";
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

        public bool TryRecordStoryTime(float seconds)
        {
            if (seconds <= 0f || (_bestStoryTime > 0f && seconds >= _bestStoryTime))
            {
                return false;
            }

            _bestStoryTime = seconds;
            return true;
        }

        public bool IsWeaponUnlocked(string id)
        {
            return !string.IsNullOrEmpty(id) && _unlockedWeapons.Contains(id);
        }

        // 새로 해금했으면 true. 이미 열려 있으면 false라 해금 알림을 두 번 띄우지 않는다.
        public bool UnlockWeapon(string id)
        {
            if (string.IsNullOrEmpty(id) || _unlockedWeapons.Contains(id))
            {
                return false;
            }

            _unlockedWeapons.Add(id);
            return true;
        }
    }
}
