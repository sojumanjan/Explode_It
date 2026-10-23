// 찐엔딩 연출 감독. 쓰러진 왕 쪽으로 카메라를 천천히 당기며 까맣게 덮고 → 오른쪽에 크레딧을 아래에서 위로 올리고,
// 왼쪽에는 맵 곳곳의 짧은 장면을 차례로 비춘다(장면 사이는 까맣게) → 빈 화면에 마지막 문구 → 까만 화면 그대로 진행 흐름에 넘긴다(결과 창).
using System;
using System.Collections.Generic;
using DG.Tweening;
using ExplodeIt.Core;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ExplodeIt.Stage
{
    // 크레딧 줄은 씬에 둔 모양별 본보기를 복제해 데이터 순서대로 쌓는다. 엔딩에서 한 번만 만들므로 풀을 쓰지 않는다.
    public class TrueEndingSequence : MonoBehaviour
    {
        private enum Step
        {
            None,
            Approach,
            Rolling,
            Final
        }

        // 왕을 쓰러뜨린 순간부터 Esc 일시정지를 막는다. 크레딧 위로 일시정지 창이 뜨면 연출이 깨진다.
        [SerializeField] private GameStateController _gameState;
        // 크레딧 화면 전체(까만 배경). 결과 창보다 아래에 둔다.
        [SerializeField] private CanvasGroup _screen;
        // 크레딧 화면 위를 덮는 까만 판. 덮였다 걷히는 연출에 쓴다.
        [SerializeField] private CanvasGroup _cover;
        // 줄이 쌓이는 곳. 위쪽 가운데 기준으로 세로 레이아웃이 붙어 있다.
        [SerializeField] private RectTransform _content;
        [SerializeField] private TMP_Text _finalText;
        [SerializeField] private TMP_Text _skipHint;

        [Header("줄 본보기 (꺼 둔다)")]
        [SerializeField] private TMP_Text _titleTemplate;
        [SerializeField] private TMP_Text _headingTemplate;
        [SerializeField] private TMP_Text _lineTemplate;
        // 아래 세 칸은 캐스트 본보기의 자식이다.
        [SerializeField] private RectTransform _castTemplate;
        [SerializeField] private Image _castSprite;
        [SerializeField] private TMP_Text _castName;
        [SerializeField] private TMP_Text _castRole;

        [Header("왼쪽 장면")]
        // 장면 자리로 카메라를 바로 옮긴다. 장면은 카메라 범위 밖(정원 등)에 있어 범위 제한을 끈다.
        [SerializeField] private CinemachineCamera _camera;
        [SerializeField] private CinemachineConfiner2D _confiner;
        // 장면을 덮는 화면 전체 까만 판(크레딧 글 아래). 장면 사이마다 까매졌다 밝아진다.
        // 왼쪽 칸만 덮으면 오른쪽 그러데이션 판의 옅은 부분으로 맵이 비쳐 보이므로 화면 전체를 덮는다.
        [SerializeField] private CanvasGroup _shotCover;
        // 왼쪽 칸 전체와 오른쪽 까만 판. 화면이 까매지는 동안 켜 두면 겹친 판 수가 달라 두 칸 경계가 비쳐 보이므로, 다 덮인 뒤에 켠다.
        [SerializeField] private GameObject _leftView;
        [SerializeField] private GameObject _rightPanel;
        // 비출 장면 순서. 다 비추면 왼쪽은 까만 채로 둔다.
        [SerializeField] private CreditsShot[] _shots;
        // 크레딧 동안 숨길 게임 화면 UI(체력·게이지 등). 왼쪽 칸으로 비쳐 보이면 안 된다.
        [SerializeField] private GameObject[] _hideDuringCredits;

        private readonly List<GameObject> _built = new List<GameObject>();
        private TrueEndingData _data;
        private Action _onFinished;
        private Step _step;
        private bool _canSkip;
        private float _viewHeight;
        private Sequence _sequence;
        private Sequence _shotSequence;
        private Sequence _shotTimeline;
        private int _shotIndex = -1;
        // 카메라를 순간 이동한 뒤 몇 프레임은 이전 자리에서 미끄러져 오지 않게 막는다.
        private int _cameraCutFrames;

        private void Awake()
        {
            _screen.gameObject.SetActive(false);
            _titleTemplate.gameObject.SetActive(false);
            _headingTemplate.gameObject.SetActive(false);
            _lineTemplate.gameObject.SetActive(false);
            _castTemplate.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            _sequence?.Kill();
            _shotSequence?.Kill();
            _shotTimeline?.Kill();
        }

        // 쓰러진 왕이 이미 카메라를 자기 쪽으로 당겨 둔 상태에서, 그 확대를 끊김 없이 이어 훨씬 깊이 들어가며 까맣게 덮는다.
        public void Play(TrueEndingData data, Action onFinished)
        {
            _data = data;
            _onFinished = onFinished;
            _step = Step.Approach;
            _gameState.IsPauseBlocked = true;
            // 이미 본 사람만 건너뛸 수 있다. 처음 보는 크레딧은 꾹 눌러 빨리 감기만 된다.
            _canSkip = SaveSystem.Data.TrueEndingSeen;

            _sequence?.Kill();
            _sequence = DOTween.Sequence()
                .AppendInterval(data.DefeatHold)
                .AppendCallback(() => GameEvents.RaiseCameraZoomRequested(data.Zoom, data.ZoomTime))
                .AppendInterval(Mathf.Max(0f, data.ZoomTime - data.CoverTime))
                .AppendCallback(() =>
                {
                    // 까만 판 한 장만 켜서 화면 전체를 고르게 어둡게 한다.
                    _screen.gameObject.SetActive(true);
                    _screen.alpha = 1f;
                    _cover.alpha = 0f;
                    _leftView.SetActive(false);
                    _rightPanel.SetActive(false);
                    _shotCover.alpha = 0f;
                    _finalText.alpha = 0f;
                    _skipHint.gameObject.SetActive(false);
                    _content.gameObject.SetActive(false);
                })
                .Append(_cover.DOFade(1f, data.CoverTime).SetEase(Ease.InQuad))
                .AppendCallback(StartCredits)
                .Append(_cover.DOFade(0f, data.RevealTime))
                .AppendCallback(() => _step = Step.Rolling)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        // 까만 화면 뒤에서 줄을 쌓고 화면 아래에 걸쳐 둔 뒤 크레딧 곡을 튼다.
        private void StartCredits()
        {
            BuildEntries();
            _content.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            _viewHeight = ((RectTransform)_content.parent).rect.height;
            _content.anchoredPosition = new Vector2(_content.anchoredPosition.x, -_viewHeight);
            AudioManager.PlayBgm(_data.CreditsBgm, _data.BgmFadeTime);
            for (int i = 0; _hideDuringCredits != null && i < _hideDuringCredits.Length; i++)
            {
                _hideDuringCredits[i].SetActive(false);
            }

            _leftView.SetActive(true);
            _rightPanel.SetActive(true);
            _shotCover.alpha = 1f;
            // 크레딧 글이 먼저 몇 줄 올라온 뒤에 장면이 밝아져야 크레딧이 주인공으로 읽힌다.
            PlayShot(0, _data.FirstShotDelay);

            bool showHint = _canSkip && !string.IsNullOrEmpty(_data.SkipHint);
            _skipHint.gameObject.SetActive(showHint);
            _skipHint.text = _data.SkipHint;
        }

        private void Update()
        {
            if (_cameraCutFrames > 0)
            {
                _cameraCutFrames--;
                _camera.PreviousStateIsValid = false;
            }

            if (_step != Step.Rolling)
            {
                return;
            }

            if (_canSkip && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                ShowFinal(0f);
                return;
            }

            bool isHolding = Mouse.current != null && Mouse.current.leftButton.isPressed;
            float speed = _data.ScrollSpeed * (isHolding ? _data.HoldSpeedMultiplier : 1f);
            Vector2 position = _content.anchoredPosition;
            position.y += speed * Time.unscaledDeltaTime;
            _content.anchoredPosition = position;

            // 마지막 줄까지 화면 위로 다 빠져나가면 빈 화면으로 잠시 둔 뒤 마지막 문구.
            if (position.y >= _content.rect.height)
            {
                ShowFinal(_data.FinalGap);
            }
        }

        private void ShowFinal(float gap)
        {
            _step = Step.Final;
            StopShots();
            _content.gameObject.SetActive(false);
            _skipHint.gameObject.SetActive(false);
            _finalText.text = _data.FinalText;
            _finalText.alpha = 0f;

            _sequence?.Kill();
            _sequence = DOTween.Sequence()
                .AppendInterval(gap)
                .Append(_finalText.DOFade(1f, _data.FinalFadeTime))
                .AppendInterval(_data.FinalHoldTime)
                .Append(_finalText.DOFade(0f, _data.EndFadeTime))
                .AppendCallback(Finish)
                .AppendInterval(_data.ClearDelay)
                .AppendCallback(() => _onFinished?.Invoke())
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        // 까만 화면은 그대로 두고 결과 창이 그 위에 뜬다. 그 전에 저장하고 업적 알림을 먼저 띄운다.
        private void Finish()
        {
            _step = Step.None;
            AudioManager.FadeOutBgm(_data.EndFadeTime);
            SaveSystem.Data.TrueEndingSeen = true;
            SaveSystem.Save();
            GameEvents.RaiseTrueEndingFinished();
        }

        // 장면 자리로 카메라를 옮기고 까만 판을 걷은 뒤 대본을 돌린다. 머무는 시간이 지나면 다시 까맣게 덮고 다음 장면.
        private void PlayShot(int index, float delay)
        {
            _shotIndex = index;
            if (_shots == null || index >= _shots.Length)
            {
                return;
            }

            CreditsShot shot = _shots[index];
            CreditsShotData data = shot.Data;
            _shotTimeline = shot.Prepare();
            CutCamera(shot.SubjectPoint, data.Zoom);
            _shotSequence = DOTween.Sequence()
                .AppendInterval(delay)
                .Append(_shotCover.DOFade(0f, data.FadeTime))
                .AppendCallback(() => _shotTimeline.Play())
                .AppendInterval(data.HoldTime)
                .Append(_shotCover.DOFade(1f, data.FadeTime))
                .AppendCallback(() =>
                {
                    EndShot();
                    PlayShot(index + 1, 0f);
                })
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void EndShot()
        {
            _shotTimeline?.Kill();
            _shotTimeline = null;
            if (_shots != null && _shotIndex >= 0 && _shotIndex < _shots.Length)
            {
                _shots[_shotIndex].Restore();
            }
        }

        private void StopShots()
        {
            _shotSequence?.Kill();
            EndShot();
            _shotIndex = -1;
            _shotCover.alpha = 1f;
        }

        // 장면이 화면 왼쪽 칸 가운데에 오도록, 화면 너비의 1/4만큼 오른쪽을 비춘다.
        private void CutCamera(Vector2 subject, float zoom)
        {
            if (_confiner != null)
            {
                _confiner.enabled = false;
            }

            GameEvents.RaiseCameraZoomRequested(zoom, 0f);
            Camera view = Camera.main;
            float aspect = view != null ? view.aspect : 16f / 9f;
            float halfWidth = _camera.Lens.OrthographicSize * aspect;
            GameEvents.RaiseCameraFocusRequested(subject + Vector2.right * (halfWidth * 0.5f), 0f);
            _camera.PreviousStateIsValid = false;
            _cameraCutFrames = 2;
        }

        private void BuildEntries()
        {
            for (int i = 0; i < _built.Count; i++)
            {
                Destroy(_built[i]);
            }

            _built.Clear();
            CreditsEntry[] entries = _data.Entries;
            for (int i = 0; entries != null && i < entries.Length; i++)
            {
                _built.Add(BuildEntry(entries[i]));
            }
        }

        private GameObject BuildEntry(CreditsEntry entry)
        {
            switch (entry.Kind)
            {
                case CreditsEntryKind.Title:
                    return BuildText(_titleTemplate, entry.Text);
                case CreditsEntryKind.Heading:
                    return BuildText(_headingTemplate, entry.Text);
                case CreditsEntryKind.Cast:
                    return BuildCast(entry);
                case CreditsEntryKind.Space:
                    return BuildSpace();
                default:
                    return BuildText(_lineTemplate, entry.Text);
            }
        }

        private GameObject BuildText(TMP_Text template, string text)
        {
            TMP_Text line = Instantiate(template, _content);
            line.text = text;
            line.gameObject.SetActive(true);
            return line.gameObject;
        }

        // 캐스트 본보기를 복제한 뒤 그 안의 같은 자리(그림·이름·설명)를 채운다. 그림은 비율을 지킨 채 정한 높이로 세운다.
        private GameObject BuildCast(CreditsEntry entry)
        {
            _castName.text = entry.Text;
            _castRole.text = entry.SubText;
            _castSprite.sprite = entry.Sprite;
            _castSprite.enabled = entry.Sprite != null;
            if (entry.Sprite != null)
            {
                Rect rect = entry.Sprite.rect;
                float height = _data.CastSpriteHeight;
                _castSprite.rectTransform.sizeDelta = new Vector2(height * rect.width / rect.height, height);
            }

            RectTransform cast = Instantiate(_castTemplate, _content);
            cast.gameObject.SetActive(true);
            return cast.gameObject;
        }

        private GameObject BuildSpace()
        {
            var space = new GameObject("Space", typeof(RectTransform), typeof(LayoutElement));
            space.transform.SetParent(_content, false);
            LayoutElement layout = space.GetComponent<LayoutElement>();
            layout.minHeight = _data.SpaceHeight;
            layout.preferredHeight = _data.SpaceHeight;
            return space;
        }
    }
}
