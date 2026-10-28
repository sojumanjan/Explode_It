using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Progression
{
    // 승천 저주 하나. 무한 모드에 골라 끼우면 판이 눈에 띄게 어려워진다.
    // 해금은 목록 순서대로 하나씩이다. 바로 앞까지 열린 상태에서, 저주를 정해진 수만큼 끼고 한 판에 정해진 처치 수를 넘기면 열린다.
    // 저주마다 이 클래스를 이어받은 데이터 클래스가 따로 있고, 그 저주의 수치(간격·범위 등)를 각자 들고 있다.
    // 플레이어가 저주에 쓰러지면 결과 화면에 저주 이름과 그림이 "쓰러뜨린 것"으로 나온다.
    public abstract class CurseData : ScriptableObject, IUnlockable, IKillerProfile
    {
        [Tooltip("저장에 쓰는 고유 ID (영문). 해금·장착 기록이 이 값으로 남으므로 한 번 정하면 바꾸지 않는다")]
        [SerializeField] private string _id;

        [Tooltip("메뉴에 보이는 이름")]
        [SerializeField] private string _displayName;

        [Tooltip("메뉴에 보이는 효과 설명")]
        [SerializeField, TextArea] private string _description;

        [Tooltip("메뉴 칸 그림 (실루엣)")]
        [SerializeField] private Sprite _icon;

        [Tooltip("처음부터 열려 있는지")]
        [SerializeField] private bool _unlockedByDefault;

        [Header("해금 조건")]
        [Tooltip("해금하려면 끼고 있어야 하는 저주 수 (개)")]
        [SerializeField, Min(0)] private int _requiredCurses = 1;

        [Tooltip("해금하려면 한 판에 넘겨야 하는 처치 수 (마리)")]
        [SerializeField, Min(1)] private int _unlockKills = 100;

        public string Id => _id;
        public string DisplayName => L10n.Data(_displayName);
        public string Description => L10n.Data(_description);
        public Sprite Icon => _icon;
        public abstract CurseKind Kind { get; }
        public bool UnlockedByDefault => _unlockedByDefault;
        public int RequiredCurses => _requiredCurses;
        public int UnlockKills => _unlockKills;
        public bool IsUnlocked => _unlockedByDefault || SaveSystem.Data.IsCurseUnlocked(_id);
        public bool IsEquipped => IsUnlocked && SaveSystem.Data.IsCurseEquipped(_id);
        public string KillerName => L10n.Data(_displayName);
        public Sprite KillerPortrait => _icon;

        // 판이 시작될 때 장비 적용기가 부른다. 스폰·체력처럼 다른 시스템이 읽는 값만 바꾸는 저주가 여기서 값을 넣는다.
        // 맵에 무언가를 내는 저주는 게임 씬의 저주 컴포넌트가 따로 맡으므로 덮어쓰지 않는다.
        public virtual void ApplyRunModifiers()
        {
        }
    }
}
