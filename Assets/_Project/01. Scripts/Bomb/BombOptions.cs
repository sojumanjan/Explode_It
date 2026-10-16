using ExplodeIt.Core;

namespace ExplodeIt.Bombs
{
    // 폭탄 한 발의 성질. 일반 폭탄은 구조물에 가리고 방패에 막히지만, 스킬 폭탄(다발폭탄)은 둘 다 무시한다.
    public readonly struct BombOptions
    {
        public static readonly BombOptions Normal = new BombOptions(false, false, DamageSource.Bomb);

        // 폭발 범위가 구조물에 잘리지 않고, 구조물 뒤의 적도 맞는다.
        public readonly bool IgnoresObstacles;
        public readonly bool IgnoresGuard;
        public readonly DamageSource Source;

        public BombOptions(bool ignoresObstacles, bool ignoresGuard, DamageSource source)
        {
            IgnoresObstacles = ignoresObstacles;
            IgnoresGuard = ignoresGuard;
            Source = source;
        }
    }
}
