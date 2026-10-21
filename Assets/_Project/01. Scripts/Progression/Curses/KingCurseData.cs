using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Progression
{
    // 마지막 칸 "???": 저주가 아니라 모든 저주를 건 채 무한 모드에 들어가면 각성한 왕과 바로 싸우는 입장권이다.
    // 왕을 쓰러뜨리면 진짜 엔딩으로 넘어간다. 보스 연출·엔딩은 스테이지 진행 쪽에서 따로 만든다.
    [CreateAssetMenu(fileName = "Curse_King", menuName = "Explode It/Curses/King")]
    public class KingCurseData : CurseData
    {
        public override CurseKind Kind => CurseKind.King;
    }
}
