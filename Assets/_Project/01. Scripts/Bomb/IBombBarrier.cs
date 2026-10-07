using UnityEngine;

namespace ExplodeIt.Bombs
{
    // 날아가는 폭탄을 막아 그 자리에서 태워 없애는 영역(화염 마법사의 화염 영역 등).
    public interface IBombBarrier
    {
        // point(바닥 기준 위치)가 막힌 곳이면 true.
        bool Blocks(Vector2 point);

        // 폭탄이 막혀 타 버리기 시작할 때 부른다. 소리와 불꽃은 영역마다 다르므로 영역 쪽이 낸다.
        void OnBombBurned(Vector2 position);
    }
}
