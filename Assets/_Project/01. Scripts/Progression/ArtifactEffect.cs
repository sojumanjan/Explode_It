namespace ExplodeIt.Progression
{
    // 아티팩트 효과의 종류. 수치(값 1·값 2)의 뜻은 종류마다 다르다(아티팩트 데이터 툴팁 참고).
    public enum ArtifactEffect
    {
        None,
        // 장전된 폭탄이 가득일 때 이동속도 증가. 값 1 = 증가 비율.
        MoveSpeedWhenBombsFull,
        // 대시를 연달아 더 쓸 수 있음. 값 1 = 추가 횟수.
        ExtraDodge,
        // 체력 증가, 대신 적이 빨리 나옴. 값 1 = 체력 칸, 값 2 = 스폰 간격 배율.
        ExtraHealth,
        // 스킬에 필요한 처치 수 감소. 값 1 = 줄일 수.
        KillsToChargeReduction,
        // 스킬 게이지 칸 증가. 값 1 = 늘릴 칸.
        ExtraAbilityCharges,
        // 가만히 서 있으면 게이지가 참. 값 1 = 시작까지 시간(초), 값 2 = 이후 간격(초).
        StillCharge,
        // 대시 중 부딪힌 적 처치, 대신 대시 쿨타임 증가. 값 1 = 처치 판정 반경(유닛), 값 2 = 쿨타임 배율.
        DodgeKill,
        // 스킬로 많이 잡으면 게이지 회복. 값 1 = 최소 처치 수, 값 2 = 회복할 처치 수.
        AbilityRefund,
        // 대시 중에도 폭탄을 던질 수 있음. 값 없음.
        ThrowWhileDodging,
        // 스킬 사용 직후 이동속도 증가. 값 1 = 증가 비율, 값 2 = 지속 시간(초).
        SpeedAfterAbility
    }
}
