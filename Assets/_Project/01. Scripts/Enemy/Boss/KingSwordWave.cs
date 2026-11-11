// 각성한 왕이 2국면에 부채꼴로 날리는 초승달 검기. 곧게 날아가며 지나는 구간을 원으로 훑어 플레이어를 맞힌다
using System;
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 콜라이더 없이 매 프레임 이번에 지나는 구간을 통째로 훑는다. 빠른 검기가 한 프레임에 플레이어를 건너뛰지 않게 한다.
    // 판정 대상은 플레이어뿐이라 벽은 뚫고 지나간다. 예고선도 벽을 무시하고 사거리 끝까지 그려지므로 보이는 대로 날아간다.
    public class KingSwordWave : MonoBehaviour
    {
        private static readonly RaycastHit2D[] HitBuffer = new RaycastHit2D[4];

        [SerializeField] private LayerMask _hitMask;
        // 날아가는 동안 반복 재생하는 그림. 오른쪽(+x)을 보는 시트라 날아가는 방향으로 돌린다.
        [SerializeField] private PixelFxPlayer _visual;

        private ContactFilter2D _hitFilter;
        private Action<KingSwordWave> _release;
        // 이 검기에 쓰러지면 결과 화면이 누구에게 당했는지 보여 준다.
        private IKillerProfile _shooter;
        private Vector2 _direction;
        private float _speed;
        private float _remainingDistance;
        private float _hitRadius;
        // 날아가는 동안 뒤에 남기는 잔상. 풀은 왕이 하나 두고 모든 검기가 나눠 쓴다.
        private PixelFxPool _trail;
        private float _trailInterval;
        private float _trailTimer;
        private float _scale;

        private void Awake()
        {
            _hitFilter = new ContactFilter2D();
            _hitFilter.SetLayerMask(_hitMask);
            _hitFilter.useTriggers = true;
        }

        public void Launch(Vector2 direction, float speed, float range, float hitRadius, float scale,
            PixelFxPool trail, float trailInterval, Action<KingSwordWave> release, IKillerProfile shooter)
        {
            _trail = trail;
            _trailInterval = trailInterval;
            _trailTimer = 0f;
            _scale = scale;
            _direction = direction;
            _speed = speed;
            _remainingDistance = range;
            _hitRadius = hitRadius;
            _release = release;
            _shooter = shooter;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
            _visual.Play(scale, null);
        }

        private void Update()
        {
            Vector2 origin = transform.position;
            float distance = Mathf.Min(_speed * Time.deltaTime, _remainingDistance);
            if (TryHit(origin, distance))
            {
                Finish();
                return;
            }

            transform.position = origin + _direction * distance;
            _remainingDistance -= distance;
            _trailTimer += Time.deltaTime;
            if (_trail != null && _trailTimer >= _trailInterval)
            {
                _trailTimer = 0f;
                _trail.Play(transform.position, _direction, _scale);
            }

            if (_remainingDistance <= 0f)
            {
                Finish();
            }
        }

        private bool TryHit(Vector2 origin, float distance)
        {
            int count = Physics2D.CircleCast(origin, _hitRadius, _direction, _hitFilter, HitBuffer, distance);
            for (int i = 0; i < count; i++)
            {
                if (HitBuffer[i].collider.TryGetComponent(out IHittable hittable))
                {
                    hittable.ReceiveHit(new HitInfo(HitBuffer[i].centroid, killer: _shooter));
                    return true;
                }
            }

            return false;
        }

        private void Finish()
        {
            _visual.Stop();
            _release(this);
        }
    }
}
