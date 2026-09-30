using UnityEngine;

namespace ExplodeIt.Enemies
{
    [CreateAssetMenu(fileName = "ArcherData", menuName = "Explode It/Enemy/Archer Data")]
    public class ArcherData : EnemyData
    {
        [Header("화살")]
        [Tooltip("화살 속도 (유닛/초)")]
        [SerializeField, Min(0f)] private float _projectileSpeed = 8f;

        [Tooltip("화살 최대 비행 거리 (유닛). 조준선 길이와 같다")]
        [SerializeField, Min(0f)] private float _projectileRange = 10f;

        [Tooltip("화살이 플레이어를 맞히는 판정 반경 (유닛)")]
        [SerializeField, Min(0f)] private float _projectileHitRadius = 0.15f;

        public float ProjectileSpeed => _projectileSpeed;
        public float ProjectileRange => _projectileRange;
        public float ProjectileHitRadius => _projectileHitRadius;
    }
}
