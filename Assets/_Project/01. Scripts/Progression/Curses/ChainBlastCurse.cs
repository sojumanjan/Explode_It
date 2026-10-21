using System;
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Progression
{
    // 저주 "유폭"을 게임 씬에서 돌린다. 처치 수에 들어가는 적이 죽을 때마다 그 자리에 원을 깔고, 다 차면 터뜨린다.
    // 폭발은 플레이어만 친다. 적끼리 연쇄로 터지면 한 번에 웨이브가 지워진다.
    public class ChainBlastCurse : CurseBehaviour<ChainBlastCurseData>
    {
        [SerializeField] private CurseCircle _circlePrefab;
        [Tooltip("폭발에 맞는 레이어 (플레이어)")]
        [SerializeField] private LayerMask _hitMask;
        // 블랙홀로 한꺼번에 수십 마리가 죽어도 새로 만들지 않도록 넉넉히 준비해 둔다.
        [SerializeField, Min(1)] private int _prewarm = 48;
        [SerializeField, Min(1)] private int _maxCircles = 160;

        private ComponentPool<CurseCircle> _pool;
        private Action<CurseCircle> _onExplode;

        protected override void Awake()
        {
            base.Awake();
            if (!enabled)
            {
                return;
            }

            _pool = new ComponentPool<CurseCircle>(_circlePrefab, transform, _prewarm, _maxCircles);
            _pool.Prewarm(_prewarm);
            _onExplode = Explode;
        }

        // 연출 재생기는 자기 OnEnable에서 준비되므로 판이 시작될 때 이펙트 풀을 채운다.
        protected override void OnRunStarted()
        {
            FeedbackPlayer.Prepare(Data.ExplodeFeedback);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            GameEvents.EnemyKilled += OnEnemyKilled;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            GameEvents.EnemyKilled -= OnEnemyKilled;
        }

        // 판이 끝난 뒤(사망·클리어)나 연출 중에 죽은 적은 터뜨리지 않는다.
        private void OnEnemyKilled(Vector2 position)
        {
            if (!IsRunning)
            {
                return;
            }

            CurseCircle circle = _pool.Get(position);
            circle.Show(position, Data.Radius, Data.RangeColor, Data.FillColor);
            circle.Countdown(Data.Delay, true, 0f, _onExplode);
        }

        private void Explode(CurseCircle circle)
        {
            CurseHits.Circle(circle.Position, circle.Radius, _hitMask, Data);
            FeedbackPlayer.Play(Data.ExplodeFeedback, circle.Position);
            AudioManager.Play(Data.ExplodeSound);
            _pool.Release(circle);
        }
    }
}
