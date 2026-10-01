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

        [Header("예고")]
        // 궁수는 나중에 조준 애니메이션으로 예고하고, 저격수는 조준선과 소리로 화면 밖에서도 위험을 알린다.
        [Tooltip("조준선 표시. 켜면 예고 동안 쏠 방향과 비행 거리를 선으로 보여준다")]
        [SerializeField] private bool _showAimLine;

        [Header("사운드")]
        [Tooltip("조준을 시작하는 순간의 효과음. 발사하거나 조준이 끊기면 멈춘다. 비워 두면 소리 없이 조준한다")]
        [SerializeField] private SoundData _aimSound;

        [Tooltip("발사하는 순간의 효과음. 비워 두면 소리 없이 쏜다")]
        [SerializeField] private SoundData _shotSound;

        public float ProjectileSpeed => _projectileSpeed;
        public bool ShowAimLine => _showAimLine;
        public SoundData AimSound => _aimSound;
        public SoundData ShotSound => _shotSound;
        public float ProjectileRange => _projectileRange;
        public float ProjectileHitRadius => _projectileHitRadius;
    }
}
