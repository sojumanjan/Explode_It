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

        // 탄이 빠르면 마지막 순간까지 따라오는 조준을 피할 방법이 없다. 발사 직전에 궤적을 멈춰 옆으로 빠질 틈을 준다.
        [Tooltip("조준 고정 시간 (초). 발사 이 시간 전부터 조준이 플레이어를 따라가지 않고 멈춘다. 0이면 발사 순간까지 따라간다. 예고 시간보다 길면 예고 시작부터 고정된다")]
        [SerializeField, Min(0f)] private float _aimLockTime;

        // 조준선이 지금 위치에 딱 붙어 따라오면, 움직여도 소용없다는 느낌이 들어 압박만 커진다(테스터 피드백).
        // 일정 시간 늦게 따라가면 대시 한 번에 조준선이 뒤늦게 휙 넘어오고, 회전 속도를 고정하면 반대편으로 넘어갈 때 굼뜨다.
        // 그래서 남은 각도에 비례해 돈다. 크게 벗어나면 빨리, 조금 비키면 천천히 따라붙는다.
        [Tooltip("조준 회전 비율 (1/초). 예고 동안 조준이 남은 각도 × 이 값의 속도(도/초)로 플레이어 쪽으로 돈다. 클수록 빨리 따라붙는다. 0이면 플레이어를 바로 따라간다")]
        [SerializeField, Min(0f)] private float _aimTurnRate;

        [Tooltip("조준 최소 회전 속도 (도/초). 남은 각도가 작아도 이 속도로는 돈다. 너무 작으면 조준이 플레이어에 끝내 닿지 않는다")]
        [SerializeField, Min(0f)] private float _aimMinTurnSpeed = 25f;

        [Tooltip("조준 최대 회전 속도 (도/초). 반대편으로 크게 넘어가도 이 속도를 넘지 않는다")]
        [SerializeField, Min(0f)] private float _aimMaxTurnSpeed = 720f;

        [Header("사운드")]
        [Tooltip("조준을 시작하는 순간의 효과음. 발사하거나 조준이 끊기면 멈춘다. 비워 두면 소리 없이 조준한다")]
        [SerializeField] private SoundData _aimSound;

        [Tooltip("발사하는 순간의 효과음. 비워 두면 소리 없이 쏜다")]
        [SerializeField] private SoundData _shotSound;

        public float ProjectileSpeed => _projectileSpeed;
        public bool ShowAimLine => _showAimLine;
        public float AimLockTime => _aimLockTime;
        public float AimTurnRate => _aimTurnRate;
        public float AimMinTurnSpeed => _aimMinTurnSpeed;
        public float AimMaxTurnSpeed => _aimMaxTurnSpeed;
        public SoundData AimSound => _aimSound;
        public SoundData ShotSound => _shotSound;
        public float ProjectileRange => _projectileRange;
        public float ProjectileHitRadius => _projectileHitRadius;
    }
}
