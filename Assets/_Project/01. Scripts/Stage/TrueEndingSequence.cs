// 찐엔딩 연출 감독. 쓰러진 왕 쪽으로 카메라를 천천히 당기며 까맣게 덮고 → 오른쪽에 크레딧을 아래에서 위로 올리고,
// 왼쪽에는 맵 곳곳의 짧은 장면을 차례로 비춘다(장면 사이는 까맣게) → 마지막 장면 뒤 화면을 보라로 덮고 그 위에 마지막 문구 → 그대로 진행 흐름에 넘긴다(결과 창).
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
        // 건너뛰기 안내 아래 게이지의 채움 판. 가로 앵커 끝(anchorMax.x)을 0~1로 늘려 채운다.
        [SerializeField] private RectTransform _skipFill;
        // 마지막 장면 뒤 화면 전체를 덮는 보라 판. 크레딧 글보다 위, 마지막 문구보다 아래에 둔다.
        [SerializeField] private Image _finalTint;

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

        // 곡 위치가 크레딧과 이 정도(초) 넘게 어긋났을 때만 옮긴다. 프레임 차이만큼의 오차로 소리가 튀지 않게 한다.
        private const float BgmSyncTolerance = 0.25f;

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
        // 크레딧이 흐른 시간과 그중 꾹 눌러 빨리 감은 시간(실제 시간). 플레이 기록에 비율로 남긴다.
        private float _rollTime;
        private float _fastTime;
        // 이번 크레딧의 글 속도(UI 단위/초). 곡이 있으면 실제 글 높이와 곡 길이로 계산한다.
        private float _scrollSpeed;
        // 마지막 문구를 띄운 채 곡이 끝나기를 기다리는 중인지. 곡이 없으면 이 시각(실제 시간)까지 기다린다.
        private bool _isWaitingBgmEnd;
        // 스페이스를 누르고 있는 시간(실제 시간). 떼면 처음부터.
        private float _skipHeld;
        private float _waitBgmEndAt = -1f;

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
                    _finalTint.gameObject.SetActive(false);
                    _skipHint.gameObject.SetActive(false);
                    _content.gameObject.SetActive(false);
                })
                .Append(_cover.DOFade(1f, data.CoverTime).SetEase(Ease.InQuad))
                .AppendCallback(StartCredits)
                .Append(_cover.DOFade(0f, data.RevealTime))
                .AppendCallback(() =>
                {
                    // 글 속도를 곡 길이에 맞춰 정하므로, 글이 움직이기 시작하는 순간에 곡을 튼다. 글 배치가 다 끝난 지금 높이를 잰다.
                    _scrollSpeed = ComputeScrollSpeed();
                    AudioManager.PlayBgm(data.CreditsBgm, data.BgmFadeTime);
                    _step = Step.Rolling;
                })
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
            _scrollSpeed = _data.ScrollSpeed;
            for (int i = 0; _hideDuringCredits != null && i < _hideDuringCredits.Length; i++)
            {
                _hideDuringCredits[i].SetActive(false);
            }

            _leftView.SetActive(true);
            _rightPanel.SetActive(true);
            _shotCover.alpha = 1f;
            // 크레딧 글이 먼저 몇 줄 올라온 뒤에 장면이 밝아져야 크레딧이 주인공으로 읽힌다.
            PlayShot(0, _data.FirstShotDelay);

            _skipHeld = 0f;
            _skipFill.anchorMax = new Vector2(0f, _skipFill.anchorMax.y);
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

            if (_isWaitingBgmEnd)
            {
                // 곡이 끝나기 조금 전에 까매져야 마지막 소리가 까만 화면 위에서 울린다.
                bool isDone = _waitBgmEndAt >= 0f
                    ? Time.unscaledTime >= _waitBgmEndAt
                    : !AudioManager.IsBgmPlaying || AudioManager.BgmTime >= AudioManager.BgmLength - _data.BgmTailTime - _data.BlackLeadTime;
                if (isDone)
                {
                    CutToBlack();
                }

                return;
            }

            if (_step != Step.Rolling)
            {
                return;
            }

            if (_canSkip && UpdateSkipHold())
            {
                ShowFinal(0f, true);
                return;
            }

            bool isHolding = Mouse.current != null && Mouse.current.leftButton.isPressed;
            float tempo = isHolding ? _data.HoldSpeedMultiplier : 1f;
            float speed = _scrollSpeed * tempo;
            _rollTime += Time.unscaledDeltaTime;
            if (isHolding)
            {
                _fastTime += Time.unscaledDeltaTime;
            }

            // 빨리 감는 동안 왼쪽 장면도 같은 배율로 흘러야 크레딧과 장면 길이가 어긋나지 않는다.
            if (_shotSequence != null && _shotSequence.IsActive())
            {
                _shotSequence.timeScale = tempo;
            }

            if (_shotTimeline != null && _shotTimeline.IsActive())
            {
                _shotTimeline.timeScale = tempo;
            }
            Vector2 position = _content.anchoredPosition;
            position.y += speed * Time.unscaledDeltaTime;
            _content.anchoredPosition = position;

            // 마지막 줄까지 화면 위로 다 빠져나가면 빈 화면으로 잠시 둔 뒤 마지막 문구.
            if (position.y >= _content.rect.height)
            {
                ShowFinal(_data.FinalGap, false);
            }
        }

        // 스페이스를 정해 둔 시간 동안 누르고 있으면 건너뛴다. 실수로 한 번 눌러 크레딧을 놓치지 않게 한다.
        // 게이지는 처음엔 빨리 차다가 끝에 갈수록 천천히 차서, 누르는 즉시 반응하면서도 끝까지 눌러야 하는 느낌을 준다.
        private bool UpdateSkipHold()
        {
            bool isPressed = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
            _skipHeld = isPressed ? _skipHeld + Time.unscaledDeltaTime : 0f;
            float t = Mathf.Clamp01(_skipHeld / Mathf.Max(_data.SkipHoldTime, 0.01f));
            float shown = 1f - (1f - t) * (1f - t) * (1f - t);
            _skipFill.anchorMax = new Vector2(shown, _skipFill.anchorMax.y);
            return t >= 1f;
        }

        // 마지막 줄이 화면 위로 빠지는 순간 곡이 "보라 덮기 + 문구 떠오르기 + 문구 머무는 시간"만 남도록 글 속도를 정한다.
        // 글 높이는 실제 배치 결과로 재야 한다. 미리 잰 값을 쓰면 글자 배치가 조금만 달라도 곡과 어긋난다.
        private float ComputeScrollSpeed()
        {
            AudioClip clip = _data.CreditsBgm != null ? _data.CreditsBgm.Clip : null;
            float rollTime = clip != null ? clip.length - _data.BgmTailTime - FinalTailTime : 0f;
            if (rollTime <= 1f)
            {
                return _data.ScrollSpeed;
            }

            return (_viewHeight + _content.rect.height) / rollTime;
        }

        private float FinalTailTime => _data.FinalTintTime + _data.FinalFadeTime + _data.FinalHoldTime;

        // 빨리 감거나 건너뛰어 곡이 뒤처졌으면, 곡을 끝부분만 남기고 앞으로 옮겨 누구나 곡 끝과 함께 까매지는 순간을 보게 한다.
        // 그대로 본 사람은 글 속도를 곡에 맞춰 두었으므로 건드리지 않는다(옮기면 소리가 튄다).
        private void SyncBgmForFinal(bool isSkipped)
        {
            float length = AudioManager.BgmLength;
            if (length <= 0f || !AudioManager.IsBgmPlaying || (!isSkipped && _fastTime <= 0f))
            {
                return;
            }

            float target = length - _data.BgmTailTime - FinalTailTime;
            if (target > 0f && AudioManager.BgmTime < target - BgmSyncTolerance)
            {
                AudioManager.BgmTime = target;
            }
        }

        // 화면을 보라로 천천히 덮은 뒤 그 위에 마지막 문구를 띄운다. 덮이는 동안 장면은 계속 흐르고, 다 덮인 뒤에 걷는다.
        // 크레딧 글이 다 올라갈 때(곡이 끝나는 순간)나 건너뛸 때 한 번만 시작한다.
        private void ShowFinal(float gap, bool isSkipped)
        {
            if (_step == Step.Final)
            {
                return;
            }

            _step = Step.Final;
            GameEvents.RaiseCreditsFinished(isSkipped, _rollTime > 0f ? _fastTime / _rollTime : 0f);
            _skipHint.gameObject.SetActive(false);
            _finalText.text = _data.FinalText;
            _finalText.alpha = 0f;
            Color tint = _data.FinalTintColor;
            tint.a = 0f;
            _finalTint.color = tint;
            _finalTint.gameObject.SetActive(true);

            _sequence?.Kill();
            _sequence = DOTween.Sequence()
                .AppendInterval(gap)
                .AppendCallback(() => SyncBgmForFinal(isSkipped))
                .Append(_finalTint.DOFade(_data.FinalTintColor.a, _data.FinalTintTime).SetEase(Ease.Linear))
                .AppendCallback(() =>
                {
                    StopShots();
                    _content.gameObject.SetActive(false);
                })
                .Append(_finalText.DOFade(1f, _data.FinalFadeTime))
                .AppendCallback(WaitForBgmEnd)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        // 문구는 곡이 끝나는 바로 그 순간까지 띄워 둔다. 곡이 없으면(꺼졌거나 이미 끝났으면) 정해 둔 시간만큼 둔다.
        private void WaitForBgmEnd()
        {
            _waitBgmEndAt = AudioManager.IsBgmPlaying ? -1f : Time.unscaledTime + _data.FinalHoldTime;
            _isWaitingBgmEnd = true;
        }

        // 곡이 뚝 끝나는 순간 문구·보라를 한 번에 걷고 까만 화면으로 바꾼 뒤 결과 창으로 넘긴다.
        private void CutToBlack()
        {
            _isWaitingBgmEnd = false;
            _finalText.alpha = 0f;
            _finalTint.gameObject.SetActive(false);
            _cover.alpha = 1f;
            Finish();
            // 잔향까지 다 울린 뒤 파일 끝의 딸깍 소리만 막을 만큼 아주 짧게 줄인다.
            float untilFade = Mathf.Max(0f, AudioManager.BgmLength - AudioManager.BgmTime - _data.BgmEndFadeTime);
            _sequence?.Kill();
            _sequence = DOTween.Sequence()
                .InsertCallback(untilFade, () => AudioManager.FadeOutBgm(_data.BgmEndFadeTime))
                .InsertCallback(_data.ClearDelay, () => _onFinished?.Invoke())
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        // 까만 화면은 그대로 두고 결과 창이 그 위에 뜬다. 그 전에 저장하고 업적 알림을 먼저 띄운다.
        private void Finish()
        {
            // 곡은 줄이지 않는다. 마지막 한 방이 묻히지 않게 끝까지 그대로 울리게 둔다.
            _step = Step.None;
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
                .SetUpdate(true)
                .SetLink(gameObject);

            // 마지막 장면(왕이 기를 모으는 순간)은 까맣게 끊지 않고, 크레딧 글이 다 올라갈 때까지 띄워 둔 채 보라로 덮인다.
            if (index == _shots.Length - 1)
            {
                return;
            }

            _shotSequence
                .Append(_shotCover.DOFade(1f, data.FadeTime))
                .AppendCallback(() =>
                {
                    EndShot();
                    PlayShot(index + 1, _data.ShotGap);
                });
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
