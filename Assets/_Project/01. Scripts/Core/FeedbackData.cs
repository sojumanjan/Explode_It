using UnityEngine;

namespace ExplodeIt.Core
{
    // 한 사건의 연출 묶음. 흔들림·히트스톱·파티클을 조합해서 "얼마나 세게 느껴질지"를 데이터로 정한다.
    // 예측 게임이라 연출이 예고선과 범위 원을 가리면 안 되므로, 기본값은 작고 짧게 둔다.
    [CreateAssetMenu(fileName = "Fb_", menuName = "Explode It/Feedback Data")]
    public class FeedbackData : ScriptableObject
    {
        [Header("화면 흔들림")]
        [Tooltip("흔들림 세기. 0이면 흔들지 않는다. 0.1~0.2 가볍게, 0.5 이상 크게")]
        [SerializeField, Min(0f)] private float _shakeForce;

        [Header("히트스톱")]
        [Tooltip("히트스톱 시간 (초, 실제 시간). 0이면 멈칫하지 않는다. 0.03~0.08 정도가 맞았다는 확신을 준다")]
        [SerializeField, Min(0f)] private float _hitStopDuration;

        [Tooltip("히트스톱 중 게임 속도 배율. 0에 가까울수록 확 멈춘다")]
        [SerializeField, Range(0f, 1f)] private float _hitStopTimeScale = 0.05f;

        [Header("파티클")]
        [Tooltip("사건 위치에 띄울 이펙트 프리팹. 비워 두면 띄우지 않는다")]
        [SerializeField] private PooledEffect _effect;

        [Tooltip("이펙트 크기 배율")]
        [SerializeField, Min(0.1f)] private float _effectScale = 1f;

        public float ShakeForce => _shakeForce;
        public float HitStopDuration => _hitStopDuration;
        public float HitStopTimeScale => _hitStopTimeScale;
        public PooledEffect Effect => _effect;
        public float EffectScale => _effectScale;
    }
}
