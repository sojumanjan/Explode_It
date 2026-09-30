using UnityEngine;

namespace ExplodeIt.Enemies
{
    [CreateAssetMenu(fileName = "ChargerData", menuName = "Explode It/Enemy/Charger Data")]
    public class ChargerData : EnemyData
    {
        [Header("돌진")]
        [Tooltip("돌진 속도 (유닛/초)")]
        [SerializeField, Min(0f)] private float _chargeSpeed = 12f;

        [Tooltip("최대 돌진 거리 (유닛). 구조물이 있으면 그 앞에서 멈추고, 예고선도 그 길이로 잘린다")]
        [SerializeField, Min(0f)] private float _chargeDistance = 6f;

        [Tooltip("돌진 중 플레이어를 맞히는 판정 반경 (유닛)")]
        [SerializeField, Min(0f)] private float _contactRadius = 0.4f;

        public float ChargeSpeed => _chargeSpeed;
        public float ChargeDistance => _chargeDistance;
        public float ContactRadius => _contactRadius;
    }
}
