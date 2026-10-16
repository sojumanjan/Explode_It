using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Progression
{
    // 무기 위에 얹는 작은 조정. 판 시작 전 정해진 칸 수만큼 골라 조합한다.
    // 효과는 종류 하나와 수치 두 개로 정한다. 판이 시작되면 아티팩트 적용기가 이 값을 판 보정값(RunModifiers)에 더한다.
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

        [Header("효과")]
        [Tooltip("효과 종류. 값 1·값 2의 뜻은 종류마다 다르다(종류 이름 위 주석 참고)")]
        [SerializeField] private ArtifactEffect _effect;

        [Tooltip("값 1. 장갑·양말=이속 증가 비율(0.1=+10%), 신발=추가 대시 횟수, 팬던트=체력 칸, 망토=줄일 처치 수, 벨트=늘릴 칸, 동전=시작까지 시간(초), 헬름=처치 판정 반경(유닛), 반지=최소 처치 수")]
        [SerializeField] private float _value1;

        [Tooltip("값 2. 팬던트=스폰 간격 배율(0.8=20% 빨리), 동전=이후 간격(초), 헬름=대시 쿨타임 배율(1.5=50% 증가), 반지=회복할 처치 수, 양말=지속 시간(초)")]
        [SerializeField] private float _value2;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public bool IsUnlocked => _unlockedByDefault || SaveSystem.Data.IsArtifactUnlocked(_id);
        public ArtifactEffect Effect => _effect;
        public float Value1 => _value1;
        public float Value2 => _value2;
    }
}
