using UnityEngine;

namespace ExplodeIt.Core
{
    // 판 전체 적의 이동 빠르기. 무한 모드에서 웨이브가 쌓이면 스테이지 진행이 올리고, 적은 걸을 때마다 읽는다.
    // 모든 적에 같은 배율을 곱하므로 움직임은 여전히 예측할 수 있다. 공격 예고·투사체 속도에는 곱하지 않는다.
    public static class EnemyTempo
    {
        // 1 = 데이터에 적은 속도 그대로.
        public static float MoveSpeedMultiplier = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            MoveSpeedMultiplier = 1f;
        }
    }
}
