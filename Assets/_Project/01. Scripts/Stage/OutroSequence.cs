// 스토리 엔딩 연출 감독. 책으로 확대·하얀 화면 → 낮 컷 → "그날 밤…" → 밤 컷 → 편지 → 까만 화면에 THE END...? 순서로 진행하고, 끝나면 진행 흐름에 알린다.
using System;
using DG.Tweening;
using ExplodeIt.Core;
using ExplodeIt.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

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

        [Header("편지")]
        // 화면 전체를 살짝 어둡게 덮고 그 위에 편지를 띄운다.
        [SerializeField] private CanvasGroup _letter;
        // 펼쳐지는 연출을 줄 편지 종이.
        [SerializeField] private RectTransform _letterPaper;
        [SerializeField] private TMP_Text _letterText;

        [Header("까만 화면")]
        // 화면 전체를 까맣게 덮는 판과 그 위의 글. 낮 → 밤 사이("그날 밤…")와 마지막(THE END...?)에 함께 쓴다.
        [SerializeField] private CanvasGroup _end;
        [SerializeField] private TMP_Text _endText;

        private OutroData _data;
        private Action _onFinished;
        private Step _step;
        private float _endReadyTime;
        private Sequence _sequence;

        private void Awake()
        {
            _flash.gameObject.SetActive(false);
            _letter.gameObject.SetActive(false);
            _end.gameObject.SetActive(false);
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
            GameEvents.RaiseCameraFocusRequested(bookPoint, data.BookZoomTime);
            GameEvents.RaiseCameraZoomRequested(data.BookZoom, data.BookZoomTime);

            _flash.gameObject.SetActive(true);
            _flash.alpha = 0f;
            _sequence?.Kill();
            _sequence = DOTween.Sequence()
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
            if (_step == Step.None || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            switch (_step)
            {
                case Step.Comic:
                    _comic.Advance();
                    break;

                case Step.Letter:
                    AudioManager.Play(_data.ClickSound);
                    CloseToEnd();
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
                })
                .SetLink(gameObject);
        }

        // 까만 화면은 그대로 두고 글만 걷은 뒤 진행 흐름에 넘긴다. 결과·해금 창이 까만 화면 위에 뜬다.
        // 화면을 걷으면 엔딩 직후 갑자기 아레나가 드러나 여운이 끊긴다. 결과 창 다음은 씬이 바뀌므로 걷을 일이 없다.
        private void Finish()
        {
            _step = Step.None;
            _sequence?.Kill();
            _sequence = DOTween.Sequence()
                .Append(_endText.DOFade(0f, _data.EndFadeTime))
                .SetLink(gameObject);
            _onFinished?.Invoke();
        }
    }
}
