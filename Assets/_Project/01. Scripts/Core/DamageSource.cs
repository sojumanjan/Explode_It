namespace ExplodeIt.Core
{
    // 무엇에 맞았는지. 스킬 업적(블랙홀로 보스 막타 등)과 스킬 강화 조건이 처치 출처를 구분할 때 쓴다.
    public enum DamageSource
    {
        // 출처를 따지지 않는 피격(적의 공격, 개발자 패널 전멸 등).
        None,
        Bomb,
        BlackHole,
        ScatterBomb,
        InstantBomb,
        // 아티팩트(가죽 헬름)로 대시 중 부딪혀 처치.
        Dodge,
        // 승천 저주(운석·낙뢰 등)에 맞음. 처치 수에는 들어가지만 무기·스킬 업적에는 들어가지 않는다.
        Curse
    }
}
