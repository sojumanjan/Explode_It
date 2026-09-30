using System;
using System.Collections.Generic;
using ExplodeIt.Core;
using ExplodeIt.Enemies;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 스폰 실행만 맡는다. 무엇을 언제 낼지는 요청하는 쪽(웨이브, 과열, 분열 등)이 정한다.
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField, Min(0)] private int _prewarmPerEnemy = 32;
        [SerializeField, Min(1)] private int _maxPerEnemy = 256;

        private readonly Dictionary<Enemy, ComponentPool<Enemy>> _pools = new Dictionary<Enemy, ComponentPool<Enemy>>();
        // 적이 죽을 때 자기 풀로 돌아가도록 풀마다 반환 델리게이트를 한 번만 만들어 둔다.
        private readonly Dictionary<Enemy, Action<Enemy>> _releases = new Dictionary<Enemy, Action<Enemy>>();

        // 첫 스폰 순간에 생성 스파이크가 나지 않도록 전투 시작 전에 호출한다.
        public void Prepare(IReadOnlyList<Enemy> prefabs)
        {
            for (int i = 0; i < prefabs.Count; i++)
            {
                CreatePool(prefabs[i]);
            }
        }

        public Enemy Spawn(Enemy prefab, Vector2 position)
        {
            if (!_pools.TryGetValue(prefab, out ComponentPool<Enemy> pool))
            {
                pool = CreatePool(prefab);
            }

            Enemy enemy = pool.Get(position);
            enemy.Initialize(_target, _releases[prefab]);
            return enemy;
        }

        private ComponentPool<Enemy> CreatePool(Enemy prefab)
        {
            if (_pools.TryGetValue(prefab, out ComponentPool<Enemy> existing))
            {
                return existing;
            }

            var pool = new ComponentPool<Enemy>(prefab, transform, _prewarmPerEnemy, _maxPerEnemy);
            pool.Prewarm(_prewarmPerEnemy);
            _pools.Add(prefab, pool);
            _releases.Add(prefab, pool.Release);
            return pool;
        }
    }
}
