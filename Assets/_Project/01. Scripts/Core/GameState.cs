namespace ExplodeIt.Core
{
    public enum GameState
    {
        None,
        Playing,
        // 입력·스폰은 Playing일 때만 동작하므로, 이 상태로 바꾸는 것만으로 전투가 멈춘다.
        Paused,
        PlayerDead,
        StageClear
    }
}
