using UnityEngine;

namespace ExplodeIt.Progression
{
    // 특수 능력: 블랙홀 폭탄. 처치로 게이지를 채워 한 번에 뭉쳐서 지우는 공격 수단이다.
    [CreateAssetMenu(fileName = "AbilityData", menuName = "Explode It/Ability Data")]
    public class AbilityData : ScriptableObject
    {
        [Header("충전")]
        [Tooltip("게이지를 채우는 데 필요한 처치 수 (마리). 블랙홀로 잡은 적은 세지 않는다")]
        [SerializeField, Min(1)] private int _killsToCharge = 9;

        [Header("블랙홀 폭탄")]
        // 기본 폭탄 반경에 곱하므로, 기본 폭탄 범위가 강화되면 블랙홀도 함께 커진다.
        [Tooltip("범위 배율 (배). 기본 폭탄 폭발 반경에 곱한다")]
        [SerializeField, Min(0.1f)] private float _radiusMultiplier = 2f;

        [Tooltip("흡입 시간 (초). 착지 후 적을 빨아들이다가 이 시간이 지나면 터진다")]
        [SerializeField, Min(0.1f)] private float _pullDuration = 1.5f;

        [Tooltip("흡입 속도 (유닛/초). 범위 안의 적이 중심 쪽으로 끌려가는 속도")]
        [SerializeField, Min(0f)] private float _pullSpeed = 4f;

        public int KillsToCharge => _killsToCharge;
        public float RadiusMultiplier => _radiusMultiplier;
        public float PullDuration => _pullDuration;
        public float PullSpeed => _pullSpeed;
    }
}
