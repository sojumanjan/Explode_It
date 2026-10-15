using System.Collections.Generic;
using UnityEngine;

namespace ExplodeIt.Core
{
    // 메뉴에서 고른 이번 판 설정. 메뉴 씬이 채우고 게임 씬이 판을 시작할 때 읽는다.
    // 씬을 넘어 값을 넘기는 용도라 정적으로 둔다. 재시작(게임 씬 다시 불러오기)해도 같은 설정으로 다시 한다.
    // 게임 씬을 에디터에서 바로 켜면 아무도 채우지 않으므로, 스토리 모드 + 저장에 남은 선택으로 시작한다.
    public static class RunSetup
    {
        private static readonly List<string> _artifactIds = new List<string>();
        private static bool _isSet;
        private static RunMode _mode;
        private static string _weaponId;

        public static RunMode Mode => _isSet ? _mode : RunMode.Story;
        public static string WeaponId => _isSet ? _weaponId : SaveSystem.Data.SelectedWeapon;
        // 고른 아티팩트 ID. 빈 칸(빈 문자열)이 섞여 있을 수 있으니 읽는 쪽에서 건너뛴다.
        public static IReadOnlyList<string> ArtifactIds => _isSet ? _artifactIds : SaveSystem.Data.SelectedArtifacts;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            _isSet = false;
            _mode = RunMode.Story;
            _weaponId = "";
            _artifactIds.Clear();
        }

        public static void Set(RunMode mode, string weaponId, IReadOnlyList<string> artifactIds)
        {
            _isSet = true;
            _mode = mode;
            _weaponId = weaponId ?? "";
            _artifactIds.Clear();
            if (artifactIds == null)
            {
                return;
            }

            for (int i = 0; i < artifactIds.Count; i++)
            {
                if (!string.IsNullOrEmpty(artifactIds[i]))
                {
                    _artifactIds.Add(artifactIds[i]);
                }
            }
        }
    }
}
