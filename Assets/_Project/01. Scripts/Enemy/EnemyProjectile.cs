using System;
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 콜라이더 없이 매 프레임 범위 검사로 판정한다. 투사체가 많아도 물리 바디가 늘지 않는다.
    public class EnemyProjectile : MonoBehaviour
    {
        private static readonly RaycastHit2D[] HitBuffer = new RaycastHit2D[4];

        [SerializeField] private LayerMask _hitMask;

        private ContactFilter2D _hitFilter;
        private Action<EnemyProjectile> _release;
        private Vector2 _direction;
        private float _speed;
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
            _direction = direction;
            _speed = speed;
            _remainingDistance = range;
            _hitRadius = hitRadius;
            _release = release;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        // 저격수 탄은 한 프레임에 몇 유닛씩 움직이므로, 끝점만 검사하면 사이에 있는 대상을 건너뛰고
        // 마지막 걸음이 사거리(벽 앞)를 넘어 벽 뒤 대상을 맞힌다. 이번 프레임에 지나는 구간을 통째로 훑고, 남은 거리 이상은 가지 않는다.
        private void Update()
        {
            Vector2 origin = transform.position;
            float distance = Mathf.Min(_speed * Time.deltaTime, _remainingDistance);

            if (TryHit(origin, distance))
            {
                _release(this);
                return;
            }

            transform.position = origin + _direction * distance;
            _remainingDistance -= distance;
            if (_remainingDistance <= 0f)
            {
                _release(this);
            }
        }

        private bool TryHit(Vector2 origin, float distance)
        {
            int count = Physics2D.CircleCast(origin, _hitRadius, _direction, _hitFilter, HitBuffer, distance);
            for (int i = 0; i < count; i++)
            {
                if (HitBuffer[i].collider.TryGetComponent(out IHittable hittable))
                {
                    hittable.ReceiveHit(new HitInfo(HitBuffer[i].centroid));
                    return true;
                }
            }

            return false;
        }
    }
}
