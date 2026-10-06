using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    [CreateAssetMenu(fileName = "ShieldBearerData", menuName = "Explode It/Enemy/Shield Bearer Data")]
    public class ShieldBearerData : EnemyData
    {
        [Header("방패")]
        [Tooltip("방패가 막는 각도 (도). 방패 정면 기준 전체 각도. 폭발 순간 폭탄 중심이 이 안이면 죽지 않는다")]
        [SerializeField, Range(10f, 360f)] private float _shieldAngle = 150f;

        // 방패가 휙 돌아 버리면 어디를 막는지 예측할 수 없으므로 일정한 속도로만 돈다.
        [Tooltip("방패 회전 속도 (도/초). 방패는 이동 방향을 향해 이 속도로 몸 주위를 돈다")]
        [SerializeField, Min(1f)] private float _shieldTurnSpeed = 120f;

        [Tooltip("방패와 몸 중심 사이 거리 (유닛). 방패 그림이 몸 주위를 도는 반지름")]
        [SerializeField, Min(0f)] private float _shieldOrbitRadius = 0.6f;

        [Tooltip("몸 중심 위치 (유닛, 발밑 기준). 방패가 도는 중심이자 막기 판정의 중심")]
        [SerializeField] private Vector2 _bodyCenterOffset = new Vector2(0f, 0.5f);

        [Header("메이스 내려치기")]
        [Tooltip("내려치기 반경 (유닛). 몸 앞쪽 원 범위. 본인 위치까지 들어오게 앞으로 내민 거리보다 크게 둔다")]
        [SerializeField, Min(0f)] private float _slamRadius = 1.2f;

        [Tooltip("내려치기 중심을 앞으로 내민 거리 (유닛). 몸 중심에서 플레이어 쪽으로 이만큼 앞이 원의 중심이다")]
        [SerializeField, Min(0f)] private float _slamForward = 0.6f;

        [Header("사운드")]
        [Tooltip("내려치는 순간의 효과음")]
        [SerializeField] private SoundData _slamSound;

        [Tooltip("방패에 폭탄이 막혔을 때의 효과음")]
        [SerializeField] private SoundData _blockSound;

        [Header("연출")]
        [Tooltip("방패에 막혔을 때의 연출 (불꽃, 흔들림). 방패 위치에 띄운다")]
        [SerializeField] private FeedbackData _blockFeedback;

        [Tooltip("방패에 막혔을 때 번쩍이는 색")]
        [SerializeField] private Color _blockFlashColor = new Color(1f, 0.9f, 0.5f, 1f);

        [Tooltip("번쩍임 시간 (초)")]
        [SerializeField, Min(0f)] private float _blockFlashDuration = 0.15f;

        public float ShieldAngle => _shieldAngle;
        public float ShieldTurnSpeed => _shieldTurnSpeed;
        public float ShieldOrbitRadius => _shieldOrbitRadius;
        public Vector2 BodyCenterOffset => _bodyCenterOffset;
        public float SlamRadius => _slamRadius;
        public float SlamForward => _slamForward;
        public SoundData SlamSound => _slamSound;
        public SoundData BlockSound => _blockSound;
        public FeedbackData BlockFeedback => _blockFeedback;
        public Color BlockFlashColor => _blockFlashColor;
        public float BlockFlashDuration => _blockFlashDuration;
    }
}
