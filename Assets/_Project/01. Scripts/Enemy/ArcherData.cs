using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    [CreateAssetMenu(fileName = "ArcherData", menuName = "Explode It/Enemy/Archer Data")]
    public class ArcherData : EnemyData
    {
        [Header("화살")]
        [Tooltip("화살 속도 (유닛/초)")]
        [SerializeField, Min(0f)] private float _projectileSpeed = 8f;

        [Tooltip("화살 최대 비행 거리 (유닛). 구조물에 막히면 그 지점에서 사라지고, 조준선도 그 길이로 잘린다")]
        [SerializeField, Min(0f)] private float _projectileRange = 10f;

        [Tooltip("화살이 플레이어를 맞히는 판정 반경 (유닛)")]
        [SerializeField, Min(0f)] private float _projectileHitRadius = 0.15f;

        [Header("사운드")]
        [Tooltip("화살을 쏘는 순간의 효과음. 비워 두면 소리 없이 쏜다")]
        [SerializeField] private SoundData _shotSound;

        public float ProjectileSpeed => _projectileSpeed;
        public SoundData ShotSound => _shotSound;
        public float ProjectileRange => _projectileRange;
        public float ProjectileHitRadius => _projectileHitRadius;
    }
}
