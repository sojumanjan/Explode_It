using System;
using UnityEngine;

namespace ExplodeIt.Core
{
    // 풀에서 꺼내 쓰는 파티클 이펙트. 파티클이 다 사라지면 스스로 풀로 돌아간다.
    [RequireComponent(typeof(ParticleSystem))]
    public class PooledEffect : MonoBehaviour
    {
        private ParticleSystem _particles;
        private Action<PooledEffect> _release;
        private Vector3 _baseScale;

        private void Awake()
        {
            _particles = GetComponent<ParticleSystem>();
            _baseScale = transform.localScale;

            // 끝나는 시점을 직접 세지 않고, 파티클이 멈출 때 오는 콜백으로 반환한다.
            ParticleSystem.MainModule main = _particles.main;
            main.stopAction = ParticleSystemStopAction.Callback;
        }

        public void Play(float scale, Action<PooledEffect> release)
        {
            _release = release;
            transform.localScale = _baseScale * scale;
            _particles.Clear(true);
            _particles.Play(true);
        }

        private void OnParticleSystemStopped()
        {
            _release?.Invoke(this);
        }
    }
}
