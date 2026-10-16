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
        KillsInOneRun,
        // 지금까지 모든 판에서 처치한 수 (이번 판 포함).
        TotalKills,
        // 업적에 정한 스킬을 한 번 발동하는 동안 처치한 수. 블랙홀·다발폭탄은 그 스킬로 잡은 적만, 순간폭탄은 발동 동안 잡은 모든 적.
        AbilityKillsInOneUse,
        // 업적에 정한 스킬을 발동하는 동안 한 마리도 잡지 않고 끝내기.
        AbilityUseWithoutKills,
        // 업적에 정한 스킬로 보스의 마지막 한 방을 넣기.
        BossKilledByAbility,
        // 업적에 정한 스킬로 지금까지 처치한 수 (모든 판 누적).
        AbilityKillsTotal
    }
}
