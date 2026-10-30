using System.Collections.Generic;
using DG.Tweening;
using ExplodeIt.Core;
using ExplodeIt.Stage;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExplodeIt.UI
{
    // 우측 하단 왕 컷인(스토리 전용). 상황마다 왕이 튀어 올라와 한마디 하고 쏙 들어간다.
    // 대사는 KingRemarkData 하나하나가 한 줄이고, 같은 상황 대사가 여럿이면 직전과 다른 것을 무작위로 고른다.
    // 사망 슬로모션 중에도 나오도록 실제 시간으로 움직인다.
    public class KingCutInView : MonoBehaviour
    {
        [SerializeField] private KingSpriteSet _kingSprites;
        [SerializeField] private RectTransform _panel;
        [SerializeField] private Image _king;
        [SerializeField] private SpeechBubble _bubble;
        [SerializeField] private KingRemarkData[] _remarks;
        // 고블린이 혼잣말로 힌트를 줄 때 왕 대신 보여 줄 그림.
        [SerializeField] private Sprite _playerPortrait;
        // 다중 처치는 자주 터지므로 이 시간(초) 안에는 다시 나오지 않는다. 나머지 상황은 정해진 순간에 한 번씩이라 막지 않는다.
        [SerializeField, Min(0f)] private float _multiKillCooldown = 8f;
        // 패널이 숨어 있을 때와 나와 있을 때의 높이(캔버스 단위), 오르내리는 시간(초), 표정 바뀔 때 뽀잉 세기(비율).
        [SerializeField] private float _hiddenY = -480f;
        [SerializeField] private float _shownY;
        [SerializeField, Min(0f)] private float _slideDuration = 0.3f;
        [SerializeField, Min(0f)] private float _moodPunch = 0.15f;
        // 쓰러진 뒤에는 결과 창이 뜨므로, 우측 하단 컷인이 결과 창 버튼을 가렸다(테스터 피드백).
        // 그래서 쓰러진 순간 컷인을 거두고, 놀리는 대사는 결과 창 안의 이 칸에 넣는다. 결과 창과 함께 보인다.
        [SerializeField] private GameObject _resultRemark;
        [SerializeField] private Image _resultKing;
        [SerializeField] private TMP_Text _resultLine;

        private readonly List<KingRemarkData> _candidates = new List<KingRemarkData>();
        private bool _isStoryMode;
        private bool _isLocked;
        // 판이 끝나 결과 창이 뜨는 중. 이때는 컷인을 띄우지 않는다.
        private bool _isRunOver;
        private bool _isShown;
        private bool _isTalking;
        private int _kills;
        private int _resumedKills;
        private float _lastMultiKillTime = float.NegativeInfinity;
        private float _holdTimer;
        private KingRemarkData _last;
        private string _currentText;
        private float _currentHold;
        private Sequence _sequence;
        private Tween _punch;
        private TweenCallback _onSlidIn;
        private TweenCallback _onSlidOut;
        // 인스펙터에 잡아 둔 왕 그림 높이(캔버스 단위). 표정 그림마다 가로세로 비율만 맞춘다.
        private float _kingSize;
        private float _resultKingSize;
        // 결과 창 칸은 가로 정렬 그룹이 그림과 대사의 크기를 읽어 가운데에 늘어놓는다. 그림 크기는 여기로 알려 준다.
        private LayoutElement _resultKingLayout;

        private void Awake()
        {
            // 패널에 늘려 붙인 그림이면 크기를 직접 정할 수 없으므로, 패널 아래 가운데에 세워 크기를 코드에서 정한다.
            RectTransform king = _king.rectTransform;
            _kingSize = king.rect.height;
            king.anchorMin = new Vector2(0.5f, 0f);
            king.anchorMax = new Vector2(0.5f, 0f);
            king.pivot = new Vector2(0.5f, 0f);
            king.anchoredPosition = Vector2.zero;
            king.sizeDelta = new Vector2(_kingSize, _kingSize);
            _onSlidIn = StartTalking;
            _onSlidOut = OnSlidOut;
            SetPanelY(_hiddenY);
            _bubble.Hide();
            _king.enabled = false;
            if (_resultRemark != null)
            {
                _resultKingSize = _resultKing.rectTransform.rect.height;
                _resultKingLayout = _resultKing.GetComponent<LayoutElement>();
                _resultRemark.SetActive(false);
            }
        }

        private void OnEnable()
        {
            GameEvents.StageStarted += OnStageStarted;
            GameEvents.StoryResumed += OnStoryResumed;
            GameEvents.WaveStarted += OnWaveStarted;
            GameEvents.BombExploded += OnBombExploded;
            GameEvents.EnemyKilled += OnEnemyKilled;
            GameEvents.BossDefeated += OnBossDefeated;
            GameEvents.PlayerDied += OnPlayerDied;
            GameEvents.StageCleared += OnStageCleared;
            GameEvents.PlayerControlLockChanged += OnControlLockChanged;
            GameEvents.PlayerHintRequested += OnPlayerHintRequested;
        }

        private void OnDisable()
        {
            GameEvents.StageStarted -= OnStageStarted;
            GameEvents.StoryResumed -= OnStoryResumed;
            GameEvents.WaveStarted -= OnWaveStarted;
            GameEvents.BombExploded -= OnBombExploded;
            GameEvents.EnemyKilled -= OnEnemyKilled;
            GameEvents.BossDefeated -= OnBossDefeated;
            GameEvents.PlayerDied -= OnPlayerDied;
            GameEvents.StageCleared -= OnStageCleared;
            GameEvents.PlayerControlLockChanged -= OnControlLockChanged;
            GameEvents.PlayerHintRequested -= OnPlayerHintRequested;
            _sequence?.Kill();
            _punch?.Kill();
        }

        private void OnStageStarted(bool isStoryMode)
        {
            _isStoryMode = isStoryMode;
            _isRunOver = false;
            // 중간부터 시작하면 건너뛴 웨이브의 처치 수 대사가 다시 나오지 않게 이어 센다.
            _kills = _resumedKills;
        }

        // 판 시작 알림보다 먼저 오므로, 처치 수를 판 시작에서 0으로 되돌린 뒤 이어 세도록 따로 들고 있는다.
        private void OnStoryResumed(int checkpoint, int startKills)
        {
            _resumedKills = startKills;
        }

        private void OnWaveStarted(int waveNumber)
        {
            TryShow(KingRemarkTrigger.WaveStart, waveNumber);
        }

        private void OnBombExploded(Vector2 position, float radius, int hitCount)
        {
            if (Time.unscaledTime < _lastMultiKillTime + _multiKillCooldown)
            {
                return;
            }

            if (TryShow(KingRemarkTrigger.MultiKill, hitCount))
            {
                _lastMultiKillTime = Time.unscaledTime;
            }
        }

        private void OnEnemyKilled(Vector2 position)
        {
            _kills++;
            TryShow(KingRemarkTrigger.KillCount, _kills);
        }

        private void OnBossDefeated(int bossNumber)
        {
            TryShow(KingRemarkTrigger.BossDefeated, bossNumber);
        }

        private void OnPlayerDied()
        {
            EndRun();
            if (!_isStoryMode || _resultRemark == null || _remarks == null)
            {
                return;
            }

            KingRemarkData remark = Pick(KingRemarkTrigger.PlayerDied, 0);
            if (remark == null)
            {
                return;
            }

            _last = remark;
            Sprite sprite = _kingSprites.Get(remark.Mood);
            _resultKing.sprite = sprite;
            // 칸 안에서 그림과 대사를 함께 가운데 정렬하므로, 그림 칸을 표정 그림 비율에 맞춰 빈 여백이 생기지 않게 한다.
            if (sprite != null)
            {
                float width = _resultKingSize * sprite.rect.width / sprite.rect.height;
                _resultKing.rectTransform.sizeDelta = new Vector2(width, _resultKingSize);
                if (_resultKingLayout != null)
                {
                    _resultKingLayout.preferredWidth = width;
                    _resultKingLayout.preferredHeight = _resultKingSize;
                }
            }

            _resultLine.text = remark.Text;
            _resultRemark.SetActive(true);
        }

        private void OnStageCleared()
        {
            EndRun();
        }

        // 결과 창이 뜨기 전에 컷인을 내리고, 그 뒤로는 새 컷인을 띄우지 않는다.
        private void EndRun()
        {
            _isRunOver = true;
            if (_isShown)
            {
                SlideOut();
            }
        }

        // 보스 등장 대화 동안은 맵 위 왕이 주인공이라 컷인을 바로 거둔다.
        private void OnControlLockChanged(bool isLocked)
        {
            _isLocked = isLocked;
            if (isLocked && _isShown)
            {
                SlideOut();
            }
        }

        private bool TryShow(KingRemarkTrigger trigger, int value)
        {
            if (!_isStoryMode || _isLocked || _isRunOver || _remarks == null)
            {
                return false;
            }

            KingRemarkData remark = Pick(trigger, value);
            if (remark == null)
            {
                return false;
            }

            Show(remark);
            return true;
        }

        // 상황에 맞는 대사 중 가장 딱 맞는 것들(웨이브·보스 번호 일치, 다중 처치는 가장 큰 기준)만 남기고 그중 하나를 고른다.
        private KingRemarkData Pick(KingRemarkTrigger trigger, int value)
        {
            _candidates.Clear();
            int best = -1;
            for (int i = 0; i < _remarks.Length; i++)
            {
                KingRemarkData remark = _remarks[i];
                if (remark == null || remark.Trigger != trigger)
                {
                    continue;
                }

                int score = Score(remark, value);
                if (score < 0)
                {
                    continue;
                }

                if (score > best)
                {
                    best = score;
                    _candidates.Clear();
                }

                if (score == best)
                {
                    _candidates.Add(remark);
                }
            }

            if (_candidates.Count > 1)
            {
                _candidates.Remove(_last);
            }

            return _candidates.Count > 0 ? _candidates[Random.Range(0, _candidates.Count)] : null;
        }

        // 음수면 해당 없음. 클수록 더 딱 맞는 대사다.
        private static int Score(KingRemarkData remark, int value)
        {
            switch (remark.Trigger)
            {
                case KingRemarkTrigger.WaveStart:
                case KingRemarkTrigger.BossDefeated:
                    return remark.Value == value ? 1 : remark.Value == 0 ? 0 : -1;
                case KingRemarkTrigger.MultiKill:
                    return remark.Value > 0 && value >= remark.Value ? remark.Value : -1;
                case KingRemarkTrigger.KillCount:
                    return remark.Value == value ? 0 : -1;
                default:
                    return 0;
            }
        }

        // 연출로 조작이 막힌 동안에는 띄우지 않는다(보스 대화가 주인공이다).
        private void OnPlayerHintRequested(string text, float hold)
        {
            if (_isLocked || _isRunOver || string.IsNullOrEmpty(text) || _playerPortrait == null)
            {
                return;
            }

            ShowLine(_playerPortrait, text, hold);
        }

        private void Show(KingRemarkData remark)
        {
            _last = remark;
            ShowLine(_kingSprites.Get(remark.Mood), remark.Text, remark.Hold);
        }

        // 이미 나와 있으면 내려가지 않고 그림만 바꿔 바로 다음 말을 한다.
        private void ShowLine(Sprite sprite, string text, float hold)
        {
            _currentText = text;
            _currentHold = hold;
            _king.sprite = sprite;
            _king.enabled = true;
            FitKingSize();
            _bubble.Hide();
            _isTalking = false;

            _sequence?.Kill();
            if (_isShown)
            {
                SetPanelY(_shownY);
                StartTalking();
                return;
            }

            _isShown = true;
            _sequence = DOTween.Sequence()
                .Append(_panel.DOAnchorPosY(_shownY, _slideDuration).SetEase(Ease.OutBack))
                .OnComplete(_onSlidIn)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void StartTalking()
        {
            _punch?.Kill();
            _king.rectTransform.localScale = Vector3.one;
            _punch = _king.rectTransform.DOPunchScale(Vector3.one * _moodPunch, 0.25f, 6).SetUpdate(true).SetLink(gameObject);
            _bubble.Show(_currentText);
            _holdTimer = _currentHold;
            _isTalking = true;
        }

        private void Update()
        {
            if (!_isTalking || _bubble.IsTyping)
            {
                return;
            }

            _holdTimer -= Time.unscaledDeltaTime;
            if (_holdTimer <= 0f)
            {
                SlideOut();
            }
        }

        private void SlideOut()
        {
            _isTalking = false;
            _bubble.Hide();
            _sequence?.Kill();
            _sequence = DOTween.Sequence()
                .Append(_panel.DOAnchorPosY(_hiddenY, _slideDuration).SetEase(Ease.InBack))
                .OnComplete(_onSlidOut)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void OnSlidOut()
        {
            _isShown = false;
            _king.enabled = false;
        }

        // 왕 그림은 고해상도(300px)라 부드럽게 줄여 그리면 또렷하다. 예전 100px 도트 그림처럼 화면 픽셀 정수 배율로 맞추면
        // 300px 단위로 크기가 크게 튀므로, 인스펙터에 잡아 둔 높이 그대로 두고 비율만 맞춘다.
        private void FitKingSize()
        {
            Sprite sprite = _king.sprite;
            if (sprite == null)
            {
                return;
            }

            _king.rectTransform.sizeDelta = new Vector2(_kingSize * sprite.rect.width / sprite.rect.height, _kingSize);
            _king.rectTransform.anchoredPosition = Vector2.zero;
        }

        private void SetPanelY(float y)
        {
            Vector2 position = _panel.anchoredPosition;
            position.y = y;
            _panel.anchoredPosition = position;
        }
    }
}
