using UnityEngine;

namespace ExplodeIt.Core
{
    public class GameStateController : MonoBehaviour
    {
        public GameState Current { get; private set; } = GameState.None;

        private void Start()
        {
            ChangeState(GameState.Playing);
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
