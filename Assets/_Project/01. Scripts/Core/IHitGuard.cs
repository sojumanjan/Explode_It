namespace ExplodeIt.Core
{
    // 피격을 판정 단계에서 막는 장치(방패 등). 막으면 그 피격으로는 죽지 않는다.
    // 무적·실드와 달리 피격 정보(어디서 터졌는지)를 보고 막을지 정한다.
    public interface IHitGuard
    {
        bool Blocks(in HitInfo hit);
    }
}
