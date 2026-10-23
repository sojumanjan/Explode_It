using UnityEngine;

namespace ExplodeIt.Core
{
    // 플레이 기록 설정을 넘겨 시작한다. 에디터에서 어느 씬부터 켜도 시작되도록 타이틀·게임 씬 모두에 둔다(두 번째부터는 무시된다).
    [DefaultExecutionOrder(-300)]
    public class AnalyticsBootstrap : MonoBehaviour
    {
        [SerializeField] private AnalyticsData _data;

        private void Awake()
        {
            PlayAnalytics.Configure(_data);
        }
    }
}
