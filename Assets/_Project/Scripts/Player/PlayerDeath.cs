using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Player
{
    [RequireComponent(typeof(HitReceiver))]
    public class PlayerDeath : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _sprite;
        [SerializeField] private Color _deadColor = new Color(0.35f, 0.35f, 0.35f, 1f);

        private HitReceiver _hitReceiver;

        private void Awake()
        {
            _hitReceiver = GetComponent<HitReceiver>();
            // 한 방 사망은 게임의 뼈대라 데이터로 빼지 않는다. 실드는 HitReceiver의 판정 단계에서 처리한다.
            _hitReceiver.ResetHits(1);
        }

        private void OnEnable()
        {
            _hitReceiver.Died += OnDied;
        }

        private void OnDisable()
        {
            _hitReceiver.Died -= OnDied;
        }

        private void OnDied(HitInfo hit)
        {
            _sprite.color = _deadColor;
            GameEvents.RaisePlayerDied();
        }
    }
}
