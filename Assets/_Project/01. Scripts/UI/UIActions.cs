using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.UI
{
    // 버튼 OnClick에 연결하는 동작 모음. 버튼마다 스크립트를 만들지 않게 한곳에 둔다.
    public class UIActions : MonoBehaviour
    {
        // 메뉴 씬에는 없으므로 비워 둬도 된다.
        [SerializeField] private GameStateController _gameState;
        // 씬을 넘기기 전에 화면을 검게 덮는다. 비워 두면 바로 넘어간다.
        [SerializeField] private ScreenFader _fader;

        private bool _isLeaving;

        public void StartGame()
        {
            SceneFlow.LoadGame();
        }

        public void Restart()
        {
            Leave(SceneFlow.LoadGame);
        }

        public void GoToMainMenu()
        {
            Leave(SceneFlow.LoadMainMenu);
        }

        // 덮이는 동안 버튼을 또 눌러 두 번 넘어가지 않게 한다.
        private void Leave(System.Action load)
        {
            if (_isLeaving)
            {
                return;
            }

            _isLeaving = true;
            if (_fader == null)
            {
                load();
                return;
            }

            _fader.FadeOut(load);
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
