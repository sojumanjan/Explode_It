using System.Collections.Generic;
using UnityEngine;

namespace ExplodeIt.Bombs
{
    // 폭탄이 막는 영역을 일일이 찾지 않도록 영역 쪽이 켜질 때 스스로 등록한다.
    // 영역은 보스 하나뿐이라 목록이 매우 짧아, 폭탄마다 매 프레임 훑어도 부담이 없다.
    public static class BombBarriers
    {
        private static readonly List<IBombBarrier> Barriers = new List<IBombBarrier>(4);

        public static void Register(IBombBarrier barrier)
        {
            if (!Barriers.Contains(barrier))
            {
                Barriers.Add(barrier);
            }
        }

        public static void Unregister(IBombBarrier barrier)
        {
            Barriers.Remove(barrier);
        }

        public static bool TryGetBlocking(Vector2 point, out IBombBarrier barrier)
        {
            for (int i = 0; i < Barriers.Count; i++)
            {
                if (Barriers[i].Blocks(point))
                {
                    barrier = Barriers[i];
                    return true;
                }
            }

            barrier = null;
            return false;
        }
    }
}
