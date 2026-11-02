using System;
using UnityEngine;

namespace ExplodeIt.Core
{
    // pixelfx 재생 프리팹 하나의 풀. 꺼내서 자리, 방향(오른쪽 기준), 크기를 맞춰 틀고, 끝나면 스스로 돌아온다.
    // 보스처럼 자기 연출을 직접 트는 쪽이 풀 만들기와 반환 콜백을 매번 따로 쓰지 않게 묶었다.
    public class PixelFxPool
    {
        private readonly ComponentPool<PixelFxPlayer> _pool;
        private readonly Action<PixelFxPlayer> _release;

        private PixelFxPool(PixelFxPlayer prefab, Transform parent, int prewarm)
        {
            _pool = new ComponentPool<PixelFxPlayer>(prefab, parent, prewarm, prewarm + 2);
            _pool.Prewarm(prewarm);
            _release = _pool.Release;
        }

        // 프리팹이 비어 있으면 null을 돌려준다. 쓰는 쪽은 ?.Play로 부른다.
        public static PixelFxPool Create(PixelFxPlayer prefab, Transform parent, int prewarm)
        {
            return prefab != null ? new PixelFxPool(prefab, parent, Mathf.Max(1, prewarm)) : null;
        }

        public void Play(Vector2 position, Vector2 facing, float scale)
        {
            PixelFxPlayer fx = _pool.Get(position);
            fx.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg);
            fx.Play(scale, _release);
        }
    }
}
