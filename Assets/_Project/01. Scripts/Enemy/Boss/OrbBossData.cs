using System;
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 구체 보스: 맵을 튕겨 다니는 구체를 모두 터트려야 실드가 풀리고, 그로기 동안 한 방에 잡을 수 있다.
    // 공통 수치(예고 = 할퀴기 준비 시간, 회복, 사망 연출)는 EnemyData를 쓴다.
    // 바퀴가 올라가도 보스 자체는 그대로이고 혼불의 개수와 속도만 달라지므로, 바퀴별 표에는 그 둘만 둔다.
    [CreateAssetMenu(fileName = "Boss_Orb", menuName = "Explode It/Enemies/Orb Boss Data")]
    public class OrbBossData : EnemyData
    {
        [Serializable]
        public class Tier
        {
            [Tooltip("혼불 수 (개). 이 혼불을 모두 터트려야 실드가 풀린다")]
            [SerializeField, Min(1)] private int _orbCount = 2;

            [Tooltip("혼불 속도 (유닛/초). 플레이어(약 6)와 비교해 정한다")]
            [SerializeField, Min(0f)] private float _orbSpeed = 6f;

            public int OrbCount => _orbCount;
            public float OrbSpeed => _orbSpeed;
        }

        [Header("바퀴별 혼불")]
        [Tooltip("바퀴별 혼불 수와 속도. 1번 칸이 첫 바퀴, 2번 칸이 두 번째 바퀴다. 칸보다 많은 바퀴는 마지막 칸을 쓴다")]
        [SerializeField] private Tier[] _tiers = { new Tier() };

        [Header("할퀴기")]
        [Tooltip("할퀴기 간격 (초). 마지막 할퀴기(또는 등장) 뒤 이 시간이 지난 뒤, 플레이어가 공통 항목의 공격 시작 거리 안에 들어오면 할퀴기를 준비한다")]
        [SerializeField, Min(0f)] private float _clawInterval = 3f;

        [Tooltip("할퀴기 길이 (유닛). 몸 중심에서 정면으로 뻗는 사각형 길이. 할퀴면서 이 끝까지 돌진한다")]
        [SerializeField, Min(0f)] private float _clawLength = 8f;

        [Tooltip("할퀴기 폭 (유닛). 걸어서는 못 빠져나가 구르기로 피하도록 넓게 둔다")]
        [SerializeField, Min(0f)] private float _clawWidth = 5f;

        [Tooltip("할퀴기 돌진 속도 (유닛/초). 할퀴는 순간부터 범위 끝까지 이 속도로 달려간다")]
        [SerializeField, Min(0.1f)] private float _clawDashSpeed = 20f;

        [Header("그로기")]
        [Tooltip("그로기 시간 (초). 혼불을 다 터트리면 이 시간 동안 실드가 풀리고 움직이지 않는다. 못 잡으면 혼불이 다시 생긴다")]
        [SerializeField, Min(0.1f)] private float _groggyDuration = 5f;

        [Header("혼불")]

        [Tooltip("구체가 맵 테두리에 튕길 때 반사각에 더하는 무작위 각도 (도, ±). 같은 궤도를 맴돌지 않게 한다")]
        [SerializeField, Range(0f, 45f)] private float _orbBounceJitter = 10f;

        [Tooltip("구체가 생기는 거리 (유닛). 보스 중심에서 이 거리만큼 떨어진 곳에서 각자 무작위 방향으로 퍼져 나간다")]
        [SerializeField, Min(0f)] private float _orbSpawnRadius = 0.5f;

        [Tooltip("등장 동작으로 혼불이 퍼져 나간 뒤 플레이어 조작이 돌아오기까지 (초). 혼불이 어디로 흩어지는지 볼 시간을 준다")]
        [SerializeField, Min(0f)] private float _entranceSettleTime = 1.5f;

        [Header("이동: 야수처럼 좌우로 파고들기")]
        // 실드가 있는 동안은 맞지 않으므로 쫓아온다기보다 플레이어 주변을 빠르게 배회하며 압박한다.
        // 플레이어 둘레 작은 원의 왼쪽·오른쪽 반원을 번갈아 한 점씩 찍고, 짧게 달려간 뒤 잠깐 멈추기를 반복한다.
        [Tooltip("목표 원 반지름 (유닛). 플레이어를 중심으로 한 이 원의 테두리 위 한 점으로 달려든다")]
        [SerializeField, Min(0f)] private float _lungeCircleRadius = 2f;

        [Tooltip("파고들기 이동 시간 (초, 최소~최대). 찍은 지점까지 이 시간 안에 빠르게 달려간다. 범위 안에서 매번 무작위")]
        [SerializeField] private Vector2 _lungeDuration = new Vector2(0.35f, 0.45f);

        [Tooltip("파고들기 사이 멈춤 (초, 최소~최대). 도착한 뒤 이만큼 멈췄다가 다음 지점을 찍고 바로 달려간다")]
        [SerializeField] private Vector2 _lungePause = new Vector2(0.15f, 0.2f);

        [Header("사운드")]
        [Tooltip("할퀴기 준비(범위가 차오르기 시작)하는 순간의 효과음. 준비가 끊기면(그로기 등) 멈춘다")]
        [SerializeField] private SoundData _clawReadySound;

        [Tooltip("할퀴는 순간의 효과음. 비워 두면 소리 없이 할퀸다")]
        [SerializeField] private SoundData _clawSound;

        [Tooltip("구체를 다 터트려 그로기에 빠지는 순간의 효과음")]
        [SerializeField] private SoundData _groggySound;

        [Tooltip("실드에 폭탄이 막혔을 때의 효과음")]
        [SerializeField] private SoundData _immuneSound;

        [Header("연출")]
        [Tooltip("실드에 폭탄이 막혔을 때의 연출 (튕기는 불꽃, 흔들림). 보스 몸에서 폭탄 쪽으로 띄운다")]
        [SerializeField] private FeedbackData _immuneFeedback;

        [Tooltip("실드에 막혔을 때 번쩍이는 색")]
        [SerializeField] private Color _immuneFlashColor = new Color(0.4f, 0.9f, 1f, 1f);

        [Tooltip("번쩍임 시간 (초)")]
        [SerializeField, Min(0f)] private float _immuneFlashDuration = 0.2f;

        [Tooltip("구체와 이어지는 선이 보스 쪽에서 시작하는 위치 (유닛, 보스 발밑 기준). 몸 한가운데쯤에 둔다")]
        [SerializeField] private Vector2 _tetherAnchorOffset = new Vector2(0f, 0.5f);

        [Header("구체(혼불) 떠다님")]
        // 이동 경로는 직선 그대로 두고 그림만 흔든다. 판정 위치가 흔들리면 어디에 폭탄을 둘지 읽기 어려워진다.
        [Tooltip("위아래로 붕붕 뜨는 높이 (유닛)")]
        [SerializeField, Min(0f)] private float _orbBobHeight = 0.12f;

        [Tooltip("위아래로 뜨는 빠르기 (회/초)")]
        [SerializeField, Min(0f)] private float _orbBobRate = 1.6f;

        [Tooltip("불꽃처럼 일렁이는 크기 변화 (비율)")]
        [SerializeField, Min(0f)] private float _orbFlicker = 0.08f;

        [Tooltip("일렁이는 빠르기 (회/초)")]
        [SerializeField, Min(0f)] private float _orbFlickerRate = 5f;

        [Tooltip("좌우로 흔들리는 각도 (도)")]
        [SerializeField, Min(0f)] private float _orbSway = 6f;

        // 보스를 잡은 순간을 길게 느끼게 한다. 히트스톱 시간을 슬로모션 시간으로 쓴다(실제 시간 기준).
        [Tooltip("보스를 잡은 순간의 연출. 히트스톱 시간을 1초, 게임 속도 배율을 낮게 두면 1초 슬로모션이 된다")]
        [SerializeField] private FeedbackData _defeatFeedback;

        public float OrbBounceJitter => _orbBounceJitter;
        public float OrbSpawnRadius => _orbSpawnRadius;
        public float EntranceSettleTime => _entranceSettleTime;
        public float LungeCircleRadius => _lungeCircleRadius;
        public Vector2 LungeDuration => _lungeDuration;
        public Vector2 LungePause => _lungePause;
        public float ClawInterval => _clawInterval;
        public float ClawLength => _clawLength;
        public float ClawWidth => _clawWidth;
        public float ClawDashSpeed => _clawDashSpeed;
        public float GroggyDuration => _groggyDuration;
        public SoundData ClawReadySound => _clawReadySound;
        public SoundData ClawSound => _clawSound;
        public SoundData GroggySound => _groggySound;
        public FeedbackData DefeatFeedback => _defeatFeedback;
        public SoundData ImmuneSound => _immuneSound;
        public FeedbackData ImmuneFeedback => _immuneFeedback;
        public Color ImmuneFlashColor => _immuneFlashColor;
        public float ImmuneFlashDuration => _immuneFlashDuration;
        public Vector2 TetherAnchorOffset => _tetherAnchorOffset;
        public float OrbBobHeight => _orbBobHeight;
        public float OrbBobRate => _orbBobRate;
        public float OrbFlicker => _orbFlicker;
        public float OrbFlickerRate => _orbFlickerRate;
        public float OrbSway => _orbSway;

        public Tier GetTier(int tier)
        {
            return _tiers[Mathf.Clamp(tier, 0, _tiers.Length - 1)];
        }

        // 구체를 미리 만들어 둘 수. 어느 바퀴가 와도 모자라지 않게 가장 많은 바퀴 기준이다.
        public int MaxOrbCount
        {
            get
            {
                int max = 0;
                for (int i = 0; i < _tiers.Length; i++)
                {
                    max = Mathf.Max(max, _tiers[i].OrbCount);
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
