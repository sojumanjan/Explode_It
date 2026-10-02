using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    [CreateAssetMenu(fileName = "WarriorData", menuName = "Explode It/Enemy/Warrior Data")]
    public class WarriorData : EnemyData
    {
        [Header("휘두르기")]
        [Tooltip("공격 반경 (유닛). 플레이어 중심이 이 거리 안이어야 맞는다. 공격 시작 거리보다 약간 작게 두면 다가온 뒤 비켜서 피할 틈이 생긴다")]
        [SerializeField, Min(0f)] private float _attackRadius = 1.5f;

        [Tooltip("공격 각도 (도). 정면 기준 부채꼴 전체 각도. 옆이나 뒤로 빠지면 피할 수 있다")]
        [SerializeField, Range(10f, 360f)] private float _attackAngle = 110f;

        [Header("사운드")]
        [Tooltip("휘두르는 순간의 효과음. 비워 두면 소리 없이 휘두른다")]
        [SerializeField] private SoundData _swingSound;

        public float AttackRadius => _attackRadius;
        public float AttackAngle => _attackAngle;
        public SoundData SwingSound => _swingSound;
    }
}
