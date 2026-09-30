using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 모든 적이 공유하는 수치. 적 고유 수치는 하위 클래스에 둔다.
    public abstract class EnemyData : ScriptableObject
    {
        [Header("공통")]
        [Tooltip("이동 상태에서의 속도 (유닛/초)")]
        [SerializeField, Min(0f)] private float _moveSpeed = 2f;

        [Tooltip("죽기까지 필요한 피격 횟수. 기본 1, 골렘처럼 단단한 적만 올린다")]
        [SerializeField, Min(1)] private int _hitsToDie = 1;

        [Tooltip("공격 시작 거리 (유닛). 플레이어가 이 안에 들어오면 예고를 시작한다. 예고 중에도 이동 속도로 계속 다가온다")]
        [SerializeField, Min(0f)] private float _attackTriggerRange = 4f;

        [Header("예고 / 회복")]
        // 예고 없는 공격은 버그로 취급하므로 0이 되지 않게 막는다. 하드모드에서도 이 아래로는 줄이지 않는다.
        [Tooltip("예고 시간 (초). 조준선이 플레이어를 따라가는 시간. 끝나는 순간의 방향으로 공격한다. 최소 0.1")]
        [SerializeField, Min(0.1f)] private float _telegraphDuration = 0.6f;

        [Tooltip("회복 시간 (초). 공격 후 멈춰 있는 시간. 플레이어가 반격할 틈이 된다")]
        [SerializeField, Min(0f)] private float _recoverDuration = 0.6f;

        public float MoveSpeed => _moveSpeed;
        public int HitsToDie => _hitsToDie;
        public float AttackTriggerRange => _attackTriggerRange;
        public float TelegraphDuration => _telegraphDuration;
        public float RecoverDuration => _recoverDuration;
    }
}
