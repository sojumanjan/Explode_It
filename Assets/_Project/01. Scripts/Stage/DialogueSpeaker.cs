namespace ExplodeIt.Stage
{
    // 보스 대화에서 이 줄을 누가 말하는지. 말풍선이 그 머리 위에 뜬다.
    // 저장된 대사의 번호가 밀리지 않게 새 값은 항상 끝에 붙인다.
    public enum DialogueSpeaker
    {
        King,
        Boss,
        // 플레이어(고블린). 최종 보스 처치 대화처럼 플레이어가 말하는 줄은 플레이어 머리 위에 뜬다.
        Player
    }
}
