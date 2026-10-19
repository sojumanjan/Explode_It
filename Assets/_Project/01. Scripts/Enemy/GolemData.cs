using System;
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    [CreateAssetMenu(fileName = "GolemData", menuName = "Explode It/Enemy/Golem Data")]
    public class GolemData : EnemyData
    {
        // 남은 맷집마다 다른 몸. 클 때는 느리고 넓게 천천히 내려치고, 깎일수록 작고 빠르고 좁게 재빨리 내려친다.
        [Serializable]
        public class Stage
        {
            [Tooltip("이 단계의 그림")]
            [SerializeField] private Sprite _sprite;

            [Tooltip("그림 크기 배율. 프리팹 Body 크기에 곱한다")]
            [SerializeField, Min(0.1f)] private float _visualScale = 1f;

            [Tooltip("폭탄에 맞는 판정 반경 (유닛). 몸이 작아지면 맞히기도 어려워진다")]
            [SerializeField, Min(0.05f)] private float _hitRadius = 0.5f;

            [Tooltip("폭탄에 맞는 판정 원의 위치 (유닛, 몸 기준). 그림이 발밑 기준이라 몸이 클수록 위로 올린다")]
            [SerializeField] private Vector2 _hitOffset;

            [Tooltip("이동 속도 (유닛/초)")]
            [SerializeField, Min(0f)] private float _moveSpeed = 1f;

            [Tooltip("공격 시작 거리 (유닛). 플레이어가 이 안에 들어오면 멈춰서 내려치기를 예고한다")]
            [SerializeField, Min(0f)] private float _attackTriggerRange = 2f;

            [Tooltip("내려치기 반경 (유닛)")]
            [SerializeField, Min(0.1f)] private float _slamRadius = 2f;

            [Tooltip("내려치기 중심을 플레이어 쪽으로 내민 거리 (유닛)")]
            [SerializeField, Min(0f)] private float _slamForward = 0.3f;

            [Tooltip("예고 시간 (초). 원이 다 차면 내려친다")]
            [SerializeField, Min(0.1f)] private float _telegraphDuration = 1f;

            public Sprite Sprite => _sprite;
            public float VisualScale => _visualScale;
            public float HitRadius => _hitRadius;
            public Vector2 HitOffset => _hitOffset;
            public float MoveSpeed => _moveSpeed;
            public float AttackTriggerRange => _attackTriggerRange;
            public float SlamRadius => _slamRadius;
            public float SlamForward => _slamForward;
            public float TelegraphDuration => _telegraphDuration;
        }

        [Header("단계 (1번 칸 = 마지막 한 대 남은 쪼꼬미, 마지막 칸 = 처음 왕건이)")]
        [Tooltip("남은 맷집별 몸. 칸 수가 맞아야 할 횟수(공통 항목)와 같아야 한다")]
        [SerializeField] private Stage[] _stages = { new Stage(), new Stage(), new Stage() };

        [Header("맞았을 때")]
        [Tooltip("맞고 버틴 뒤 멈칫하는 시간 (초). 예고 중이었으면 내려치기가 취소된다")]
        [SerializeField, Min(0f)] private float _hurtStun = 0.3f;

        [Tooltip("맞고 버틴 순간의 연출 (돌 부스러기, 흔들림 등)")]
        [SerializeField] private FeedbackData _hurtFeedback;

        [Tooltip("맞고 버틴 순간 번쩍이는 색")]
        [SerializeField] private Color _hurtFlashColor = Color.white;

        [Tooltip("번쩍임 시간 (초)")]
        [SerializeField, Min(0f)] private float _hurtFlashDuration = 0.12f;

        [Header("사운드")]
        [Tooltip("내려치는 순간의 효과음")]
        [SerializeField] private SoundData _slamSound;

        [Tooltip("맞고 버틴 순간의 효과음")]
        [SerializeField] private SoundData _hurtSound;

        public float HurtStun => _hurtStun;
        public FeedbackData HurtFeedback => _hurtFeedback;
        public Color HurtFlashColor => _hurtFlashColor;
        public float HurtFlashDuration => _hurtFlashDuration;
        public SoundData SlamSound => _slamSound;
        public SoundData HurtSound => _hurtSound;

        // remainingHits는 1부터. 범위를 넘으면 가장 가까운 칸을 쓴다.
        public Stage GetStage(int remainingHits)
        {
            return _stages[Mathf.Clamp(remainingHits - 1, 0, _stages.Length - 1)];
        }

        private void OnValidate()
        {
            if (_stages == null || _stages.Length == 0)
            {
                _stages = new[] { new Stage() };
            }

            if (_stages.Length != HitsToDie)
            {
                Debug.LogWarning($"{name}: 단계 칸 수({_stages.Length})가 맞아야 할 횟수({HitsToDie})와 다릅니다.", this);
            }
        }
    }
}
