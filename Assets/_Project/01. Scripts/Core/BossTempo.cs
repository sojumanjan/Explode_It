using UnityEngine;

namespace ExplodeIt.Core
{
    // 무한 모드에서 바퀴마다 빨라지는 보스 행동. 스테이지 진행이 보스를 내보내기 전에 정하고, 보스는 시간·속도 값을 쓸 때 여기를 거친다.
    // 반응은 어렵게 하되 아예 못 피하게 되면 안 되므로, 예고(경고 표시가 떠 있는 시간)는 행동보다 덜 줄이고,
    // 시간은 원래의 일정 비율 밑으로, 속도는 일정 배율 위로 가지 않게 막는다. 스토리 모드는 늘 1배다.
    public static class BossTempo
    {
        private static float _actionMinRatio = 1f;
        private static float _telegraphMinRatio = 1f;
        private static float _speedMaxMultiplier = 1f;

        // 행동 배율. 클수록 빠르다(쿨타임·간격·이동 시간이 줄고 속도가 는다).
        public static float Action { get; private set; } = 1f;
        // 예고 배율. 클수록 경고가 짧다.
        public static float Telegraph { get; private set; } = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            Set(1f, 1f, 1f, 1f, 1f);
        }

        public static void Set(float action, float telegraph, float actionMinRatio, float telegraphMinRatio, float speedMaxMultiplier)
        {
            Action = Mathf.Max(1f, action);
            Telegraph = Mathf.Max(1f, telegraph);
            _actionMinRatio = actionMinRatio;
            _telegraphMinRatio = telegraphMinRatio;
            _speedMaxMultiplier = Mathf.Max(1f, speedMaxMultiplier);
        }

        // 쿨타임·간격·이동 시간처럼 짧아질수록 몰아치는 시간.
        public static float ActionTime(float time)
        {
            return time * Mathf.Max(1f / Action, _actionMinRatio);
        }

        // 경고 표시가 떠 있는 시간. 이게 너무 짧으면 반응이 아니라 운이 된다.
        public static float TelegraphTime(float time)
        {
            return time * Mathf.Max(1f / Telegraph, _telegraphMinRatio);
        }

        // 이동·회전·투사체 속도.
        public static float Speed(float speed)
        {
            return speed * Mathf.Min(Action, _speedMaxMultiplier);
        }
    }
}
