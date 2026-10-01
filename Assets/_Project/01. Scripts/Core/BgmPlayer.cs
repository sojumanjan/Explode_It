using UnityEngine;

namespace ExplodeIt.Core
{
    // 씬에 두면 시작할 때 이 씬의 배경음으로 바꾼다. 이미 같은 곡이 나오고 있으면 그대로 둔다.
    public class BgmPlayer : MonoBehaviour
    {
        [SerializeField] private BgmData _bgm;

        private void Start()
        {
            AudioManager.PlayBgm(_bgm);
        }
    }
}
