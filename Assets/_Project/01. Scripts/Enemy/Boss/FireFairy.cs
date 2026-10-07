using System;
using DG.Tweening;
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 화염 마법사의 영역을 붙잡고 있는 요정. 마법사 둘레를 일정한 속도로 돌며, 모두 터지면 영역이 풀린다.
    // 궤도가 완전히 정해져 있어서 1초 뒤 지나갈 자리에 폭탄을 두는 것이 과제다. 그래서 블랙홀에도 끌려가지 않는다.
    // 닿아도 플레이어는 죽지 않는다. 공격이 아니라 맞혀야 하는 과녁이라 예고가 필요 없다.
    [RequireComponent(typeof(Rigidbody2D), typeof(HitReceiver))]
    public class FireFairy : MonoBehaviour
    {
        // 그림만 붕붕 뜨는 높이(유닛)와 빠르기(회/초). 판정 위치는 궤도 그대로라 그림 전용이다.
        private const float BobHeight = 0.12f;
        private const float BobRate = 1.8f;
        // 날갯짓처럼 그림만 살짝 늘었다 줄었다(비율) 기우뚱거리는(도) 연출.
        private const float FlapSquash = 0.08f;
        private const float FlapRate = 3.5f;
        private const float SwayAngle = 10f;
        private const float SwayRate = 1.1f;
        // 죽는 연출: 움찔 커졌다가 빙글 돌며 위로 떠오르면서 사라진다.
        private const float DiePunch = 0.35f;
        private const float DieDuration = 0.55f;
        private const float DieRise = 0.5f;
        private const float DieSpin = 200f;
        // 방향 반전 예고: 멈춘 채 좌우로 떨며(유닛, 회/초) 살짝 부푼다(비율).
        private const float HoldShake = 0.07f;
        private const float HoldShakeRate = 18f;
        private const float HoldSwell = 0.15f;
        // 나타날 때 0에서 톡 튀어나오며 커지는 시간 (초).
        private const float AppearDuration = 0.25f;

        [SerializeField] private FeedbackData _popFeedback;
        // 떠다니는 연출을 줄 그림.
        [SerializeField] private Transform _visual;
        [SerializeField] private SpriteRenderer _sprite;
        [SerializeField] private Sprite _dieSprite;

        private Rigidbody2D _rigidbody;
        private HitReceiver _hitReceiver;
        private Collider2D _collider;
        private Sprite _aliveSprite;
        private Color _aliveColor;
        private Vector3 _visualScale;
        private Sequence _dieSequence;
        private TweenCallback _onDieComplete;
        private bool _isDying;
        // 반전 예고로 멈춰 있는 남은 시간. 끝나면 _pendingSpeed로 돈다.
        private float _holdTime;
        private float _pendingSpeed;
        private float _appearTime;
        private Vector2 _center;
        private float _radius;
        private float _angle;
        private float _speed;
        private float _bobTime;

        public event Action<FireFairy> Popped;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _hitReceiver = GetComponent<HitReceiver>();
            _collider = GetComponent<Collider2D>();
            _aliveSprite = _sprite.sprite;
            _aliveColor = _sprite.color;
            _visualScale = _visual.localScale;
            _onDieComplete = OnDieComplete;
        }

        private void OnEnable()
        {
            _hitReceiver.Died += OnDied;
        }

        private void OnDisable()
        {
            _hitReceiver.Died -= OnDied;
            _dieSequence?.Kill();
            _dieSequence = null;
        }

        // angle: 궤도 위 시작 각도 (도). speed: 부호가 방향(양수 반시계). 요정끼리 같은 속도로 돌아 처음 벌려 둔 간격이 그대로 유지된다.
        public void Launch(Vector2 center, float radius, float angle, float speed)
        {
            _center = center;
            _radius = radius;
            _angle = angle;
            _speed = speed;
            _holdTime = 0f;
            _appearTime = 0f;
            _bobTime = UnityEngine.Random.Range(0f, 10f);
            Vector2 position = PositionAt(_angle);
            transform.position = position;
            gameObject.SetActive(true);
            _rigidbody.position = position;
            _hitReceiver.ResetHits(1);

            _dieSequence?.Kill();
            _dieSequence = null;
            _isDying = false;
            _collider.enabled = true;
            _sprite.sprite = _aliveSprite;
            _sprite.color = _aliveColor;
            _visual.localScale = _visualScale;
            _visual.localRotation = Quaternion.identity;
        }

        // 동료가 잡혔을 때: 잠깐 멈춰 떨다가 새 속도(부호가 방향)로 돈다. 남은 요정이 모두 같은 시간 멈추므로 간격은 유지된다.
        public void ChangeOrbit(float speed, float holdTime)
        {
            if (_isDying || !gameObject.activeSelf)
            {
                return;
            }

            _pendingSpeed = speed;
            _holdTime = holdTime;
            if (holdTime <= 0f)
            {
                _speed = speed;
            }
        }

        // 터트리지 않고 거둘 때(보스 사망, 판 재시작). 실드 계산에 들어가지 않도록 알리지 않는다.
        public void Retract()
        {
            gameObject.SetActive(false);
        }

        private void FixedUpdate()
        {
            if (_isDying)
            {
                return;
            }

            if (_holdTime > 0f)
            {
                _holdTime -= Time.fixedDeltaTime;
                if (_holdTime > 0f)
                {
                    return;
                }

                _speed = _pendingSpeed;
            }

            _angle += _speed * Time.fixedDeltaTime;
            _rigidbody.MovePosition(PositionAt(_angle));
        }

        private void LateUpdate()
        {
            if (_isDying)
            {
                return;
            }

            _bobTime += Time.deltaTime;
            _appearTime += Time.deltaTime;
            const float tau = 2f * Mathf.PI;
            bool holding = _holdTime > 0f;
            float shake = holding ? Mathf.Sin(_bobTime * HoldShakeRate * tau) * HoldShake : 0f;
            float swell = (holding ? 1f + HoldSwell : 1f) * AppearScale();
            _visual.localPosition = new Vector3(shake, Mathf.Sin(_bobTime * BobRate * tau) * BobHeight, 0f);
            float flap = Mathf.Sin(_bobTime * FlapRate * tau) * FlapSquash;
            _visual.localScale = new Vector3(_visualScale.x * (1f - flap) * swell, _visualScale.y * (1f + flap) * swell, _visualScale.z);
            _visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_bobTime * SwayRate * tau) * SwayAngle);
        }

        // 살짝 넘쳤다가 제 크기로 돌아오는 튀어나옴(OutBack).
        private float AppearScale()
        {
            float t = Mathf.Clamp01(_appearTime / AppearDuration);
            if (t >= 1f)
            {
                return 1f;
            }

            const float overshoot = 1.70158f;
            float u = t - 1f;
            return 1f + (overshoot + 1f) * u * u * u + overshoot * u * u;
        }

        private Vector2 PositionAt(float angle)
        {
            float radians = angle * Mathf.Deg2Rad;
            return _center + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * _radius;
        }

        private void OnDied(HitInfo hit)
        {
            FeedbackPlayer.Play(_popFeedback, _rigidbody.position);
            PlayDie();
            Popped?.Invoke(this);
        }

        // 터진 순간부터 판정은 없고 그림만 남아 사라진다. 다른 폭탄이 시체를 또 맞히지 않게 콜라이더를 끈다.
        private void PlayDie()
        {
            _isDying = true;
            _collider.enabled = false;
            if (_dieSprite != null)
            {
                _sprite.sprite = _dieSprite;
            }

            _visual.localScale = _visualScale;
            _visual.localRotation = Quaternion.identity;
            Color clear = _aliveColor;
            clear.a = 0f;

            _dieSequence?.Kill();
            _dieSequence = DOTween.Sequence()
                .Append(_visual.DOPunchScale(_visualScale * DiePunch, DieDuration * 0.35f, 6))
                .Append(_visual.DOLocalMoveY(_visual.localPosition.y + DieRise, DieDuration * 0.65f).SetEase(Ease.OutQuad))
                .Join(_visual.DOLocalRotate(new Vector3(0f, 0f, DieSpin), DieDuration * 0.65f, RotateMode.FastBeyond360).SetEase(Ease.InQuad))
                .Join(_visual.DOScale(_visualScale * 0.6f, DieDuration * 0.65f).SetEase(Ease.InQuad))
                .Join(_sprite.DOColor(clear, DieDuration * 0.65f).SetEase(Ease.InQuad))
                .OnComplete(_onDieComplete);
        }

        private void OnDieComplete()
        {
            _dieSequence = null;
            gameObject.SetActive(false);
        }
    }
}
