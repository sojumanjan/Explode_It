using ExplodeIt.Core;
using ExplodeIt.Enemies;
using UnityEngine;

namespace ExplodeIt.Progression
{
    // 저주가 맵에 내는 원형 공격. 폭탄과 같은 규칙(대상 중심 한 점이 원 안이면 맞음)이라 결과를 똑같이 읽을 수 있다.
    // 구조물에는 막히지 않는다. 하늘에서 떨어지거나 땅에서 터지는 공격이라 원 그대로가 범위다.
    public static class CurseHits
    {
        // 낙뢰처럼 넓은 범위에 적이 많이 들어와도 놓치지 않을 만큼 넉넉히 둔다.
        private static readonly Collider2D[] Buffer = new Collider2D[128];

        public static void Circle(Vector2 center, float radius, LayerMask mask, IKillerProfile killer)
        {
            var filter = new ContactFilter2D();
            filter.SetLayerMask(mask);
            // 적 콜라이더는 트리거다.
            filter.useTriggers = true;
            int count = Physics2D.OverlapCircle(center, radius, filter, Buffer);
            float sqrRadius = radius * radius;
            for (int i = 0; i < count; i++)
            {
                Collider2D target = Buffer[i];
                if (((Vector2)target.bounds.center - center).sqrMagnitude > sqrRadius)
                {
                    continue;
                }

                // 보스는 저주에 맞지 않는다. 저주는 플레이어를 괴롭히는 난이도라, 찐엔딩 왕 결전 등에서 보스를 대신 깎아 주면 의도와 어긋난다(11/8 사용자).
                // 실드를 푸는 보스 기믹(화염 마법사 요정, 구슬 보스 구슬)도 저주가 대신 깨 주면 같은 문제라 함께 뺀다.
                // 콜라이더가 본체의 자식에 있을 수 있어 부모까지 본다.
                if (target.GetComponentInParent<IBoss>() != null || target.GetComponentInParent<FireFairy>() != null
                    || target.GetComponentInParent<BossOrb>() != null)
                {
                    continue;
                }

                // 방패로 막을 수 있는 공격이 아니다.
                if (target.TryGetComponent(out IHittable hittable))
                {
                    hittable.ReceiveHit(new HitInfo(center, true, DamageSource.Curse, killer));
                }
            }
        }
    }
}
