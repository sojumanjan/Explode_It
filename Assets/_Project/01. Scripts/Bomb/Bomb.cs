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
        private static readonly RaycastHit2D[] LineBuffer = new RaycastHit2D[1];

        [SerializeField] private SpriteRenderer _body;
        [SerializeField] private ExplosionShape _rangeOutline;
        [SerializeField] private ExplosionShape _fuseFill;
        // 플레이어 레이어를 빼 두어 자폭 판정이 생기지 않게 한다.
        [SerializeField] private LayerMask _hitMask;
        [SerializeField] private LayerMask _obstacleMask;
        [SerializeField] private Color _explodeFlashColor = new Color(1f, 1f, 1f, 0.5f);
        [SerializeField, Min(0f)] private float _explosionFxDuration = 0.15f;

        private ContactFilter2D _hitFilter;
        private ContactFilter2D _obstacleFilter;
        private Sequence _sequence;
        private Action<Bomb> _onFinished;
        private float _radius;
        private Vector3 _bodyScale;

        // 매 투척마다 메서드 그룹 변환으로 델리게이트가 할당되지 않도록 캐싱한다.
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

            _obstacleFilter = new ContactFilter2D();
            _obstacleFilter.SetLayerMask(_obstacleMask);

            // 착지 펀치 연출이 스케일을 흔드므로, 재사용 시 되돌릴 원래 크기를 기억한다.
            _bodyScale = _body.transform.localScale;

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
        }

        // 폭탄은 포물선으로 날아가므로 구조물 너머로 던질 수 있다. 비행 중에는 충돌 검사를 하지 않는다.
        public void Launch(Vector2 target, WeaponStats stats, Action<Bomb> onFinished)
        {
            _onFinished = onFinished;
            _radius = stats.ExplosionRadius;
            ResetVisuals();

            _sequence = DOTween.Sequence()
                .Append(transform.DOJump(target, stats.ArcHeight, 1, stats.FlightDuration).SetEase(Ease.Linear))
                .Join(_body.transform.DOLocalRotate(new Vector3(0f, 0f, -360f), stats.FlightDuration, RotateMode.FastBeyond360))
                .AppendCallback(_onLanded)
                // 채움 원이 바깥 모양에 닿는 순간 터지므로, 남은 시간을 눈으로 읽을 수 있다.
                // 모양 전체를 확대하면 벽 쪽이 처음부터 찌그러져 보이므로, 원으로 퍼지다 벽에서 멈추게 반경을 키운다.
                .Append(DOVirtual.Float(0f, _radius, stats.FuseDelay, _onFillGrow).SetEase(Ease.Linear))
                .Join(_body.transform.DOPunchScale(_bodyScale * 0.3f, 0.15f, 6))
                .AppendCallback(_onExplode)
                .Append(DOVirtual.Float(1f, 0f, _explosionFxDuration, _onFade))
                .OnComplete(_onComplete);
        }

        // 구조물은 움직이지 않으므로 착지 순간 계산한 모양이 폭발 순간에도 그대로 유효하다.
        private void OnLanded()
        {
            _rangeOutline.Build(transform.position, _radius, _obstacleFilter);
            _fuseFill.CopyLimits(_rangeOutline);
            _fuseFill.SetRadius(0f);
            _rangeOutline.Visible = true;
            _fuseFill.Visible = true;
        }

        private void Explode()
        {
            Vector2 position = transform.position;
            int candidateCount = Physics2D.OverlapCircle(position, _radius, _hitFilter, HitBuffer);
            var hit = new HitInfo(position);
            int hitCount = 0;

            for (int i = 0; i < candidateCount; i++)
            {
                Collider2D candidate = HitBuffer[i];
                // 몸 일부만 가려진 경우에도 결과가 한 가지로 읽히도록 중심 한 점만 본다.
                if (IsBlocked(position, candidate.bounds.center))
                {
                    continue;
                }

                if (candidate.TryGetComponent(out IHittable hittable))
                {
                    hittable.ReceiveHit(hit);
                    hitCount++;
                }
            }

            _body.enabled = false;
            _fuseFill.SetColor(_explodeFlashColor);

            GameEvents.RaiseBombExploded(position, _radius, hitCount);
        }

        private bool IsBlocked(Vector2 from, Vector2 to)
        {
            return Physics2D.Linecast(from, to, _obstacleFilter, LineBuffer) > 0;
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
            _onFinished?.Invoke(this);
        }

        private void ResetVisuals()
        {
            _body.enabled = true;
            _body.transform.localRotation = Quaternion.identity;
            _body.transform.localScale = _bodyScale;

            _rangeOutline.Visible = false;
            _rangeOutline.ResetColor();

            _fuseFill.Visible = false;
            _fuseFill.ResetColor();
        }
    }
}
