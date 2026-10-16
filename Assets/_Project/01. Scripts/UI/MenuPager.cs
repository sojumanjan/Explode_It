using System.Collections.Generic;
using DG.Tweening;
using ExplodeIt.Core;
using UnityEngine;
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
        [SerializeField] private SoundData _pageSound;
        // 타이틀 ↔ 책처럼 배경이 바뀌는 넘김에서 화면을 검게 덮는 막과, 덮고 걷는 시간(초, 각각).
        [SerializeField] private ScreenFader _fader;
        [SerializeField, Min(0.05f)] private float _blackFadeTime = 0.25f;

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
            Show(_hasPassedFirstPage && _returnPage != null ? _returnPage : _firstPage, false);
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
            _overlayBase.interactable = false;
            _overlayBase.blocksRaycasts = false;
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
                from.interactable = false;
                from.blocksRaycasts = false;
                _current = page;
                _fader.FadeThrough(_blackFadeTime, () => SwapInstantly(from, page));
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
            page.interactable = false;
            page.blocksRaycasts = false;
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
    }
}
