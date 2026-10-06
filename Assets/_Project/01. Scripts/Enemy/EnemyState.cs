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
        // 기믹으로 무너져 아무것도 못 하는 상태(보스 그로기 등). 정해진 시간이 지나면 이동부터 다시 시작한다.
        Stunned,
        Dead
    }
}
