using System.Collections.Generic;
using DG.Tweening;
using ExplodeIt.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ExplodeIt.UI
{
    // 메뉴 씬의 페이지 넘기기. 한 번에 한 페이지만 보이고, 연 순서를 기억해 뒤로가기(Esc 포함)로 되돌아간다.
    // 페이지는 CanvasGroup 하나로 다룬다. 버튼 OnClick에 페이지를 넘겨 바로 연결할 수 있게 하려는 것이다.
    // 페이지에 IMenuPage가 붙어 있으면 열리고 닫힐 때 알려 준다.
    public class MenuPager : MonoBehaviour
    {
        // 페이지가 바뀌는 시간(초)과 들어오고 나갈 때의 크기 배율. 책장을 넘기는 연출은 아트를 바꿀 때 다시 만든다.
        private const float FadeTime = 0.18f;
        private const float EnterScale = 1.04f;
        private const float ExitScale = 0.96f;

        [SerializeField] private CanvasGroup[] _pages;
        [SerializeField] private CanvasGroup _firstPage;
        // 타이틀을 한 번 넘긴 뒤 게임에서 돌아오면 타이틀을 다시 보이지 않고 이 페이지(메인 메뉴)부터 연다.
        [SerializeField] private CanvasGroup _returnPage;
        [SerializeField] private InputActionReference _backAction;
        // 확인 창이 떠 있으면 뒤로가기는 페이지 대신 창을 닫는다.
        [SerializeField] private ConfirmDialog _dialog;
        // 모드 기록 창도 같다.
        [SerializeField] private ModeRecordPopup _recordPopup;
        [SerializeField] private SoundData _pageSound;
        // 타이틀 ↔ 메인처럼 배경이 바뀌는 넘김에서 화면을 검게 덮는 막.
        [SerializeField] private ScreenFader _fader;
        [Tooltip("타이틀 ↔ 메인을 오갈 때 화면이 검게 어두워지는 시간 (초). 길게 두어 장면이 천천히 끝나는 느낌을 준다")]
        [SerializeField, Min(0.05f)] private float _coverTime = 1.5f;
        [Tooltip("검은 화면이 걷히며 다음 화면이 드러나는 시간 (초)")]
        [SerializeField, Min(0.05f)] private float _revealTime = 0.5f;
        [Tooltip("게임에서 메인 메뉴로 돌아왔을 때 검은 화면이 걷히는 시간 (초)")]
        [SerializeField, Min(0.05f)] private float _returnRevealTime = 0.8f;
        [Tooltip("게임에서 돌아왔을 때 메인 메뉴 배경음이 올라오는 시간 (초, 실제 시간)")]
        [SerializeField, Min(0.05f)] private float _returnBgmFadeTime = 1.2f;

        [Header("배경음 (타이틀과 책이 다르다)")]
        [SerializeField] private BgmData _titleBgm;
        [SerializeField] private BgmData _menuBgm;

        private static bool _hasPassedFirstPage;

        private readonly Stack<CanvasGroup> _history = new Stack<CanvasGroup>();
        private CanvasGroup _current;
        // 덮개로 연 페이지 아래에 그대로 보이는 페이지. 덮개를 닫으면 그 페이지로 바로 돌아간다.
        private CanvasGroup _overlayBase;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            _hasPassedFirstPage = false;
        }

        private void Awake()
        {
            for (int i = 0; i < _pages.Length; i++)
            {
                _pages[i].gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            bool isReturning = !SceneFlow.OpensTitle && (_hasPassedFirstPage || SceneFlow.IsReturningFromGame);
            CanvasGroup first = isReturning && _returnPage != null ? _returnPage : _firstPage;
            Show(first, false);
            if (first == _firstPage)
            {
                // 게임에서 "타이틀로"로 나온 경우도 검은 화면에서 드러난다.
                if (SceneFlow.OpensTitle)
                {
                    _fader.FadeIn(_returnRevealTime);
                    AudioManager.PlayBgm(_titleBgm, _returnBgmFadeTime);
                    return;
                }

                AudioManager.PlayBgm(_titleBgm);
                return;
            }

            // 게임에서 돌아온 경우. 검은 화면에서 천천히 책이 드러나고 배경음도 같이 올라온다.
            _fader.FadeIn(_returnRevealTime);
            AudioManager.PlayBgm(_menuBgm, _returnBgmFadeTime);
        }

        // 책의 "메인화면" 버튼. 어디서 왔든(게임에서 돌아와 기록이 비어 있어도) 타이틀로 돌아간다.
        public void ReturnToTitle()
        {
            _history.Clear();
            SwitchTo(_firstPage);
        }

        private void OnEnable()
        {
            _backAction.action.performed += OnBackInput;
            _backAction.action.Enable();
        }

        private void OnDisable()
        {
            _backAction.action.performed -= OnBackInput;
        }

        public void Open(CanvasGroup page)
        {
            if (page == _current)
            {
                return;
            }

            if (_current != null)
            {
                _history.Push(_current);
            }

            _hasPassedFirstPage = true;
            SwitchTo(page);
        }

        // 지금 페이지를 그대로 보이게 둔 채 그 위에 바로 띄운다(전환 연출 없음). 타이틀 배경 위에 설정 책만 띄울 때 쓴다.
        public void OpenOverlay(CanvasGroup page)
        {
            if (page == _current || _current == null)
            {
                return;
            }

            AudioManager.Play(_pageSound);
            _history.Push(_current);
            _overlayBase = _current;
            BlockInput(_overlayBase);
            Show(page, false);
        }

        // 되돌아갈 곳 없이 연다. 다시 돌아갈 일이 없는 넘김에 쓴다.
        public void OpenRoot(CanvasGroup page)
        {
            _hasPassedFirstPage = true;
            _history.Clear();
            SwitchTo(page);
        }

        public void Back()
        {
            if (_dialog != null && _dialog.IsOpen)
            {
                _dialog.Cancel();
                return;
            }

            if (_recordPopup != null && _recordPopup.IsOpen)
            {
                _recordPopup.Close();
                return;
            }

            if (_history.Count > 0)
            {
                SwitchTo(_history.Pop());
            }
        }

        private void OnBackInput(InputAction.CallbackContext context)
        {
            Back();
        }

        private void SwitchTo(CanvasGroup page)
        {
            AudioManager.Play(_pageSound);
            if (page == _overlayBase)
            {
                CloseOverlay();
                return;
            }

            // 타이틀은 배경이 통째로 달라서, 책과 오갈 때는 검게 덮었다가 걷어 내며 바꾼다.
            bool isBackgroundChange = page == _firstPage || _current == _firstPage;
            if (isBackgroundChange && _fader != null && _current != null)
            {
                CanvasGroup from = _current;
                BlockInput(from);
                _current = page;
                bool isToTitle = page == _firstPage;
                // 배경음도 화면과 같은 박자로: 어두워지는 동안 지금 곡이 잦아들고, 밝아지는 동안 다음 곡이 들어온다.
                AudioManager.FadeOutBgm(_coverTime);
                BgmData nextBgm = isToTitle ? _titleBgm : _menuBgm;
                _fader.FadeThrough(_coverTime, _revealTime, () =>
                {
                    SwapInstantly(from, page);
                    AudioManager.PlayBgm(nextBgm, _revealTime);
                });
                return;
            }

            if (_current != null)
            {
                Hide(_current);
            }

            Show(page, true);
        }

        private void CloseOverlay()
        {
            CanvasGroup overlay = _current;
            if (overlay.TryGetComponent(out IMenuPage listener))
            {
                listener.OnPageClosed();
            }

            overlay.DOKill();
            overlay.gameObject.SetActive(false);
            _current = _overlayBase;
            _overlayBase = null;
            _current.interactable = true;
            _current.blocksRaycasts = true;
        }

        private void SwapInstantly(CanvasGroup from, CanvasGroup to)
        {
            if (from.TryGetComponent(out IMenuPage listener))
            {
                listener.OnPageClosed();
            }

            from.DOKill();
            from.gameObject.SetActive(false);
            Show(to, false);
        }

        private void Show(CanvasGroup page, bool animate)
        {
            _current = page;
            page.DOKill();
            page.transform.DOKill();
            page.gameObject.SetActive(true);
            page.interactable = true;
            page.blocksRaycasts = true;
            if (animate)
            {
                page.alpha = 0f;
                page.transform.localScale = Vector3.one * EnterScale;
                page.DOFade(1f, FadeTime).SetUpdate(true).SetLink(page.gameObject);
                page.transform.DOScale(1f, FadeTime).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(page.gameObject);
            }
            else
            {
                page.alpha = 1f;
                page.transform.localScale = Vector3.one;
            }

            if (page.TryGetComponent(out IMenuPage listener))
            {
                listener.OnPageOpened();
            }
        }

        // 사라지는 동안 눌리지 않게 바로 입력을 막는다.
        private void Hide(CanvasGroup page)
        {
            BlockInput(page);
            if (page.TryGetComponent(out IMenuPage listener))
            {
                listener.OnPageClosed();
            }

            page.DOKill();
            page.transform.DOKill();
            page.DOFade(0f, FadeTime).SetUpdate(true).SetLink(page.gameObject)
                .OnComplete(() => page.gameObject.SetActive(false));
            page.transform.DOScale(ExitScale, FadeTime).SetUpdate(true).SetLink(page.gameObject);
        }

        // interactable을 끄면 버튼이 비활성 색으로 바뀌어, 어두워지는 동안 화면이 얼룩져 보인다.
        // 그래서 색은 그대로 두고 클릭만 막고, 눌렀던 버튼 선택을 풀어 엔터·스페이스로 다시 눌리지 않게 한다.
        private static void BlockInput(CanvasGroup page)
        {
            page.blocksRaycasts = false;
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }
    }
}
