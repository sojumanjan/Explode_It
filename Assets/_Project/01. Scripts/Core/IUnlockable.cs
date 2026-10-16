using UnityEngine;

namespace ExplodeIt.Core
{
    // 메뉴에서 고르고 업적으로 해금하는 것(무기·스킬·아티팩트). 고르기 화면과 해금 알림이 종류를 몰라도 같은 방식으로 다룬다.
    public interface IUnlockable
    {
        string Id { get; }
        string DisplayName { get; }
        string Description { get; }
        Sprite Icon { get; }
        bool IsUnlocked { get; }
    }
}
