using System;
using System.Collections.Generic;
using DG.Tweening;
using ExplodeIt.Core;
using ExplodeIt.Enemies;
using UnityEngine;

namespace ExplodeIt.Bombs
{
    // 특수 능력 폭탄. 착지 후 범위 안의 적을 중심으로 빨아들이다가 터진다.
    // 흡입과 피해 모두 구조물을 무시한다. 단, 끌려가는 적의 몸은 벽을 통과하지 못해 벽 앞에서 멈춘다.
    public class BlackHoleBomb : MonoBehaviour
    {
        private static readonly Collider2D[] OverlapBuffer = new Collider2D[256];

        [SerializeField] private SpriteRenderer _body;
        [SerializeField] private ExplosionShape _rangeOutline;
        [SerializeField] private ExplosionShape _fuseFill;
        // 플레이어 레이어를 빼 두어 자폭 판정이 생기지 않게 한다.
        [SerializeField] private LayerMask _hitMask;
        [SerializeField] private Color _explodeFlashColor = new Color(1f, 1f, 1f, 0.6f);
        [SerializeField, Min(0f)] private float _explosionFxDuration = 0.25f;
        // 블랙홀은 하나뿐인 특수 폭탄이라 전역 이벤트 대신 자기 데이터로 소리를 낸다.
        // 흡입음은 착지부터 폭발까지 나고, 폭발하는 순간 끊는다.
        [SerializeField] private SoundData _pullSound;
        [SerializeField] private SoundData _explodeSound;
        [SerializeField] private FeedbackData _explodeFeedback;
        // 차단 영역에 닿아 타 버릴 때: 뾰잉 하고 부풀었다가(배율, 초) 0까지 쪼그라든다(초). 판정과 상관없는 그림 연출이다.
        [SerializeField] private Color _burnTint = new Color(1f, 0.45f, 0.2f, 1f);
        [SerializeField, Min(1f)] private float _burnPopScale = 1.35f;
        [SerializeField, Min(0f)] private float _burnPopDuration = 0.08f;
        [SerializeField, Min(0f)] private float _burnShrinkDuration = 0.3f;

        private SoundHandle _pullHandle = SoundHandle.None;

        // 흡입은 매 물리 스텝 범위 안 대상을 다시 찾으므로, 콜라이더별로 끌려가는 대상(적, 혼불)을 한 번만 찾아 둔다.
        // 적과 혼불은 재사용되어 콜라이더가 바뀌지 않으므로 캐시가 무효화되지 않는다.
        private readonly Dictionary<Collider2D, IPullable> _pullableCache = new Dictionary<Collider2D, IPullable>();

        private ContactFilter2D _hitFilter;
        private Sequence _sequence;
        private Action _onFinished;
        private float _radius;
        private float _pullSpeed;
        private bool _isPulling;
        private Vector3 _bodyScale;
        private Color _bodyColor;
        private float _pullDuration;
        // 날아가는 동안의 바닥 위치를 알기 위한 값. 포물선 그림은 높이가 섞여 있어 바닥 위치로 쓸 수 없다.
        private bool _isFlying;
        private Vector2 _flightStart;
        private Vector2 _flightTarget;
        private float _flightDuration;
        private float _flightTime;

        private TweenCallback _onStartFuse;
        private TweenCallback _onLanded;
        private TweenCallback _onExplode;
        private TweenCallback _onComplete;
        private TweenCallback<float> _onFade;
        private TweenCallback<float> _onFillGrow;

        private void Awake()
        {
            _hitFilter = new ContactFilter2D();
            _hitFilter.SetLayerMask(_hitMask);
            _hitFilter.useTriggers = true;

            _bodyScale = _body.transform.localScale;
            _bodyColor = _body.color;

            _onStartFuse = StartFuse;
            _onLanded = OnLanded;
            _onExplode = Explode;
            _onComplete = Finish;
            _onFade = Fade;
            _onFillGrow = GrowFill;
        }

        private void OnDisable()
        {
            _sequence?.Kill();
            _sequence = null;
            _isPulling = false;
            _isFlying = false;
            // 흡입 도중 재시작으로 꺼지면, 씬을 넘어 살아 있는 오디오 매니저에서 소리가 계속 나지 않게 끊는다.
            StopPullSound();
        }

        private void StopPullSound()
        {
            AudioManager.Stop(_pullHandle);
            _pullHandle = SoundHandle.None;
        }

        public void Launch(Vector2 target, float radius, float pullDuration, float pullSpeed,
            WeaponStats weapon, Action onFinished)
        {
            _onFinished = onFinished;
            _radius = radius;
            _pullSpeed = pullSpeed;
            _pullDuration = pullDuration;
            ResetVisuals();

            _isFlying = true;
            _flightStart = transform.position;
            _flightTarget = target;
            _flightDuration = weapon.FlightDuration;
            _flightTime = 0f;

            _sequence = DOTween.Sequence()
                .Append(transform.DOJump(target, weapon.ArcHeight, 1, weapon.FlightDuration).SetEase(Ease.Linear))
                .Join(_body.transform.DOLocalRotate(new Vector3(0f, 0f, -360f), weapon.FlightDuration, RotateMode.FastBeyond360))
                .OnComplete(_onStartFuse);
        }

