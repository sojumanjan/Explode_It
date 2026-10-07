using DG.Tweening;
using ExplodeIt.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ExplodeIt.Stage
{
    // 스토리 보스전 등장 대화. 보스가 내려앉으면 옆에 왕이 스티커처럼 찰싹 붙어 나와 보스와 대사를 주고받고, 끝나면 떼어지듯 사라진다.
    // 진행 흐름(StageRunner)이 대화가 끝날 때까지 보스 등장 동작을 미룬다. 줄은 시간이 지나거나 클릭하면 넘어간다.
    public class BossDialoguePlayer : MonoBehaviour
    {
        [SerializeField] private KingSpriteSet _kingSprites;
        [SerializeField] private SpriteRenderer _king;
        [SerializeField] private SpeechBubble _kingBubble;
        [SerializeField] private SpeechBubble _bossBubble;
        // 왕이 보스 옆 어디에 서는지(유닛). 플레이어 쪽으로 서서 카메라 안에 들어오게 한다.
        [SerializeField, Min(0f)] private float _kingSideDistance = 2.6f;
        // 말하는 쪽 머리 위와 말풍선 사이 간격(유닛).
        [SerializeField, Min(0f)] private float _bubbleGap = 0.15f;
        // 왕이 붙고 떨어지는 시간(초), 표정이 바뀔 때 뽀잉 세기(비율).
        [SerializeField, Min(0f)] private float _appearDuration = 0.25f;
        [SerializeField, Min(0f)] private float _leaveDuration = 0.3f;
        [SerializeField, Min(0f)] private float _moodPunch = 0.25f;

        private BossDialogueData _data;
        private Transform _boss;
        private float _bossTopOffset;
        private int _line;
        private float _holdTimer;
        private bool _isTalking;
        private float _side = 1f;
        private Vector3 _kingScale;
        private Sequence _sequence;
        private Tween _punch;

        public bool IsPlaying { get; private set; }

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

        public void Play(BossDialogueData data, Transform boss, Vector2 player)
        {
            if (data == null || data.LineCount == 0 || boss == null)
            {
                return;
            }

            _data = data;
            _boss = boss;
            _line = -1;
            _isTalking = false;
            IsPlaying = true;
            _bossTopOffset = TopOf(boss) - boss.position.y;

            _side = player.x >= boss.position.x ? 1f : -1f;
            Transform king = _king.transform;
            king.position = boss.position + Vector3.right * (_side * _kingSideDistance);
            _king.sprite = _kingSprites.Get(FirstKingMood());
            _king.gameObject.SetActive(true);

            // 스티커처럼 크게 비스듬히 떴다가 찰싹 붙는다.
            _sequence?.Kill();
            king.localScale = _kingScale * 1.5f;
            king.localRotation = Quaternion.Euler(0f, 0f, -12f * _side);
            _king.color = new Color(1f, 1f, 1f, 0f);
            _sequence = DOTween.Sequence()
                .Append(king.DOScale(_kingScale, _appearDuration).SetEase(Ease.OutBack))
                .Join(king.DOLocalRotate(Vector3.zero, _appearDuration))
                .Join(_king.DOFade(1f, _appearDuration * 0.5f))
                .OnComplete(NextLine)
                .SetLink(gameObject);
        }

        private void Update()
        {
            if (!_isTalking)
            {
                return;
            }

            bool click = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            SpeechBubble bubble = CurrentBubble();
            if (bubble.IsTyping)
            {
                if (click)
                {
                    bubble.CompleteTyping();
                }

                return;
            }

            _holdTimer -= Time.deltaTime;
            if (click || _holdTimer <= 0f)
            {
                NextLine();
            }
        }

        private void NextLine()
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
            _holdTimer = line.Hold;
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
                Vector3 boss = _boss != null ? _boss.position : _king.transform.position;
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

        // 대화 시작에 한 번만 잰다. 보스는 대화 동안 움직이지 않는다.
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
