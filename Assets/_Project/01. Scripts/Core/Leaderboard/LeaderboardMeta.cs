// 순위표 기록에 곁들이는 정보(장착한 저주 ID, 버틴 시간)를 JSON으로 읽는 틀
using System;

namespace ExplodeIt.Core
{
    // 서버가 돌려주는 메타데이터 JSON을 JsonUtility로 읽는다. JSON 키와 이름이 같아야 해서 공개 필드로 둔다.
    [Serializable]
    public class LeaderboardMeta
    {
        public const string CursesKey = "curses";
        public const string SecondsKey = "seconds";

        // 저주 ID를 쉼표로 이어 붙인 것.
        public string curses;
        public float seconds;
    }
}
