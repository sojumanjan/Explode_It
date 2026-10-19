using ExplodeIt.Enemies;

namespace ExplodeIt.Stage
{
    // 무한 모드 생성기가 낸 군집 하나: 몇 초에, 어느 구역에서, 어떤 적이 몇 마리.
    // 한 마리씩 나오는 시간 배치는 스토리 웨이브와 같은 방식으로 스테이지 진행이 펼친다.
    public readonly struct EndlessGroupOrder
    {
        public readonly float Time;
        public readonly SpawnArea Area;
        public readonly Enemy Prefab;
        public readonly int Count;

        public EndlessGroupOrder(float time, SpawnArea area, Enemy prefab, int count)
        {
            Time = time;
            Area = area;
            Prefab = prefab;
            Count = count;
        }
    }
}
