using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.UI
{
    // 버튼 OnClick에 연결하는 동작 모음. 버튼마다 스크립트를 만들지 않게 한곳에 둔다.
    public class UIActions : MonoBehaviour
    {
        // 타이틀 씬에는 없으므로 비워 둬도 된다.
        [SerializeField] private GameStateController _gameState;

        public void StartGame()
        {
            SceneFlow.LoadGame();
        }

        public void Restart()
        {
            SceneFlow.LoadGame();
        }

        public void GoToTitle()
        {
            SceneFlow.LoadTitle();
        }

        public void Resume()
        {
            _gameState.Resume();
        }

        public void Quit()
        {
            Application.Quit();
        }
    }
}
