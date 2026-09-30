using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ExplodeIt.Core
{
    // 풀, 이벤트 구독, 트윈, 상태를 하나씩 되돌리면 빠뜨리는 곳이 생기므로 씬을 통째로 다시 불러온다.
    public class GameRestarter : MonoBehaviour
    {
        [SerializeField] private InputActionReference _retryAction;
        [SerializeField] private InputActionReference _quickRestartAction;

        private bool _isPlayerDead;
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
            // 살아 있을 때 누르면 판이 날아가므로 사망 후에만 받는다.
            if (_isPlayerDead)
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
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void OnGameStateChanged(GameState previous, GameState current)
        {
            _isPlayerDead = current == GameState.PlayerDead;
        }
    }
}
