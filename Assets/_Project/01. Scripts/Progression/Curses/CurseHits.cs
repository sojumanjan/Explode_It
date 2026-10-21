using ExplodeIt.Core;
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

                // 방패로 막을 수 있는 공격이 아니다.
                if (target.TryGetComponent(out IHittable hittable))
                {
                    hittable.ReceiveHit(new HitInfo(center, true, DamageSource.Curse, killer));
                }
            }
        }
    }
}
