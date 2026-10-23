namespace ExplodeIt.Stage
{
    // 크레딧 한 줄의 모양. 씬의 크레딧 화면에 모양마다 본보기가 하나씩 있다.
    public enum CreditsEntryKind
    {
        // 게임 제목처럼 크게.
        Title,
        // "캐스트", "사용한 도구" 같은 묶음 제목.
        Heading,
        // 이름·내용 한 줄.
        Line,
        // 캐릭터 그림을 세워 두고 옆에 이름과 설명.
        Cast,
        // 빈 줄. 높이는 데이터의 빈 줄 높이를 따른다.
        Space
    }
}
