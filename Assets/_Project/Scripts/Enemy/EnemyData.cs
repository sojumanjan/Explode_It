using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 모든 적이 공유하는 수치. 적 고유 수치는 하위 클래스에 둔다.
    public abstract class EnemyData : ScriptableObject
    {
        [Header("공통")]
        [SerializeField, Min(0f)] private float _moveSpeed = 2f;
        [SerializeField, Min(1)] private int _hitsToDie = 1;
        [SerializeField, Min(0f)] private float _attackTriggerRange = 4f;

        [Header("예고 / 회복")]
        // 예고 없는 공격은 버그로 취급하므로 0이 되지 않게 막는다. 하드모드에서도 이 아래로는 줄이지 않는다.
        [SerializeField, Min(0.1f)] private float _telegraphDuration = 0.6f;
        [SerializeField, Min(0f)] private float _recoverDuration = 0.6f;

        public float MoveSpeed => _moveSpeed;
        public int HitsToDie => _hitsToDie;
        public float AttackTriggerRange => _attackTriggerRange;
        public float TelegraphDuration => _telegraphDuration;
        public float RecoverDuration => _recoverDuration;
    }
}
