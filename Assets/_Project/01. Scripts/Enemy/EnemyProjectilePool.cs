using System;
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 궁수마다 풀을 두면 적 수만큼 풀이 늘어나므로 씬에 하나만 두고 공유한다.
    // 적 프리팹은 씬 오브젝트를 참조할 수 없어서 정적 접근을 연다.
    public class EnemyProjectilePool : MonoBehaviour
    {
        [SerializeField] private EnemyProjectile _prefab;
        [SerializeField, Min(1)] private int _prewarm = 32;
        [SerializeField, Min(1)] private int _maxSize = 256;

        private ComponentPool<EnemyProjectile> _pool;
        private Action<EnemyProjectile> _release;

        public static EnemyProjectilePool Current { get; private set; }

        private void Awake()
        {
            _release = Release;
            _pool = new ComponentPool<EnemyProjectile>(_prefab, transform, _prewarm, _maxSize);
            _pool.Prewarm(_prewarm);
        }

        private void OnEnable()
        {
            Current = this;
        }

        private void OnDisable()
        {
            if (Current == this)
            {
                Current = null;
            }
        }

        public void Fire(Vector2 origin, Vector2 direction, float speed, float range, float hitRadius)
        {
            EnemyProjectile projectile = _pool.Get(origin);
            projectile.Launch(direction, speed, range, hitRadius, _release);
        }

        private void Release(EnemyProjectile projectile)
        {
            _pool.Release(projectile);
        }
    }
}
