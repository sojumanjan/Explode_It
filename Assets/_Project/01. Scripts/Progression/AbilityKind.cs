namespace ExplodeIt.Progression
{
    // 스킬 종류. 종류마다 발동 방식이 다르고, 스킬 데이터에서 그 종류의 칸만 쓴다.
    public enum AbilityKind
    {
        // 착지 후 범위 안의 적을 빨아들였다가 터진다.
        BlackHole,
        // 사방으로 폭탄을 뿌린다. 벽을 무시하고 방패를 뚫는다.
        ScatterBomb,
        // 한동안 던진 폭탄이 땅에 닿자마자 터지고 공격 속도가 빨라진다.
        InstantBomb
    }
}
