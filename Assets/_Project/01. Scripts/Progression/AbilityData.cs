using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Progression
{
    // 특수 능력(스킬): 처치로 게이지를 채워 우클릭으로 쓴다. 종류(블랙홀·다발폭탄·순간폭탄)마다 아래 해당 칸만 쓴다.
    // 메인 메뉴의 스킬 칸에서 고르고, 업적으로 해금한다. 강화(AbilityUpgradeData)도 업적으로 해금하면 판마다 자동으로 붙는다.
    [CreateAssetMenu(fileName = "AbilityData", menuName = "Explode It/Ability Data")]
    public class AbilityData : ScriptableObject, IUnlockable
    {
        [Header("해금·메뉴 표시")]
        [Tooltip("저장에 쓰는 고유 ID (영문). 해금·선택 기록이 이 값으로 남으므로 한 번 정하면 바꾸지 않는다")]
        [SerializeField] private string _id;

        [Tooltip("처음부터 쓸 수 있는 스킬인지. 끄면 업적으로 해금해야 고를 수 있다")]
        [SerializeField] private bool _unlockedByDefault;

        [Tooltip("메뉴에 보이는 이름")]
        [SerializeField] private string _displayName;

        [Tooltip("메뉴에 보이는 설명")]
        [SerializeField, TextArea] private string _description;

        [Tooltip("메뉴 스킬 칸·카드 그림. 비워 두면 이름만 보인다")]
        [SerializeField] private Sprite _icon;

        [Tooltip("스킬 종류. 종류에 맞는 아래 칸만 쓴다")]
        [SerializeField] private AbilityKind _kind;

        [Header("충전")]
        [Tooltip("게이지 한 칸을 채우는 데 필요한 처치 수 (마리). 스킬이 발동 중일 때 잡은 적은 세지 않는다")]
        [SerializeField, Min(1)] private int _killsToCharge = 9;

        // 칸마다 따로 쓸 수 있어서, 하나 바로 쓸지 두 칸 다 모아 연달아 쓸지를 플레이어가 고르게 된다.
        [Tooltip("게이지 칸 수 (칸). 한 칸이 차면 한 번 쓸 수 있고, 다 쓰지 않으면 다음 칸이 이어서 찬다")]
        [SerializeField, Min(1)] private int _maxCharges = 2;

        [Tooltip("시작할 때 차 있는 칸 수 (칸). 첫 위기에 바로 쓸 수 있게 한다")]
        [SerializeField, Min(0)] private int _startCharges = 1;

        [Header("블랙홀")]
        // 일반 폭탄 사거리 원과 무관하다. 화면에 따로 표시하지 않으므로 커서를 둔 곳에 거의 그대로 떨어진다.
        [Tooltip("최대 사거리 (유닛). 커서가 이보다 멀면 이 거리 지점에 떨어진다")]
        [SerializeField, Min(0f)] private float _maxThrowRange = 15f;

        // 무기마다 블랙홀 크기가 달라지지 않게 고정한다. 커지는 건 범위 강화를 꼈을 때뿐이다.
        [Tooltip("블랙홀 범위 (유닛, 반경). 무기와 상관없이 고정이고, 범위 강화를 끼면 그 배율이 곱해진다")]
        [SerializeField, Min(0.1f)] private float _blackHoleRadius = 3.74f;

        [Tooltip("흡입 시간 (초). 착지 후 적을 빨아들이다가 이 시간이 지나면 터진다")]
        [SerializeField, Min(0.1f)] private float _pullDuration = 1.5f;

        [Tooltip("흡입 속도 (유닛/초). 범위 안의 적이 중심 쪽으로 끌려가는 속도")]
        [SerializeField, Min(0f)] private float _pullSpeed = 4f;

        [Header("다발폭탄 (무기와 상관없이 아래 수치를 쓴다)")]
        [Tooltip("한 번에 뿌리는 폭탄 수 (개). 360도를 이 수로 나눠 한 칸에 하나씩 던진다")]
        [SerializeField, Min(1)] private int _scatterCount = 10;

        [Tooltip("칸 가운데에서 틀어질 수 있는 각도 (도, ±). 매번 무작위로 살짝 비틀어 똑같은 무늬가 되지 않게 한다")]
        [SerializeField, Min(0f)] private float _scatterAngleJitter = 5f;

        [Tooltip("폭탄이 떨어지는 거리의 최소 (유닛). 폭탄마다 플레이어에게서 최소~최대 사이 무작위 거리에 떨어진다")]
        [SerializeField, Min(0f)] private float _scatterDistanceMin = 3f;

        [Tooltip("폭탄이 떨어지는 거리의 최대 (유닛)")]
        [UnityEngine.Serialization.FormerlySerializedAs("_scatterDistance")]
        [SerializeField, Min(0f)] private float _scatterDistanceMax = 5f;

        // 멀리만 뿌리면 바로 붙은 적이 비어 애매하므로, 한 발은 늘 발밑에 떨군다.
        [Tooltip("폭탄 수 중 한 발은 플레이어 발밑 이 반경 안에 떨어진다 (유닛). 나머지는 사방으로 뿌린다")]
        [SerializeField, Min(0f)] private float _scatterSelfDropRadius = 0.2f;

        [Tooltip("폭탄 하나의 폭발 반경 (유닛)")]
        [SerializeField, Min(0.1f)] private float _scatterRadius = 1.4f;

        [Tooltip("손을 떠나는 시각을 폭탄마다 0~이 값 사이에서 무작위로 늦춘다 (초). 한꺼번에 날아가지 않고 우르르 흩어지게 한다")]
        [SerializeField, Min(0f)] private float _scatterThrowDelayMax = 0.35f;

        [Tooltip("착지부터 폭발까지의 최소 (초). 폭탄마다 최소~최대 사이에서 무작위로 정해 동시에 터지지 않게 한다")]
        [UnityEngine.Serialization.FormerlySerializedAs("_scatterFuseDelay")]
        [SerializeField, Min(0f)] private float _scatterFuseDelayMin = 0.4f;

        [Tooltip("착지부터 폭발까지의 최대 (초)")]
        [SerializeField, Min(0f)] private float _scatterFuseDelayMax = 1f;

        [Tooltip("날아가는 시간 (초)")]
        [SerializeField, Min(0.01f)] private float _scatterFlightDuration = 0.45f;

        [Tooltip("포물선 높이 (유닛)")]
        [SerializeField, Min(0f)] private float _scatterArcHeight = 1.5f;

        [Tooltip("폭탄 그림. 비워 두면 폭탄 프리팹 기본 그림")]
        [SerializeField] private Sprite _scatterSprite;

        [Tooltip("폭탄 그림의 화면 크기 (유닛, 긴 쪽 기준). 0이면 프리팹 크기")]
        [SerializeField, Min(0f)] private float _scatterSpriteSize = 0.7f;

        [Header("다발폭탄 강화: 작은 폭탄 (강화를 해금했을 때만)")]
        [Tooltip("터진 자리에서 다시 튀어나오는 작은 폭탄 수 (최소~최대, 개). 매번 이 사이에서 고른다")]
        [SerializeField, Min(1)] private int _childCountMin = 2;

        [SerializeField, Min(1)] private int _childCountMax = 3;

        [Tooltip("작은 폭탄이 튀어 나가는 거리의 최소 (유닛). 최소~최대 사이에서 무작위")]
        [SerializeField, Min(0f)] private float _childDistanceMin = 1f;

        [Tooltip("작은 폭탄이 튀어 나가는 거리의 최대 (유닛)")]
        [UnityEngine.Serialization.FormerlySerializedAs("_childDistance")]
        [SerializeField, Min(0f)] private float _childDistanceMax = 1.8f;

        [Tooltip("작은 폭탄의 폭발 반경 (유닛)")]
        [SerializeField, Min(0.1f)] private float _childRadius = 0.9f;

        [Tooltip("작은 폭탄이 튀어나오는 시각을 0~이 값 사이에서 무작위로 늦춘다 (초)")]
        [SerializeField, Min(0f)] private float _childThrowDelayMax = 0.2f;

        [Tooltip("작은 폭탄의 착지부터 폭발까지 최소 (초). 최소~최대 사이에서 무작위")]
        [UnityEngine.Serialization.FormerlySerializedAs("_childFuseDelay")]
        [SerializeField, Min(0f)] private float _childFuseDelayMin = 0.15f;

        [Tooltip("작은 폭탄의 착지부터 폭발까지 최대 (초)")]
        [SerializeField, Min(0f)] private float _childFuseDelayMax = 0.5f;

        [Tooltip("작은 폭탄이 날아가는 시간 (초)")]
        [SerializeField, Min(0.01f)] private float _childFlightDuration = 0.3f;

        [Tooltip("작은 폭탄 그림 크기 (유닛)")]
        [SerializeField, Min(0f)] private float _childSpriteSize = 0.45f;

        [Header("순간폭탄")]
        [Tooltip("지속 시간 (초). 이 동안 던진 폭탄은 땅에 닿자마자 터진다")]
        [SerializeField, Min(0.1f)] private float _instantDuration = 10f;

        [Tooltip("지속 동안 연사·충전 속도 배율 (배)")]
        [SerializeField, Min(1f)] private float _instantSpeedMultiplier = 4f;

        [Tooltip("지속 동안 최대 사거리 배율 (배). 무기 사거리에 곱한다")]
        [SerializeField, Min(1f)] private float _instantRangeMultiplier = 1.5f;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public bool IsUnlocked => _unlockedByDefault || SaveSystem.Data.IsAbilityUnlocked(_id);
        public AbilityKind Kind => _kind;
        public int KillsToCharge => _killsToCharge;
        public int MaxCharges => _maxCharges;
        public int StartCharges => Mathf.Min(_startCharges, _maxCharges);
        public float MaxThrowRange => _maxThrowRange;
        public float BlackHoleRadius => _blackHoleRadius;
        public float PullDuration => _pullDuration;
        public float PullSpeed => _pullSpeed;
        public int ScatterCount => _scatterCount;
        public float ScatterAngleJitter => _scatterAngleJitter;
        public float ScatterDistanceMin => _scatterDistanceMin;
        public float ScatterDistanceMax => Mathf.Max(_scatterDistanceMin, _scatterDistanceMax);
        public float ScatterSelfDropRadius => _scatterSelfDropRadius;
        public float ScatterRadius => _scatterRadius;
        public float ScatterThrowDelayMax => _scatterThrowDelayMax;
        public float ScatterFuseDelayMin => _scatterFuseDelayMin;
        public float ScatterFuseDelayMax => Mathf.Max(_scatterFuseDelayMin, _scatterFuseDelayMax);
        public float ScatterFlightDuration => _scatterFlightDuration;
        public float ScatterArcHeight => _scatterArcHeight;
        public Sprite ScatterSprite => _scatterSprite;
        public float ScatterSpriteSize => _scatterSpriteSize;
        public int ChildCountMin => _childCountMin;
        public int ChildCountMax => Mathf.Max(_childCountMin, _childCountMax);
        public float ChildDistanceMin => _childDistanceMin;
        public float ChildDistanceMax => Mathf.Max(_childDistanceMin, _childDistanceMax);
        public float ChildRadius => _childRadius;
        public float ChildThrowDelayMax => _childThrowDelayMax;
        public float ChildFuseDelayMin => _childFuseDelayMin;
        public float ChildFuseDelayMax => Mathf.Max(_childFuseDelayMin, _childFuseDelayMax);
        public float ChildFlightDuration => _childFlightDuration;
        public float ChildSpriteSize => _childSpriteSize;
        public float InstantDuration => _instantDuration;
        public float InstantSpeedMultiplier => _instantSpeedMultiplier;
        public float InstantRangeMultiplier => _instantRangeMultiplier;
    }
}