        // 날아가는 도중 차단 영역(화염 영역 등)에 닿으면 일반 폭탄처럼 터지지 않고 그 자리에서 타 버린다.
        private void Update()
        {
            if (!_isFlying)
            {
                return;
            }

            _flightTime += Time.deltaTime;
            float t = _flightDuration > 0f ? Mathf.Clamp01(_flightTime / _flightDuration) : 1f;
            Vector2 ground = Vector2.Lerp(_flightStart, _flightTarget, t);
            if (BombBarriers.TryGetBlocking(ground, out IBombBarrier barrier))
            {
                Burn(barrier);
            }
        }

        private void Burn(IBombBarrier barrier)
        {
            _isFlying = false;
            _sequence?.Kill();
            barrier.OnBombBurned(transform.position);

            Transform body = _body.transform;
            _sequence = DOTween.Sequence()
                .Append(body.DOScale(_bodyScale * _burnPopScale, _burnPopDuration).SetEase(Ease.OutQuad))
                .Join(_body.DOColor(_burnTint, _burnPopDuration))
                .Append(body.DOScale(Vector3.zero, _burnShrinkDuration).SetEase(Ease.InBack))
                .OnComplete(_onComplete);
        }

        // 착지 → 빨아들이며 채움 원이 차오름 → 터짐. 일반 폭탄과 같은 읽는 법: 채움 원이 바깥 원에 닿는 순간 터진다.
        private void StartFuse()
        {
            _isFlying = false;
            _sequence = DOTween.Sequence()
                .AppendCallback(_onLanded)
                .Append(DOVirtual.Float(0f, _radius, _pullDuration, _onFillGrow).SetEase(Ease.Linear))
                .Join(_body.transform.DOLocalRotate(new Vector3(0f, 0f, -1080f), _pullDuration, RotateMode.FastBeyond360).SetEase(Ease.InQuad))
                .AppendCallback(_onExplode)
                .Append(DOVirtual.Float(1f, 0f, _explosionFxDuration, _onFade))
                .OnComplete(_onComplete);
        }

        // 적 이동과 같은 물리 스텝에서 끌어당겨야 MovePosition이 덮어써지지 않는다.
        private void FixedUpdate()
        {
            if (!_isPulling)
            {
                return;
            }

            Vector2 center = transform.position;
            float step = _pullSpeed * Time.fixedDeltaTime;
            int count = Physics2D.OverlapCircle(center, _radius, _hitFilter, OverlapBuffer);
            for (int i = 0; i < count; i++)
            {
                IPullable pullable = GetPullable(OverlapBuffer[i]);
                if (pullable != null)
                {
                    pullable.PullToward(center, step);
                }
            }
        }

        private IPullable GetPullable(Collider2D collider)
        {
            if (!_pullableCache.TryGetValue(collider, out IPullable pullable))
            {
                collider.TryGetComponent(out pullable);
                _pullableCache[collider] = pullable;
            }

            return pullable;
        }

        private void OnLanded()
        {
            _rangeOutline.BuildCircle(_radius);
            _fuseFill.CopyLimits(_rangeOutline);
            _fuseFill.SetRadius(0f);
            _rangeOutline.Visible = true;
            _fuseFill.Visible = true;
            _isPulling = true;
            _pullHandle = AudioManager.Play(_pullSound);
            GameEvents.RaiseBombLanded(transform.position, _radius);
        }

        private void Explode()
        {
            _isPulling = false;
            StopPullSound();

            Vector2 position = transform.position;
            int count = Physics2D.OverlapCircle(position, _radius, _hitFilter, OverlapBuffer);
            // 적을 가운데로 모아 터지므로 폭발점이 방패 정면이 되기 쉽다. 방패병에게 쓰는 수단이 되도록 방패를 관통한다.
            var hit = new HitInfo(position, ignoresGuard: true);
            for (int i = 0; i < count; i++)
            {
                if (OverlapBuffer[i].TryGetComponent(out IHittable hittable))
                {
                    hittable.ReceiveHit(hit);
                }
            }

            _body.enabled = false;
            _fuseFill.SetColor(_explodeFlashColor);
            AudioManager.Play(_explodeSound);
            FeedbackPlayer.Play(_explodeFeedback, position);
        }

        private void GrowFill(float radius)
        {
            _fuseFill.SetRadius(radius);
        }

        private void Fade(float opacity)
        {
            _rangeOutline.SetOpacity(opacity);
            _fuseFill.SetOpacity(opacity);
        }

        private void Finish()
        {
            _sequence = null;
            _onFinished?.Invoke();
        }

        private void ResetVisuals()
        {
            _body.enabled = true;
            _body.color = _bodyColor;
            _body.transform.localRotation = Quaternion.identity;
            _body.transform.localScale = _bodyScale;

            _rangeOutline.Visible = false;
            _rangeOutline.ResetColor();

            _fuseFill.Visible = false;
            _fuseFill.ResetColor();
        }
    }
}
