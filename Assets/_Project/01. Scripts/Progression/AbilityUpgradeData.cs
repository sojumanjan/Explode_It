using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Progression
{
    // 스킬 강화 하나. 업적으로 해금한 뒤 스킬 고르기 화면에서 장착해야 그 스킬을 쓰는 판에 붙는다.
    // 장착을 고르게 둔 것은 "적을 안 잡으면 범위 증가"처럼 플레이 방식이 갈리는 강화를 끄고 싶을 수 있어서다.
    // 아티팩트는 반응형 효과를 맡고, 스킬 자체의 성능은 이 강화로만 올린다.
    [CreateAssetMenu(fileName = "Upgrade", menuName = "Explode It/Progression/Ability Upgrade Data")]
    public class AbilityUpgradeData : ScriptableObject, IUnlockable
    {
        [Tooltip("저장에 쓰는 고유 ID (영문). 해금 기록이 이 값으로 남으므로 한 번 정하면 바꾸지 않는다")]
        [SerializeField] private string _id;

        [Tooltip("강화되는 스킬")]
        [SerializeField] private AbilityData _ability;

        [Tooltip("메뉴·해금 알림에 보이는 이름")]
        [SerializeField] private string _displayName;

        [Tooltip("메뉴에 보이는 효과 설명")]
        [SerializeField, TextArea] private string _description;

        [Tooltip("무엇을 바꾸는지")]
        [SerializeField] private AbilityUpgradeEffect _effect;

        [Tooltip("값. 범위 배율이면 곱할 배수(1.3 = +30%), 지속 시간이면 더할 초, 폭탄 수면 더할 개수. 작은 폭탄은 쓰지 않는다")]
        [SerializeField] private float _value = 1f;

        public string Id => _id;
        public AbilityData Ability => _ability;
        public string DisplayName => L10n.Data(_displayName);
        public string Description => L10n.Data(_description);
        public Sprite Icon => _ability != null ? _ability.Icon : null;
        public bool IsUnlocked => SaveSystem.Data.IsUpgradeUnlocked(_id);
        public bool IsEquipped => IsUnlocked && SaveSystem.Data.IsUpgradeEquipped(_id);
        public AbilityUpgradeEffect Effect => _effect;
        public float Value => _value;
    }
}
