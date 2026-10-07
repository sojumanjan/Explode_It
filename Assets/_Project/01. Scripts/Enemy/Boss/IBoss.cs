namespace ExplodeIt.Enemies
{
    // 진행 흐름이 보스를 소환한 직후 몇 번째 보스인지, 몇 바퀴째 난이도인지 알려준다.
    public interface IBoss
    {
        // bossNumber: 이번 판에서 몇 번째 보스전인지(1부터). tier: 바퀴(0부터). 같은 보스가 바퀴마다 강해진다.
        void BeginBoss(int bossNumber, int tier);

        // 내려앉은 뒤 잠깐 멈춘 다음 호출된다. 플레이어가 아직 묶여 있는 동안 보여줄 등장 동작을 시작한다.
        void PlayEntrance();

        // 등장 동작이 끝났는지. 참이 되는 순간 플레이어 조작이 돌아오고 카메라가 복귀한다.
        bool IsEntranceDone { get; }

        // 등장 연출이 끝나 카메라가 플레이어에게 돌아온 뒤 호출된다. 이때부터 움직이고 공격한다.
        void StartFight();
    }
}
