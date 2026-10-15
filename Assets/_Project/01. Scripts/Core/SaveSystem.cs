using System;
using UnityEngine;

namespace ExplodeIt.Core
{
    // 저장 창구. 처음 읽을 때 한 번 불러오고, 바꾼 쪽이 알맞은 때 Save를 부른다.
    // 웹 빌드에서도 되도록 PlayerPrefs(웹은 브라우저 저장소)에 JSON 한 덩어리로 넣는다. 파일 입출력은 웹에서 쓸 수 없다.
    // 웹은 탭을 닫을 때 저장이 보장되지 않으므로 Save마다 바로 디스크에 쓴다. 그래서 매 프레임이나 매 처치처럼 자주 부르지 않는다.
    // 오디오 같은 공용 시스템도 쓰므로 Core에 둔다.
    public static class SaveSystem
    {
        private const string SaveKey = "ExplodeIt.Save";
        // 통합 저장 이전에 따로 쓰던 키. 처음 한 번 옮겨 담고 지운다.
        private const string LegacyIntroKey = "ExplodeIt.IntroSeen";
        private const string LegacySfxKey = "Audio.SfxVolume";
        private const string LegacyBgmKey = "Audio.BgmVolume";

        private static SaveData _data;

        public static SaveData Data
        {
            get
            {
                if (_data == null)
                {
                    Load();
                }

                return _data;
            }
        }

        // 도메인 리로드를 끈 플레이모드 설정에서도 지난 세션 값을 들고 있지 않게, 다음 접근 때 다시 불러온다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            _data = null;
        }

        public static void Save()
        {
            if (_data == null)
            {
                return;
            }

            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(_data));
            PlayerPrefs.Save();
        }

        // 개발자 패널의 저장 초기화. 옛 키도 함께 지워 다시 옮겨 오지 않게 한다.
        public static void ResetAll()
        {
            DeleteLegacyKeys();
            _data = new SaveData();
            Save();
        }

        private static void Load()
        {
            _data = new SaveData();
            if (!PlayerPrefs.HasKey(SaveKey))
            {
                MigrateLegacy();
                Save();
                return;
            }

            // 없는 칸은 초기값을 그대로 두도록 새로 만든 객체 위에 덮어쓴다.
            try
            {
                JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString(SaveKey), _data);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[SaveSystem] 저장을 읽지 못해 새로 시작합니다: {exception.Message}");
                _data = new SaveData();
            }
        }

        private static void MigrateLegacy()
        {
            _data.IntroSeen = PlayerPrefs.GetInt(LegacyIntroKey, 0) == 1;
            _data.SfxVolume = PlayerPrefs.GetFloat(LegacySfxKey, 1f);
            _data.BgmVolume = PlayerPrefs.GetFloat(LegacyBgmKey, 1f);
            DeleteLegacyKeys();
        }

        private static void DeleteLegacyKeys()
        {
            PlayerPrefs.DeleteKey(LegacyIntroKey);
            PlayerPrefs.DeleteKey(LegacySfxKey);
            PlayerPrefs.DeleteKey(LegacyBgmKey);
        }
    }
}
