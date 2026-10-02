using UnityEngine;

namespace ExplodeIt.Player
{
    // 주인공 그림의 트윈 동작 수치. 판정과 무관하고 그림(Body)만 움직인다.
    // 기울기 각도는 "+ = 커서 쪽으로 숙임, - = 뒤로 젖힘"으로 통일한다.
    [CreateAssetMenu(fileName = "Motion_Player", menuName = "Explode It/Player Motion")]
    public class PlayerMotionData : ScriptableObject
    {
        [Header("복귀 탄성")]
        [Tooltip("자세가 목표로 돌아가는 빠르기 (Hz). 높을수록 빠릿하게 돌아온다")]
        [SerializeField, Min(0.1f)] private float _springFrequency = 7f;

        [Tooltip("복귀 감쇠 (0~1). 낮을수록 출렁이며 돌아오고, 1이면 출렁임 없이 멈춘다")]
        [SerializeField, Range(0.05f, 1f)] private float _springDamping = 0.4f;

        [Header("대기: 숨쉬기")]
        [Tooltip("숨쉬는 빠르기 (회/초)")]
        [SerializeField, Min(0f)] private float _breathRate = 0.8f;

        [Tooltip("숨쉴 때 세로로 늘었다 줄었다 하는 정도 (비율)")]
        [SerializeField, Min(0f)] private float _breathAmount = 0.03f;

        [Header("걷기: 통통 뛰기")]
        [Tooltip("초당 뛰는 횟수 (회/초)")]
        [SerializeField, Min(0f)] private float _hopRate = 4f;

        [Tooltip("뛰는 높이 (유닛)")]
        [SerializeField, Min(0f)] private float _hopHeight = 0.1f;

        [Tooltip("공중에서 위로 늘어나는 정도 (비율)")]
        [SerializeField, Min(0f)] private float _hopStretch = 0.06f;

        [Tooltip("착지 순간 납작해지는 정도 (비율)")]
        [SerializeField, Min(0f)] private float _hopSquash = 0.12f;

        [Tooltip("한 번 뛸 때마다 좌우로 번갈아 기우는 각도 (도)")]
        [SerializeField, Min(0f)] private float _hopTilt = 5f;

        [Header("구르기")]
        [Tooltip("구르기 중 가장 눌렸을 때 크기 배율 (가로, 세로)")]
        [SerializeField] private Vector2 _dodgeScale = new Vector2(1.2f, 0.7f);

        [Tooltip("구르기 시작부터 가장 눌린 모양까지 걸리는 시간 (초)")]
        [SerializeField, Min(0.01f)] private float _dodgeSquashTime = 0.1f;

        [Tooltip("가장 눌린 모양에서 원래 크기로 펴지는 시간 (초). 끝에서 살짝 넘쳤다가 멈춘다")]
        [SerializeField, Min(0.01f)] private float _dodgeRecoverTime = 0.3f;

        [Header("던지기: 순간 자세 → 탄성 복귀")]
        [Tooltip("던지는 순간 크기 배율 (가로, 세로)")]
        [SerializeField] private Vector2 _throwScale = new Vector2(0.9f, 1.1f);

        [Tooltip("던지는 순간 기울기 (도). 커서 쪽으로 숙인다")]
        [SerializeField] private float _throwAngle = 10f;

        public float SpringFrequency => _springFrequency;
        public float SpringDamping => _springDamping;
        public float BreathRate => _breathRate;
        public float BreathAmount => _breathAmount;
        public float HopRate => _hopRate;
        public float HopHeight => _hopHeight;
        public float HopStretch => _hopStretch;
        public float HopSquash => _hopSquash;
        public float HopTilt => _hopTilt;
        public Vector2 DodgeScale => _dodgeScale;
        public float DodgeSquashTime => _dodgeSquashTime;
        public float DodgeRecoverTime => _dodgeRecoverTime;
        public Vector2 ThrowScale => _throwScale;
        public float ThrowAngle => _throwAngle;
    }
}
