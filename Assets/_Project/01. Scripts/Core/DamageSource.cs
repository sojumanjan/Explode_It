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
        InstantBomb
    }
}
