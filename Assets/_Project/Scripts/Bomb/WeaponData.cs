using UnityEngine;

namespace ExplodeIt.Bombs
{
    [CreateAssetMenu(fileName = "WeaponData", menuName = "Explode It/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        [Header("투척")]
        [SerializeField, Min(1)] private int _charges = 3;
        [SerializeField, Min(0f)] private float _throwInterval = 0.25f;
        [SerializeField, Min(0f)] private float _rechargeTime = 1.5f;
        [SerializeField, Min(0f)] private float _maxThrowRange = 6f;

        [Header("폭발")]
        // 기획상 0.8~1.2초 범위에서 테스트한다.
        [SerializeField, Min(0f)] private float _fuseDelay = 1f;
        [SerializeField, Min(0f)] private float _explosionRadius = 1.5f;

        [Header("비행 연출")]
        [SerializeField, Min(0.01f)] private float _flightDuration = 0.35f;
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
