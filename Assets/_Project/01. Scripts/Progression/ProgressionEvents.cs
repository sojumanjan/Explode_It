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

        // 스킬 발동 시작·끝(블랙홀이 터져 사라질 때, 다발폭탄이 모두 터졌을 때, 순간폭탄 시간이 끝났을 때).
        // 스킬 강화 업적이 "한 번 발동하는 동안"을 센다.
        public static event Action<AbilityData> AbilityStarted;
        public static event Action<AbilityData> AbilityEnded;

        public static void RaiseAbilityStarted(AbilityData ability)
        {
            AbilityStarted?.Invoke(ability);
        }

        public static void RaiseAbilityEnded(AbilityData ability)
        {
            AbilityEnded?.Invoke(ability);
        }

        // 도메인 리로드를 끈 플레이모드 설정에서도 이전 세션의 구독자가 남지 않게 한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSubscribers()
        {
            AchievementCompleted = null;
            AbilityStarted = null;
            AbilityEnded = null;
        }
    }
}
