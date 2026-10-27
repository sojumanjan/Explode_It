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
        private static readonly List<string> _curseIds = new List<string>();
        private static bool _isSet;
        private static RunMode _mode;
        private static string _weaponId;
        private static string _abilityId;
        private static int _startCheckpoint;

        public static RunMode Mode => _isSet ? _mode : RunMode.Story;
        public static string WeaponId => _isSet ? _weaponId : SaveSystem.Data.SelectedWeapon;
        public static string AbilityId => _isSet ? _abilityId : SaveSystem.Data.SelectedAbility;
        // 고른 아티팩트 ID. 빈 칸(빈 문자열)이 섞여 있을 수 있으니 읽는 쪽에서 건너뛴다.
        public static IReadOnlyList<string> ArtifactIds => _isSet ? _artifactIds : SaveSystem.Data.SelectedArtifacts;
        // 건 저주 ID. 무한 모드에서만 채워진다. 게임 씬을 바로 켜면 스토리라 비어 있다.
        public static IReadOnlyList<string> CurseIds => _curseIds;
        // 스토리를 어디서부터 시작하는지 (저장의 스토리 시작 지점과 같은 번호). 0이면 처음부터. 무한 모드는 늘 0이다.
        // 다시 하기는 같은 설정으로 씬을 다시 불러오므로, 결과 창 버튼이 고른 지점으로 바꿔 놓고 다시 부른다.
        public static int StartCheckpoint => _mode == RunMode.Story ? _startCheckpoint : 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            _isSet = false;
            _mode = RunMode.Story;
            _weaponId = "";
            _abilityId = "";
            _startCheckpoint = 0;
            _artifactIds.Clear();
            _curseIds.Clear();
        }

        public static void Set(RunMode mode, string weaponId, string abilityId, IReadOnlyList<string> artifactIds,
            IReadOnlyList<string> curseIds, int startCheckpoint = 0)
        {
            _isSet = true;
            _mode = mode;
            _weaponId = weaponId ?? "";
            _abilityId = abilityId ?? "";
            _startCheckpoint = Mathf.Max(0, startCheckpoint);
            CopyIds(artifactIds, _artifactIds);
            CopyIds(mode == RunMode.Endless ? curseIds : null, _curseIds);
        }

        public static void SetStartCheckpoint(int checkpoint)
        {
            _startCheckpoint = Mathf.Max(0, checkpoint);
        }

        // 개발자 패널은 지금 설정(같은 목록)을 그대로 다시 넘기므로, 비우기 전에 같은 목록인지 본다.
        private static void CopyIds(IReadOnlyList<string> source, List<string> target)
        {
            if (ReferenceEquals(source, target))
            {
                return;
            }

            target.Clear();
            if (source == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                if (!string.IsNullOrEmpty(source[i]))
                {
                    target.Add(source[i]);
                }
            }
        }
    }
}
