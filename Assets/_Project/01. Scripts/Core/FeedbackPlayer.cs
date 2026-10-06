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

        // 동시에 걸린 히트스톱들. 짧고 강한 멈칫(폭탄)과 길고 약한 슬로모션(보스 처치)이 겹쳐도,
        // 짧은 쪽이 끝나면 남은 긴 쪽 속도로 돌아가야 해서 하나로 합치지 않고 따로 기억한다.
        private const int MaxHitStops = 4;
        private readonly float[] _stopEnds = new float[MaxHitStops];
        private readonly float[] _stopScales = new float[MaxHitStops];

        private bool _isPlaying;
        private bool _isHitStopping;
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
            if (_isHitStopping)
            {
                ApplyHitStops();
            }
        }

        // 아직 안 끝난 히트스톱 중 가장 느린 속도를 쓴다. 모두 끝나면 원래 속도로 되돌린다.
        private void ApplyHitStops()
        {
            float now = Time.unscaledTime;
            float scale = 1f;
            bool isActive = false;
            for (int i = 0; i < MaxHitStops; i++)
            {
                if (now < _stopEnds[i])
                {
                    scale = Mathf.Min(scale, _stopScales[i]);
                    isActive = true;
                }
            }

            if (!isActive)
            {
                _isHitStopping = false;
                Time.timeScale = _savedTimeScale;
                return;
            }

            Time.timeScale = _savedTimeScale * scale;
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

        // 처음 걸릴 때의 속도만 기억해 두고, 칸이 모자라면 가장 먼저 끝나는 칸을 덮어쓴다.
        private void StartHitStop(float duration, float timeScale)
        {
            float now = Time.unscaledTime;
            if (!_isHitStopping)
            {
                _savedTimeScale = Time.timeScale;
                _isHitStopping = true;
                for (int i = 0; i < MaxHitStops; i++)
                {
                    _stopEnds[i] = 0f;
                }
            }

            int slot = 0;
            for (int i = 1; i < MaxHitStops; i++)
            {
                if (_stopEnds[i] < _stopEnds[slot])
                {
                    slot = i;
                }
            }

            _stopEnds[slot] = now + duration;
            _stopScales[slot] = timeScale;
            ApplyHitStops();
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
