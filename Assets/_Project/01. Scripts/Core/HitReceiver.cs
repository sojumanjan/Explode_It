using System;
using UnityEngine;

namespace ExplodeIt.Core
{
    // 피격 → 판정 → 사망을 단계로 나눈다.
    // 실드(플레이어)나 여러 번 맞아야 죽는 적(골렘)은 판정 단계만 달라진다.
    public class HitReceiver : MonoBehaviour, IHittable
    {
        private int _remainingHits = 1;
        private bool _isDead;

        // 피격 연출용. 죽지 않는 피격에도 발행된다.
        public event Action<HitInfo> Hit;
        public event Action<HitInfo> Died;

        public bool IsDead => _isDead;

        // 풀에서 재사용될 때마다 소유자가 호출한다.
        public void ResetHits(int hitsToDie)
        {
            _remainingHits = Mathf.Max(1, hitsToDie);
            _isDead = false;
        }

        public void ReceiveHit(in HitInfo hit)
        {
            if (_isDead)
            {
                return;
            }

            Hit?.Invoke(hit);

            if (!Judge(hit))
            {
                return;
            }

            _isDead = true;
            Died?.Invoke(hit);
        }

        // 이 피격으로 죽는지 결정한다. 실드는 여기서 피격을 흡수하게 된다.
        private bool Judge(in HitInfo hit)
        {
            _remainingHits--;
            return _remainingHits <= 0;
        }
    }
}
