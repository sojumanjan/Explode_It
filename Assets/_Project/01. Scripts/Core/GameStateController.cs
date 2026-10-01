using UnityEngine;

namespace ExplodeIt.Core
{
    public class GameStateController : MonoBehaviour
    {
        public GameState Current { get; private set; } = GameState.None;

        private void OnEnable()
        {
            GameEvents.PlayerDied += OnPlayerDied;
            GameEvents.StageCleared += OnStageCleared;
        }

        private void OnDisable()
        {
            GameEvents.PlayerDied -= OnPlayerDied;
            GameEvents.StageCleared -= OnStageCleared;
        }

        // 마지막 적과 같은 순간에 죽었다면 사망이 우선한다.
        private void OnStageCleared()
        {
            if (Current == GameState.Playing)
            {
                ChangeState(GameState.StageClear);
            }
        }

        private void Start()
        {
            ChangeState(GameState.Playing);
        }

        private void OnPlayerDied()
        {
            ChangeState(GameState.PlayerDead);
        }

        public void ChangeState(GameState next)
        {
            if (next == Current)
            {
                return;
            }

            GameState previous = Current;
            Current = next;
            GameEvents.RaiseGameStateChanged(previous, next);
        }
    }
}
