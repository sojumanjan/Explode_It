namespace ExplodeIt.Progression
{
    // 스킬 강화가 바꾸는 것. 값의 뜻은 종류마다 다르다(강화 데이터의 값 칸 설명 참고).
    public enum AbilityUpgradeEffect
    {
        // 범위에 곱한다 (배). 블랙홀 범위, 순간폭탄 동안 폭탄 범위.
        RadiusMultiplier,
        // 지속 시간에 더한다 (초). 블랙홀 흡입 시간, 순간폭탄 지속 시간.
        DurationAdd,
        // 다발폭탄 수에 더한다 (개).
        CountAdd,
        // 다발폭탄이 터진 자리에서 작은 폭탄이 다시 튀어나온다. 값은 쓰지 않는다.
        ChildBombs
    }
}
