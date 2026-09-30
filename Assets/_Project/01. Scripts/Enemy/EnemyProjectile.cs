using System;
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 콜라이더 없이 매 프레임 범위 검사로 판정한다. 투사체가 많아도 물리 바디가 늘지 않는다.
    public class EnemyProjectile : MonoBehaviour
    {
        private static readonly Collider2D[] HitBuffer = new Collider2D[4];

        [SerializeField] private LayerMask _hitMask;

        private ContactFilter2D _hitFilter;
        private Action<EnemyProjectile> _release;
        private Vector2 _velocity;
        private float _remainingDistance;
        private float _hitRadius;

        private void Awake()
        {
            _hitFilter = new ContactFilter2D();
            _hitFilter.SetLayerMask(_hitMask);
            _hitFilter.useTriggers = true;
        }

        public void Launch(Vector2 direction, float speed, float range, float hitRadius, Action<EnemyProjectile> release)
        {
            _velocity = direction * speed;
            _remainingDistance = range;
            _hitRadius = hitRadius;
            _release = release;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void Update()
        {
            Vector2 step = _velocity * Time.deltaTime;
            Vector2 position = (Vector2)transform.position + step;
            transform.position = position;
            _remainingDistance -= step.magnitude;

            if (TryHit(position) || _remainingDistance <= 0f)
            {
                _release(this);
            }
        }

        private bool TryHit(Vector2 position)
        {
            int count = Physics2D.OverlapCircle(position, _hitRadius, _hitFilter, HitBuffer);
            bool hitAny = false;
            for (int i = 0; i < count; i++)
            {
                if (HitBuffer[i].TryGetComponent(out IHittable hittable))
                {
                    hittable.ReceiveHit(new HitInfo(position));
                    hitAny = true;
                }
            }

            return hitAny;
        }
    }
}
