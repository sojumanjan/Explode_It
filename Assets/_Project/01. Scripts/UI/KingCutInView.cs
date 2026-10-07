using System.Collections.Generic;
using DG.Tweening;
using ExplodeIt.Core;
using ExplodeIt.Stage;
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
        // 다중 처치는 자주 터지므로 이 시간(초) 안에는 다시 나오지 않는다. 나머지 상황은 정해진 순간에 한 번씩이라 막지 않는다.
        [SerializeField, Min(0f)] private float _multiKillCooldown = 8f;
        // 패널이 숨어 있을 때와 나와 있을 때의 높이(캔버스 단위), 오르내리는 시간(초), 표정 바뀔 때 뽀잉 세기(비율).
        [SerializeField] private float _hiddenY = -480f;
        [SerializeField] private float _shownY;
        [SerializeField, Min(0f)] private float _slideDuration = 0.3f;
        [SerializeField, Min(0f)] private float _moodPunch = 0.15f;

        private readonly List<KingRemarkData> _candidates = new List<KingRemarkData>();
        private bool _isStoryMode;
        private bool _isLocked;
        private bool _isShown;
        private bool _isTalking;
        private int _kills;
        private float _lastMultiKillTime = float.NegativeInfinity;
        private float _holdTimer;
        private KingRemarkData _last;
        private KingRemarkData _current;
        private Sequence _sequence;
        private Tween _punch;
        private TweenCallback _onSlidIn;
        private TweenCallback _onSlidOut;

        private void Awake()
        {
            _onSlidIn = StartTalking;
            _onSlidOut = OnSlidOut;
            SetPanelY(_hiddenY);
            _bubble.Hide();
            _king.enabled = false;
        }

        private void OnEnable()
        {
            GameEvents.StageStarted += OnStageStarted;
            GameEvents.WaveStarted += OnWaveStarted;
            GameEvents.BombExploded += OnBombExploded;
            GameEvents.EnemyKilled += OnEnemyKilled;
            GameEvents.BossDefeated += OnBossDefeated;
            GameEvents.PlayerDied += OnPlayerDied;
            GameEvents.PlayerControlLockChanged += OnControlLockChanged;
        }

        private void OnDisable()
        {
            GameEvents.StageStarted -= OnStageStarted;
            GameEvents.WaveStarted -= OnWaveStarted;
            GameEvents.BombExploded -= OnBombExploded;
            GameEvents.EnemyKilled -= OnEnemyKilled;
            GameEvents.BossDefeated -= OnBossDefeated;
            GameEvents.PlayerDied -= OnPlayerDied;
            GameEvents.PlayerControlLockChanged -= OnControlLockChanged;
            _sequence?.Kill();
            _punch?.Kill();
        }

        private void OnStageStarted(bool isStoryMode)
        {
            _isStoryMode = isStoryMode;
            _kills = 0;
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
            TryShow(KingRemarkTrigger.PlayerDied, 0);
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
            if (!_isStoryMode || _isLocked || _remarks == null)
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

        // 이미 나와 있으면 내려가지 않고 표정만 바꿔 바로 다음 말을 한다.
        private void Show(KingRemarkData remark)
        {
            _last = remark;
            _current = remark;
            _king.sprite = _kingSprites.Get(remark.Mood);
            _king.enabled = true;
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
            _bubble.Show(_current.Text);
            _holdTimer = _current.Hold;
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

        private void SetPanelY(float y)
        {
            Vector2 position = _panel.anchoredPosition;
            position.y = y;
            _panel.anchoredPosition = position;
        }
    }
}
