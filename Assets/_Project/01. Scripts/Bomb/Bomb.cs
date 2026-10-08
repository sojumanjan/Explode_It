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
        // 차단 영역에 닿아 타 버릴 때: 뾰잉 하고 부풀었다가(배율, 초) 0까지 쪼그라든다(초). 판정과 상관없는 그림 연출이다.
        [SerializeField] private Color _burnTint = new Color(1f, 0.45f, 0.2f, 1f);
        [SerializeField, Min(1f)] private float _burnPopScale = 1.35f;
        [SerializeField, Min(0f)] private float _burnPopDuration = 0.08f;
        [SerializeField, Min(0f)] private float _burnShrinkDuration = 0.3f;
        // 착지 후 터지기 전까지 터질 듯 말 듯 부풀었다 줄었다 하는 연출. 처음엔 느리고 작게, 터지기 직전엔 빠르고 크게 + 붉게 달아오른다.
        // 남은 시간은 채움 원이 정확히 보여주므로 그림 전용이다. 부풂(비율): 처음/끝, 횟수: 착지부터 폭발까지 몇 번 뛰는지.
        [SerializeField, Min(0f)] private float _throbStart = 0.06f;
        [SerializeField, Min(0f)] private float _throbEnd = 0.25f;
        [SerializeField, Min(0f)] private float _throbCycles = 3f;
        [SerializeField] private Color _throbTint = new Color(1f, 0.35f, 0.3f, 1f);

        private ContactFilter2D _hitFilter;
        private ContactFilter2D _obstacleFilter;
        private Sequence _sequence;
        private Action<Bomb> _onFinished;
        private float _radius;
        private Vector3 _bodyScale;
        private Color _bodyColor;
        // 날아가는 동안의 바닥 위치를 알기 위한 값. 포물선 그림은 높이가 섞여 있어 바닥 위치로 쓸 수 없다.
        private bool _isFlying;
        private Vector2 _flightStart;
        private Vector2 _flightTarget;
        private float _flightDuration;
        private float _flightTime;

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
            _bodyColor = _body.color;

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
            _isFlying = false;
        }

        // 날아가는 도중 차단 영역(화염 영역 등)에 닿으면 터지지 않고 그 자리에서 타 버린다. 영역 안쪽으로는 폭탄을 넣을 수 없다.
        // 터트리면 영역 둘레를 도는 요정이 우연히 휘말려, 1초 뒤 자리를 노리는 예측과 상관없이 잡히기 때문이다.
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

        // 공중에서 닿은 그대로 타 버리므로 바닥으로 옮기지 않는다.
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

        // 폭탄은 포물선으로 날아가므로 구조물 너머로 던질 수 있다. 비행 중에는 충돌 검사를 하지 않는다.
        public void Launch(Vector2 target, WeaponStats stats, Action<Bomb> onFinished)
        {
            _onFinished = onFinished;
            _radius = stats.ExplosionRadius;
            ResetVisuals();
            _isFlying = true;
            _flightStart = transform.position;
            _flightTarget = target;
            _flightDuration = stats.FlightDuration;
            _flightTime = 0f;

            _sequence = DOTween.Sequence()
                .Append(transform.DOJump(target, stats.ArcHeight, 1, stats.FlightDuration).SetEase(Ease.Linear))
                .Join(_body.transform.DOLocalRotate(new Vector3(0f, 0f, -360f), stats.FlightDuration, RotateMode.FastBeyond360))
                .AppendCallback(_onLanded)
                // 채움 원이 바깥 모양에 닿는 순간 터지므로, 남은 시간을 눈으로 읽을 수 있다.
                // 모양 전체를 확대하면 벽 쪽이 처음부터 찌그러져 보이므로, 원으로 퍼지다 벽에서 멈추게 반경을 키운다.
                .Append(DOVirtual.Float(0f, _radius, stats.FuseDelay, _onFillGrow).SetEase(Ease.Linear))
                .AppendCallback(_onExplode)
                .Append(DOVirtual.Float(1f, 0f, _explosionFxDuration, _onFade))
                .OnComplete(_onComplete);
        }

        // 구조물은 움직이지 않으므로 착지 순간 계산한 모양이 폭발 순간에도 그대로 유효하다.
        private void OnLanded()
        {
            _isFlying = false;
            _rangeOutline.Build(transform.position, _radius, _obstacleFilter);
            _fuseFill.CopyLimits(_rangeOutline);
            _fuseFill.SetRadius(0f);
            _rangeOutline.Visible = true;
            _fuseFill.Visible = true;
            GameEvents.RaiseBombLanded(transform.position, _radius);
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
            Throb(_radius > 0f ? radius / _radius : 1f);
        }

        // progress 0(착지) → 1(폭발). 뛰는 간격이 점점 짧아지도록 위상을 progress의 제곱으로 키운다.
        private void Throb(float progress)
        {
            float phase = _throbCycles * (0.35f * progress + 0.65f * progress * progress);
            float beat = 0.5f - 0.5f * Mathf.Cos(phase * 2f * Mathf.PI);
            float amount = Mathf.Lerp(_throbStart, _throbEnd, progress) * beat;
            _body.transform.localScale = _bodyScale * (1f + amount);
            _body.color = Color.Lerp(_bodyColor, _throbTint, beat * progress);
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
