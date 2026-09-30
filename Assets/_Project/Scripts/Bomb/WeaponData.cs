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

        [Tooltip("쿨타임 (초). 마지막 투척 후 이 시간 동안 던지지 않으면 보유 개수가 한 번에 가득 찬다")]
        [SerializeField, Min(0f)] private float _rechargeTime = 1.5f;

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
        public float MaxThrowRange => _maxThrowRange;
        public float FuseDelay => _fuseDelay;
        public float ExplosionRadius => _explosionRadius;
        public float FlightDuration => _flightDuration;
        public float ArcHeight => _arcHeight;
    }
}
