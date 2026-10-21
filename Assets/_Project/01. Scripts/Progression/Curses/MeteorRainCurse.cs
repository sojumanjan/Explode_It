using System;
using ExplodeIt.Core;
using ExplodeIt.Enemies;
using UnityEngine;

namespace ExplodeIt.Progression
{
    // 저주 "운석"을 게임 씬에서 돌린다. 화염 마법사의 운석 그림을 그대로 쓰고(원이 차오르면 떨어진다), 적과 플레이어 모두 친다.
    // 적이 운석에 죽으면 처치 수에 들어가고, 유폭이 함께 걸려 있으면 그 자리가 또 터진다.
    public class MeteorRainCurse : CurseBehaviour<MeteorRainCurseData>
    {
        [SerializeField] private Meteor _meteorPrefab;
        [Tooltip("운석에 맞는 레이어 (플레이어 + 적)")]
        [SerializeField] private LayerMask _hitMask;
        [SerializeField, Min(1)] private int _prewarm = 4;
        [SerializeField, Min(1)] private int _maxMeteors = 16;

        // 운석이 떨어진 자리와 반경. 덫처럼 운석에 부서지는 것이 듣는다.
        public static event Action<Vector2, float> Impacted;

        private ComponentPool<Meteor> _pool;
        private Action<Meteor> _onImpact;
        private float _nextTime = float.PositiveInfinity;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            Impacted = null;
        }

        protected override void Awake()
        {
            base.Awake();
            if (!enabled)
            {
                return;
            }

            _pool = new ComponentPool<Meteor>(_meteorPrefab, transform, _prewarm, _maxMeteors);
            _pool.Prewarm(_prewarm);
            _onImpact = OnImpact;
        }

        protected override void OnRunStarted()
        {
            FeedbackPlayer.Prepare(Data.ImpactFeedback);
            _nextTime = Time.time + Data.FirstDelay;
        }

        private void Update()
        {
            // 연출로 멈춘 동안 미뤄진 운석이 풀리자마자 몰려 떨어지지 않게, 멈춘 동안은 다음 시각을 뒤로 민다.
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
            for (int i = 0; i < Data.Count; i++)
            {
                Drop();
            }
        }

        private void Drop()
        {
            Vector2 point = PointAround(PlayerPosition, Data.MinDistance, Data.MaxDistance);
            Meteor meteor = _pool.Get(point);
            meteor.UsesCurseClock = true;
            meteor.Launch(point, Data.FallTime, Data.Radius, _onImpact);
        }

        private void OnImpact(Meteor meteor)
        {
            CurseHits.Circle(meteor.Position, meteor.Radius, _hitMask, Data);
            Impacted?.Invoke(meteor.Position, meteor.Radius);
            FeedbackPlayer.Play(Data.ImpactFeedback, meteor.Position);
            AudioManager.Play(Data.ImpactSound);
            _pool.Release(meteor);
        }
    }
}
