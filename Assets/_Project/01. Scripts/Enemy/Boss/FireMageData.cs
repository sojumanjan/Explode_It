using System;
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 화염 마법사(boss1): 제자리에 서서 화염 영역으로 몸을 감싸고, 영역 둘레를 도는 요정을 모두 잡아야 영역이 풀려 그로기에 빠진다.
    // 공통 항목 중 예고 시간은 쓰지 않는다(패턴마다 따로 둔다). 회복 시간은 패턴이 끝난 뒤 숨 고르는 시간으로 쓴다.
    // 바퀴가 올라가도 보스 자체는 그대로이고 요정 속도와 메테오 수만 달라지므로, 바퀴별 표에는 그 둘만 둔다.
    [CreateAssetMenu(fileName = "Boss_FireMage", menuName = "Explode It/Enemies/Fire Mage Data")]
    public class FireMageData : EnemyData
    {
        [Serializable]
        public class Tier
        {
            [Tooltip("요정 수 (마리). 같은 간격으로 영역 둘레를 돈다. 모두 잡으면 영역이 풀린다")]
            [SerializeField, Min(1)] private int _fairyCount = 3;

            [Tooltip("요정 회전 속도 (도/초). 처음 요정들이 도는 빠르기. 빠를수록 1초 뒤 위치를 맞히기 어렵다")]
            [SerializeField, Min(0f)] private float _fairyOrbitSpeed = 60f;

            [Tooltip("메테오 한 번에 떨어지는 개수 (개)")]
            [SerializeField, Min(1)] private int _meteorCount = 8;

            public int FairyCount => _fairyCount;
            public float FairyOrbitSpeed => _fairyOrbitSpeed;
            public int MeteorCount => _meteorCount;
        }

        [Header("바퀴별")]
        [Tooltip("바퀴별 요정 수·속도와 메테오 수. 1번 칸이 첫 바퀴다. 칸보다 많은 바퀴는 마지막 칸을 쓴다")]
        [SerializeField] private Tier[] _tiers = { new Tier() };

        [Header("화염 영역")]
        [Tooltip("영역 반경 (유닛). 날아오던 폭탄은 이 원에 닿는 순간 터지지 않고 타 버리고, 플레이어는 닿으면 죽는다")]
        [SerializeField, Min(0f)] private float _fieldRadius = 3f;

        [Tooltip("영역이 0에서 다 펼쳐지기까지 걸리는 시간 (초). 등장할 때와 그로기가 끝나 다시 펼칠 때 쓴다. 퍼지는 모습이 곧 예고다")]
        [SerializeField, Min(0.05f)] private float _fieldGrowTime = 1f;

        [Tooltip("플레이어 사망 판정을 보이는 영역보다 안쪽으로 줄이는 거리 (유닛). 판정이 플레이어 몸 테두리 기준이고 그림 가장자리 불꽃이 옅어서, 닿지 않았는데 죽는 느낌을 없앤다. 폭탄이 타는 경계는 그대로 보이는 크기다")]
        [SerializeField, Min(0f)] private float _fieldKillInset = 0.5f;

        [Tooltip("영역 그림이 도는 빠르기 (도/초). 양수면 반시계 방향. 판정과 상관없는 그림 연출")]
        [SerializeField] private float _fieldSpinSpeed = 30f;

        [Header("요정")]
        [Tooltip("요정이 도는 원의 반경 (유닛). 영역 안에 들어간 폭탄은 타 버리므로, 폭발 반경이 영역 밖에서 닿도록 영역보다 넉넉히 커야 한다")]
        [SerializeField, Min(0f)] private float _fairyOrbitRadius = 3.8f;

        [Tooltip("요정 하나가 잡힐 때마다 남은 요정의 회전 속도 증가량 (도/초). 잡을 때마다 방향이 뒤집히고 이만큼 빨라져 박자를 다시 읽어야 한다")]
        [SerializeField, Min(0f)] private float _fairySpeedUpPerKill = 20f;

        [Tooltip("방향이 뒤집히기 전 남은 요정이 제자리에서 떨며 멈칫하는 시간 (초). 반전 예고")]
        [SerializeField, Min(0f)] private float _fairyReverseHold = 0.35f;

        [Header("그로기")]
        [Tooltip("그로기 시간 (초). 이 동안 영역이 없고 한 방에 잡힌다. 못 잡으면 영역과 요정이 다시 생긴다")]
        [SerializeField, Min(0.1f)] private float _groggyDuration = 5f;

        [Header("패턴 공통")]
        [Tooltip("패턴 사이 쉬는 시간 (초). 회복이 끝난 뒤 이만큼 서 있다가 다음 패턴을 쓴다. 메테오와 화염구를 번갈아 쓴다")]
        [SerializeField, Min(0f)] private float _patternInterval = 1.5f;

        [Header("메테오")]
        [Tooltip("메테오 시전 동작 시간 (초). 이 뒤부터 하나씩 떨어지기 시작한다")]
        [SerializeField, Min(0f)] private float _meteorCastTime = 0.5f;

        [Tooltip("메테오 사이 간격 (초). 하나씩 그 순간 플레이어 위치를 노린다")]
        [SerializeField, Min(0.01f)] private float _meteorInterval = 0.25f;

        [Tooltip("메테오 예고 시간 (초). 바닥 원이 차오르고 다 차는 순간 떨어진다. 이 동안 플레이어가 걷는 거리(이동 속도 × 시간)가 반경보다 길어야 일자로 뛰어 피할 수 있다")]
        [SerializeField, Min(0.1f)] private float _meteorFallTime = 0.9f;

        [Tooltip("메테오 피해 반경 (유닛). 구조물과 상관없이 원 안이면 맞는다")]
        [SerializeField, Min(0f)] private float _meteorRadius = 1.3f;

        [Header("화염구")]
        [Tooltip("화염구 차징 시간 (초). 이 동안 조준선이 플레이어를 따라온다")]
        [SerializeField, Min(0.1f)] private float _fireballChargeTime = 2f;

        [Tooltip("조준 고정 시간 (초). 발사 이 시간 전부터 조준이 멈춘다. 0이면 발사 순간까지 따라와 걸어서는 피할 수 없다")]
        [SerializeField, Min(0f)] private float _fireballAimLockTime;

        [Tooltip("화염구 속도 (유닛/초). 걸어서는 못 피하게 빠르게 둔다. 벽 뒤에 숨거나 구르기 무적으로 피한다")]
        [SerializeField, Min(0f)] private float _fireballSpeed = 35f;

        [Tooltip("화염구 판정 반경 (유닛). 조준선 굵기도 이 크기로 그린다")]
        [SerializeField, Min(0f)] private float _fireballHitRadius = 0.4f;

        [Tooltip("화염구 최대 비행 거리 (유닛). 구조물에 막히면 그 지점에서 사라지고 조준선도 그 길이로 잘린다")]
        [SerializeField, Min(0f)] private float _fireballRange = 40f;

        [Tooltip("한 번 차징에 연달아 쏘는 화염구 수 (발). 첫 발은 조준선 그대로, 나머지는 조준선에서 흩어진 각도로 나간다")]
        [SerializeField, Min(1)] private int _fireballCount = 4;

        [Tooltip("연사 간격 (초). 화염구 사이 시간")]
        [SerializeField, Min(0f)] private float _fireballBurstInterval = 0.1f;

        [Tooltip("두 번째 발부터 조준선에서 흩어지는 최대 각도 (도). 이 범위 안에서 발마다 무작위로 정한다")]
        [SerializeField, Min(0f)] private float _fireballSpread = 5f;

        [Header("사운드")]
        [SerializeField, Tooltip("메테오 시전을 시작하는 순간의 효과음")] private SoundData _meteorCastSound;
        [SerializeField, Tooltip("메테오가 떨어지는 순간의 효과음")] private SoundData _meteorImpactSound;
        [SerializeField, Tooltip("화염구 차징 동안의 효과음. 발사하거나 끊기면 멈춘다")] private SoundData _fireballChargeSound;
        [SerializeField, Tooltip("화염구를 쏘는 순간의 효과음")] private SoundData _fireballShotSound;
        [SerializeField, Tooltip("요정을 다 잡아 영역이 풀리는 순간의 효과음")] private SoundData _fieldBreakSound;
        [SerializeField, Tooltip("그로기에 빠지는 순간의 효과음")] private SoundData _groggySound;
        [SerializeField, Tooltip("폭탄이 영역에 닿아 타 버리는 순간의 효과음")] private SoundData _bombBurnSound;
        [SerializeField, Tooltip("실드에 폭탄이 막혔을 때의 효과음")] private SoundData _immuneSound;

        [Header("연출")]
        [SerializeField, Tooltip("메테오가 떨어지는 순간의 연출 (흔들림, 불꽃)")] private FeedbackData _meteorImpactFeedback;
        [SerializeField, Tooltip("영역이 풀리는 순간의 연출")] private FeedbackData _fieldBreakFeedback;
        [SerializeField, Tooltip("폭탄이 영역에 닿아 타 버리는 순간의 연출 (불타는 파티클)")] private FeedbackData _bombBurnFeedback;
        [SerializeField, Tooltip("실드에 폭탄이 막혔을 때의 연출")] private FeedbackData _immuneFeedback;
        [SerializeField, Tooltip("보스를 잡은 순간의 연출 (슬로모션)")] private FeedbackData _defeatFeedback;
        [SerializeField, Tooltip("실드에 막혔을 때 번쩍이는 색")] private Color _immuneFlashColor = new Color(1f, 0.7f, 0.3f, 1f);
        [SerializeField, Min(0f), Tooltip("번쩍임 시간 (초)")] private float _immuneFlashDuration = 0.2f;

        public float FieldRadius => _fieldRadius;
        public float FieldGrowTime => _fieldGrowTime;
        public float FieldSpinSpeed => _fieldSpinSpeed;
        public float FieldKillInset => _fieldKillInset;
        public SoundData BombBurnSound => _bombBurnSound;
        public FeedbackData BombBurnFeedback => _bombBurnFeedback;
        public float FairyOrbitRadius => _fairyOrbitRadius;
        public float FairySpeedUpPerKill => _fairySpeedUpPerKill;
        public float FairyReverseHold => _fairyReverseHold;
        public float GroggyDuration => _groggyDuration;
        public float PatternInterval => _patternInterval;
        public float MeteorCastTime => _meteorCastTime;
        public float MeteorInterval => _meteorInterval;
        public float MeteorFallTime => _meteorFallTime;
        public float MeteorRadius => _meteorRadius;
        public float FireballChargeTime => _fireballChargeTime;
        public float FireballAimLockTime => _fireballAimLockTime;
        public float FireballSpeed => _fireballSpeed;
        public float FireballHitRadius => _fireballHitRadius;
        public float FireballRange => _fireballRange;
        public int FireballCount => _fireballCount;
        public float FireballBurstInterval => _fireballBurstInterval;
        public float FireballSpread => _fireballSpread;
        public SoundData MeteorCastSound => _meteorCastSound;
        public SoundData MeteorImpactSound => _meteorImpactSound;
        public SoundData FireballChargeSound => _fireballChargeSound;
        public SoundData FireballShotSound => _fireballShotSound;
        public SoundData FieldBreakSound => _fieldBreakSound;
        public SoundData GroggySound => _groggySound;
        public SoundData ImmuneSound => _immuneSound;
        public FeedbackData MeteorImpactFeedback => _meteorImpactFeedback;
        public FeedbackData FieldBreakFeedback => _fieldBreakFeedback;
        public FeedbackData ImmuneFeedback => _immuneFeedback;
        public FeedbackData DefeatFeedback => _defeatFeedback;
        public Color ImmuneFlashColor => _immuneFlashColor;
        public float ImmuneFlashDuration => _immuneFlashDuration;

        public Tier GetTier(int tier)
        {
            return _tiers[Mathf.Clamp(tier, 0, _tiers.Length - 1)];
        }

        // 메테오를 미리 만들어 둘 수. 한 번에 떨어지는 수가 가장 많은 바퀴 기준이다.
        // 요정은 판마다 미리 만들어 두므로 가장 많은 바퀴 기준으로 만든다.
        public int MaxFairyCount
        {
            get
            {
                int max = 1;
                for (int i = 0; i < _tiers.Length; i++)
                {
                    max = Mathf.Max(max, _tiers[i].FairyCount);
                }

                return max;
            }
        }

        public int MaxMeteorCount
        {
            get
            {
                int max = 0;
                for (int i = 0; i < _tiers.Length; i++)
                {
                    max = Mathf.Max(max, _tiers[i].MeteorCount);
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
