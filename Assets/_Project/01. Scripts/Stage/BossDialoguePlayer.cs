using System;
using DG.Tweening;
using ExplodeIt.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ExplodeIt.Stage
{
    // 스토리 보스전 등장 대화. 보스보다 왕이 먼저 보스 자리 옆에 스티커처럼 찰싹 붙어 나와 말하고,
    // 데이터에 표시한 줄이 시작될 때 보스를 불러내고(BossSpawnRequested), 표시한 줄을 넘길 때 보스 등장 동작을 시작시킨다(EntranceRequested).
    // 줄은 클릭해야 넘어간다. 글자가 다 나오면 말풍선에 ▼가 통통 튀어 클릭을 유도한다.
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

        private BossDialogueData _data;
        private Transform _boss;
        private Vector2 _bossPoint;
        private float _bossTopOffset;
        private int _line;
        private bool _isTalking;
        private float _side = 1f;
        private Vector3 _kingScale;
        private Sequence _sequence;
        private Tween _punch;

        public bool IsPlaying { get; private set; }

        public event Action BossSpawnRequested;
        public event Action EntranceRequested;

        private void Awake()
        {
            _kingScale = _king.transform.localScale;
            HideAll();
        }

        private void OnDisable()
        {
            _sequence?.Kill();
            _punch?.Kill();
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
            _bossPoint = bossPoint;
            _bossTopOffset = _emptyBossHeight;
            _line = -1;
            _isTalking = false;
            IsPlaying = true;

            _side = player.x >= bossPoint.x ? 1f : -1f;
            Transform king = _king.transform;
            king.position = bossPoint + Vector2.right * (_side * _kingSideDistance);
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
        }

        // 보스가 내려앉은 뒤 불린다. 머리 높이는 이때 한 번만 잰다. 대화 동안 보스는 제자리에 있다.
        public void SetBoss(Transform boss)
        {
            _boss = boss;
            if (boss != null)
            {
                _bossTopOffset = TopOf(boss) - boss.position.y;
            }
        }

        private void Update()
        {
            if (!_isTalking || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
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

        private void NextLine()
        {
            // 등장 동작은 표시한 줄을 다 읽고 넘기는 순간 시작한다. 마지막 줄에 표시하면 대화가 끝나며 시작한다.
            if (_line >= 0 && _data.GetLine(_line).StartEntrance)
            {
                EntranceRequested?.Invoke();
            }

            _line++;
            _kingBubble.Hide();
            _bossBubble.Hide();
            if (_line >= _data.LineCount)
            {
                Leave();
                return;
            }

            BossDialogueData.Line line = _data.GetLine(_line);
            if (line.SpawnBoss || (_line == 0 && !_data.HasSpawnLine))
            {
                BossSpawnRequested?.Invoke();
            }

            _isTalking = true;
            if (line.Speaker == DialogueSpeaker.King)
            {
                // 표정이 바뀔 때마다 뽀잉 하고 튀어서 대사보다 표정이 먼저 읽히게 한다.
                _king.sprite = _kingSprites.Get(line.Mood);
                _punch?.Kill();
                _king.transform.localScale = _kingScale;
                _punch = _king.transform.DOPunchScale(_kingScale * _moodPunch, 0.25f, 6).SetLink(gameObject);
                _kingBubble.transform.position = new Vector3(_king.transform.position.x, _king.bounds.max.y + _bubbleGap, 0f);
                _kingBubble.Show(line.Text);
            }
            else
            {
                Vector2 boss = _boss != null ? (Vector2)_boss.position : _bossPoint;
                _bossBubble.transform.position = new Vector3(boss.x, boss.y + _bossTopOffset + _bubbleGap, 0f);
                _bossBubble.Show(line.Text);
            }
        }

        // 스티커가 떼어지듯 젖혀지며 납작해져 사라진다.
        private void Leave()
        {
            _isTalking = false;
            _punch?.Kill();
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

        private void HideAll()
        {
            _king.gameObject.SetActive(false);
            _kingBubble.Hide();
            _bossBubble.Hide();
        }

        private SpeechBubble CurrentBubble()
        {
            return _data.GetLine(_line).Speaker == DialogueSpeaker.King ? _kingBubble : _bossBubble;
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
