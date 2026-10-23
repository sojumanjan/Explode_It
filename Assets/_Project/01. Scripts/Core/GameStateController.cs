using UnityEngine;
using UnityEngine.InputSystem;

namespace ExplodeIt.Core
{
    public class GameStateController : MonoBehaviour
    {
        [SerializeField] private InputActionReference _pauseAction;

        // 개발자 패널로 배속을 바꾼 상태에서 일시정지했다가 풀면 그 배속으로 돌아간다.
        private float _resumeTimeScale = 1f;

        public GameState Current { get; private set; } = GameState.None;
        // 엔딩 연출(크레딧 등) 동안 Esc로 일시정지 창이 뜨지 않게 막는다. 연출이 켜고, 씬이 바뀌면 새 컨트롤러라 저절로 풀린다.
        public bool IsPauseBlocked { get; set; }

        private void OnEnable()
        {
            GameEvents.PlayerDied += OnPlayerDied;
            GameEvents.StageCleared += OnStageCleared;

            _pauseAction.action.performed += OnPauseInput;
            _pauseAction.action.Enable();
        }

        private void OnDisable()
        {
            GameEvents.PlayerDied -= OnPlayerDied;
            GameEvents.StageCleared -= OnStageCleared;

            _pauseAction.action.performed -= OnPauseInput;
        }

        private void Start()
        {
            ChangeState(GameState.Playing);
        }

        private void OnPauseInput(InputAction.CallbackContext context)
        {
            if (Current == GameState.Playing && !IsPauseBlocked)
            {
                Pause();
            }
            else if (Current == GameState.Paused)
            {
                Resume();
            }
        }

        public void Pause()
        {
            if (Current != GameState.Playing)
            {
                return;
            }

            // 히트스톱으로 잠깐 느려진 순간에 멈춰도, 풀 때는 원래 속도로 돌아가게 한다.
            _resumeTimeScale = FeedbackPlayer.BaseTimeScale;
            Time.timeScale = 0f;
            ChangeState(GameState.Paused);
        }

        public void Resume()
        {
            if (Current != GameState.Paused)
            {
                return;
            }

            Time.timeScale = _resumeTimeScale;
            ChangeState(GameState.Playing);
        }

        private void OnPlayerDied()
        {
            ChangeState(GameState.PlayerDead);
        }

        // 마지막 적과 같은 순간에 죽었다면 사망이 우선한다.
        private void OnStageCleared()
        {
            if (Current == GameState.Playing)
            {
                ChangeState(GameState.StageClear);
            }
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
