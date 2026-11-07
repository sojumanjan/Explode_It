// 씬이 시작될 때 순위표 설정을 LeaderboardService에 넘기는 컴포넌트
using UnityEngine;

namespace ExplodeIt.Core
{
    // AnalyticsBootstrap처럼 Title, Game 씬 모두에 하나씩 둔다. 어느 씬에서 시작해도 설정이 들어가게 한다.
    public class LeaderboardBootstrap : MonoBehaviour
    {
        [SerializeField] private LeaderboardData _data;

        private void Awake()
        {
            LeaderboardService.Configure(_data);
        }
    }
}
