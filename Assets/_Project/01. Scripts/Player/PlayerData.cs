using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Player
{
    [CreateAssetMenu(fileName = "PlayerData", menuName = "Explode It/Player Data")]
    public class PlayerData : ScriptableObject
    {
        [Tooltip("최대 이동 속도 (유닛/초)")]
        [SerializeField, Min(0f)] private float _moveSpeed = 6f;

        // 한 방 사망 게임이라 회피 반응이 둔하면 억울함이 커진다. 가감속은 높게 시작해 줄여가며 조정한다.
        [Tooltip("입력 중 최대 속도까지 붙는 가속도 (유닛/초²). 높을수록 출발이 즉각적이다")]
        [SerializeField, Min(0f)] private float _acceleration = 80f;

        [Tooltip("입력을 뗐을 때 멈추는 감속도 (유닛/초²). 낮으면 미끄러진다")]
        [SerializeField, Min(0f)] private float _deceleration = 100f;

        [Header("체력")]
        [Tooltip("스토리 모드에서 버틸 수 있는 피격 수 (칸)")]
        [SerializeField, Min(1)] private int _storyHealth = 2;

        [Tooltip("무한 모드에서 버틸 수 있는 피격 수 (칸)")]
        [SerializeField, Min(1)] private int _endlessHealth = 2;

        [Tooltip("맞고 살아남은 뒤 무적 시간 (초). 이 동안 몸이 반투명해지며 다시 맞지 않는다")]
        [SerializeField, Min(0f)] private float _hurtInvulnerability = 1.5f;

        [Tooltip("맞은 뒤 무적 동안 몸의 불투명도 (0~1). 낮을수록 흐리다")]
        [SerializeField, Range(0f, 1f)] private float _hurtAlpha = 0.45f;

        [Tooltip("맞고 살아남은 순간의 연출 (흔들림, 히트스톱, 소리 등). 비워 두면 연출 없음")]
        [SerializeField] private FeedbackData _hurtFeedback;

        [Tooltip("맞았을 때 소리. 살아남든 쓰러지든 맞는 순간 난다. 비워 두면 소리 없음")]
        [SerializeField] private SoundData _hurtSound;

        [Header("사망 연출")]
        [Tooltip("쓰러진 고블린 그림. 비워 두면 기본 그림을 어둡게 칠한다")]
        [SerializeField] private Sprite _deathSprite;

        [Tooltip("죽는 순간 카메라 배율. 1이면 평소 크기, 작을수록 고블린에게 가깝게 다가간다")]
        [SerializeField, Range(0.3f, 1f)] private float _deathZoom = 0.6f;

        [Tooltip("카메라가 고블린에게 다가가는 시간 (초). 결과 창은 사망 패널의 대기 시간이 지난 뒤 뜬다")]
        [SerializeField, Min(0f)] private float _deathZoomTime = 0.8f;

        [Tooltip("죽는 순간 걸리는 슬로모션 배속 (1이면 정상 속도)")]
        [SerializeField, Range(0.05f, 1f)] private float _deathSlowScale = 0.3f;

        [Tooltip("슬로모션이 정상 속도로 돌아오기까지 (초, 실제 시간)")]
        [SerializeField, Min(0f)] private float _deathSlowTime = 1.2f;

        public float MoveSpeed => _moveSpeed;
        public float DeathSlowScale => _deathSlowScale;
        public float DeathSlowTime => _deathSlowTime;
        public float HurtAlpha => _hurtAlpha;
        public Sprite DeathSprite => _deathSprite;
        public float DeathZoom => _deathZoom;
        public float DeathZoomTime => _deathZoomTime;
        public int StoryHealth => _storyHealth;
        public int EndlessHealth => _endlessHealth;
        public float HurtInvulnerability => _hurtInvulnerability;
        public FeedbackData HurtFeedback => _hurtFeedback;
        public SoundData HurtSound => _hurtSound;
        public float Acceleration => _acceleration;
        public float Deceleration => _deceleration;
    }
}
