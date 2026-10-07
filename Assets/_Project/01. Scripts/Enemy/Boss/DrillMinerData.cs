using System;
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 드릴 광부 보스: 땅속에서 다가오다 구멍 여러 개를 동시에 뚫고, 그중 진짜 구멍에서 튀어나온다.
    // 노출된 동안 폭탄을 맞히면 드릴이 과열되고, 정해진 횟수만큼 과열되면 드릴이 멈춰 그로기에 빠진다.
    // 공통 수치 중 예고 시간 = 구멍이 보이고 분출하기까지, 회복 시간 = 튀어나온 뒤 노출 시간으로 쓴다.
    // 바퀴가 올라가면 구멍 수만 늘어난다.
    [CreateAssetMenu(fileName = "Boss2", menuName = "Explode It/Enemies/Drill Miner Boss Data")]
    public class DrillMinerData : EnemyData
    {
        [Serializable]
        public class Tier
        {
            [Tooltip("동시에 뚫리는 구멍 수 (개). 하나는 광부가 나오는 진짜 구멍, 나머지는 분출 공격만 한다")]
            [SerializeField, Min(1)] private int _holeCount = 1;

            public int HoleCount => _holeCount;
        }

        [Header("바퀴별")]
        [Tooltip("바퀴별 구멍 수. 1번 칸이 첫 바퀴다. 칸보다 많은 바퀴는 마지막 칸을 쓴다")]
        [SerializeField] private Tier[] _tiers = { new Tier() };

        [Header("과열")]
        [Tooltip("과열 횟수 (회). 노출 때 폭탄을 이만큼 맞히면 드릴이 멈춰 그로기에 빠진다. 한 번 노출에 최대 1회만 오른다")]
        [SerializeField, Min(1)] private int _heatToOverheat = 3;

        [Tooltip("그로기 시간 (초). 이 동안 실드가 풀려 한 방에 잡힌다. 못 잡으면 과열이 0으로 돌아가고 다시 도망간다")]
        [SerializeField, Min(0.1f)] private float _groggyDuration = 5f;

        [Tooltip("다 과열됐을 때의 색. 과열될수록 흰색에서 이 색으로 물든다")]
        [SerializeField] private Color _overheatTint = new Color(1f, 0.35f, 0.25f, 1f);

        [Header("잠수")]
        [Tooltip("흙더미가 또렷이 보이는 시간 (초). 파고 들어간 직후 플레이어 쪽으로 다가온다")]
        [SerializeField, Min(0f)] private float _moundVisibleTime = 1f;

        [Tooltip("흙더미가 희미해지는 시간 (초). 이 동안 점점 사라진다")]
        [SerializeField, Min(0f)] private float _moundFadeTime = 2f;

        [Tooltip("완전히 안 보이는 시간 (초). 이 시간이 지나면 구멍들이 보인다")]
        [SerializeField, Min(0f)] private float _hiddenTime = 1f;

        [Tooltip("땅속 이동 속도 (유닛/초). 흙더미가 플레이어 쪽으로 다가오는 속도. 땅속이라 구조물을 통과한다")]
        [SerializeField, Min(0f)] private float _undergroundSpeed = 3f;

        [Header("구멍")]
        [Tooltip("분출 반경 (유닛). 구멍마다 이 원 안의 플레이어를 친다")]
        [SerializeField, Min(0f)] private float _holeRadius = 1.2f;

        [Tooltip("진짜 구멍 거리 (유닛, 최소~최대). 구멍이 보이는 순간 플레이어로부터 이 거리 안 무작위 방향에 놓인다")]
        [SerializeField] private Vector2 _realHoleDistance = new Vector2(1.5f, 3f);

        [Tooltip("가짜 구멍 거리 (유닛, 최소~최대). 플레이어로부터 이 거리 안에 놓인다")]
        [SerializeField] private Vector2 _fakeHoleDistance = new Vector2(1.5f, 4.5f);

        [Tooltip("구멍끼리 최소 간격 (유닛). 원이 겹쳐 진짜 구멍 표시가 가려지지 않게 한다")]
        [SerializeField, Min(0f)] private float _holeSpacing = 2.6f;

        [Header("다이너마이트 (가짜 구멍)")]
        // 가짜 구멍이 분출한 뒤 다이너마이트가 사방으로 튀어 연쇄로 터진다. 가짜 구멍 근처도 곧바로 안전하지 않게 한다.
        [Tooltip("가짜 구멍 하나에서 튀어나오는 다이너마이트 수 (개, 최소~최대)")]
        [SerializeField] private Vector2Int _dynamiteCount = new Vector2Int(3, 4);

        // 모두 같은 순간에 터지면 한 번 펑 하고 끝나 밋밋하다. 개별로 조금씩 어긋나 연쇄로 터지게 한다.
        [Tooltip("분출부터 다이너마이트가 터지기까지 (초, 최소~최대). 하나마다 이 사이에서 따로 정한다. 날아가 떨어지는 시간도 여기에 포함된다")]
        [SerializeField] private Vector2 _dynamiteDelayRange = new Vector2(0.95f, 1.05f);

        [Tooltip("다이너마이트가 떨어지는 거리 (유닛, 최소~최대). 가짜 구멍 중심에서 서로 다른 방향으로 이 거리 안에 떨어진다")]
        [SerializeField] private Vector2 _dynamiteDistance = new Vector2(0.5f, 2f);

        [Tooltip("다이너마이트 폭발 반경 (구멍 분출 반경 대비 비율)")]
        [SerializeField, Range(0.1f, 1f)] private float _dynamiteRadiusRatio = 0.5f;

        [Tooltip("다이너마이트가 날아갈 때 떠오르는 높이 (유닛)")]
        [SerializeField, Min(0f)] private float _dynamiteArcHeight = 0.8f;

        [Header("튀어나오기")]
        // 분출 순간 그 자리에 툭 나타나면 어색해서, 구멍에서 솟구쳐 올랐다 착지하는 모습을 보여준다. 그림만 뜨고 판정 위치는 구멍 그대로다.
        [Tooltip("구멍에서 튀어나올 때 솟구치는 높이 (유닛)")]
        [SerializeField, Min(0f)] private float _emergeHopHeight = 0.8f;

        [Tooltip("솟구쳤다 착지하기까지 걸리는 시간 (초). 노출 시간 안에 포함된다")]
        [SerializeField, Min(0.05f)] private float _emergeHopDuration = 0.35f;

        [Header("도약 · 파고들기")]
        [Tooltip("도약 거리 (유닛). 노출이 끝나거나 맞는 순간 플레이어 반대쪽으로 이만큼 뛴다. 구조물을 넘어간다")]
        [SerializeField, Min(0f)] private float _leapDistance = 7f;

        [Tooltip("도약 시간 (초). 도약 중에는 무적")]
        [SerializeField, Min(0.05f)] private float _leapDuration = 0.5f;

        [Tooltip("도약 높이 (유닛). 그림만 이만큼 떠올랐다 내려온다")]
        [SerializeField, Min(0f)] private float _leapHeight = 1.5f;

        [Tooltip("뒷도약 때 뒤로 젖히는 각도 (도). 플레이어를 바라본 채 뒤로 뛰는 느낌을 준다")]
        [SerializeField, Min(0f)] private float _leapBackLean = 20f;

        [Tooltip("파고들기 시간 (초). 착지한 자리에서 땅속으로 사라지는 시간. 이 동안에도 무적")]
        [SerializeField, Min(0.05f)] private float _digDuration = 1f;

        [Tooltip("파고들 때 그림이 가라앉는 깊이 (유닛)")]
        [SerializeField, Min(0f)] private float _digSinkDepth = 0.6f;

        [Tooltip("파고들기 직전 살짝 뛰어오르는 높이 (유닛). 드릴을 내리꽂기 전 준비 동작")]
        [SerializeField, Min(0f)] private float _digHopHeight = 0.35f;

        [Tooltip("파고드는 동안 바라보는 쪽으로 기우는 각도 (도). 드릴을 앞으로 꽂아 넣는 느낌을 준다")]
        [SerializeField] private float _digLean = 30f;

        [Tooltip("파고드는 동안 드릴 진동으로 좌우로 떠는 폭 (유닛)")]
        [SerializeField, Min(0f)] private float _digJitter = 0.06f;

        [Tooltip("파고드는 동안 흙이 튀는 간격 (초). 짧을수록 흙이 계속 튄다")]
        [SerializeField, Min(0.02f)] private float _digDirtInterval = 0.15f;

        [Tooltip("도약 착지점이 맵 테두리·구조물에서 떨어질 거리 (유닛)")]
        [SerializeField, Min(0f)] private float _landingClearance = 1f;

        [Header("사운드")]
        [SerializeField, Tooltip("파고들기 시작하는 순간의 효과음")] private SoundData _digSound;
        [SerializeField, Tooltip("구멍에서 분출하며 튀어나오는 순간의 효과음")] private SoundData _burstSound;
        [SerializeField, Tooltip("과열이 1 오르는 순간의 효과음")] private SoundData _heatSound;
        [SerializeField, Tooltip("다이너마이트가 터지는 순간의 효과음")] private SoundData _dynamiteSound;
        [SerializeField, Tooltip("땅속에서 이동하는 동안의 효과음. 구멍이 보이는 순간 멈춘다")] private SoundData _diggingMoveSound;
        [SerializeField, Tooltip("다 과열돼 드릴이 멈추는 순간의 효과음")] private SoundData _overheatSound;
        [SerializeField, Tooltip("그로기에 빠지는 순간의 효과음 (보스 공용)")] private SoundData _groggySound;
        [SerializeField, Tooltip("노출 외 시간(도약 등)에 폭탄이 막혔을 때의 효과음")] private SoundData _immuneSound;

        [Header("연출")]
        [SerializeField, Tooltip("분출 순간의 연출 (흔들림, 흙 파편). 구멍마다 띄운다")] private FeedbackData _burstFeedback;
        [SerializeField, Tooltip("드릴을 땅에 내리꽂는 순간의 연출 (흙 크게 튐, 살짝 흔들림)")] private FeedbackData _digFeedback;
        [SerializeField, Tooltip("파고드는 동안 계속 튀는 흙 연출 (흔들림 없이 작게)")] private FeedbackData _digDirtFeedback;
        [SerializeField, Tooltip("과열이 오르는 순간의 연출 (김, 불꽃)")] private FeedbackData _heatFeedback;
        [SerializeField, Tooltip("다이너마이트가 터지는 순간의 연출")] private FeedbackData _dynamiteFeedback;
        [SerializeField, Tooltip("막혔을 때의 연출")] private FeedbackData _immuneFeedback;
        [SerializeField, Tooltip("보스를 잡은 순간의 연출 (슬로모션)")] private FeedbackData _defeatFeedback;
        [SerializeField, Tooltip("과열이 오를 때 번쩍이는 색")] private Color _heatFlashColor = new Color(1f, 0.6f, 0.3f, 1f);
        [SerializeField, Tooltip("막혔을 때 번쩍이는 색")] private Color _immuneFlashColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        [SerializeField, Min(0f), Tooltip("번쩍임 시간 (초)")] private float _flashDuration = 0.2f;

        public int HeatToOverheat => _heatToOverheat;
        public float GroggyDuration => _groggyDuration;
        public Color OverheatTint => _overheatTint;
        public float MoundVisibleTime => _moundVisibleTime;
        public float MoundFadeTime => _moundFadeTime;
        public float HiddenTime => _hiddenTime;
        public float UndergroundDuration => _moundVisibleTime + _moundFadeTime + _hiddenTime;
        public float UndergroundSpeed => _undergroundSpeed;
        public float HoleRadius => _holeRadius;
        public Vector2 RealHoleDistance => _realHoleDistance;
        public Vector2 FakeHoleDistance => _fakeHoleDistance;
        public float HoleSpacing => _holeSpacing;
        public Vector2Int DynamiteCount => _dynamiteCount;
        public Vector2 DynamiteDelayRange => _dynamiteDelayRange;
        public Vector2 DynamiteDistance => _dynamiteDistance;
        public float DynamiteRadius => _holeRadius * _dynamiteRadiusRatio;
        public float DynamiteArcHeight => _dynamiteArcHeight;
        public SoundData DynamiteSound => _dynamiteSound;
        public SoundData DiggingMoveSound => _diggingMoveSound;
        public FeedbackData DynamiteFeedback => _dynamiteFeedback;
        public float LeapDistance => _leapDistance;
        public float LeapDuration => _leapDuration;
        public float EmergeHopHeight => _emergeHopHeight;
        public float EmergeHopDuration => _emergeHopDuration;
        public float LeapHeight => _leapHeight;
        public float LeapBackLean => _leapBackLean;
        public float DigDuration => _digDuration;
        public float DigSinkDepth => _digSinkDepth;
        public float DigHopHeight => _digHopHeight;
        public float DigJitter => _digJitter;
        public float DigLean => _digLean;
        public float DigDirtInterval => _digDirtInterval;
        public float LandingClearance => _landingClearance;
        public SoundData DigSound => _digSound;
        public SoundData BurstSound => _burstSound;
        public SoundData HeatSound => _heatSound;
        public SoundData OverheatSound => _overheatSound;
        public SoundData GroggySound => _groggySound;
        public SoundData ImmuneSound => _immuneSound;
        public FeedbackData BurstFeedback => _burstFeedback;
        public FeedbackData DigFeedback => _digFeedback;
        public FeedbackData DigDirtFeedback => _digDirtFeedback;
        public FeedbackData HeatFeedback => _heatFeedback;
        public FeedbackData ImmuneFeedback => _immuneFeedback;
        public FeedbackData DefeatFeedback => _defeatFeedback;
        public Color HeatFlashColor => _heatFlashColor;
        public Color ImmuneFlashColor => _immuneFlashColor;
        public float FlashDuration => _flashDuration;

        public Tier GetTier(int tier)
        {
            return _tiers[Mathf.Clamp(tier, 0, _tiers.Length - 1)];
        }

        // 구멍 표시를 미리 만들어 둘 수. 어느 바퀴가 와도 모자라지 않게 가장 많은 바퀴 기준이다.
        public int MaxHoleCount
        {
            get
            {
                int max = 0;
                for (int i = 0; i < _tiers.Length; i++)
                {
                    max = Mathf.Max(max, _tiers[i].HoleCount);
                }

                return max;
            }
        }

        private void OnValidate()
        {
            if (_tiers == null || _tiers.Length == 0)
            {
                _tiers = new[] { new Tier() };
            }
        }
    }
}
