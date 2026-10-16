using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Progression
{
    // 무기 위에 얹는 작은 조정. 판 시작 전 정해진 칸 수만큼 골라 조합한다.
    // 지금은 메뉴에 보여 줄 정보와 해금 여부만 있다. 효과 칸은 아티팩트를 구현할 때 더한다.
    [CreateAssetMenu(fileName = "Artifact", menuName = "Explode It/Progression/Artifact Data")]
    public class ArtifactData : ScriptableObject, IUnlockable
    {
        [Tooltip("저장에 쓰는 고유 ID (영문). 해금·선택 기록이 이 값으로 남으므로 한 번 정하면 바꾸지 않는다")]
        [SerializeField] private string _id;

        [Tooltip("메뉴에 보이는 이름")]
        [SerializeField] private string _displayName;

        [Tooltip("메뉴에 보이는 효과 설명")]
        [SerializeField, TextArea] private string _description;

        [Tooltip("메뉴 카드 그림. 비워 두면 빈 그림으로 보인다")]
        [SerializeField] private Sprite _icon;

        [Tooltip("처음부터 쓸 수 있는지. 끄면 업적 보상으로 해금해야 고를 수 있다")]
        [SerializeField] private bool _unlockedByDefault;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public bool IsUnlocked => _unlockedByDefault || SaveSystem.Data.IsArtifactUnlocked(_id);
    }
}
