using System;
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 적 근접 공격과 방패 막힘에 pixelfx 시트를 튼다. 적 코드는 이벤트만 내고 연출을 모른다.
    // 베기는 공격 반경에 맞춰 키우고 정면으로 돌린다. 막힘은 방패가 향한 쪽(폭발이 온 쪽)으로 불꽃이 튀게 돌린다.
    public class EnemyPixelFx : MonoBehaviour
    {
        [Header("베기")]
        [Tooltip("근접 공격 순간 틀 재생 프리팹 (PxFx_). 비워 두면 틀지 않는다")]
        [SerializeField] private PixelFxPlayer _slashPrefab;
        [Tooltip("배율 1일 때 베기 궤적이 닿는 반경 (유닛). topdown-slash는 34px ÷ PPU 20 = 1.7")]
        [SerializeField, Min(0.01f)] private float _slashRadius = 1.7f;
        // 각도는 크기처럼 늘릴 수 없어 시트에 구워져 있다(topdown-slash sweep 100도 = 전사, 사마귀 공격 각도). 각도가 다른 적이 생기면 시트를 따로 뽑는다.
        [Tooltip("공격 반경에 맞춘 크기에 한 번 더 곱하는 배율. 1이면 궤적 바깥 끝이 공격 반경에 딱 닿는다")]
        [SerializeField, Min(0.1f)] private float _slashScale = 1f;

        [Header("방패 막힘")]
        [Tooltip("방패에 폭발이 막혔을 때 틀 재생 프리팹 (PxFx_). 비워 두면 틀지 않는다")]
        [SerializeField] private PixelFxPlayer _blockPrefab;
        [Tooltip("막힘 이펙트 크기 배율")]
        [SerializeField, Min(0.1f)] private float _blockScale = 1f;

        [Header("풀")]
        [Tooltip("종류마다 미리 만들어 둘 수 (개)")]
        [SerializeField, Min(1)] private int _prewarm = 6;
        [Tooltip("종류마다 풀 최대 크기 (개)")]
        [SerializeField, Min(1)] private int _maxSize = 24;

        private ComponentPool<PixelFxPlayer> _slashPool;
        private ComponentPool<PixelFxPlayer> _blockPool;
        private Action<PixelFxPlayer> _releaseSlash;
        private Action<PixelFxPlayer> _releaseBlock;

        private void Awake()
        {
            if (_slashPrefab != null)
            {
                _slashPool = new ComponentPool<PixelFxPlayer>(_slashPrefab, transform, _prewarm, _maxSize);
                _slashPool.Prewarm(_prewarm);
                _releaseSlash = _slashPool.Release;
            }

            if (_blockPrefab != null)
            {
                _blockPool = new ComponentPool<PixelFxPlayer>(_blockPrefab, transform, _prewarm, _maxSize);
                _blockPool.Prewarm(_prewarm);
                _releaseBlock = _blockPool.Release;
            }
        }

        private void OnEnable()
        {
            GameEvents.EnemySwung += OnEnemySwung;
            GameEvents.EnemyHitBlocked += OnEnemyHitBlocked;
        }

        private void OnDisable()
        {
            GameEvents.EnemySwung -= OnEnemySwung;
            GameEvents.EnemyHitBlocked -= OnEnemyHitBlocked;
        }

        private void OnEnemySwung(Vector2 center, Vector2 direction, float radius)
        {
            if (_slashPool == null)
            {
                return;
            }

            PixelFxPlayer fx = _slashPool.Get(center);
            fx.transform.rotation = Facing(direction);
            fx.Play(radius / _slashRadius * _slashScale, _releaseSlash);
        }

        private void OnEnemyHitBlocked(Vector2 position, Vector2 facing)
        {
            if (_blockPool == null)
            {
                return;
            }

            PixelFxPlayer fx = _blockPool.Get(position);
            fx.transform.rotation = Facing(facing);
            fx.Play(_blockScale, _releaseBlock);
        }

        // pixelfx 방향 이펙트는 오른쪽을 본다.
        private static Quaternion Facing(Vector2 direction)
        {
            return Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }
    }
}
