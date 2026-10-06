using System;
using ExplodeIt.Core;
using ExplodeIt.Stage;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 구체 보스의 실드를 붙잡고 있는 혼불(분노가 몸 밖으로 빠져나온 덩어리). 직선으로 날아다니다 맵 테두리에서만 튕기고, 내부 구조물은 통과한다.
    // 그림만 붕붕 떠다니고 일렁인다. 판정 위치는 직선 경로 그대로다.
    // 닿아도 플레이어는 죽지 않는다. 공격이 아니라 맞혀야 하는 과녁이라 예고가 필요 없다.
    [RequireComponent(typeof(Rigidbody2D), typeof(HitReceiver))]
    public class BossOrb : MonoBehaviour, IPullable
    {
        [SerializeField] private FeedbackData _popFeedback;
        // 보스와 이어지는 선. 구체가 보스를 지키고 있다는 걸 보여주고, 터지면 구체와 함께 사라진다.
        [SerializeField] private LineRenderer _tether;
        // 떠다니는 연출을 줄 그림.
        [SerializeField] private Transform _visual;

        private Rigidbody2D _rigidbody;
        private HitReceiver _hitReceiver;
        private Collider2D _collider;
        private Vector2 _velocity;
        private OrbBossData _data;
        private Transform _anchor;
        private Vector3 _visualBaseScale;
        private float _floatTime;
        // 블랙홀에 끌려가는 동안은 자기 직선 이동을 멈추고 끌려가기만 한다. 적이 끌려갈 때와 같은 규칙이다.
        private Vector2 _pullStep;
        private float _lastPulledTime = -1f;

        public event Action<BossOrb> Popped;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _hitReceiver = GetComponent<HitReceiver>();
            _collider = GetComponent<Collider2D>();
            if (_visual != null)
            {
                _visualBaseScale = _visual.localScale;
            }
        }

        private void OnEnable()
        {
            _hitReceiver.Died += OnDied;
        }

        private void OnDisable()
        {
            _hitReceiver.Died -= OnDied;
        }

        public void Launch(Vector2 position, Vector2 direction, float speed, OrbBossData data, Transform anchor)
        {
            _data = data;
            _anchor = anchor;
            transform.position = position;
            _velocity = direction.normalized * speed;
            _lastPulledTime = -1f;
            _pullStep = Vector2.zero;
            // 여러 개가 같은 박자로 떠다니면 기계적으로 보여서 시작 박자를 흩어 둔다.
            _floatTime = UnityEngine.Random.Range(0f, 10f);
            gameObject.SetActive(true);
            _rigidbody.position = position;
            _hitReceiver.ResetHits(1);
        }

        // 터트리지 않고 거둘 때(보스 사망, 판 재시작). 실드 계산에 들어가지 않도록 알리지 않는다.
        public void Retract()
        {
            gameObject.SetActive(false);
        }

        // 혼불은 구조물을 통과하므로 벽 검사 없이 중심 쪽으로 끌려간다. 중심을 지나쳐 떨리지 않게 남은 거리까지만 간다.
        public void PullToward(Vector2 center, float step)
        {
            _lastPulledTime = Time.fixedTime;
            Vector2 toCenter = center - _rigidbody.position;
            float distance = toCenter.magnitude;
            _pullStep = distance > 0.0001f ? toCenter / distance * Mathf.Min(step, distance) : Vector2.zero;
        }

        private void FixedUpdate()
        {
            // 끌어당기는 쪽과 이 컴포넌트의 FixedUpdate 순서는 정해져 있지 않으므로 한 스텝 여유를 둔다.
            bool isPulled = Time.fixedTime - _lastPulledTime <= Time.fixedDeltaTime * 1.5f;
            Vector2 next = isPulled
                ? _rigidbody.position + _pullStep
                : _rigidbody.position + _velocity * Time.fixedDeltaTime;
            _pullStep = Vector2.zero;

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

        // 물리 보간된 위치가 정해진 뒤 그림을 흔들고 선을 잇는다.
        private void LateUpdate()
        {
            if (_data == null)
            {
                return;
            }

            _floatTime += Time.deltaTime;
            if (_visual != null)
            {
                float bob = Mathf.Sin(_floatTime * _data.OrbBobRate * 2f * Mathf.PI) * _data.OrbBobHeight;
                float flicker = 1f + Mathf.Sin(_floatTime * _data.OrbFlickerRate * 2f * Mathf.PI) * _data.OrbFlicker;
                float sway = Mathf.Sin(_floatTime * _data.OrbBobRate * Mathf.PI) * _data.OrbSway;
                _visual.localPosition = new Vector3(0f, bob, 0f);
                _visual.localScale = new Vector3(_visualBaseScale.x * (2f - flicker), _visualBaseScale.y * flicker, _visualBaseScale.z);
                _visual.localRotation = Quaternion.Euler(0f, 0f, sway);
            }

            if (_tether != null && _anchor != null)
            {
                _tether.SetPosition(0, (Vector2)_anchor.position + _data.TetherAnchorOffset);
                _tether.SetPosition(1, _visual != null ? _visual.position : transform.position);
            }
        }

        // 같은 궤도를 맴돌지 않도록 반사각을 조금 흔든다. 흔든 뒤에도 벽 안쪽(inward)을 향하게 한다.
        private void Jitter(Vector2 inward)
        {
            float angle = UnityEngine.Random.Range(-_data.OrbBounceJitter, _data.OrbBounceJitter);
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
