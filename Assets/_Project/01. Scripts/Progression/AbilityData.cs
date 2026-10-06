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

        // 칸마다 따로 쓸 수 있어서, 하나 바로 쓸지 두 칸 다 모아 연달아 쓸지를 플레이어가 고르게 된다.
        [Tooltip("게이지 칸 수 (칸). 한 칸이 차면 한 번 쓸 수 있고, 다 쓰지 않으면 다음 칸이 이어서 찬다")]
        [SerializeField, Min(1)] private int _maxCharges = 2;

        [Tooltip("시작할 때 차 있는 칸 수 (칸). 첫 위기에 바로 쓸 수 있게 한다")]
        [SerializeField, Min(0)] private int _startCharges = 1;

        [Header("블랙홀 폭탄")]
        // 일반 폭탄 사거리 원과 무관하다. 화면에 따로 표시하지 않으므로 커서를 둔 곳에 거의 그대로 떨어진다.
        [Tooltip("최대 사거리 (유닛). 커서가 이보다 멀면 이 거리 지점에 떨어진다")]
        [SerializeField, Min(0f)] private float _maxThrowRange = 15f;

        // 기본 폭탄 반경에 곱하므로, 기본 폭탄 범위가 강화되면 블랙홀도 함께 커진다.
        [Tooltip("범위 배율 (배). 기본 폭탄 폭발 반경에 곱한다")]
        [SerializeField, Min(0.1f)] private float _radiusMultiplier = 2f;

        [Tooltip("흡입 시간 (초). 착지 후 적을 빨아들이다가 이 시간이 지나면 터진다")]
        [SerializeField, Min(0.1f)] private float _pullDuration = 1.5f;

        [Tooltip("흡입 속도 (유닛/초). 범위 안의 적이 중심 쪽으로 끌려가는 속도")]
        [SerializeField, Min(0f)] private float _pullSpeed = 4f;

        public int KillsToCharge => _killsToCharge;
        public int MaxCharges => _maxCharges;
        public int StartCharges => Mathf.Min(_startCharges, _maxCharges);
        public float MaxThrowRange => _maxThrowRange;
        public float RadiusMultiplier => _radiusMultiplier;
        public float PullDuration => _pullDuration;
        public float PullSpeed => _pullSpeed;
    }
}
