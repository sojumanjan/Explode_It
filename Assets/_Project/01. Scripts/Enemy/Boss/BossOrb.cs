using System;
using ExplodeIt.Core;
using ExplodeIt.Stage;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 구체 보스의 실드를 붙잡고 있는 구체. 직선으로 날아다니다 맵 테두리에서만 튕기고, 내부 구조물은 통과한다.
    // 닿아도 플레이어는 죽지 않는다. 공격이 아니라 맞혀야 하는 과녁이라 예고가 필요 없다.
    [RequireComponent(typeof(Rigidbody2D), typeof(HitReceiver))]
    public class BossOrb : MonoBehaviour
    {
        [SerializeField] private FeedbackData _popFeedback;

        private Rigidbody2D _rigidbody;
        private HitReceiver _hitReceiver;
        private Collider2D _collider;
        private Vector2 _velocity;
        private float _bounceJitter;

        public event Action<BossOrb> Popped;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _hitReceiver = GetComponent<HitReceiver>();
            _collider = GetComponent<Collider2D>();
        }

        private void OnEnable()
        {
            _hitReceiver.Died += OnDied;
        }

        private void OnDisable()
        {
            _hitReceiver.Died -= OnDied;
        }

        public void Launch(Vector2 position, Vector2 direction, float speed, float bounceJitter)
        {
            transform.position = position;
            _velocity = direction.normalized * speed;
            _bounceJitter = bounceJitter;
            gameObject.SetActive(true);
            _rigidbody.position = position;
            _hitReceiver.ResetHits(1);
        }

        // 터트리지 않고 거둘 때(보스 사망, 판 재시작). 실드 계산에 들어가지 않도록 알리지 않는다.
        public void Retract()
        {
            gameObject.SetActive(false);
        }

        private void FixedUpdate()
        {
            Vector2 next = _rigidbody.position + _velocity * Time.fixedDeltaTime;

            ArenaBounds arena = ArenaBounds.Current;
            if (arena != null)
            {
                Bounds bounds = arena.Bounds;
                float radius = _collider.bounds.extents.x;
                Vector2 min = (Vector2)bounds.min + new Vector2(radius, radius);
                Vector2 max = (Vector2)bounds.max - new Vector2(radius, radius);

                if (next.x < min.x || next.x > max.x)
                {
                    _velocity.x = next.x < min.x ? Mathf.Abs(_velocity.x) : -Mathf.Abs(_velocity.x);
                    Jitter(Vector2.right * Mathf.Sign(_velocity.x));
                }

                if (next.y < min.y || next.y > max.y)
                {
                    _velocity.y = next.y < min.y ? Mathf.Abs(_velocity.y) : -Mathf.Abs(_velocity.y);
                    Jitter(Vector2.up * Mathf.Sign(_velocity.y));
                }

                next = new Vector2(Mathf.Clamp(next.x, min.x, max.x), Mathf.Clamp(next.y, min.y, max.y));
            }

            _rigidbody.MovePosition(next);
        }

        // 같은 궤도를 맴돌지 않도록 반사각을 조금 흔든다. 흔든 뒤에도 벽 안쪽(inward)을 향하게 한다.
        private void Jitter(Vector2 inward)
        {
            float angle = UnityEngine.Random.Range(-_bounceJitter, _bounceJitter);
            Vector2 rotated = Quaternion.Euler(0f, 0f, angle) * _velocity;
            if (Vector2.Dot(rotated, inward) > 0.1f * rotated.magnitude)
            {
                _velocity = rotated;
            }
        }

        private void OnDied(HitInfo hit)
        {
            FeedbackPlayer.Play(_popFeedback, _rigidbody.position);
            gameObject.SetActive(false);
            Popped?.Invoke(this);
        }
    }
}
