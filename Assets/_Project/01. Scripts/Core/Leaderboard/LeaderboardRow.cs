// 순위표 한 줄(순위, 이름, 처치 수, 장착한 저주 ID)
namespace ExplodeIt.Core
{
    // 서버 응답(LeaderboardEntry)을 화면에 바로 쓸 값으로 바꿔 둔 것. UI가 서비스 패키지를 몰라도 되게 한다.
    public readonly struct LeaderboardRow
    {
        // 1부터 센다.
        public readonly int Rank;
        // 이름 뒤에 서버가 붙인 같은 이름 구분 번호(#1234)를 뗀 이름.
        public readonly string Name;
        public readonly int Kills;
        // 그 기록을 세운 판에 장착한 저주 ID들(쉼표로 이어 붙임). 없으면 빈 문자열.
        public readonly string CurseIds;
        public readonly bool IsMine;

        public LeaderboardRow(int rank, string name, int kills, string curseIds, bool isMine)
        {
            Rank = rank;
            Name = name;
            Kills = kills;
            CurseIds = curseIds;
            IsMine = isMine;
        }
    }
}
