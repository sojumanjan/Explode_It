using System;
using DG.Tweening;
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Bombs
{
    public class Bomb : MonoBehaviour
    {
        // 폭탄은 메인 스레드에서 하나씩 터지므로 버퍼 하나를 모든 폭탄이 공유해도 된다.
        // 크기는 화면 내 적 200마리 목표보다 넉넉하게 잡는다.
        private static readonly Collider2D[] HitBuffer = new Collider2D[256];

        [SerializeField] private SpriteRenderer _body;
        [SerializeField] private SpriteRenderer _rangeOutline;
        [SerializeField] private SpriteRenderer _fuseFill;
        // 플레이어 레이어를 빼 두어 자폭 판정이 생기지 않게 한다.
        [SerializeField] private LayerMask _hitMask;
        [SerializeField, Min(0f)] private float _explosionFxDuration = 0.15f;

        private ContactFilter2D _hitFilter;
        private Sequence _sequence;
        private Action<Bomb> _onFinished;
        private float _radius;
        private float _circleSpriteSize;
        private Color _outlineColor;
        private Color _fillColor;
        private Vector3 _bodyScale;

        // 매 투척마다 메서드 그룹 변환으로 델리게이트가 할당되지 않도록 캐싱한다.
        private TweenCallback _onLanded;
        private TweenCallback _onExplode;
        private TweenCallback _onComplete;

        private void Awake()
        {
            _hitFilter = new ContactFilter2D();
            _hitFilter.SetLayerMask(_hitMask);
            _hitFilter.useTriggers = true;

            // 스프라이트 크기가 1유닛이라고 가정하지 않고, 반경을 실제 월드 크기로 맞추기 위해 기준 크기를 잰다.
            _circleSpriteSize = _rangeOutline.sprite.bounds.size.x;
            _outlineColor = _rangeOutline.color;
            _fillColor = _fuseFill.color;
            // 착지 펀치 연출이 스케일을 흔드므로, 재사용 시 되돌릴 원래 크기를 기억한다.
            _bodyScale = _body.transform.localScale;

            _onLanded = OnLanded;
            _onExplode = Explode;
            _onComplete = Finish;
        }

        private void OnDisable()
        {
            _sequence?.Kill();
            _sequence = null;
        }

        public void Launch(Vector2 target, WeaponStats stats, Action<Bomb> onFinished)
        {
            _onFinished = onFinished;
            _radius = stats.ExplosionRadius;
            ResetVisuals();

            float diameterScale = _radius * 2f / _circleSpriteSize;

            _sequence = DOTween.Sequence()
                .Append(transform.DOJump(target, stats.ArcHeight, 1, stats.FlightDuration).SetEase(Ease.Linear))
                .Join(_body.transform.DOLocalRotate(new Vector3(0f, 0f, -360f), stats.FlightDuration, RotateMode.FastBeyond360))
                .AppendCallback(_onLanded)
                // 채움 원이 바깥 원에 닿는 순간 터지므로, 남은 시간을 눈으로 읽을 수 있다.
                .Append(_fuseFill.transform.DOScale(diameterScale, stats.FuseDelay).SetEase(Ease.Linear))
                .Join(_body.transform.DOPunchScale(_bodyScale * 0.3f, 0.15f, 6))
                .AppendCallback(_onExplode)
                .Append(_fuseFill.DOFade(0f, _explosionFxDuration))
                .Join(_rangeOutline.DOFade(0f, _explosionFxDuration))
                .OnComplete(_onComplete);
        }

        private void OnLanded()
        {
            float diameterScale = _radius * 2f / _circleSpriteSize;
            _rangeOutline.transform.localScale = new Vector3(diameterScale, diameterScale, 1f);
            _rangeOutline.enabled = true;
            _fuseFill.enabled = true;
        }

        private void Explode()
        {
            Vector2 position = transform.position;
            int hitCount = Physics2D.OverlapCircle(position, _radius, _hitFilter, HitBuffer);
            var hit = new HitInfo(position);
            for (int i = 0; i < hitCount; i++)
            {
                if (HitBuffer[i].TryGetComponent(out IHittable hittable))
                {
                    hittable.ReceiveHit(hit);
                }
            }

            _body.enabled = false;
            _fuseFill.color = new Color(1f, 1f, 1f, _fillColor.a);

            GameEvents.RaiseBombExploded(position, _radius, hitCount);
        }

        private void Finish()
        {
            _sequence = null;
            _onFinished?.Invoke(this);
        }

        private void ResetVisuals()
        {
            _body.enabled = true;
            _body.transform.localRotation = Quaternion.identity;
            _body.transform.localScale = _bodyScale;

            _rangeOutline.enabled = false;
            _rangeOutline.color = _outlineColor;

            _fuseFill.enabled = false;
            _fuseFill.color = _fillColor;
            _fuseFill.transform.localScale = Vector3.zero;
        }
    }
}
