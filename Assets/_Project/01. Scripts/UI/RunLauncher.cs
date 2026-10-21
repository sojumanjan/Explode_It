using System.Collections.Generic;
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.UI
{
    // 메뉴에서 판을 시작하는 단 하나의 출구. 이번 판 설정을 넘기고 화면을 덮은 뒤 게임 씬으로 넘어간다.
    // 첫 스토리라면 게임 씬이 열리자마자 오프닝 만화가 이어진다.
    public class RunLauncher : MonoBehaviour
    {
        [SerializeField] private ScreenFader _fader;
        [SerializeField] private SoundData _clickSound;
        // 첫 판은 타이틀에서 책을 거치지 않고 바로 넘어가므로, 책을 펼칠 때와 같은 길이로 천천히 덮는다.
        [Tooltip("첫 판(타이틀에서 바로 시작)일 때 화면을 덮는 시간 (초, 실제 시간)")]
        [SerializeField, Min(0.05f)] private float _firstRunFadeTime = 1.5f;

        private bool _isLaunching;

        public void Launch(RunMode mode, string weaponId, string abilityId, IReadOnlyList<string> artifactIds,
            IReadOnlyList<string> curseIds)
        {
            // 덮이는 동안 버튼을 또 눌러 두 번 넘어가지 않게 한다.
            if (_isLaunching)
            {
                return;
            }

            _isLaunching = true;
            AudioManager.Play(_clickSound);
            RunSetup.Set(mode, weaponId, abilityId, artifactIds, curseIds);
            if (SaveSystem.Data.FirstRunDone)
            {
                _fader.FadeOut(SceneFlow.LoadGame);
            }
            else
            {
                _fader.FadeOut(_firstRunFadeTime, SceneFlow.LoadGame);
            }
        }
    }
}
