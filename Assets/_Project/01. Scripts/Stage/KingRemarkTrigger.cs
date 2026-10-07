namespace ExplodeIt.Stage
{
    // 우측 하단 왕 컷인이 나오는 상황.
    public enum KingRemarkTrigger
    {
        // 값: 웨이브 번호(1부터). 0이면 아무 웨이브.
        WaveStart,
        // 값: 한 폭발에 이 수 이상 처치. 조건을 넘는 대사 중 값이 가장 큰 것이 나온다.
        MultiKill,
        // 값: 이번 판 누적 처치 수가 정확히 이 수가 되는 순간.
        KillCount,
        // 값: 몇 번째 보스(1부터). 0이면 아무 보스.
        BossDefeated,
        // 값: 쓰지 않음.
        PlayerDied
    }
}
