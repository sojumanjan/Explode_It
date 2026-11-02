using System;
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Bombs
{
    // 폭탄이 터질 때 pixelfx 폭발 시트를 그 자리에 튼다. 폭발 반경에 맞춰 키워서 무기마다 범위가 달라도 링이 범위 끝에 닿는다.
    // 일반, 다발, 순간폭탄이 내는 BombExploded만 듣는다. 블랙홀은 자기 연출이 따로 있어서 뺀다.
    public class BombPixelFx : MonoBehaviour
    {
        [Tooltip("폭발 때 틀 pixelfx 재생 프리팹 (PxFx_)")]
        [SerializeField] private PixelFxPlayer _prefab;
        [Tooltip("배율 1일 때 이펙트 링이 닿는 반경 (유닛). 시트의 이펙트 반경 픽셀 ÷ PPU. bomb-pop-a는 38px ÷ 20 = 1.9")]
        [SerializeField, Min(0.01f)] private float _effectRadius = 1.9f;
        [Tooltip("반경에 맞춘 크기에 한 번 더 곱하는 배율. 1이면 링이 폭발 범위 끝에 딱 닿는다")]
        [SerializeField, Min(0.1f)] private float _scaleMultiplier = 1f;
        [Tooltip("미리 만들어 둘 수 (개). 동시에 터지는 폭탄 수보다 넉넉하게")]
        [SerializeField, Min(1)] private int _prewarm = 8;
        [Tooltip("풀 최대 크기 (개)")]
        [SerializeField, Min(1)] private int _maxSize = 32;

        private ComponentPool<PixelFxPlayer> _pool;
        private Action<PixelFxPlayer> _release;

        private void Awake()
        {
            _pool = new ComponentPool<PixelFxPlayer>(_prefab, transform, _prewarm, _maxSize);
            _pool.Prewarm(_prewarm);
            _release = _pool.Release;
        }

        private void OnEnable()
        {
            GameEvents.BombExploded += OnBombExploded;
        }

        private void OnDisable()
        {
            GameEvents.BombExploded -= OnBombExploded;
        }

        private void OnBombExploded(Vector2 position, float radius, int hitCount)
        {
            PixelFxPlayer fx = _pool.Get(position);
            fx.Play(radius / _effectRadius * _scaleMultiplier, _release);
        }
    }
}
