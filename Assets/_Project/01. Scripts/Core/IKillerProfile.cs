using UnityEngine;

namespace ExplodeIt.Core
{
    // 플레이어를 쓰러뜨릴 수 있는 쪽의 이름과 얼굴. 사망 결과 화면이 "누구에게 당했는지" 보여 줄 때 쓴다.
    // 공용 계층이 적 데이터를 직접 알지 않도록 이 모양만 주고받는다.
    public interface IKillerProfile
    {
        string KillerName { get; }
        Sprite KillerPortrait { get; }
    }
}
