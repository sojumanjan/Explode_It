using UnityEngine;
using UnityEngine.Pool;

namespace ExplodeIt.Core
{
    // 전투 중 Instantiate/Destroy를 막기 위한 ObjectPool 래퍼.
    public class ComponentPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly ObjectPool<T> _pool;

        public int CountActive => _pool.CountActive;

        public ComponentPool(T prefab, Transform parent, int defaultCapacity, int maxSize)
        {
            _prefab = prefab;
            _parent = parent;
            // 이중 반환 검사는 비용이 있으므로 에디터에서만 켠다.
            _pool = new ObjectPool<T>(Create, null, OnRelease, OnDestroyItem,
                Application.isEditor, defaultCapacity, maxSize);
        }

        // 웹 빌드에서 첫 웨이브 도중 생성 스파이크가 나지 않도록 로딩 시점에 미리 채운다.
        public void Prewarm(int count)
        {
            var items = new T[count];
            for (int i = 0; i < count; i++)
            {
                items[i] = _pool.Get();
            }

            for (int i = 0; i < count; i++)
            {
                _pool.Release(items[i]);
            }
        }

        // 활성화 전에 위치를 잡아야 OnEnable이 이전 위치에서 실행되지 않는다.
        public T Get(Vector3 position)
        {
            T item = _pool.Get();
            item.transform.position = position;
            item.gameObject.SetActive(true);
            return item;
        }

        public void Release(T item)
        {
            _pool.Release(item);
        }

        private T Create()
        {
            T item = Object.Instantiate(_prefab, _parent);
            item.gameObject.SetActive(false);
            return item;
        }

        private static void OnRelease(T item)
        {
            item.gameObject.SetActive(false);
        }

        private static void OnDestroyItem(T item)
        {
            Object.Destroy(item.gameObject);
        }
    }
}
