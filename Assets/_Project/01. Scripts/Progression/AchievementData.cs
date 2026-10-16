using System.Collections.Generic;
using ExplodeIt.Bombs;
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Progression
{
    // 도감에 스티커 한 장으로 붙는 업적. 달성하면 보상 무기·스킬·아티팩트가 해금된다.
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

        [Header("조건")]
        [Tooltip("달성 조건의 종류")]
        [SerializeField] private AchievementCondition _condition;

        [Tooltip("목표 수. 연속 처치면 연속 횟수(번), 그 밖에는 처치 수(마리). 보스 막타·무처치 조건은 쓰지 않는다")]
        [SerializeField, Min(1)] private int _target = 1;

        [Tooltip("스킬 조건(스킬 한 번 처치, 무처치, 보스 막타, 스킬 누적 처치)이 기준으로 삼는 스킬")]
        [SerializeField] private AbilityData _ability;

        [Header("보상 (모두 비워도 된다)")]
        [Tooltip("달성하면 해금되는 무기")]
        [SerializeField] private WeaponData _rewardWeapon;

        [Tooltip("달성하면 해금되는 스킬")]
        [SerializeField] private AbilityData _rewardAbility;

        [Tooltip("달성하면 해금되는 스킬 강화")]
        [SerializeField] private AbilityUpgradeData _rewardUpgrade;

        [Tooltip("달성하면 해금되는 아티팩트")]
        [SerializeField] private ArtifactData _rewardArtifact;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Sticker => _sticker;
        public bool IsHidden => _isHidden;
        public AchievementCondition Condition => _condition;
        public int Target => _target;
        public WeaponData RewardWeapon => _rewardWeapon;
        public ArtifactData RewardArtifact => _rewardArtifact;
        public AbilityData RewardAbility => _rewardAbility;
        public AbilityUpgradeData RewardUpgrade => _rewardUpgrade;
        public AbilityData Ability => _ability;

        // 이 업적의 보상들을 담는다. 해금 알림이 종류를 몰라도 차례로 보여 줄 수 있게 한다.
        public void CollectRewards(List<IUnlockable> into)
        {
            if (_rewardWeapon != null)
            {
                into.Add(_rewardWeapon);
            }

            if (_rewardAbility != null)
            {
                into.Add(_rewardAbility);
            }

            if (_rewardUpgrade != null)
            {
                into.Add(_rewardUpgrade);
            }

            if (_rewardArtifact != null)
            {
                into.Add(_rewardArtifact);
            }
        }

        public bool IsCompleted => SaveSystem.Data.IsAchievementCompleted(_id);
    }
}
