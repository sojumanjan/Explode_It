using UnityEngine;

namespace ExplodeIt.Core
{
    // 이번 판에 장착한 아티팩트가 바꾸는 값. 판이 시작될 때 아티팩트 적용기가 한 번 채우고, 각 시스템은 여기서 읽기만 한다.
    // 시스템끼리 아티팩트를 직접 알지 않게, 숫자만 모아 두는 칸이다. 아티팩트가 없으면 모두 기본값(효과 없음)이다.
    public static class RunModifiers
    {
        // 장전된 폭탄이 가득일 때 이동속도에 더하는 비율 (0.1 = +10%).
        public static float MoveSpeedBonusWhenBombsFull;
        // 대시가 끝난 직후 이어 구를 수 있는 횟수.
        public static int ExtraDodges;
        // 대시 쿨타임 배율.
        public static float DodgeCooldownMultiplier = 1f;
        // 0보다 크면 대시 중 이 반경(유닛) 안에 닿은 적을 처치한다.
        public static float DodgeKillRadius;
        public static bool CanThrowWhileDodging;
        public static int ExtraHealth;
        // 웨이브 적이 나오는 간격 배율 (0.8 = 20% 빨리).
        public static float SpawnIntervalMultiplier = 1f;
        public static int KillsToChargeReduction;
        public static int ExtraAbilityCharges;
        // 0보다 크면 이동하지 않은 지 이 시간(초)이 지나 게이지가 차기 시작한다.
        public static float StillChargeDelay;
        public static float StillChargeInterval;
        // 0보다 크면 스킬 한 번으로 이만큼 이상 잡았을 때 게이지를 돌려준다.
        public static int AbilityRefundMinKills;
        public static int AbilityRefundAmount;
        // 스킬을 쓴 직후 이동속도에 더하는 비율과 시간(초).
        public static float MoveSpeedBonusAfterAbility;
        public static float MoveSpeedBonusAfterAbilityDuration;

        // 도메인 리로드를 끈 채 플레이를 다시 눌러도 지난 판 값이 남지 않게 한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            MoveSpeedBonusWhenBombsFull = 0f;
            ExtraDodges = 0;
            DodgeCooldownMultiplier = 1f;
            DodgeKillRadius = 0f;
            CanThrowWhileDodging = false;
            ExtraHealth = 0;
            SpawnIntervalMultiplier = 1f;
            KillsToChargeReduction = 0;
            ExtraAbilityCharges = 0;
            StillChargeDelay = 0f;
            StillChargeInterval = 0f;
            AbilityRefundMinKills = 0;
            AbilityRefundAmount = 0;
            MoveSpeedBonusAfterAbility = 0f;
            MoveSpeedBonusAfterAbilityDuration = 0f;
        }
    }
}
