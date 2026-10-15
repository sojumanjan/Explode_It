namespace ExplodeIt.Progression
{
    // 업적 달성 조건의 종류. 목표 수는 업적 데이터에 따로 둔다.
    public enum AchievementCondition
    {
        None,
        // 폭탄 하나에 딱 1마리씩 연속으로 처치. 빗맞힌 폭탄은 무시하고, 한 폭탄에 2마리 이상 잡으면 처음부터.
        SingleKillStreak,
        // 폭탄 하나로 동시에 처치한 수.
        KillsInOneBomb,
        // 한 판에 처치한 수 (블랙홀 포함).
        KillsInOneRun
    }
}
