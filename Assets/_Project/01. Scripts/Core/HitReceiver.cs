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
        // 무적·실드에 막힌 피격. "맞았지만 안 먹혔다"는 반응(튕김 연출)을 보여줄 때 쓴다.
        public event Action<HitInfo> Blocked;

        public bool IsDead => _isDead;
        private float _invulnerableUntil;

        // 개발자 패널의 무적 치트. 구르기 무적과 따로 두어, 구르기가 끝날 때 치트까지 풀리지 않게 한다.
        public bool CheatInvulnerable { get; set; }

        // 기믹을 풀기 전까지 켜져 있는 실드(보스 등). 시간제 무적과 따로 두어 서로 덮어쓰지 않게 한다.
        public bool IsShielded { get; set; }

        // 판정 단계에서 피격을 무시한다.
        public bool IsInvulnerable => CheatInvulnerable || IsShielded || Time.time < _invulnerableUntil;

        // 여러 곳에서 겹쳐 줘도 가장 늦게 끝나는 쪽을 따른다.
        public void GrantInvulnerability(float duration)
        {
            _invulnerableUntil = Mathf.Max(_invulnerableUntil, Time.time + duration);
        }

        // 풀에서 재사용될 때마다 소유자가 호출한다.
        public void ResetHits(int hitsToDie)
        {
            _remainingHits = Mathf.Max(1, hitsToDie);
            _isDead = false;
            IsShielded = false;
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

        // 판정을 건너뛰고 바로 죽인다. 개발자 패널의 적 전멸처럼 여러 번 맞아야 하는 적도 한 번에 지울 때 쓴다.
        public void Kill()
        {
            if (_isDead)
            {
                return;
            }

            _isDead = true;
            Died?.Invoke(new HitInfo(transform.position));
        }

        // 이 피격으로 죽는지 결정한다. 실드는 여기서 피격을 흡수하게 된다.
        private bool Judge(in HitInfo hit)
        {
            if (IsInvulnerable)
            {
                Blocked?.Invoke(hit);
                return false;
            }

            _remainingHits--;
            return _remainingHits <= 0;
        }
    }
}
