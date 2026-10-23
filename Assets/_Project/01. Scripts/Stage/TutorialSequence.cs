using System;
using DG.Tweening;
using ExplodeIt.Core;
using ExplodeIt.Player;
using TMPro;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 첫 판 조작 튜토리얼. 오프닝 뒤 첫 웨이브 전에 진행한다.
    // 화면 가운데 살짝 위에 체크박스가 달린 과제가 하나씩 뜨고, 해내면 체크되며 다음 과제로 넘어간다. 그냥 읽고 지나가지 않고 직접 해 보게 한다.
    //   이동 → 폭탄 → 대시(무적 설명) → 스킬. 블랙홀이 터지면 화면 아래 게이지만 밝게 남기고 설명을 띄우며 첫 웨이브를 연다.
    // 과제판은 화면 UI에 고정한다. 고블린 머리 위를 따라가게 하면 카메라가 따라가는 속도와 어긋나 떨려 보인다.
    // 스킬 차례 전에 게이지를 써 버리지 않게 입력 읽기를 직접 잡아 막는다.
    public class TutorialSequence : MonoBehaviour
    {
        // 과제판이 뜰 때·체크될 때 뽀잉 시간(초).
        private const float PopTime = 0.2f;
        private static readonly Color DoneTextColor = new Color(0.6f, 1f, 0.6f, 1f);
        // 게이지 강조 구멍의 여백, 설명 글과 게이지 사이 간격(화면 UI 단위), 어두워지고 걷히는 시간(초).
        private const float FocusPadding = 18f;
        private const float FocusTextGap = 24f;
        private const float FocusFadeTime = 0.3f;

        private enum Step
        {
            None,
            Start,
            Move,
            Throw,
            Dash,
            Ability,
            BlackHole,
            Completed,
            FocusWait,
            Focus
        }

        [SerializeField] private TutorialData _data;
        [SerializeField] private PlayerInputReader _input;
        // 이동 과제에서 움직인 거리를 잰다.
        [SerializeField] private Transform _player;

        [Header("과제판 (화면 UI)")]
        // 판 크기는 레이아웃 컴포넌트가 글자에 맞춰 정한다.
        [SerializeField] private RectTransform _label;
        [SerializeField] private TMP_Text _labelText;
        // 체크 표시. 과제를 해내면 튀어나온다.
        [SerializeField] private RectTransform _checkMark;

        [Header("게이지 강조")]
        // 강조할 스킬 게이지. 그 둘레만 비우고 나머지 화면을 어둡게 덮는다.
        [SerializeField] private RectTransform _gauge;
        [SerializeField] private CanvasGroup _focus;
        // 위·아래·왼쪽·오른쪽을 덮는 어두운 판 4장.
        [SerializeField] private RectTransform[] _focusDims;
        [SerializeField] private RectTransform _focusFrame;
        [SerializeField] private TMP_Text _focusText;

        private Step _step;
        private float _stepTime;
        // 체크된 과제 다음에 이어질 단계. 체크된 채로 잠깐 보여 준 뒤 넘어간다.
        private Step _nextStep;
        private float _completeHold;
        private Action _onBasicsDone;
        private Vector2 _lastPlayerPosition;
        private float _moved;
        private Color _textColor;
        private Vector3 _labelScale;
        private Vector3 _checkScale;
        private Tween _labelPop;
        private Tween _checkPop;
        private Tween _focusFade;
        private readonly Vector3[] _corners = new Vector3[4];

        // 따로 기록을 두지 않는다. 오프닝을 본 판(맨 처음 한 번)에만 이어서 진행한다.
        public bool CanPlay => _data != null;

        private void Awake()
        {
            _labelScale = _label.localScale;
            _checkScale = _checkMark.localScale;
            _textColor = _labelText.color;
            _label.gameObject.SetActive(false);
            _focus.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.BombThrown += OnBombThrown;
            GameEvents.PlayerDodged += OnPlayerDodged;
            GameEvents.AbilityUsed += OnAbilityUsed;
            GameEvents.AbilityFinished += OnAbilityFinished;
            GameEvents.PlayerDied += OnPlayerDied;
        }

        private void OnDisable()
        {
            GameEvents.BombThrown -= OnBombThrown;
            GameEvents.PlayerDodged -= OnPlayerDodged;
            GameEvents.AbilityUsed -= OnAbilityUsed;
            GameEvents.AbilityFinished -= OnAbilityFinished;
            GameEvents.PlayerDied -= OnPlayerDied;
            _labelPop?.Kill();
            _checkPop?.Kill();
            _focusFade?.Kill();
        }

        // 과제를 시작한다. 마지막 게이지 설명이 뜨는 순간 onBasicsDone을 불러 첫 웨이브를 연다.
        public void PlayBasics(Action onBasicsDone)
        {
            _onBasicsDone = onBasicsDone;
            _input.IsAbilityBlocked = true;
            EnterStep(Step.Start);
        }

        private void EnterStep(Step step)
        {
            _step = step;
            _stepTime = 0f;
        }

        private void Update()
        {
            if (_step == Step.None)
            {
                return;
            }

            _stepTime += Time.deltaTime;
            switch (_step)
            {
                case Step.Start:
                    if (_stepTime >= _data.StartDelay)
                    {
                        _moved = 0f;
                        _lastPlayerPosition = _player.position;
                        ShowTask(_data.MoveTask);
                        EnterStep(Step.Move);
                    }
                    break;

                case Step.Move:
                    Vector2 position = _player.position;
                    _moved += (position - _lastPlayerPosition).magnitude;
                    _lastPlayerPosition = position;
                    if (_moved >= _data.MoveDistance)
                    {
                        Complete(null, Step.Throw, _data.TaskCompleteHold);
                    }
                    break;

                case Step.Completed:
                    if (_stepTime >= _completeHold)
                    {
                        StartTask(_nextStep);
                    }
                    break;

                case Step.FocusWait:
                    if (_stepTime >= _data.GaugeFocusDelay)
                    {
                        StartFocus();
                    }
                    break;

                case Step.Focus:
                    if (_stepTime >= _data.GaugeFocusTime)
                    {
                        EndFocus();
                    }
                    break;
            }
        }

        private void StartTask(Step step)
        {
            switch (step)
            {
                case Step.Throw:
                    ShowTask(_data.ThrowTask);
                    break;
                case Step.Dash:
                    ShowTask(_data.DashTask);
                    break;
                case Step.Ability:
                    _input.IsAbilityBlocked = false;
                    ShowTask(_data.AbilityTask);
                    break;
            }

            EnterStep(step);
        }

        private void OnBombThrown()
        {
            if (_step == Step.Throw)
            {
                Complete(null, Step.Dash, _data.TaskCompleteHold);
            }
        }

        private void OnPlayerDodged()
        {
            if (_step == Step.Dash)
            {
                Complete(_data.DashDoneText, Step.Ability, _data.DashDoneHold);
            }
        }

        // 블랙홀을 던지면 체크하고 블랙홀 설명으로 바꾼다. 터져 사라질 때까지 체크된 채로 두어, 적이 빨려 드는 걸 보며 읽게 한다.
        private void OnAbilityUsed()
        {
            if (_step == Step.Ability)
            {
                Check(_data.AbilityDoneText);
                EnterStep(Step.BlackHole);
            }
        }

        private void OnAbilityFinished()
        {
            if (_step == Step.BlackHole)
            {
                HideLabel();
                EnterStep(Step.FocusWait);
            }
        }

        // 체크하고, 정한 시간 동안 체크된 채로 보여 준 뒤 다음 과제로 넘어간다. doneText가 있으면 글자도 그 설명으로 바뀐다.
        private void Complete(string doneText, Step next, float hold)
        {
            Check(doneText);
            _nextStep = next;
            _completeHold = hold;
            EnterStep(Step.Completed);
        }

        private void Check(string doneText)
        {
            GameEvents.RaiseTutorialStepCompleted(_step.ToString());
            if (!string.IsNullOrEmpty(doneText))
            {
                _labelText.text = doneText;
            }

            _labelText.color = DoneTextColor;
            _checkMark.gameObject.SetActive(true);
            _checkMark.localScale = Vector3.zero;
            _checkPop?.Kill();
            _checkPop = _checkMark.DOScale(_checkScale, PopTime).SetEase(Ease.OutBack).SetLink(gameObject);
            _labelPop?.Kill();
            _label.localScale = _labelScale;
            _labelPop = _label.DOPunchScale(_labelScale * 0.12f, 0.25f, 6).SetLink(gameObject);
        }

        // ── 과제판

        private void ShowTask(string text)
        {
            _label.gameObject.SetActive(true);
            _labelText.color = _textColor;
            _labelText.text = text;
            _checkMark.gameObject.SetActive(false);
            _labelPop?.Kill();
            _label.localScale = _labelScale * 0.6f;
            _labelPop = _label.DOScale(_labelScale, PopTime).SetEase(Ease.OutBack).SetLink(gameObject);
        }

        private void HideLabel()
        {
            _labelPop?.Kill();
            _checkPop?.Kill();
            _label.gameObject.SetActive(false);
        }

        // ── 게이지 강조: 게이지 둘레만 비우고 화면을 어둡게 덮고, 그 위에 설명을 띄운다. 설명이 걷힌 뒤 첫 웨이브가 시작된다.
        // 웨이브 시작 때 왕 컷인이 나오므로, 설명과 겹치지 않게 다 걷힌 다음 연다.

        private void StartFocus()
        {
            EnterStep(Step.Focus);
            _focusText.text = _data.GaugeText;
            _focus.gameObject.SetActive(true);
            LayoutFocus();
            _focus.alpha = 0f;
            _focusFade?.Kill();
            _focusFade = _focus.DOFade(1f, FocusFadeTime).SetUpdate(true).SetLink(gameObject);
        }

        private void EndFocus()
        {
            EnterStep(Step.None);
            _focusFade?.Kill();
            _focusFade = _focus.DOFade(0f, FocusFadeTime).SetUpdate(true)
                .OnComplete(OnFocusHidden).SetLink(gameObject);
        }

        private void OnFocusHidden()
        {
            _focus.gameObject.SetActive(false);
            _onBasicsDone?.Invoke();
        }

        // 게이지의 화면 위치를 강조 판의 좌표로 바꿔, 구멍 둘레 네 장의 어두운 판과 테두리·설명 위치를 정한다.
        private void LayoutFocus()
        {
            RectTransform root = (RectTransform)_focus.transform;
            _gauge.GetWorldCorners(_corners);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, _corners[0], null, out Vector2 min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, _corners[2], null, out Vector2 max);
            min -= Vector2.one * FocusPadding;
            max += Vector2.one * FocusPadding;
            Rect area = root.rect;

            SetRect(_focusDims[0], new Vector2(area.xMin, max.y), new Vector2(area.xMax, area.yMax));
            SetRect(_focusDims[1], new Vector2(area.xMin, area.yMin), new Vector2(area.xMax, min.y));
            SetRect(_focusDims[2], new Vector2(area.xMin, min.y), new Vector2(min.x, max.y));
            SetRect(_focusDims[3], new Vector2(max.x, min.y), new Vector2(area.xMax, max.y));
            SetRect(_focusFrame, min, max);

            RectTransform text = _focusText.rectTransform;
            text.anchorMin = text.anchorMax = new Vector2(0.5f, 0.5f);
            text.pivot = new Vector2(0.5f, 0f);
            float halfWidth = text.rect.width * 0.5f;
            float x = Mathf.Clamp((min.x + max.x) * 0.5f, area.xMin + halfWidth, area.xMax - halfWidth);
            text.anchoredPosition = new Vector2(x, max.y + FocusTextGap);
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = (min + max) * 0.5f;
            rect.sizeDelta = new Vector2(Mathf.Max(0f, max.x - min.x), Mathf.Max(0f, max.y - min.y));
        }

        private void OnPlayerDied()
        {
            HideLabel();
            if (_step != Step.Focus)
            {
                EnterStep(Step.None);
            }
        }
    }
}
