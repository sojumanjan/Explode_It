using UnityEngine;

namespace ExplodeIt.Player
{
    [CreateAssetMenu(fileName = "PlayerData", menuName = "Explode It/Player Data")]
    public class PlayerData : ScriptableObject
    {
        [SerializeField, Min(0f)] private float _moveSpeed = 6f;
        // 한 방 사망 게임이라 회피 반응이 둔하면 억울함이 커진다. 가감속은 높게 시작해 줄여가며 조정한다.
        [SerializeField, Min(0f)] private float _acceleration = 80f;
        [SerializeField, Min(0f)] private float _deceleration = 100f;

        public float MoveSpeed => _moveSpeed;
        public float Acceleration => _acceleration;
        public float Deceleration => _deceleration;
    }
}
