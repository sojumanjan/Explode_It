using UnityEngine;

namespace ExplodeIt.Player
{
    [CreateAssetMenu(fileName = "PlayerData", menuName = "Explode It/Player Data")]
    public class PlayerData : ScriptableObject
    {
        [Tooltip("최대 이동 속도 (유닛/초)")]
        [SerializeField, Min(0f)] private float _moveSpeed = 6f;

        // 한 방 사망 게임이라 회피 반응이 둔하면 억울함이 커진다. 가감속은 높게 시작해 줄여가며 조정한다.
        [Tooltip("입력 중 최대 속도까지 붙는 가속도 (유닛/초²). 높을수록 출발이 즉각적이다")]
        [SerializeField, Min(0f)] private float _acceleration = 80f;

        [Tooltip("입력을 뗐을 때 멈추는 감속도 (유닛/초²). 낮으면 미끄러진다")]
        [SerializeField, Min(0f)] private float _deceleration = 100f;

        public float MoveSpeed => _moveSpeed;
        public float Acceleration => _acceleration;
        public float Deceleration => _deceleration;
    }
}
