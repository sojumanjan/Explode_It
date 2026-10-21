using ExplodeIt.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace ExplodeIt.Progression
{
    // 저주 "덫": 주기마다 플레이어 근처에 덫이 깔린다. 밟는 순간 덫이 닫히며 플레이어가 맞는다. 적은 밟지 않는다.
    // 대시(구르기) 중에는 덫을 뛰어넘는다. 폭탄이나 운석에 휩쓸리면 덫이 부서진다.
    // 덫은 오래 두면 맵을 덮으므로 수명과 최대 개수를 둔다.
    [CreateAssetMenu(fileName = "Curse_Trap", menuName = "Explode It/Curses/Trap")]
    public class TrapCurseData : CurseData
    {
        [Header("깔기")]
        [Tooltip("덫이 깔리는 간격 (초)")]
        [SerializeField, Min(0.5f)] private float _interval = 10f;

        [Tooltip("판이 시작되고 첫 덫까지 시간 (초)")]
        [SerializeField, Min(0f)] private float _firstDelay = 5f;

        [Tooltip("한 번에 깔리는 덫 수 (개)")]
        [SerializeField, Min(1)] private int _count = 3;

        [Header("플레이어와의 거리")]
        [Tooltip("플레이어에게서 최소 이만큼 떨어져 깔린다 (유닛). 발밑에 바로 깔리지 않게 한다")]
        [SerializeField, Min(0f), FormerlySerializedAs("_minRadius")] private float _minDistance = 1.5f;

        [Tooltip("플레이어에게서 최대 이만큼 떨어진 곳까지 깔린다 (유닛)")]
        [SerializeField, Min(0f), FormerlySerializedAs("_maxRadius")] private float _maxDistance = 4f;

        [Tooltip("깔리고 나서 밟아도 발동하지 않는 시간 (초). 그동안 서서히 나타난다")]
        [SerializeField, Min(0f)] private float _appearTime = 0.5f;

        [Tooltip("밟지 않은 덫이 사라지기까지 시간 (초)")]
        [SerializeField, Min(1f)] private float _lifetime = 25f;

        [Tooltip("맵에 동시에 있을 수 있는 덫 수 (개). 넘으면 가장 오래된 덫부터 사라진다")]
        [SerializeField, Min(1)] private int _maxTraps = 9;

        [Header("발동")]
        [Tooltip("플레이어 중심이 이 거리 안으로 들어오면 밟은 것으로 본다 (유닛)")]
        [SerializeField, Min(0.05f)] private float _triggerRadius = 0.35f;

        [Header("표시와 연출")]
        [Tooltip("덫 그림")]
        [SerializeField] private Sprite _trapSprite;

        [Tooltip("밟힌 덫 그림이 탁 닫히고 사라지기까지 시간 (초)")]
        [SerializeField, Min(0.05f)] private float _snapVanishTime = 0.35f;

        [Tooltip("밟힘 판정 원 색 (덫 밑에 깔린다)")]
        [SerializeField] private Color _idleColor = new Color(0.3f, 0.15f, 0.05f, 0.35f);

        [SerializeField] private SoundData _placeSound;
        [Tooltip("밟혔을 때 소리 (덫이 탁 닫히는 소리)")]
        [SerializeField] private SoundData _triggerSound;
        [Tooltip("밟혔을 때 연출 (흔들림·이펙트)")]
        [SerializeField] private FeedbackData _triggerFeedback;
        [Tooltip("폭탄·운석에 부서질 때 연출")]
        [SerializeField] private FeedbackData _breakFeedback;
        [Tooltip("폭탄·운석에 부서질 때 소리")]
        [SerializeField] private SoundData _breakSound;

        public override CurseKind Kind => CurseKind.Trap;
        public float Interval => _interval;
        public float FirstDelay => _firstDelay;
        public int Count => _count;
        public float MinDistance => _minDistance;
        public float MaxDistance => Mathf.Max(_minDistance, _maxDistance);
        public float AppearTime => _appearTime;
        public float Lifetime => _lifetime;
        public int MaxTraps => _maxTraps;
        public float TriggerRadius => _triggerRadius;
        public Sprite TrapSprite => _trapSprite;
        public float SnapVanishTime => _snapVanishTime;
        public Color IdleColor => _idleColor;
        public SoundData PlaceSound => _placeSound;
        public SoundData TriggerSound => _triggerSound;
        public FeedbackData TriggerFeedback => _triggerFeedback;
        public FeedbackData BreakFeedback => _breakFeedback;
        public SoundData BreakSound => _breakSound;
    }
}
