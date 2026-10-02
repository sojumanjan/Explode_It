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
        // 살아 있는 적. 화면 내 적 수 측정과 개발자 패널의 전멸에 쓴다.
        private readonly List<Enemy> _active = new List<Enemy>(256);
        private Action<Enemy> _removeActive;

        public int ActiveCount => _active.Count;

        private void Awake()
        {
            _removeActive = RemoveActive;
        }

        // 죽으면 목록에서 빠지므로 뒤에서부터 지운다.
        public void KillAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (i < _active.Count && _active[i].TryGetComponent(out HitReceiver hitReceiver))
                {
                    hitReceiver.Kill();
                }
            }
        }

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
            enemy.Initialize(_target, _removeActive, _releases[prefab]);
            _active.Add(enemy);
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

        // 죽는 순간 목록에서 뺀다. 사망 연출 중인 몸은 웨이브 클리어와 전멸 대상에 들어가지 않는다.
        private void RemoveActive(Enemy enemy)
        {
            _active.Remove(enemy);
        }
    }
}
