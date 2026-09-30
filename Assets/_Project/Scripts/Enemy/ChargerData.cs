using UnityEngine;

namespace ExplodeIt.Enemies
{
    [CreateAssetMenu(fileName = "ChargerData", menuName = "Explode It/Enemy/Charger Data")]
    public class ChargerData : EnemyData
    {
        [Header("돌진")]
        [SerializeField, Min(0f)] private float _chargeSpeed = 12f;
        [SerializeField, Min(0f)] private float _chargeDistance = 6f;
        [SerializeField, Min(0f)] private float _contactRadius = 0.4f;

        public float ChargeSpeed => _chargeSpeed;
        public float ChargeDistance => _chargeDistance;
        public float ContactRadius => _contactRadius;
    }
}
