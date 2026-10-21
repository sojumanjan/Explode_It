using ExplodeIt.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace ExplodeIt.Progression
{
    // 저주 "운석": 주기마다 플레이어 근처 무작위 자리에 운석이 떨어진다. 원이 다 차면 떨어지고, 적도 맞으면 죽는다.
    [CreateAssetMenu(fileName = "Curse_MeteorRain", menuName = "Explode It/Curses/Meteor Rain")]
    public class MeteorRainCurseData : CurseData
    {
        [Header("운석")]
        [Tooltip("운석이 떨어지는 간격 (초)")]
        [SerializeField, Min(0.5f)] private float _interval = 5f;

        [Tooltip("판이 시작되고 첫 운석까지 시간 (초)")]
        [SerializeField, Min(0f)] private float _firstDelay = 3f;

        [Tooltip("한 번에 떨어지는 운석 수 (개)")]
        [SerializeField, Min(1)] private int _count = 1;

        [Header("플레이어와의 거리")]
        [Tooltip("운석이 떨어지는 곳의 플레이어로부터 최소 거리 (유닛). 0이면 발밑에도 떨어진다")]
        [SerializeField, Min(0f)] private float _minDistance;

        [Tooltip("운석이 떨어지는 곳의 플레이어로부터 최대 거리 (유닛)")]
        [SerializeField, Min(0f), FormerlySerializedAs("_areaRadius")] private float _maxDistance = 3f;

        [Tooltip("운석 폭발 반경 (유닛)")]
        [SerializeField, Min(0.1f)] private float _radius = 1.3f;

        [Tooltip("원이 깔리고 운석이 떨어지기까지 시간 (초). 원이 차오르는 시간이 곧 예고다")]
        [SerializeField, Min(0.1f)] private float _fallTime = 1.2f;

        [Header("연출")]
        [SerializeField] private FeedbackData _impactFeedback;
        [SerializeField] private SoundData _impactSound;

        public override CurseKind Kind => CurseKind.MeteorRain;
        public float Interval => _interval;
        public float FirstDelay => _firstDelay;
        public int Count => _count;
        public float MinDistance => _minDistance;
        public float MaxDistance => Mathf.Max(_minDistance, _maxDistance);
        public float Radius => _radius;
        public float FallTime => _fallTime;
        public FeedbackData ImpactFeedback => _impactFeedback;
        public SoundData ImpactSound => _impactSound;
    }
}
