using System.Collections.Generic;
using ExplodeIt.Core;
using ExplodeIt.Enemies;
using ExplodeIt.Stage;
using UnityEngine;

namespace ExplodeIt.Progression
{
    // 저주 "광폭"을 게임 씬에서 돌린다. 적이 나올 때마다 확률로 광폭으로 만들어 몸을 붉게 칠하고, 발밑에 잔상을 붙여 따라다니게 한다.
    // 적 프리팹을 고치지 않도록 잔상은 따로 풀에서 꺼내 위치만 맞춘다.
    public class BerserkCurse : CurseBehaviour<BerserkCurseData>
    {
        private struct Runner
        {
            public Enemy Enemy;
            public TrailRenderer Trail;
        }

        [SerializeField] private EnemySpawner _spawner;
        [Tooltip("발밑 잔상 프리팹 (TrailRenderer)")]
        [SerializeField] private TrailRenderer _trailPrefab;
        [SerializeField, Min(1)] private int _prewarm = 12;
        [SerializeField, Min(1)] private int _maxTrails = 64;

        private readonly List<Runner> _runners = new List<Runner>(32);
        private ComponentPool<TrailRenderer> _pool;

        protected override void Awake()
        {
            base.Awake();
            if (!enabled)
            {
                return;
            }

            _pool = new ComponentPool<TrailRenderer>(_trailPrefab, transform, _prewarm, _maxTrails);
            _pool.Prewarm(_prewarm);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _spawner.Spawned += OnSpawned;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _spawner.Spawned -= OnSpawned;
        }

        private void OnSpawned(Enemy enemy)
        {
            if (!enemy.CountsAsKill || !Data.CanAffect(enemy.EnemyData) || Random.value >= Data.Chance)
            {
                return;
            }

            enemy.SpeedOverride = Data.MoveSpeed;
            float[] telegraphs = Data.GetTelegraphs(enemy.EnemyData);
            if (telegraphs != null)
            {
                enemy.SetAttackOverride(telegraphs, Data.RecoverDuration);
            }

            // 나올 때 한 번만 찾는다. 몸 색은 그림 쪽이 매 프레임 유지 색으로 칠한다.
            EnemyVisual visual = enemy.GetComponentInChildren<EnemyVisual>();
            if (visual != null)
            {
                visual.Tint = Data.Tint;
            }

            TrailRenderer trail = _pool.Get(FeetOf(enemy));
            trail.Clear();
            _runners.Add(new Runner { Enemy = enemy, Trail = trail });
            AudioManager.Play(Data.SpawnSound);
        }

        // 적이 움직인 뒤 잔상을 옮겨야 한 프레임 늦게 따라오지 않는다.
        private void LateUpdate()
        {
            for (int i = _runners.Count - 1; i >= 0; i--)
            {
                Runner runner = _runners[i];
                Enemy enemy = runner.Enemy;
                if (!enemy.isActiveAndEnabled || enemy.CurrentState == EnemyState.Dead)
                {
                    runner.Trail.Clear();
                    _pool.Release(runner.Trail);
                    _runners.RemoveAt(i);
                    continue;
                }

                runner.Trail.transform.position = FeetOf(enemy);
            }
        }

        private Vector3 FeetOf(Enemy enemy)
        {
            return (Vector2)enemy.transform.position + Data.TrailOffset;
        }
    }
}
