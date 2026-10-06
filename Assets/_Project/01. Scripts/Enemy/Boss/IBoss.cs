namespace ExplodeIt.Enemies
{
    // 진행 흐름이 보스를 소환한 직후 몇 번째 보스인지, 몇 바퀴째 난이도인지 알려준다.
    public interface IBoss
    {
        // bossNumber: 이번 판에서 몇 번째 보스전인지(1부터). tier: 바퀴(0부터). 같은 보스가 바퀴마다 강해진다.
        void BeginBoss(int bossNumber, int tier);
    }
}
