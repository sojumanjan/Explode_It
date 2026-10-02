using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

namespace ExplodeIt.Core
{
    // 연출 재생기. 흔들림은 Cinemachine Impulse, 히트스톱은 잠깐의 timeScale 감소, 파티클은 풀링으로 처리한다.
    // 연출을 낼 쪽은 FeedbackPlayer.Play(데이터, 위치)만 부른다.
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public class FeedbackPlayer : MonoBehaviour
    {
        private static FeedbackPlayer _current;

        [SerializeField, Min(0)] private int _effectPrewarm = 8;
        [SerializeField, Min(1)] private int _effectMaxSize = 64;

        private readonly Dictionary<PooledEffect, ComponentPool<PooledEffect>> _pools = new Dictionary<PooledEffect, ComponentPool<PooledEffect>>();
        private readonly Dictionary<PooledEffect, Action<PooledEffect>> _releases = new Dictionary<PooledEffect, Action<PooledEffect>>();
        private CinemachineImpulseSource _impulse;

        private bool _isPlaying;
        private bool _isHitStopping;
        private float _hitStopEnd;
        private float _savedTimeScale = 1f;

        // 개발자 패널에서 연출마다 켜고 꺼서 있을 때와 없을 때를 비교한다.
        public static bool ShakeEnabled { get; set; } = true;
        public static bool HitStopEnabled { get; set; } = true;
        public static bool EffectsEnabled { get; set; } = true;

        // 히트스톱 중에는 timeScale이 잠깐 낮아져 있으므로, 일시정지가 되돌아갈 속도로 이 값을 쓴다.
        public static float BaseTimeScale => _current != null && _current._isHitStopping ? _current._savedTimeScale : Time.timeScale;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            _current = null;
            ShakeEnabled = true;
            HitStopEnabled = true;
            EffectsEnabled = true;
        }

        private void Awake()
        {
            _current = this;
            _impulse = GetComponent<CinemachineImpulseSource>();
        }

        private void OnEnable()
        {
            GameEvents.GameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.GameStateChanged -= OnGameStateChanged;
        }

        private void OnDestroy()
        {
            if (_current == this)
            {
                _current = null;
            }
        }

        public static void Play(FeedbackData data, Vector2 position)
        {
            if (_current != null && data != null)
            {
                _current.PlayInternal(data, position);
            }
        }

        // 첫 사용 순간에 이펙트 생성 스파이크가 나지 않도록 전투 전에 풀을 채워 둔다.
        public static void Prepare(FeedbackData data)
        {
            if (_current != null && data != null && data.Effect != null)
            {
                _current.GetPool(data.Effect);
            }
        }

        // 히트스톱은 실제 시간으로 끝나야 하므로 timeScale과 무관한 시간으로 잰다.
        private void Update()
        {
            if (_isHitStopping && Time.unscaledTime >= _hitStopEnd)
            {
                _isHitStopping = false;
                Time.timeScale = _savedTimeScale;
            }
        }

        private void PlayInternal(FeedbackData data, Vector2 position)
        {
            if (ShakeEnabled && data.ShakeForce > 0f)
            {
                // 방향을 매번 바꿔야 같은 사건이 반복돼도 단조롭지 않다.
                Vector2 direction = UnityEngine.Random.insideUnitCircle.normalized;
                _impulse.GenerateImpulseAtPositionWithVelocity(position, direction * data.ShakeForce);
            }

            if (HitStopEnabled && _isPlaying && data.HitStopDuration > 0f)
            {
                StartHitStop(data.HitStopDuration, data.HitStopTimeScale);
            }

            if (EffectsEnabled && data.Effect != null)
            {
                PooledEffect effect = GetPool(data.Effect).Get(position);
                effect.Play(data.EffectScale, _releases[data.Effect]);
            }
        }

        // 겹치면 더 늦게 끝나는 쪽을 따르고, 처음 걸릴 때의 속도만 기억해 둔다.
        private void StartHitStop(float duration, float timeScale)
        {
            if (!_isHitStopping)
            {
                _savedTimeScale = Time.timeScale;
                _isHitStopping = true;
            }

            Time.timeScale = Mathf.Min(Time.timeScale, _savedTimeScale * timeScale);
            _hitStopEnd = Mathf.Max(_hitStopEnd, Time.unscaledTime + duration);
        }

        private ComponentPool<PooledEffect> GetPool(PooledEffect prefab)
        {
            if (!_pools.TryGetValue(prefab, out ComponentPool<PooledEffect> pool))
            {
                pool = new ComponentPool<PooledEffect>(prefab, transform, _effectPrewarm, _effectMaxSize);
                pool.Prewarm(_effectPrewarm);
                _pools.Add(prefab, pool);
                _releases.Add(prefab, pool.Release);
            }

            return pool;
        }

        // 히트스톱 도중 판이 멈추면: 일시정지는 GameStateController가 BaseTimeScale로 되돌릴 속도를 이미 챙겼고,
        // 사망·클리어는 timeScale을 건드리지 않으므로 여기서 원래 속도로 되돌린다.
        private void OnGameStateChanged(GameState previous, GameState current)
        {
            _isPlaying = current == GameState.Playing;
            if (!_isHitStopping || _isPlaying)
            {
                return;
            }

            _isHitStopping = false;
            if (current != GameState.Paused)
            {
                Time.timeScale = _savedTimeScale;
            }
        }
    }
}
