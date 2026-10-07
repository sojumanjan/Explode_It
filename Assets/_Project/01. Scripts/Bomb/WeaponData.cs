using UnityEngine;

namespace ExplodeIt.Bombs
{
    [CreateAssetMenu(fileName = "WeaponData", menuName = "Explode It/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        [Header("투척")]
        [Tooltip("보유 개수. 쿨타임 없이 연속으로 던질 수 있는 폭탄 수")]
        [SerializeField, Min(1)] private int _charges = 3;

        [Tooltip("연사 간격 (초). 연속 투척 사이 최소 시간")]
        [SerializeField, Min(0f)] private float _throwInterval = 0.25f;

        [Tooltip("쿨타임 (초). 보유 개수가 가득 차 있지 않으면 이 시간마다 연발 수만큼(보통 하나씩) 찬다. 던져도 진행 중인 충전은 끊기지 않는다")]
        [SerializeField, Min(0f)] private float _rechargeTime = 1.5f;

        [Tooltip("연발 수 (발). 한 번 누르면 이만큼 자동으로 연달아 던진다. 발마다 보유 개수를 하나씩 쓰고, 충전도 한 번에 이만큼씩 찬다. 보통 무기는 1")]
        [SerializeField, Min(1)] private int _burstCount = 1;

        [Tooltip("연발 간격 (초). 연발 사이 시간. 다음 발은 그 순간의 커서 위치로 날아간다")]
        [SerializeField, Min(0f)] private float _burstInterval = 0.2f;

        [Tooltip("최대 사거리 (유닛). 커서가 이보다 멀면 사거리 원 경계에 떨어진다")]
        [SerializeField, Min(0f)] private float _maxThrowRange = 6f;

        [Header("폭발")]
        [Tooltip("딜레이 (초). 착지부터 폭발까지 걸리는 시간. 기획상 0.8~1.2초 범위에서 테스트한다")]
        [SerializeField, Min(0f)] private float _fuseDelay = 1f;

        [Tooltip("폭발 반경 (유닛). 바닥에 표시되는 범위 원과 실제 판정이 같다")]
        [SerializeField, Min(0f)] private float _explosionRadius = 1.5f;

        [Header("비행 연출")]
        [Tooltip("비행 시간 (초). 거리와 무관하게 일정해서 클릭부터 폭발까지 = 비행 시간 + 딜레이")]
        [SerializeField, Min(0.01f)] private float _flightDuration = 0.35f;

        [Tooltip("포물선 높이 (유닛). 보이는 연출만 바뀌고 착지 지점은 그대로다")]
        [SerializeField, Min(0f)] private float _arcHeight = 1f;

        public int Charges => _charges;
        public float ThrowInterval => _throwInterval;
        public float RechargeTime => _rechargeTime;
        public int BurstCount => _burstCount;
        public float BurstInterval => _burstInterval;
        public float MaxThrowRange => _maxThrowRange;
        public float FuseDelay => _fuseDelay;
        public float ExplosionRadius => _explosionRadius;
        public float FlightDuration => _flightDuration;
        public float ArcHeight => _arcHeight;
    }
}
