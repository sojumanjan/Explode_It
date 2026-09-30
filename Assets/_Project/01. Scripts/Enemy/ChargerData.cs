using UnityEngine;

namespace ExplodeIt.Enemies
{
    [CreateAssetMenu(fileName = "ChargerData", menuName = "Explode It/Enemy/Charger Data")]
    public class ChargerData : EnemyData
    {
        [Header("돌진")]
        [Tooltip("돌진 속도 (유닛/초)")]
        [SerializeField, Min(0f)] private float _chargeSpeed = 12f;

        [Tooltip("돌진 거리 (유닛). 예고선 길이와 같다")]
        [SerializeField, Min(0f)] private float _chargeDistance = 6f;

        [Tooltip("돌진 중 플레이어를 맞히는 판정 반경 (유닛)")]
        [SerializeField, Min(0f)] private float _contactRadius = 0.4f;

        public float ChargeSpeed => _chargeSpeed;
        public float ChargeDistance => _chargeDistance;
        public float ContactRadius => _contactRadius;
    }
}
