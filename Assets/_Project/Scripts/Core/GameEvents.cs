using System;
using UnityEngine;

namespace ExplodeIt.Core
{
    // 시스템끼리 직접 참조하지 않도록 전투·진행 이벤트를 한곳에서 발행한다.
    // 이벤트는 필요해지는 단계에서 하나씩 추가한다.
    public static class GameEvents
    {
        public static event Action<GameState, GameState> GameStateChanged;

        public static void RaiseGameStateChanged(GameState previous, GameState current)
        {
            GameStateChanged?.Invoke(previous, current);
        }

        // 도메인 리로드를 끈 플레이모드 설정에서도 이전 세션의 구독자가 남지 않게 한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSubscribers()
        {
            GameStateChanged = null;
        }
    }
}
