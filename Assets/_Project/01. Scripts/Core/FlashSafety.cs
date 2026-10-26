using UnityEngine;

namespace ExplodeIt.Core
{
    // 설정의 "번쩍임 줄이기". 화면을 크게 덮거나 빠르게 깜빡이는 연출이 이 값을 보고 약하게 낸다.
    // 빛에 민감한 사람을 위한 옵션이라 위험 예고(깜빡임) 자체는 없애지 않고 느리고 옅게만 바꾼다.
    public static class FlashSafety
    {
        // 줄였을 때 번쩍임 불투명도 배율.
        private const float ReducedIntensity = 0.3f;

        public static bool IsReduced => SaveSystem.Data.ReduceFlashes;

        public static Color Apply(Color color)
        {
            if (IsReduced)
            {
                color.a *= ReducedIntensity;
            }

            return color;
        }
    }
}
