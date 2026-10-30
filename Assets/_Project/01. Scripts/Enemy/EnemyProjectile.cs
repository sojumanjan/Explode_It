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
        // 원거리 탄이 날아가는 게 눈에 남도록 잔상을 단다(테스터 피드백). 비워 두면 잔상 없이 맞는 즉시 사라진다.
        [SerializeField] private TrailRenderer _trail;
        [SerializeField] private Renderer _body;

        private ContactFilter2D _hitFilter;
        private Action<EnemyProjectile> _release;
        private Vector2 _direction;
        private float _speed;
        private float _remainingDistance;
        private float _hitRadius;
        // 쏜 적. 플레이어가 이 탄에 쓰러지면 결과 화면이 누구에게 당했는지 보여 준다.
        private IKillerProfile _shooter;
        // 맞거나 사거리 끝에 닿은 뒤 잔상이 사라지기를 기다리는 남은 시간. 음수면 날아가는 중이다.
        private float _fadeLeft = -1f;

        private void Awake()
        {
            _hitFilter = new ContactFilter2D();
            _hitFilter.SetLayerMask(_hitMask);
            _hitFilter.useTriggers = true;
        }

        public void Launch(Vector2 direction, float speed, float range, float hitRadius, Action<EnemyProjectile> release,
            IKillerProfile shooter)
        {
            _shooter = shooter;
            _direction = direction;
            _speed = speed;
            _remainingDistance = range;
            _hitRadius = hitRadius;
            _release = release;
            _fadeLeft = -1f;
            if (_body != null)
            {
                _body.enabled = true;
            }

            // 풀에서 꺼낸 탄은 지난 비행의 잔상을 들고 있어, 지우지 않으면 지난 자리에서 새 자리까지 선이 그어진다.
            if (_trail != null)
            {
                _trail.Clear();
                _trail.emitting = true;
            }

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        // 저격수 탄은 한 프레임에 몇 유닛씩 움직이므로, 끝점만 검사하면 사이에 있는 대상을 건너뛰고
        // 마지막 걸음이 사거리(벽 앞)를 넘어 벽 뒤 대상을 맞힌다. 이번 프레임에 지나는 구간을 통째로 훑고, 남은 거리 이상은 가지 않는다.
        private void Update()
        {
            if (_fadeLeft >= 0f)
            {
                _fadeLeft -= Time.deltaTime;
                if (_fadeLeft < 0f)
                {
                    _release(this);
                }

                return;
            }

            Vector2 origin = transform.position;
            float distance = Mathf.Min(_speed * Time.deltaTime, _remainingDistance);

            if (TryHit(origin, distance, out Vector2 hitPoint))
            {
                // 잔상이 맞은 자리까지 이어지게 그 지점으로 옮긴 뒤 멈춘다.
                transform.position = hitPoint;
                Finish();
                return;
            }

            transform.position = origin + _direction * distance;
            _remainingDistance -= distance;
            if (_remainingDistance <= 0f)
            {
                Finish();
            }
        }

        // 맞거나 사거리 끝에 닿아도 잔상이 뚝 끊기지 않게, 몸만 숨기고 잔상이 다 사라진 뒤 풀로 돌아간다.
        // 숨은 동안은 판정하지 않는다.
        private void Finish()
        {
            if (_trail == null)
            {
                _release(this);
                return;
            }

            if (_body != null)
            {
                _body.enabled = false;
            }

            _trail.emitting = false;
            _fadeLeft = _trail.time;
        }

        private bool TryHit(Vector2 origin, float distance, out Vector2 hitPoint)
        {
            int count = Physics2D.CircleCast(origin, _hitRadius, _direction, _hitFilter, HitBuffer, distance);
            for (int i = 0; i < count; i++)
            {
                if (HitBuffer[i].collider.TryGetComponent(out IHittable hittable))
                {
                    hittable.ReceiveHit(new HitInfo(HitBuffer[i].centroid, killer: _shooter));
                    hitPoint = HitBuffer[i].centroid;
                    return true;
                }
            }

            hitPoint = origin;
            return false;
        }
    }
}
