using UnityEngine;

namespace ExplodeIt.Core
{
    public class GameStateController : MonoBehaviour
    {
        public GameState Current { get; private set; } = GameState.None;

        private void OnEnable()
        {
            GameEvents.PlayerDied += OnPlayerDied;
        }

        private void OnDisable()
        {
            GameEvents.PlayerDied -= OnPlayerDied;
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
