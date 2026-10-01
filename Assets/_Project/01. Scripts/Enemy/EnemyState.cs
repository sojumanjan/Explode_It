namespace ExplodeIt.Enemies
{
    public enum EnemyState
    {
        Move,
        Telegraph,
        Attack,
        Recover,
        // 블랙홀 같은 외부 힘에 끌려가는 중. 예고·공격은 끊기고, 끝나면 이동부터 다시 시작한다.
        Pulled,
        Dead
    }
}
