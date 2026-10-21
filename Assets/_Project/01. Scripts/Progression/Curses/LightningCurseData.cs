using DG.Tweening;
using ExplodeIt.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace ExplodeIt.Progression
{
    // 저주 "낙뢰": 주기마다 플레이어가 서 있던 곳을 중심으로 아주 넓은 범위에 번개가 내리친다. 적도 맞으면 죽는다.
    // 원이 차오르지 않아 언제 칠지 알 수 없고, 치기 직전에만 번쩍인다. 밖으로 나가는 것이 정답이고, 대시는 번쩍임에 맞춰야 한다.
    [CreateAssetMenu(fileName = "Curse_Lightning", menuName = "Explode It/Curses/Lightning")]
    public class LightningCurseData : CurseData
    {
        [Header("낙뢰")]
        [Tooltip("번개가 치는 간격 (초). 예고가 뜨는 순간부터 다음 예고까지")]
        [SerializeField, Min(1f)] private float _interval = 18f;

        [Tooltip("판이 시작되고 첫 예고까지 시간 (초)")]
        [SerializeField, Min(0f)] private float _firstDelay = 10f;

        [Header("플레이어와의 거리")]
        [Tooltip("번개 중심의 플레이어로부터 최소 거리 (유닛). 0이면 플레이어 자리 그대로일 수 있다")]
        [SerializeField, Min(0f)] private float _minDistance;

        [Tooltip("번개 중심의 플레이어로부터 최대 거리 (유닛). 반경보다 작게 두면 플레이어는 늘 범위 안에서 시작한다")]
        [SerializeField, Min(0f)] private float _maxDistance = 2f;

        [Tooltip("번개 범위 반경 (유닛)")]
        [SerializeField, Min(0.5f)] private float _radius = 7f;

        [Tooltip("예고가 뜨고 번개가 치기까지 시간 (초)")]
        [SerializeField, Min(0.5f)] private float _warningTime = 5f;

        [Tooltip("치기 직전 번쩍이는 시간 (초)")]
        [SerializeField, Min(0f)] private float _flashTime = 0.5f;

        [Header("표시와 연출")]
        [Tooltip("예고 원이 0에서 원래 크기로 커지는 시간 (초). 예고 시간 안에 들어간다")]
        [SerializeField, Min(0f)] private float _appearTime = 2f;

        [Tooltip("예고 원이 커지는 빠르기 곡선. OutSine이면 처음부터 끝까지 천천히 고르게 퍼진다")]
        [SerializeField] private Ease _appearEase = Ease.OutSine;

        [Tooltip("예고 범위 색")]
        [SerializeField] private Color _rangeColor = new Color(0.55f, 0.75f, 1f, 0.18f);

        [Tooltip("치기 직전 번쩍이는 색")]
        [SerializeField] private Color _flashColor = new Color(0.9f, 0.95f, 1f, 0.55f);

        [Header("번개 줄기")]
        [Tooltip("내리치는 번개 그림 (차례로 넘기는 프레임). 그림 아래 끝(땅에 닿는 곳)이 기준점이다")]
        [SerializeField] private Sprite[] _boltFrames = new Sprite[0];

        [Tooltip("번개 프레임 하나가 보이는 시간 (초)")]
        [SerializeField, Min(0.01f)] private float _boltFrameTime = 0.05f;

        [Tooltip("번개 줄기가 보이는 시간 (초). 끝 40% 동안 흐려진다")]
        [SerializeField, Min(0.05f)] private float _boltDuration = 0.45f;

        [Tooltip("한 번에 내리치는 번개 줄기 수 (개). 하나는 늘 범위 한가운데에 친다")]
        [SerializeField, Min(1)] private int _boltCount = 5;

        [Tooltip("나머지 줄기가 떨어지는 범위 = 낙뢰 반경 × 이 비율")]
        [SerializeField, Range(0f, 1f)] private float _boltSpread = 0.75f;

        [Tooltip("번개 줄기 높이 (유닛)")]
        [SerializeField, Min(0.5f)] private float _boltHeight = 8f;

        [SerializeField] private SoundData _warnSound;
        [SerializeField] private FeedbackData _strikeFeedback;
        [SerializeField] private SoundData _strikeSound;

        public override CurseKind Kind => CurseKind.Lightning;
        public float Interval => _interval;
        public float FirstDelay => _firstDelay;
        public float Radius => _radius;
        public float MinDistance => _minDistance;
        public float MaxDistance => Mathf.Max(_minDistance, _maxDistance);
        public float WarningTime => _warningTime;
        public float FlashTime => _flashTime;
        public float AppearTime => _appearTime;
        public Ease AppearEase => _appearEase;
        public Color RangeColor => _rangeColor;
        public Color FlashColor => _flashColor;
        public SoundData WarnSound => _warnSound;
        public Sprite[] BoltFrames => _boltFrames;
        public float BoltFrameTime => _boltFrameTime;
        public float BoltDuration => _boltDuration;
        public int BoltCount => _boltCount;
        public float BoltSpread => _boltSpread;
        public float BoltHeight => _boltHeight;
        public FeedbackData StrikeFeedback => _strikeFeedback;
        public SoundData StrikeSound => _strikeSound;
    }
}
