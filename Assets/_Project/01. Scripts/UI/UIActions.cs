using ExplodeIt.Core;
using TMPro;
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
        // 화면이 덮이는 동안 게임 곡을 미리 줄여, 메뉴 곡으로 뚝 끊겨 바뀌지 않게 한다. 메뉴에서 다음 곡이 같은 시간에 걸쳐 올라온다.
        [Tooltip("메뉴로 나갈 때 게임 배경음이 줄어드는 시간 (초, 실제 시간)")]
        [SerializeField, Min(0.05f)] private float _menuBgmFadeTime = 1.2f;

        [Header("일시정지 메뉴 (게임 씬)")]
        // 판 도중 나가도 결과 창(기록·해금 창)은 꼭 보여 준다. 나가기를 고르면 이 피격 판정을 바로 쓰러뜨려
        // 평소처럼 쓰러지는 연출과 결과 창을 거치고, 결과 창의 나가기 버튼이 고른 곳으로 보낸다.
        [SerializeField] private HitReceiver _player;
        // 실수로 판을 날리지 않게 한 번 더 묻는다.
        [SerializeField] private ConfirmDialog _confirm;
        [SerializeField, TextArea] private string _mainMenuMessage = "메인 메뉴로 나갈까요?\n이번 판은 여기서 끝나요.";
        [SerializeField, TextArea] private string _titleMessage = "타이틀로 나갈까요?\n이번 판은 여기서 끝나요.";
        // 결과 창 나가기 버튼 글자. 타이틀로 나가기를 골랐으면 이 글자로 바꾼다.
        // 코드가 바꾸는 글자라 씬 글자 번역을 붙이지 않고, 판이 시작될 때 지금 언어로 직접 채운다.
        [SerializeField] private TMP_Text[] _resultLeaveLabels;
        [SerializeField] private string _resultMainMenuText = "메인으로";
        [SerializeField] private string _resultTitleText = "타이틀로";

        [Header("결과 창 스토리 중간부터 다시 하기 (게임 씬)")]
        // 스토리 판에서만 보인다. 결과 창 다시하기는 스토리에서 처음부터 다시 하므로 글자를 바꿔 구분한다.
        [SerializeField] private CheckpointRetryButton[] _resultCheckpoints;
        [SerializeField] private TMP_Text[] _resultRetryLabels;
        [SerializeField] private string _retryText = "다시하기";
        [SerializeField] private string _storyRetryText = "처음부터 다시하기";

        private bool _isLeaving;
        private bool _leavesToTitle;

        private void Awake()
        {
            bool isStory = RunSetup.Mode == RunMode.Story;
            for (int i = 0; _resultCheckpoints != null && i < _resultCheckpoints.Length; i++)
            {
                _resultCheckpoints[i].Bind(RestartFrom);
            }

            string retryText = L10n.Tr(isStory ? _storyRetryText : _retryText);
            for (int i = 0; _resultRetryLabels != null && i < _resultRetryLabels.Length; i++)
            {
                _resultRetryLabels[i].text = retryText;
            }

            string leaveText = L10n.Tr(_resultMainMenuText);
            for (int i = 0; _resultLeaveLabels != null && i < _resultLeaveLabels.Length; i++)
            {
                _resultLeaveLabels[i].text = leaveText;
            }
        }

        private void OnEnable()
        {
            GameEvents.GameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.GameStateChanged -= OnGameStateChanged;
        }

        // ESC로 일시정지를 풀면 떠 있던 확인 창도 함께 닫는다.
        private void OnGameStateChanged(GameState previous, GameState current)
        {
            if (previous == GameState.Paused && _confirm != null && _confirm.IsOpen)
            {
                _confirm.Cancel();
            }
        }

        public void StartGame()
        {
            SceneFlow.LoadGame();
        }

        // 스토리는 중간부터 시작한 판이어도 처음부터 다시 한다. 중간부터는 결과 창의 지점 버튼으로 고른다.
        public void Restart()
        {
            RestartFrom(0);
        }

        private void RestartFrom(int checkpoint)
        {
            if (_isLeaving)
            {
                return;
            }

            RunSetup.SetStartCheckpoint(checkpoint);
            Leave(SceneFlow.LoadGame);
        }

        public void GoToMainMenu()
        {
            FadeOutBgm();
            Leave(SceneFlow.LoadMainMenu);
        }

        // 일시정지 메뉴 "메인메뉴로". 확인하면 판을 끝내고(사망 연출 → 결과 창) 결과 창에서 메인 메뉴로 나간다.
        public void AskGoToMainMenu()
        {
            _confirm.Show(_mainMenuMessage, EndRunToMainMenu);
        }

        // 일시정지 메뉴 "타이틀로". 확인하면 판을 끝내고 결과 창에서 타이틀 화면으로 나간다.
        public void AskGoToTitle()
        {
            _confirm.Show(_titleMessage, EndRunToTitle);
        }

        // 결과 창 나가기 버튼. 일시정지 메뉴에서 고른 곳(기본은 메인 메뉴)으로 나간다.
        public void LeaveFromResult()
        {
            if (_leavesToTitle)
            {
                GoToTitle();
                return;
            }

            GoToMainMenu();
        }

        private void EndRunToMainMenu()
        {
            EndRun(false);
        }

        private void EndRunToTitle()
        {
            EndRun(true);
        }

        private void EndRun(bool toTitle)
        {
            _leavesToTitle = toTitle;
            if (toTitle)
            {
                for (int i = 0; i < _resultLeaveLabels.Length; i++)
                {
                    _resultLeaveLabels[i].text = L10n.Tr(_resultTitleText);
                }
            }

            _gameState.Resume();
            _player.Kill();
        }

        private void GoToTitle()
        {
            FadeOutBgm();
            Leave(SceneFlow.LoadTitle);
        }

        private void FadeOutBgm()
        {
            if (!_isLeaving)
            {
                AudioManager.FadeOutBgm(_menuBgmFadeTime);
            }
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
