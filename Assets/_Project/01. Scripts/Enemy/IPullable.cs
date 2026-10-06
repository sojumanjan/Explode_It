using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 블랙홀 같은 외부 힘에 끌려가는 대상. 적뿐 아니라 보스의 혼불처럼 적이 아닌 것도 끌려갈 수 있게 한다.
    public interface IPullable
    {
        // center 쪽으로 이번 물리 스텝에 최대 step만큼 끌려간다.
        void PullToward(Vector2 center, float step);
    }
}
