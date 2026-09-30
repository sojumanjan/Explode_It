namespace ExplodeIt.Core
{
    // 폭탄과 적 공격이 대상의 종류를 모른 채 피격을 전달하기 위한 입구.
    public interface IHittable
    {
        void ReceiveHit(in HitInfo hit);
    }
}
