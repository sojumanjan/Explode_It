using System;
using System.Collections.Generic;
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Progression
{
    // 저주 "낙뢰"를 게임 씬에서 돌린다. 주기마다 플레이어 근처를 중심으로 넓은 원을 깔고, 정해진 시간 뒤 내리친다.
    // 원은 차오르지 않아 남은 시간을 알 수 없고, 치기 직전에만 번쩍인다. 적과 플레이어 모두 친다.
    // 치는 순간 원 안 여기저기에 번개 줄기 그림이 내리꽂힌다(판정은 원 전체라 그림은 연출일 뿐이다).
    public class LightningCurse : CurseBehaviour<LightningCurseData>
    {
        private struct Bolt
        {
            public SpriteRenderer Renderer;
            public float Age;
            // 줄기마다 같은 프레임을 동시에 넘기면 복사한 티가 나므로 어긋낸다.
            public int FrameOffset;
        }

        [SerializeField] private CurseCircle _circlePrefab;
        [Tooltip("번개 줄기 그림 프리팹 (SpriteRenderer)")]
        [SerializeField] private SpriteRenderer _boltPrefab;
        [Tooltip("번개에 맞는 레이어 (플레이어 + 적)")]
        [SerializeField] private LayerMask _hitMask;

        // 번개 줄기가 흐려지기 시작하는 지점 (보이는 시간 대비 비율). 그림 전용 연출이라 데이터로 빼지 않는다.
        private const float BoltFadeStart = 0.6f;

        private readonly List<Bolt> _bolts = new List<Bolt>(16);
        private ComponentPool<CurseCircle> _pool;
        private ComponentPool<SpriteRenderer> _boltPool;
        private Action<CurseCircle> _onStrike;
        private float _nextTime = float.PositiveInfinity;

        protected override void Awake()
        {
            base.Awake();
            if (!enabled)
            {
                return;
            }

            // 예고가 끝나기 전에 다음 예고가 겹칠 수 있도록(간격을 예고보다 짧게 둘 때) 둘을 둔다.
            _pool = new ComponentPool<CurseCircle>(_circlePrefab, transform, 2, 4);
            _pool.Prewarm(2);
            int bolts = Mathf.Max(1, Data.BoltCount);
            _boltPool = new ComponentPool<SpriteRenderer>(_boltPrefab, transform, bolts, bolts * 3);
            _boltPool.Prewarm(bolts);
            _onStrike = Strike;
        }

        protected override void OnRunStarted()
        {
            FeedbackPlayer.Prepare(Data.StrikeFeedback);
            _nextTime = Time.time + Data.FirstDelay;
        }

        private void Update()
        {
            TickBolts(RunCurses.DeltaTime);
            if (!IsRunning)
            {
                _nextTime += Time.deltaTime;
                return;
            }

            if (Time.time < _nextTime)
            {
                return;
            }

            _nextTime = Time.time + Data.Interval;
            Vector2 center = PointAround(PlayerPosition, Data.MinDistance, Data.MaxDistance);
            CurseCircle circle = _pool.Get(center);
            circle.Show(center, Data.Radius, Data.RangeColor, Data.FlashColor);
            circle.Countdown(Data.WarningTime, false, Data.FlashTime, _onStrike);
            circle.PlayAppear(Data.AppearTime, Data.AppearEase);
            AudioManager.Play(Data.WarnSound);
        }

        private void Strike(CurseCircle circle)
        {
            CurseHits.Circle(circle.Position, circle.Radius, _hitMask, Data);
            FeedbackPlayer.Play(Data.StrikeFeedback, circle.Position);
            AudioManager.Play(Data.StrikeSound);
            DropBolts(circle.Position, circle.Radius);
            _pool.Release(circle);
        }

        private void DropBolts(Vector2 center, float radius)
        {
            Sprite[] frames = Data.BoltFrames;
            if (frames.Length == 0)
            {
                return;
            }

            float spread = radius * Data.BoltSpread;
            for (int i = 0; i < Data.BoltCount; i++)
            {
                Vector2 point = i == 0 ? center : center + UnityEngine.Random.insideUnitCircle * spread;
                SpriteRenderer renderer = _boltPool.Get(point);
                int offset = UnityEngine.Random.Range(0, frames.Length);
                renderer.sprite = frames[offset];
                renderer.color = Color.white;
                // 그림 높이를 정한 높이에 맞춘다. 좌우를 무작위로 뒤집어 모양을 더 섞는다.
                float height = frames[0].bounds.size.y;
                float scale = height > 0.0001f ? Data.BoltHeight / height : 1f;
                renderer.transform.localScale = new Vector3(UnityEngine.Random.value < 0.5f ? -scale : scale, scale, 1f);
                _bolts.Add(new Bolt { Renderer = renderer, Age = 0f, FrameOffset = offset });
            }
        }

        private void TickBolts(float deltaTime)
        {
            if (_bolts.Count == 0)
            {
                return;
            }

            Sprite[] frames = Data.BoltFrames;
            float duration = Data.BoltDuration;
            for (int i = _bolts.Count - 1; i >= 0; i--)
            {
                Bolt bolt = _bolts[i];
                bolt.Age += deltaTime;
                if (bolt.Age >= duration)
                {
                    _boltPool.Release(bolt.Renderer);
                    _bolts.RemoveAt(i);
                    continue;
                }

                int frame = (int)(bolt.Age / Data.BoltFrameTime) + bolt.FrameOffset;
                bolt.Renderer.sprite = frames[frame % frames.Length];
                float fade = Mathf.Clamp01((bolt.Age / duration - BoltFadeStart) / (1f - BoltFadeStart));
                Color color = Color.white;
                color.a = 1f - fade;
                bolt.Renderer.color = color;
                _bolts[i] = bolt;
            }
        }
    }
}
