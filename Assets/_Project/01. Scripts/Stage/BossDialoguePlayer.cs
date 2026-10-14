using System;
using DG.Tweening;
using ExplodeIt.Core;
using ExplodeIt.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ExplodeIt.Stage
{
    // 스토리 보스전 등장 대화. 보스보다 왕이 먼저 보스 자리 옆에 스티커처럼 찰싹 붙어 나와 말하고,
    // 데이터에 표시한 줄이 시작될 때 보스를 불러내고(BossSpawnRequested), 표시한 줄을 넘길 때 보스 등장 동작을 시작시킨다(EntranceRequested).
    // 줄은 클릭해야 넘어간다. 글자가 다 나오면 말풍선에 ▼가 통통 튀어 클릭을 유도한다.
    // 화면 오른쪽 위 건너뛰기 버튼을 길게 누르면(또는 스페이스를 길게 누르면) 남은 대사를 건너뛰고, 등장 동작을 표시한 줄을 넘긴 것처럼 바로 등장 동작을 시작한다.
    public class BossDialoguePlayer : MonoBehaviour
    {
        [SerializeField] private KingSpriteSet _kingSprites;
        [SerializeField] private SpriteRenderer _king;
        [SerializeField] private SpeechBubble _kingBubble;
        [SerializeField] private SpeechBubble _bossBubble;
        // 왕이 보스 자리 옆 어디에 서는지(유닛). 플레이어 쪽으로 서서 카메라 안에 들어오게 한다.
        [SerializeField, Min(0f)] private float _kingSideDistance = 2.6f;
        // 말하는 쪽 머리 위와 말풍선 사이 간격(유닛). 보스가 아직 없을 때 보스 줄은 자리 위 이 높이(유닛)에 뜬다.
        [SerializeField, Min(0f)] private float _bubbleGap = 0.15f;
        [SerializeField, Min(0f)] private float _emptyBossHeight = 2f;
        // 보스전 시작 후 카메라가 옮겨 가는 동안 기다렸다가 왕이 붙는 시간(초).
        [SerializeField, Min(0f)] private float _startDelay = 0.5f;
        // 왕이 붙고 떨어지는 시간(초), 표정이 바뀔 때 뽀잉 세기(비율).
        [SerializeField, Min(0f)] private float _appearDuration = 0.25f;
        [SerializeField, Min(0f)] private float _leaveDuration = 0.3f;
        [SerializeField, Min(0f)] private float _moodPunch = 0.25f;
        // 건너뛰기 버튼. 화면 UI 캔버스 오른쪽 위에 고정해 두고, 마우스 판정은 대사 넘기기와 같이 직접 한다.
        [SerializeField] private RectTransform _skipButton;
        // 길게 누르는 동안 버튼 바탕이 왼쪽부터 차오르는 칸(Filled 이미지). 다 차면 건너뛴다.
        [SerializeField] private UnityEngine.UI.Image _skipFill;
        // 건너뛰려면 누르고 있어야 하는 시간(초). 대사를 넘기려고 연타하다 실수로 건너뛰지 않게 한다.
        [SerializeField, Min(0.1f)] private float _skipHoldTime = 0.8f;

        [Header("엔딩 연출 (처치 대화)")]
        // 왕이 던지는 스티커 책. 바닥에 놓여 고블린이 뛰어든다.
        [SerializeField] private SpriteRenderer _book;
        // 책이 날아가 떨어지는 시간(초)과 포물선 높이(유닛), 왕이 하늘로 날아가는 시간(초)과 높이(유닛).
        [SerializeField, Min(0.05f)] private float _throwDuration = 0.6f;
        [SerializeField, Min(0f)] private float _throwArc = 2f;
        [SerializeField, Min(0.05f)] private float _flyAwayDuration = 0.5f;
        [SerializeField, Min(0f)] private float _flyAwayHeight = 14f;
        // 고블린이 책으로 뛰어드는 시간(초)과 포물선 높이(유닛), 빨려 들어가는 시간(초)과 도는 바퀴 수.
        [SerializeField, Min(0.05f)] private float _jumpDuration = 0.6f;
        [SerializeField, Min(0f)] private float _jumpArc = 2f;
        [SerializeField, Min(0.05f)] private float _suckDuration = 1.2f;
        [SerializeField, Min(0f)] private float _suckSpins = 3f;
        // 빨려 들어가는 줄은 글자가 다 나온 뒤 이 시간(초)이 지나면 클릭 없이 저절로 빨려 들어간다.
        [SerializeField, Min(0f)] private float _autoSuckDelay = 1.5f;
        // 2등신 왕이 쏙 사라진 각성 왕 자리에서 말랑하게 튀어나오는 시간(초). 다 나온 뒤 대사가 뜬다.
        [SerializeField, Min(0.05f)] private float _revertPopDuration = 0.35f;
        [SerializeField] private SoundData _bookLandSound;
        [SerializeField] private SoundData _flyAwaySound;
        [SerializeField] private SoundData _jumpSound;
        [SerializeField] private SoundData _suckSound;
        // 마우스를 올렸을 때 스킵 버튼이 커지는 배율.
        [SerializeField, Min(1f)] private float _skipHoverScale = 1.12f;

        private BossDialogueData _data;
        private Transform _boss;
        // 처치 대화에서 고블린 말풍선을 띄울 플레이어. 등장 대화에서는 비어 있다.
        private Transform _player;
        // 처치 대화에서 쓰러진 각성 왕이 2등신 왕으로 돌아왔는지. 돌아온 뒤에는 2등신 왕 그림이 말하고 스티커처럼 퇴장한다.
        private bool _isReverted;
        private Vector3 _bookScale;
        // 씬에 배치한 왕 말풍선 크기. 빨려 들어가며 줄었다가 이 크기로 되돌린다.
        private Vector3 _kingBubbleScale = Vector3.one;
        private Vector3 _playerScale;
        private Tween _action;
        private TweenCallback _onAdvanceLine;
        private TweenCallback _onShowLine;
        // 빨려 들어가는 줄의 글자가 다 나온 실제 시각. 음수면 아직이다.
        private float _autoSuckReadyTime = -1f;
        private TweenCallback _onRevertEnd;
        private Vector2 _bossPoint;
        private float _bossTopOffset;
        private int _line;
        private bool _isTalking;
        private float _side = 1f;
        private Vector3 _kingScale;
        private Sequence _sequence;
        private Tween _punch;
        private Tween _pause;
        private TweenCallback _onPauseEnd;
        private Tween _shoutHide;
        private TweenCallback _onShoutEnd;
        private Vector3 _skipScale;
        private bool _isSkipHovered;
        // 건너뛰기를 누르고 있은 시간(초). 손을 떼면 빠르게 줄어든다.
        private float _skipHold;

        public bool IsPlaying { get; private set; }

        public event Action BossSpawnRequested;
        public event Action EntranceRequested;

        private void Awake()
        {
            _kingScale = _king.transform.localScale;
            _onPauseEnd = ShowCurrentLine;
            _onShoutEnd = HideShout;
            _onRevertEnd = ShowRevertedKing;
            _kingBubbleScale = _kingBubble.transform.localScale;
            _onAdvanceLine = AdvanceLine;
            _onShowLine = ShowCurrentLine;
            if (_book != null)
            {
                _bookScale = _book.transform.localScale;
                _book.enabled = false;
            }
            if (_skipButton != null)
            {
                _skipScale = _skipButton.localScale;
            }

            HideAll();
        }

        private void OnEnable()
        {
            GameEvents.BossLineRequested += OnBossLineRequested;
        }

        private void OnDisable()
        {
            GameEvents.BossLineRequested -= OnBossLineRequested;
            _sequence?.Kill();
            _punch?.Kill();
            _pause?.Kill();
            _shoutHide?.Kill();
            _action?.Kill();
        }

        // 싸우는 도중 보스가 외치는 대사. 등장 대화와 같은 보스 말풍선을 쓰고, 클릭 없이 시간이 지나면 사라진다.
        // 보스 모습이 등장 때와 달라졌을 수 있어(최종 보스 변신) 머리 높이는 이때 다시 잰다.
        private void OnBossLineRequested(string text, float duration)
        {
            if (_boss == null || IsPlaying || string.IsNullOrEmpty(text))
            {
                return;
            }

            _bossBubble.transform.position = new Vector3(_boss.position.x, TopOf(_boss) + _bubbleGap, 0f);
            _bossBubble.Show(text, false);
            _shoutHide?.Kill();
            _shoutHide = DOVirtual.DelayedCall(duration, _onShoutEnd).SetLink(gameObject);
        }

        private void HideShout()
        {
            if (!IsPlaying)
            {
                _bossBubble.Hide();
            }
        }

        // bossPoint: 보스가 내려앉을 자리. 보스는 아직 없고, 표시한 줄에서 진행 흐름이 불러낸 뒤 SetBoss로 알려 준다.
        public void Play(BossDialogueData data, Vector2 bossPoint, Vector2 player)
        {
            if (data == null || data.LineCount == 0)
            {
                return;
            }

            _data = data;
            _boss = null;
            _player = null;
            _isReverted = false;
            _bossPoint = bossPoint;
            _bossTopOffset = _emptyBossHeight;
            _line = -1;
            _isTalking = false;
            IsPlaying = true;

            _side = player.x >= bossPoint.x ? 1f : -1f;
            Transform king = _king.transform;
            // 왕이 곧 보스면 보스 자리에 바로 서서, 대화가 끝나는 순간 같은 자리의 보스로 이어진다.
            Vector2 offset = data.KingBecomesBoss ? Vector2.up * data.KingStandHeight : Vector2.right * (_side * _kingSideDistance);
            king.position = bossPoint + offset;
            _king.sprite = _kingSprites.Get(FirstKingMood());
            _king.gameObject.SetActive(true);

            // 스티커처럼 크게 비스듬히 떴다가 찰싹 붙는다.
            _sequence?.Kill();
            king.localScale = _kingScale * 1.5f;
            king.localRotation = Quaternion.Euler(0f, 0f, -12f * _side);
            _king.color = new Color(1f, 1f, 1f, 0f);
            _sequence = DOTween.Sequence()
                .AppendInterval(_startDelay)
                .Append(king.DOScale(_kingScale, _appearDuration).SetEase(Ease.OutBack))
                .Join(king.DOLocalRotate(Vector3.zero, _appearDuration))
                .Join(_king.DOFade(1f, _appearDuration * 0.5f))
                .OnComplete(NextLine)
                .SetLink(gameObject);
            ShowSkipButton(_startDelay);
        }

        // 왕과 보스 자리의 가운데, 둘 중 높은 머리보다 위에 띄운다. 왕이 곧 보스면 그 한 명의 머리 위다.
        // 건너뛰기는 화면 오른쪽 위에 고정이다. 말풍선·캐릭터와 다른 층이라 위치와 상관없이 겹치지 않는다.
        private void ShowSkipButton(float delay)
        {
            if (_skipButton == null)
            {
                return;
            }

            _skipButton.gameObject.SetActive(true);
            _isSkipHovered = false;
            _skipHold = 0f;
            SetSkipFill(0f);
            _skipButton.DOKill();
            _skipButton.localScale = Vector3.zero;
            _skipButton.DOScale(_skipScale, _appearDuration).SetDelay(delay).SetEase(Ease.OutBack).SetUpdate(true).SetLink(_skipButton.gameObject);
        }

        private void SetSkipFill(float amount)
        {
            if (_skipFill != null)
            {
                _skipFill.fillAmount = amount;
            }
        }

        // 최종 보스를 쓰러뜨린 뒤의 대화. 왕 그림 없이 쓰러진 보스 머리 위(왕 줄)와 플레이어 머리 위(고블린 줄)에 말풍선이 뜬다.
        public void PlayDefeat(BossDialogueData data, Transform boss, Transform player)
        {
            if (data == null || data.LineCount == 0 || boss == null)
            {
                return;
            }

            _data = data;
            _boss = boss;
            _player = player;
            _isReverted = false;
            _playerScale = player != null ? player.localScale : Vector3.one;
            if (_book != null)
            {
                _book.enabled = false;
            }
            _bossPoint = boss.position;
            _bossTopOffset = TopOf(boss) - boss.position.y;
            _line = -1;
            _isTalking = false;
            IsPlaying = true;
            _sequence?.Kill();
            _punch?.Kill();
            _king.gameObject.SetActive(false);

            ShowSkipButton(0f);
            NextLine();
        }


        // 보스가 내려앉은 뒤 불린다. 머리 높이는 이때 한 번만 잰다. 대화 동안 보스는 제자리에 있다.
        public void SetBoss(Transform boss)
        {
            _boss = boss;
            if (boss != null)
            {
                _bossTopOffset = TopOf(boss) - boss.position.y;
                // 왕이 곧 보스면 같은 자리에 나타난 보스(2등신 모습)가 이어받으므로 대화용 왕 그림은 숨긴다.
                if (_data != null && _data.KingBecomesBoss)
                {
                    _punch?.Kill();
                    _king.gameObject.SetActive(false);
                }
            }
        }

        private void Update()
        {
            if (!IsPlaying || Mouse.current == null)
            {
                return;
            }

            TickAutoSuck();
            bool isOverSkip = IsPointerOverSkip();
            UpdateSkipHover(isOverSkip);
            if (TickSkipHold(isOverSkip))
            {
                return;
            }

            // 건너뛰기 버튼을 누르는 클릭은 대사를 넘기지 않는다.
            if (!Mouse.current.leftButton.wasPressedThisFrame || isOverSkip)
            {
                return;
            }

            if (!_isTalking || IsAutoSuckLine())
            {
                return;
            }

            // 글자가 나오는 중이면 한 번에 다 보여 주고, 다 나온 뒤 클릭해야 다음 줄로 넘어간다.
            SpeechBubble bubble = CurrentBubble();
            if (bubble.IsTyping)
            {
                bubble.CompleteTyping();
                return;
            }

            NextLine();
        }

        // 빨려 들어가는 줄은 클릭으로 넘길 수 없고, 말풍선이 뜬 뒤 정한 시간이 지나면 저절로 빨려 들어간다.
        // 말하던 고블린이 말을 다 맺기도 전에 말풍선째 빨려 들어가는 게 웃음 포인트다.
        private bool IsAutoSuckLine()
        {
            return _line >= 0 && _line < _data.LineCount && _data.GetLine(_line).EndAction == DialogueAction.PlayerSuckedIn;
        }

        private void TickAutoSuck()
        {
            if (!_isTalking || !IsAutoSuckLine() || _autoSuckReadyTime < 0f)
            {
                return;
            }

            if (Time.unscaledTime - _autoSuckReadyTime >= _autoSuckDelay)
            {
                _autoSuckReadyTime = -1f;
                NextLine();
            }
        }

        private void NextLine()
        {
            // 등장 동작은 표시한 줄을 다 읽고 넘기는 순간 시작한다. 마지막 줄에 표시하면 대화가 끝나며 시작한다.
            if (_line >= 0 && _data.GetLine(_line).StartEntrance)
            {
                EntranceRequested?.Invoke();
            }

            // 이 줄을 넘길 때의 연출이 있으면 말풍선을 닫고 연출을 다 보여 준 뒤 다음 줄로 간다.
            if (_line >= 0 && _data.GetLine(_line).EndAction != DialogueAction.None)
            {
                _isTalking = false;
                // 빨려 들어갈 때는 말하던 말풍선도 함께 빨려 들어가게 남겨 둔다.
                if (_data.GetLine(_line).EndAction != DialogueAction.PlayerSuckedIn)
                {
                    _kingBubble.Hide();
                    _bossBubble.Hide();
                }

                PlayAction(_data.GetLine(_line).EndAction, _onAdvanceLine);
                return;
            }

            AdvanceLine();
        }

        private void AdvanceLine()
        {
            _line++;
            _kingBubble.Hide();
            _bossBubble.Hide();
            if (_line >= _data.LineCount)
            {
                Leave();
                return;
            }

            BossDialogueData.Line line = _data.GetLine(_line);
            _isTalking = false;
            if (line.Bgm != null)
            {
                AudioManager.PlayBgm(line.Bgm);
            }

            if (line.BossSprite != null && !_isReverted)
            {
                GameEvents.RaiseBossPoseRequested(line.BossSprite);
            }

            // 처치 대화: 쓰러진 각성 왕이 펑 하고 2등신으로 돌아간 뒤, 그 자리에 2등신 왕이 뽀잉 나타나 이 줄을 말한다.
            if (line.RevertToKing && !_isReverted && _boss != null)
            {
                _isReverted = true;
                GameEvents.RaiseBossRevertRequested();
                _pause?.Kill();
                _pause = DOVirtual.DelayedCall(_data.RevertPause, _onRevertEnd).SetLink(gameObject);
                return;
            }

            // 이 줄이 시작될 때의 연출이 있으면 다 보여 준 뒤 말풍선을 띄운다.
            if (line.StartAction != DialogueAction.None)
            {
                PlayAction(line.StartAction, _onShowLine);
                return;
            }

            // 왕이 곧 보스인 대화는 표시한 줄에서만 보스(변신)를 부른다. 표시가 없으면 대화가 끝난 뒤 부른다.
            if (line.SpawnBoss || (_line == 0 && !_data.HasSpawnLine && !_data.KingBecomesBoss))
            {
                BossSpawnRequested?.Invoke();
                // 착지 연출과 대사가 겹치면 착지에 눈이 안 가므로, 잠깐 말없이 보여 준 뒤 이 줄을 띄운다.
                if (_data.SpawnPause > 0f)
                {
                    _pause?.Kill();
                    _pause = DOVirtual.DelayedCall(_data.SpawnPause, _onPauseEnd).SetLink(gameObject);
                    return;
                }
            }

            ShowCurrentLine();
        }

        private void ShowCurrentLine()
        {
            BossDialogueData.Line line = _data.GetLine(_line);
            _isTalking = true;
            // 빨려 들어가는 줄은 첫 글자가 뜨는 지금부터 시간을 센다.
            _autoSuckReadyTime = IsAutoSuckLine() ? Time.unscaledTime : -1f;
            // 저절로 넘어가는 줄에는 클릭을 부르는 ▼를 띄우지 않는다.
            bool waitsForClick = !IsAutoSuckLine();
            if (line.Speaker == DialogueSpeaker.Player && _player != null)
            {
                _kingBubble.transform.position = new Vector3(_player.position.x, TopOf(_player) + _bubbleGap, 0f);
                _kingBubble.Show(line.Text, waitsForClick);
            }
            else if (UsesKingBubble(line))
            {
                // 표정이 바뀔 때마다 뽀잉 하고 튀어서 대사보다 표정이 먼저 읽히게 한다.
                _king.sprite = _kingSprites.Get(line.Mood);
                _punch?.Kill();
                _king.transform.localScale = _kingScale;
                _punch = _king.transform.DOPunchScale(_kingScale * _moodPunch, 0.25f, 6).SetLink(gameObject);
                _kingBubble.transform.position = new Vector3(_king.transform.position.x, _king.bounds.max.y + _bubbleGap, 0f);
                _kingBubble.Show(line.Text, waitsForClick);
            }
            else
            {
                Vector2 boss = _boss != null ? (Vector2)_boss.position : _bossPoint;
                _bossBubble.transform.position = new Vector3(boss.x, boss.y + _bossTopOffset + _bubbleGap, 0f);
                _bossBubble.Show(line.Text, waitsForClick);
            }
        }

        // 화면 UI 캔버스(Overlay)에 둔 버튼이라 카메라 없이 화면 좌표로 판정한다.
        private bool IsPointerOverSkip()
        {
            return _skipButton != null && _skipButton.gameObject.activeSelf
                && RectTransformUtility.RectangleContainsScreenPoint(_skipButton, Mouse.current.position.ReadValue(), null);
        }

        // 버튼 위에서 마우스를 누르고 있거나 스페이스를 누르고 있으면 차오른다. 다 차면 건너뛴다.
        private bool TickSkipHold(bool isOverSkip)
        {
            if (_skipButton == null || !_skipButton.gameObject.activeSelf)
            {
                return false;
            }

            bool isHolding = (isOverSkip && Mouse.current.leftButton.isPressed)
                || (Keyboard.current != null && Keyboard.current.spaceKey.isPressed);
            float deltaTime = Time.unscaledDeltaTime;
            _skipHold = isHolding ? _skipHold + deltaTime : Mathf.Max(0f, _skipHold - deltaTime * 3f);
            SetSkipFill(_skipHold / _skipHoldTime);
            if (_skipHold < _skipHoldTime)
            {
                return false;
            }

            _skipHold = 0f;
            Skip();
            return true;
        }

        private void UpdateSkipHover(bool isOver)
        {
            if (isOver == _isSkipHovered)
            {
                return;
            }

            _isSkipHovered = isOver;
            _skipButton.DOKill();
            _skipButton.DOScale(_skipScale * (isOver ? _skipHoverScale : 1f), 0.1f).SetLink(_skipButton.gameObject);
        }

        // 남은 대사를 건너뛴다. 진행 흐름은 등장 동작 요청을 받으면 보스가 아직 없을 때 먼저 불러내므로,
        // 어느 줄에서 누르든 "등장 동작을 표시한 줄을 넘긴 것"과 같아진다. 이미 시작했으면 진행 흐름이 다시 시작하지 않는다.
        private void Skip()
        {
            _pause?.Kill();
            _action?.Kill();
            _isTalking = false;
            _kingBubble.Hide();
            _bossBubble.Hide();
            EntranceRequested?.Invoke();
            _line = _data.LineCount;
            Leave();
        }

        // 2등신 왕을 쓰러진 보스 자리에 뽀잉 띄우고 이 줄을 말한다. 왕이 보스 왼쪽·오른쪽 어느 쪽으로 젖혀 떠날지는 플레이어 쪽을 본다.
        private void ShowRevertedKing()
        {
            Transform king = _king.transform;
            Vector2 boss = _boss.position;
            king.position = boss + Vector2.up * _data.KingStandHeight;
            _side = _player != null && _player.position.x < boss.x ? -1f : 1f;
            _king.sprite = _kingSprites.Get(_data.GetLine(_line).Mood);
            _king.color = Color.white;
            king.localRotation = Quaternion.identity;
            king.localScale = Vector3.zero;
            _king.gameObject.SetActive(true);
            // 쏙 사라진 각성 왕 자리에서 말랑하게 늘었다 줄며 튀어나온다. 다 나온 뒤에 말한다.
            // 말을 먼저 띄우면 표정 뽀잉이 크기를 바로 원래대로 덮어써 "탁" 하고 나타나 보였다.
            Vector3 wide = new Vector3(_kingScale.x * 1.25f, _kingScale.y * 0.8f, _kingScale.z);
            Vector3 tall = new Vector3(_kingScale.x * 0.9f, _kingScale.y * 1.12f, _kingScale.z);
            _sequence?.Kill();
            _sequence = DOTween.Sequence()
                .Append(king.DOScale(wide, _revertPopDuration * 0.5f).SetEase(Ease.OutQuad))
                .Append(king.DOScale(tall, _revertPopDuration * 0.25f).SetEase(Ease.InOutQuad))
                .Append(king.DOScale(_kingScale, _revertPopDuration * 0.25f).SetEase(Ease.OutQuad))
                .OnComplete(_onShowLine)
                .SetLink(gameObject);
        }

        // ── 엔딩 연출: 줄의 시작·끝 연출. 다 끝나면 done을 부른다.

        private void PlayAction(DialogueAction action, TweenCallback done)
        {
            _action?.Kill();
            switch (action)
            {
                case DialogueAction.ThrowBook:
                    _action = ThrowBook().OnComplete(done);
                    break;
                case DialogueAction.KingFlyAway:
                    _action = FlyAway().OnComplete(done);
                    break;
                case DialogueAction.PlayerJumpToBook:
                    _action = JumpToBook().OnComplete(done);
                    break;
                case DialogueAction.PlayerSuckedIn:
                    _action = SuckIntoBook().OnComplete(done);
                    break;
                default:
                    done();
                    return;
            }

            _action.SetLink(gameObject);
        }

        // 왕 손에서 왕과 고블린 사이 바닥으로 포물선을 그리며 빙글 돌아 떨어진다. 닿는 순간 납작하게 눌렸다 돌아온다.
        private Tween ThrowBook()
        {
            Transform book = _book.transform;
            Vector2 from = _king.gameObject.activeSelf ? (Vector2)_king.bounds.center : (Vector2)_boss.position;
            Vector2 king = _king.gameObject.activeSelf ? (Vector2)_king.transform.position : (Vector2)_boss.position;
            Vector2 to = _player != null ? Vector2.Lerp(king, _player.position, 0.5f) : king + Vector2.right * 2f;
            _book.enabled = true;
            book.position = from;
            book.localScale = _bookScale * 0.4f;
            book.localRotation = Quaternion.identity;
            return DOTween.Sequence()
                .Append(DOVirtual.Float(0f, 1f, _throwDuration, t =>
                {
                    book.position = Vector2.Lerp(from, to, t) + Vector2.up * (4f * _throwArc * t * (1f - t));
                    book.localScale = _bookScale * Mathf.Lerp(0.4f, 1f, t);
                    book.localRotation = Quaternion.Euler(0f, 0f, -360f * t);
                }).SetEase(Ease.Linear))
                .AppendCallback(() => AudioManager.Play(_bookLandSound))
                .Append(book.DOPunchScale(new Vector3(0.2f, -0.2f, 0f) * _bookScale.x, 0.25f, 6));
        }

        // 2등신 왕이 살짝 웅크렸다가 하늘로 쏜살같이 솟구쳐 사라진다.
        private Tween FlyAway()
        {
            Transform king = _king.transform;
            Vector3 start = king.position;
            AudioManager.Play(_flyAwaySound);
            return DOTween.Sequence()
                .Append(king.DOScale(new Vector3(_kingScale.x * 1.2f, _kingScale.y * 0.75f, _kingScale.z), 0.12f))
                .Append(king.DOScale(new Vector3(_kingScale.x * 0.8f, _kingScale.y * 1.3f, _kingScale.z), 0.08f))
                .Append(king.DOMove(start + Vector3.up * _flyAwayHeight, _flyAwayDuration).SetEase(Ease.InQuad))
                .Join(king.DOLocalRotate(new Vector3(0f, 0f, 20f * _side), _flyAwayDuration))
                .AppendCallback(() => _king.gameObject.SetActive(false));
        }

        // 고블린이 책 한가운데로 포물선 점프해 탁 착지한다.
        private Tween JumpToBook()
        {
            Vector2 from = _player.position;
            Vector2 to = _book.transform.position;
            AudioManager.Play(_jumpSound);
            return DOTween.Sequence()
                .Append(DOVirtual.Float(0f, 1f, _jumpDuration, t =>
                {
                    _player.position = Vector2.Lerp(from, to, t) + Vector2.up * (4f * _jumpArc * t * (1f - t));
                }).SetEase(Ease.Linear))
                .AppendCallback(() => AudioManager.Play(_bookLandSound))
                .Append(_player.DOPunchScale(new Vector3(0.25f, -0.25f, 0f) * _playerScale.x, 0.25f, 6));
        }

        // 책이 번쩍이며 부풀고, 고블린이 빙글빙글 돌며 작아져 책 한가운데로 빨려 들어간다. 고블린 위 말풍선도 같이 돌며 빨려 들어간다.
        private Tween SuckIntoBook()
        {
            Vector2 from = _player.position;
            Vector2 to = _book.transform.position;
            Transform bubble = _kingBubble.gameObject.activeSelf ? _kingBubble.transform : null;
            Vector2 bubbleOffset = bubble != null ? (Vector2)bubble.position - from : Vector2.zero;
            AudioManager.Play(_suckSound);
            return DOTween.Sequence()
                .Append(_book.transform.DOPunchScale(_bookScale * 0.15f, 0.4f, 4))
                .Join(_book.DOColor(new Color(1f, 1f, 0.75f, 1f), 0.2f).SetLoops(2, LoopType.Yoyo))
                .Append(DOVirtual.Float(0f, 1f, _suckDuration, t =>
                {
                    float eased = t * t;
                    _player.position = Vector2.Lerp(from, to, eased);
                    _player.localScale = _playerScale * (1f - eased);
                    _player.localRotation = Quaternion.Euler(0f, 0f, -360f * _suckSpins * eased);
                    if (bubble != null)
                    {
                        // 말풍선은 고블린을 따라 붙은 채, 고블린보다 조금 더 크게 휘돌며 줄어든다.
                        bubble.position = (Vector2)_player.position + (Vector2)(Quaternion.Euler(0f, 0f, -360f * _suckSpins * eased) * bubbleOffset) * (1f - eased);
                        bubble.localRotation = Quaternion.Euler(0f, 0f, -360f * _suckSpins * eased);
                        bubble.localScale = _kingBubbleScale * (1f - eased);
                    }
                }).SetEase(Ease.Linear))
                .AppendCallback(ResetBubbleTransform)
                .Append(_book.transform.DOPunchScale(_bookScale * 0.2f, 0.3f, 6));
        }

        // 스티커가 떼어지듯 젖혀지며 납작해져 사라진다.
        private void Leave()
        {
            _isTalking = false;
            HideSkipButton();
            _punch?.Kill();
            // 왕이 곧 보스면 떼어지지 않고 그 자리에서 바로 보스 몸으로 바뀐다. 처치 대화에서 2등신으로 돌아온 왕은 스티커처럼 떠난다.
            // 이미 하늘로 날아가 사라진 왕은 떼어 낼 것이 없다.
            if ((_data.KingBecomesBoss && !_isReverted) || !_king.gameObject.activeSelf)
            {
                _sequence?.Kill();
                Finish();
                return;
            }

            Transform king = _king.transform;
            _sequence?.Kill();
            _sequence = DOTween.Sequence()
                .Append(king.DOLocalRotate(new Vector3(0f, 0f, 25f * _side), _leaveDuration).SetEase(Ease.InQuad))
                .Join(king.DOScaleY(0f, _leaveDuration).SetEase(Ease.InBack))
                .Join(_king.DOFade(0f, _leaveDuration))
                .OnComplete(Finish)
                .SetLink(gameObject);
        }

        private void Finish()
        {
            HideAll();
            IsPlaying = false;
        }

        private void HideSkipButton()
        {
            if (_skipButton != null)
            {
                _skipButton.DOKill();
                _skipButton.gameObject.SetActive(false);
            }
        }

        // 빨려 들어가며 돌고 줄어든 말풍선을 숨기고 원래 모양으로 되돌려 둔다.
        private void ResetBubbleTransform()
        {
            _kingBubble.Hide();
            _kingBubble.transform.localRotation = Quaternion.identity;
            _kingBubble.transform.localScale = _kingBubbleScale;
        }

        private void HideAll()
        {
            _kingBubble.transform.localRotation = Quaternion.identity;
            _kingBubble.transform.localScale = _kingBubbleScale;
            HideSkipButton();
            _king.gameObject.SetActive(false);
            _kingBubble.Hide();
            _bossBubble.Hide();
        }

        private SpeechBubble CurrentBubble()
        {
            BossDialogueData.Line line = _data.GetLine(_line);
            return UsesKingBubble(line) || (line.Speaker == DialogueSpeaker.Player && _player != null) ? _kingBubble : _bossBubble;
        }

        // 왕이 보스로 바뀐 뒤의 왕 대사는 보스 머리 위에 띄운다.
        // 처치 대화의 왕 줄은 쓰러진 보스 머리 위에 띄운다(왕 그림이 없다).
        private bool UsesKingBubble(BossDialogueData.Line line)
        {
            if (line.Speaker != DialogueSpeaker.King)
            {
                return false;
            }

            return _isReverted || (!(_data.KingBecomesBoss && _boss != null) && _player == null);
        }

        private KingMood FirstKingMood()
        {
            for (int i = 0; i < _data.LineCount; i++)
            {
                if (_data.GetLine(i).Speaker == DialogueSpeaker.King)
                {
                    return _data.GetLine(i).Mood;
                }
            }

            return KingMood.Smug;
        }

        private static float TopOf(Transform target)
        {
            float top = float.NegativeInfinity;
            SpriteRenderer[] renderers = target.GetComponentsInChildren<SpriteRenderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].enabled && renderers[i].sprite != null)
                {
                    top = Mathf.Max(top, renderers[i].bounds.max.y);
                }
            }

            return float.IsNegativeInfinity(top) ? target.position.y + 1f : top;
        }
    }
}
