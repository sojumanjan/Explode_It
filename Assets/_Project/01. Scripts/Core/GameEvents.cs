using System;
using UnityEngine;

namespace ExplodeIt.Core
{
    // 시스템끼리 직접 참조하지 않도록 전투·진행 이벤트를 한곳에서 발행한다.
    // 이벤트는 필요해지는 단계에서 하나씩 추가한다.
    public static class GameEvents
    {
        public static event Action<GameState, GameState> GameStateChanged;

        // 위치, 반경, 범위 안 대상 수. 다중 처치 업적·재화, 잔불 장판 등이 폭탄 코드를 몰라도 되게 한다.
        public static event Action<Vector2, float, int> BombExploded;
        public static event Action PlayerDied;
        // 처치 위치. 재화 드롭, 처치 수 업적, 이펙트가 구독한다.
        public static event Action<Vector2> EnemyKilled;
        // 현재 충전량, 발동에 필요한 양. 게이지 UI가 능력 코드를 몰라도 되게 한다.
        public static event Action<int, int> AbilityChargeChanged;

        public static void RaiseAbilityChargeChanged(int current, int required)
        {
            AbilityChargeChanged?.Invoke(current, required);
        }

        public static void RaiseGameStateChanged(GameState previous, GameState current)
        {
            GameStateChanged?.Invoke(previous, current);
        }

        public static void RaiseBombExploded(Vector2 position, float radius, int hitCount)
        {
            BombExploded?.Invoke(position, radius, hitCount);
        }

        public static void RaisePlayerDied()
        {
            PlayerDied?.Invoke();
        }

        public static void RaiseEnemyKilled(Vector2 position)
        {
            EnemyKilled?.Invoke(position);
        }

        // 도메인 리로드를 끈 플레이모드 설정에서도 이전 세션의 구독자가 남지 않게 한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSubscribers()
        {
            GameStateChanged = null;
            BombExploded = null;
            PlayerDied = null;
            EnemyKilled = null;
            AbilityChargeChanged = null;
        }
    }
}
