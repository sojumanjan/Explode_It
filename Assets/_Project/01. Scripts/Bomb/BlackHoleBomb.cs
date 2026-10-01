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

        private SoundHandle _pullHandle = SoundHandle.None;

        // 흡입은 매 물리 스텝 범위 안 적을 다시 찾으므로, 콜라이더별 Enemy를 한 번만 찾아 둔다.
        // 적은 풀에서 재사용되어 콜라이더가 바뀌지 않으므로 캐시가 무효화되지 않는다.
        private readonly Dictionary<Collider2D, Enemy> _enemyCache = new Dictionary<Collider2D, Enemy>();

        private ContactFilter2D _hitFilter;
        private Sequence _sequence;
        private Action _onFinished;
        private float _radius;
        private float _pullSpeed;
        private bool _isPulling;
        private Vector3 _bodyScale;

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
            ResetVisuals();

            _sequence = DOTween.Sequence()
                .Append(transform.DOJump(target, weapon.ArcHeight, 1, weapon.FlightDuration).SetEase(Ease.Linear))
                .Join(_body.transform.DOLocalRotate(new Vector3(0f, 0f, -360f), weapon.FlightDuration, RotateMode.FastBeyond360))
                .AppendCallback(_onLanded)
                // 일반 폭탄과 같은 읽는 법: 채움 원이 바깥 원에 닿는 순간 터진다.
                .Append(DOVirtual.Float(0f, _radius, pullDuration, _onFillGrow).SetEase(Ease.Linear))
                .Join(_body.transform.DOLocalRotate(new Vector3(0f, 0f, -1080f), pullDuration, RotateMode.FastBeyond360).SetEase(Ease.InQuad))
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
                Enemy enemy = GetEnemy(OverlapBuffer[i]);
                if (enemy != null)
                {
                    enemy.PullToward(center, step);
                }
            }
        }

        private Enemy GetEnemy(Collider2D collider)
        {
            if (!_enemyCache.TryGetValue(collider, out Enemy enemy))
            {
                collider.TryGetComponent(out enemy);
                _enemyCache[collider] = enemy;
            }

            return enemy;
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
        }

        private void Explode()
        {
            _isPulling = false;
            StopPullSound();

            Vector2 position = transform.position;
            int count = Physics2D.OverlapCircle(position, _radius, _hitFilter, OverlapBuffer);
            var hit = new HitInfo(position);
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
            _body.transform.localRotation = Quaternion.identity;
            _body.transform.localScale = _bodyScale;

            _rangeOutline.Visible = false;
            _rangeOutline.ResetColor();

            _fuseFill.Visible = false;
            _fuseFill.ResetColor();
        }
    }
}
