// 스토리 엔딩 연출 감독. 책으로 확대·하얀 화면 → 낮 컷 → "그날 밤…" → 밤 컷 → 편지 → 까만 화면에 THE END...? 순서로 진행하고, 끝나면 진행 흐름에 알린다.
using System;
using DG.Tweening;
using ExplodeIt.Core;
using ExplodeIt.UI;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ExplodeIt.Stage
{
    // 클릭 판정은 여기서 한다(만화 페이지는 보여 주기만 한다). 판이 끝나는 처리는 진행 흐름(StageRunner)이 콜백으로 받는다.
    public class OutroSequence : MonoBehaviour
    {
        // 편지가 펼쳐질 때 처음 크기 배율과 기울기(도). 손에 든 편지를 눈앞으로 확 들이미는 느낌만 내는 연출 값이다.
        private const float LetterStartScale = 0.6f;
        private const float LetterStartTilt = -8f;

        private enum Step
        {
            None,
            Book,
            Comic,
            Night,
            Opening,
            Letter,
            Closing,
            End
        }

        [SerializeField] private IntroComic _comic;
        // 책 속으로 넘어갈 때 화면 전체를 덮는 하얀 판. 만화 페이지보다 위에 둔다.
        [SerializeField] private CanvasGroup _flash;
        // 책이 맵 가장자리에 있어도 화면 가운데에 놓이도록, 책으로 다가가는 동안만 카메라 범위 제한을 끈다.
        [SerializeField] private CinemachineConfiner2D _confiner;

        [Header("편지")]
        // 화면 전체를 살짝 어둡게 덮고 그 위에 편지를 띄운다.
        [SerializeField] private CanvasGroup _letter;
        // 펼쳐지는 연출을 줄 편지 종이.
        [SerializeField] private RectTransform _letterPaper;
        [SerializeField] private TMP_Text _letterText;
        // 편지는 이 버튼으로만 닫는다. 컷을 넘기던 연타에 편지를 읽기도 전에 지나가지 않게 한다.
        [SerializeField] private Button _letterCloseButton;

        [Header("까만 화면")]
        // 화면 전체를 까맣게 덮는 판과 그 위의 글. 낮 → 밤 사이("그날 밤…")와 마지막(THE END...?)에 함께 쓴다.
        [SerializeField] private CanvasGroup _end;
        [SerializeField] private TMP_Text _endText;

        private OutroData _data;
        private Action _onFinished;
        private Step _step;
        private float _endReadyTime;
        private float _endAutoTime;
        private Sequence _sequence;

        private void Awake()
        {
            _flash.gameObject.SetActive(false);
            _letter.gameObject.SetActive(false);
            _end.gameObject.SetActive(false);
            _letterCloseButton.onClick.AddListener(OnLetterClose);
        }

        // 버튼에 클릭음이 따로 붙어 있어 여기서는 소리를 내지 않는다.
        private void OnLetterClose()
        {
            if (_step == Step.Letter)
            {
                CloseToEnd();
            }
        }

        private void OnDisable()
        {
            _sequence?.Kill();
        }

        // bookPoint: 고블린이 빨려 들어간 책 자리. 카메라가 그쪽으로 확 다가가며 하얗게 덮인 뒤 만화로 넘어간다.
        public void Play(OutroData data, Vector2 bookPoint, Action onFinished)
        {
            _data = data;
            _onFinished = onFinished;
            _step = Step.Book;
            // 먼저 책을 화면 가운데로 옮겨 놓고, 그다음 확대한다. 둘을 같이 하면 카메라가 따라가는 동안 책 위쪽이 확대된다.
            if (_confiner != null)
            {
                _confiner.enabled = false;
            }

            GameEvents.RaiseCameraFocusRequested(bookPoint, data.BookMoveTime);

            _flash.gameObject.SetActive(true);
            _flash.alpha = 0f;
            _sequence?.Kill();
            _sequence = DOTween.Sequence()
                .AppendInterval(data.BookMoveTime)
                .AppendCallback(() => GameEvents.RaiseCameraZoomRequested(data.BookZoom, data.BookZoomTime))
                .AppendInterval(Mathf.Max(0f, data.BookZoomTime - data.WhiteInTime))
                .Append(_flash.DOFade(1f, data.WhiteInTime).SetEase(Ease.InQuad))
                .AppendCallback(ShowComicUnderWhite)
                .AppendInterval(data.WhiteHoldTime)
                .Append(_flash.DOFade(0f, data.WhiteOutTime))
                .AppendCallback(() =>
                {
                    _flash.gameObject.SetActive(false);
                    _step = Step.Comic;
                })
                .SetLink(gameObject);
        }

        // 하얀 화면 뒤에서 카메라를 평소대로 되돌려 두고 만화를 켠다. 엔딩이 끝나 결과 창이 뜰 때 아레나가 평소 모습으로 보인다.
        private void ShowComicUnderWhite()
        {
            GameEvents.RaiseCameraFocusReleased(0f);
            GameEvents.RaiseCameraZoomRequested(1f, 0f);
            if (_confiner != null)
            {
                _confiner.enabled = true;
            }
            _comic.Play(_data, OnFirstPageDone, 0, _data.FirstPageCount, true);
        }

        // 낮 컷을 다 넘기면 까맣게 덮고 "그날 밤…"을 띄운 뒤, 그 뒤에서 밤 컷 페이지로 바꿔 걷어 낸다.
        // 낮에서 밤으로 바로 넘어가면 시간이 흐른 게 읽히지 않는다.
        private void OnFirstPageDone()
        {
            int first = _data.FirstPageCount;
            if (first >= _data.PanelCount)
            {
                OnComicDone();
                return;
            }

            _step = Step.Night;
            _endText.text = _data.NightText;
            _endText.alpha = 0f;
            _end.gameObject.SetActive(true);
            _end.alpha = 0f;

            float fade = _data.NightFadeTime;
            _sequence?.Kill();
            _sequence = DOTween.Sequence()
                .Append(_end.DOFade(1f, fade))
                .Append(_endText.DOFade(1f, fade))
                .AppendInterval(_data.NightHoldTime)
                .AppendCallback(() => _comic.Play(_data, OnComicDone, first, -1, true))
                .Append(_end.DOFade(0f, fade))
                .AppendCallback(() =>
                {
                    _end.gameObject.SetActive(false);
                    _step = Step.Comic;
                })
                .SetLink(gameObject);
        }

        private void Update()
        {
            // 끝 글은 클릭하지 않아도 잠시 뒤 저절로 넘어간다.
            if (_step == Step.End && Time.unscaledTime >= _endAutoTime)
            {
                Finish();
                return;
            }

            if (_step == Step.None || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            switch (_step)
            {
                case Step.Comic:
                    _comic.Advance();
                    break;

                case Step.End:
                    if (Time.unscaledTime >= _endReadyTime)
                    {
                        AudioManager.Play(_data.ClickSound);
                        Finish();
                    }
                    break;
            }
        }

        // 마지막 컷을 넘기면 바로 그 앞으로 편지가 기울어진 채 작게 나왔다가 탁 펴진다.
        private void OnComicDone()
        {
            _step = Step.Opening;
            _letterText.text = _data.LetterText;
            _letter.gameObject.SetActive(true);
            _letter.alpha = 0f;
            _letterPaper.localScale = Vector3.one * LetterStartScale;
            _letterPaper.localRotation = Quaternion.Euler(0f, 0f, LetterStartTilt);

            float time = _data.LetterOpenTime;
            _sequence?.Kill();
            AudioManager.Play(_data.LetterSound);
            _sequence = DOTween.Sequence()
                .Append(_letter.DOFade(1f, time * 0.5f))
                .Join(_letterPaper.DOScale(1f, time).SetEase(Ease.OutBack))
                .Join(_letterPaper.DOLocalRotate(Vector3.zero, time).SetEase(Ease.OutQuad))
                .AppendCallback(() => _step = Step.Letter)
                .SetLink(gameObject);
        }

        // 까맣게 덮은 뒤 만화와 편지를 걷고, 끝 글을 띄운다.
        private void CloseToEnd()
        {
            _step = Step.Closing;
            _endText.text = _data.EndText;
            _endText.alpha = 0f;
            _end.gameObject.SetActive(true);
            _end.alpha = 0f;

            float time = _data.EndFadeTime;
            _sequence?.Kill();
            _sequence = DOTween.Sequence()
                .Append(_end.DOFade(1f, time))
                .AppendCallback(() =>
                {
                    _comic.Hide();
                    _letter.gameObject.SetActive(false);
                })
                .Append(_endText.DOFade(1f, time))
                .AppendCallback(() =>
                {
                    _step = Step.End;
                    _endReadyTime = Time.unscaledTime + _data.EndHoldTime;
                    _endAutoTime = Time.unscaledTime + _data.EndAutoTime;
                })
                .SetLink(gameObject);
        }

        // 까만 화면은 그대로 두고 글만 걷는다. 그 순간 스토리 클리어 업적 알림이 까만 화면 위에 먼저 올라오고,
        // 잠시 뒤 진행 흐름에 넘겨 결과·해금 창을 띄운다. 알림과 결과 창이 한꺼번에 뜨면 알림이 묻힌다.
        // 화면을 걷으면 엔딩 직후 갑자기 아레나가 드러나 여운이 끊긴다. 결과 창 다음은 씬이 바뀌므로 걷을 일이 없다.
        private void Finish()
        {
            _step = Step.None;
            GameEvents.RaiseStoryEndingFinished();
            _sequence?.Kill();
            _sequence = DOTween.Sequence()
                .Append(_endText.DOFade(0f, _data.EndFadeTime))
                .AppendInterval(Mathf.Max(0f, _data.ClearDelay - _data.EndFadeTime))
                .AppendCallback(() => _onFinished?.Invoke())
                .SetLink(gameObject);
        }
    }
}
