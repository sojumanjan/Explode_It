using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    [CreateAssetMenu(fileName = "MantisData", menuName = "Explode It/Enemy/Mantis Data")]
    public class MantisData : EnemyData
    {
        [Header("이동: 정지 → 순간 돌진")]
        [Tooltip("돌진 사이에 멈춰 있는 시간 (초). 이 동안만 공격을 시작할 수 있다")]
        [SerializeField, Min(0f)] private float _pauseDuration = 0.75f;

        [Tooltip("한 번 돌진하는 거리 (유닛). 플레이어가 더 가까우면 공격 시작 거리 바로 앞에서 멈춘다")]
        [SerializeField, Min(0.1f)] private float _dashDistance = 3f;

        [Tooltip("한 번 돌진에 걸리는 시간 (초). 짧을수록 순간이동처럼 보인다")]
        [SerializeField, Min(0.02f)] private float _dashDuration = 0.12f;

        [Tooltip("돌진 중 눌린 모양 (가로, 세로 배율). 멈추면 튕기듯 돌아온다")]
        [SerializeField] private Vector2 _dashSquash = new Vector2(1.4f, 0.55f);

        [Tooltip("돌진이 끝난 뒤 눌린 모양에서 원래 크기로 돌아오는 시간 (초). 끝에서 살짝 넘쳤다가 멈춘다")]
        [SerializeField, Min(0.01f)] private float _squashRecoverTime = 0.15f;

        [Header("휘두르기")]
        [Tooltip("공격 반경 (유닛). 플레이어 중심이 이 거리 안이어야 맞는다")]
        [SerializeField, Min(0f)] private float _attackRadius = 1.5f;

        [Tooltip("공격 각도 (도). 정면 기준 부채꼴 전체 각도. 옆이나 뒤로 빠지면 피할 수 있다")]
        [SerializeField, Range(10f, 360f)] private float _attackAngle = 100f;

        [Header("사운드")]
        [Tooltip("휘두르는 순간의 효과음. 비워 두면 소리 없이 휘두른다")]
        [SerializeField] private SoundData _swingSound;

        public float PauseDuration => _pauseDuration;
        public float DashDistance => _dashDistance;
        public float DashDuration => _dashDuration;
        public Vector2 DashSquash => _dashSquash;
        public float SquashRecoverTime => _squashRecoverTime;
        public float AttackRadius => _attackRadius;
        public float AttackAngle => _attackAngle;
        public SoundData SwingSound => _swingSound;
    }
}
