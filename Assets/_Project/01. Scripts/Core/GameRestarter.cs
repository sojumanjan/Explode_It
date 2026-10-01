using UnityEngine;
using UnityEngine.InputSystem;

namespace ExplodeIt.Core
{
    // 키보드로 바로 다시 하기. 버튼으로 하는 재시작은 UI가 같은 SceneFlow를 부른다.
    public class GameRestarter : MonoBehaviour
    {
        [SerializeField] private InputActionReference _retryAction;
        [SerializeField] private InputActionReference _quickRestartAction;

        private bool _canRetry;
        private bool _isRestarting;

        private void OnEnable()
        {
            GameEvents.GameStateChanged += OnGameStateChanged;

            _retryAction.action.performed += OnRetry;
            _retryAction.action.Enable();
            _quickRestartAction.action.performed += OnQuickRestart;
            _quickRestartAction.action.Enable();
        }

        private void OnDisable()
        {
            GameEvents.GameStateChanged -= OnGameStateChanged;

            _retryAction.action.performed -= OnRetry;
            _quickRestartAction.action.performed -= OnQuickRestart;
        }

        private void OnRetry(InputAction.CallbackContext context)
        {
            // 판 도중에 누르면 판이 날아가므로 판이 끝난 뒤에만 받는다.
            if (_canRetry)
            {
                Restart();
            }
        }

        // 수치 조정 중 반복 테스트용. 배포 빌드에서는 막는다.
        private void OnQuickRestart(InputAction.CallbackContext context)
        {
            if (Debug.isDebugBuild)
            {
                Restart();
            }
        }

        // 두 키를 같은 프레임에 누르면 로드가 두 번 예약되므로 한 번만 받는다.
        private void Restart()
        {
            if (_isRestarting)
            {
                return;
            }

            _isRestarting = true;
            SceneFlow.LoadGame();
        }

        private void OnGameStateChanged(GameState previous, GameState current)
        {
            _canRetry = current == GameState.PlayerDead || current == GameState.StageClear;
        }
    }
}
