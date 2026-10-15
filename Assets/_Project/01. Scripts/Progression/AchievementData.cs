using ExplodeIt.Bombs;
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Progression
{
    // 도감에 스티커 한 장으로 붙는 업적. 달성하면 보상 무기·아티팩트가 해금된다.
    // 지금은 도감에 보여 줄 정보와 보상만 있다. 달성 조건은 업적 판정을 구현할 때 더한다.
    [CreateAssetMenu(fileName = "Achievement", menuName = "Explode It/Progression/Achievement Data")]
    public class AchievementData : ScriptableObject
    {
        [Tooltip("저장에 쓰는 고유 ID (영문). 달성 기록이 이 값으로 남으므로 한 번 정하면 바꾸지 않는다")]
        [SerializeField] private string _id;

        [Tooltip("도감에 보이는 이름")]
        [SerializeField] private string _displayName;

        [Tooltip("달성 조건 설명. 잠긴 무기·아티팩트 카드에도 해금 조건으로 그대로 보인다")]
        [SerializeField, TextArea] private string _description;

        [Tooltip("달성하면 도감 칸에 붙는 스티커 그림")]
        [SerializeField] private Sprite _sticker;

        [Tooltip("켜면 달성 전까지 이름·조건을 ???로 가린다. 찾아내는 재미를 주는 업적용")]
        [SerializeField] private bool _isHidden;

        [Header("보상 (둘 다 비워도 된다)")]
        [Tooltip("달성하면 해금되는 무기")]
        [SerializeField] private WeaponData _rewardWeapon;

        [Tooltip("달성하면 해금되는 아티팩트")]
        [SerializeField] private ArtifactData _rewardArtifact;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Sticker => _sticker;
        public bool IsHidden => _isHidden;
        public WeaponData RewardWeapon => _rewardWeapon;
        public ArtifactData RewardArtifact => _rewardArtifact;
        public bool IsCompleted => SaveSystem.Data.IsAchievementCompleted(_id);
    }
}
