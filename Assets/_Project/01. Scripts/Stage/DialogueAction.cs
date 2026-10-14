namespace ExplodeIt.Stage
{
    // 대화 줄이 시작될 때나 넘길 때 함께 보여 줄 연출. 최종 보스 처치 대화의 엔딩 장면(책 던지기, 왕 퇴장, 고블린이 책으로 들어감)에 쓴다.
    // 저장된 대사의 번호가 밀리지 않게 새 값은 항상 끝에 붙인다.
    public enum DialogueAction
    {
        None,
        // 2등신 왕이 스티커 책을 왕과 고블린 사이 바닥으로 툭 던진다.
        ThrowBook,
        // 2등신 왕이 하늘로 푸쉭 날아가 사라진다.
        KingFlyAway,
        // 고블린이 포물선을 그리며 책 위로 점프해 착지한다.
        PlayerJumpToBook,
        // 책이 번쩍이며 고블린이 빙글빙글 돌며 책 속으로 빨려 들어간다.
        PlayerSuckedIn
    }
}
