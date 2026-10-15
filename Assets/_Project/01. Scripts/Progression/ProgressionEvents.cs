using System;
using UnityEngine;

namespace ExplodeIt.Progression
{
    // 업적·해금 쪽 이벤트. 업적 데이터 타입을 그대로 넘기려고 Core의 GameEvents와 따로 둔다.
    public static class ProgressionEvents
    {
        // 업적을 달성했다(보상 해금과 저장은 이미 끝난 뒤다). 알림 UI가 듣는다.
        public static event Action<AchievementData> AchievementCompleted;

        public static void RaiseAchievementCompleted(AchievementData achievement)
        {
            AchievementCompleted?.Invoke(achievement);
        }

        // 도메인 리로드를 끈 플레이모드 설정에서도 이전 세션의 구독자가 남지 않게 한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSubscribers()
        {
            AchievementCompleted = null;
        }
    }
}
