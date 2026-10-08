using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 최종 보스(각성한 왕) 수치. 왕관이 머리 위에 떠 있는 동안은 무적이고 폭탄을 보면 굴러서 피한다.
    // 왕관 부메랑으로 왕관이 날아가 있는 동안만 맞는다. 세 번 맞으면 그로기, 그로기 때 한 번 더 맞히면 쓰러진다.
    [CreateAssetMenu(fileName = "Boss_AwakenedKing", menuName = "Explode It/Enemies/Awakened King Data")]
    public class AwakenedKingData : EnemyData
    {
        [Header("체력")]
        [Tooltip("왕관을 벗겨 맞혀야 하는 횟수 (번). 이만큼 맞으면 그로기에 빠지고, 그로기 때 한 번 더 맞히면 쓰러진다")]
        [SerializeField, Min(1)] private int _crownHits = 3;

        [Tooltip("맞은 뒤 왕관이 다시 생겨 무적이 되기까지 (초). 이 동안 비틀거린 뒤 다음 국면으로 넘어간다")]
        [SerializeField, Min(0f)] private float _crownRegainTime = 0.2f;

        [Tooltip("그로기 시간 (초). 왕관 없이 쓰러져 있어 한 방에 잡힌다. 못 잡으면 왕관이 다시 생긴다")]
        [SerializeField, Min(0.1f)] private float _groggyDuration = 5f;

        [Header("등장")]
        [Tooltip("2등신 왕이 붉게 달아올라 떠는 시간 (초). 끝나는 순간 펑 하고 각성 모습으로 바뀐다. 보스가 나타나는 대화 줄의 멈춤 시간보다 짧게 둔다")]
        [SerializeField, Min(0.1f)] private float _transformDuration = 1.4f;

        [Tooltip("등장 동작: 왕관이 머리 위로 내려와 빛나기까지 (초). 끝나면 조작이 돌아온다")]
        [SerializeField, Min(0.05f)] private float _crownSummonDuration = 0.6f;

        [Header("이동")]
        [Tooltip("패턴 사이에 플레이어와 유지하려는 거리 (유닛). 이보다 멀면 다가온다")]
        [SerializeField, Min(0f)] private float _preferredDistance = 5f;

        [Tooltip("패턴 사이 쉬는 시간 (초). 이동 상태로 이만큼 지나면 다음 패턴을 시작한다")]
        [SerializeField, Min(0f)] private float _patternInterval = 1f;

        [Tooltip("패턴 시작 자세 (초). 왕관 부메랑을 뺀 패턴이 시작되기 전 잠깐 멈춰 기합을 넣는 시간")]
        [SerializeField, Min(0.05f)] private float _patternStartup = 0.3f;

        [Header("구르기")]
        [Tooltip("폭탄 범위에서 이만큼 더 바깥까지 닿아도 피한다 (유닛)")]
        [SerializeField, Min(0f)] private float _dodgeMargin = 0.4f;

        [Tooltip("웅크렸다가 구르기 시작하기까지 (초)")]
        [SerializeField, Min(0f)] private float _dodgeWindup = 0.08f;

        [Tooltip("구르는 거리 (유닛). 폭탄 중심 반대쪽으로 구르고, 벽이 있으면 그 앞에서 멈춘다")]
        [SerializeField, Min(0f)] private float _dodgeDistance = 3.5f;

        [Tooltip("구르는 시간 (초)")]
        [SerializeField, Min(0.05f)] private float _dodgeDuration = 0.25f;

        [Header("왕검 연속 베기")]
        [Tooltip("한 패턴에 베는 횟수 (번)")]
        [SerializeField, Min(1)] private int _slashCount = 3;

        [Tooltip("베기 사이 플레이어에게 일직선으로 파고드는 속도 (유닛/초). 파고들기 시작할 때 방향을 고정하고, 벨 거리에 닿으면 멈춰 벤다")]
        [SerializeField, Min(0.1f)] private float _slashDashSpeed = 22f;

        [Tooltip("이 거리까지 파고들어 멈춘 뒤 벤다 (유닛). 이미 이 안이면 파고들지 않고 바로 벤다")]
        [SerializeField, Min(0f)] private float _slashReach = 2f;

        [Tooltip("베기 예고 시간 (초). 부채꼴 범위가 차오르고, 다 차는 순간 벤다")]
        [SerializeField, Min(0.05f)] private float _slashWindup = 0.4f;

        [Tooltip("베기 반경 (유닛)")]
        [SerializeField, Min(0f)] private float _slashRadius = 2.8f;

        [Tooltip("베기 각도 (도)")]
        [SerializeField, Range(10f, 360f)] private float _slashAngle = 130f;

        [Tooltip("베고 다음 베기까지 쉬는 시간 (초)")]
        [SerializeField, Min(0f)] private float _slashPause = 0.15f;

        [Tooltip("베는 동작 그림 한 장을 보여 주는 시간 (초). 베는 순간부터 그림을 차례로 넘기고 마지막 그림에서 멈춘다. 장수 × 이 값이 쉬는 시간보다 짧아야 끝까지 보인다")]
        [SerializeField, Min(0.01f)] private float _slashFrameTime = 0.04f;

        [Header("돌진 찌르기")]
        [Tooltip("한 패턴에 돌진하는 횟수 (번)")]
        [SerializeField, Min(1)] private int _lungeCount = 3;

        [Tooltip("첫 돌진 전 플레이어 반대쪽으로 물러나는 거리 (유닛). 멀리서 돌진해 오게 한다")]
        [SerializeField, Min(0f)] private float _backstepDistance = 6f;

        [Tooltip("물러나는 시간 (초)")]
        [SerializeField, Min(0.05f)] private float _backstepDuration = 0.35f;

        [Tooltip("돌진 예고 시간 (초). 예고선이 플레이어를 따라가다 고정되고, 다 되면 돌진한다")]
        [SerializeField, Min(0.05f)] private float _lungeAimTime = 0.6f;

        [Tooltip("예고선을 고정하는 시간 (초). 예고 마지막 이만큼은 방향이 움직이지 않는다")]
        [SerializeField, Min(0f)] private float _lungeLockTime = 0.25f;

        [Tooltip("돌진 속도 (유닛/초)")]
        [SerializeField, Min(0f)] private float _lungeSpeed = 24f;

        [Tooltip("플레이어 위치를 이만큼 지나쳐서 멈춘다 (유닛)")]
        [SerializeField, Min(0f)] private float _lungeOvershoot = 3f;

        [Tooltip("최대 돌진 거리 (유닛). 돌진은 벽을 뚫고 지나간다. 멈출 자리가 벽 속이나 맵 밖이면 그 앞으로 당겨지고 예고선도 그 길이로 잘린다")]
        [SerializeField, Min(0f)] private float _lungeMaxDistance = 14f;

        [Tooltip("돌진 판정 폭 (유닛). 예고선 굵기와 같다")]
        [SerializeField, Min(0f)] private float _lungeWidth = 1.4f;

        [Tooltip("돌진 사이 쉬는 시간 (초)")]
        [SerializeField, Min(0f)] private float _lungePause = 0.2f;

        [Header("왕관 부메랑 (유일한 공격 기회)")]
        [Tooltip("던지기 예고 시간 (초). 던질 방향 예고선이 보인다")]
        [SerializeField, Min(0.05f)] private float _crownWindup = 0.6f;

        [Tooltip("왕관이 날아가는 속도 (유닛/초). 벽을 뚫고 간다")]
        [SerializeField, Min(0.1f)] private float _crownSpeed = 10f;

        [Tooltip("왕관이 날아가는 거리 (유닛). 끝에서 잠깐 머물렀다가 왕에게 돌아온다. 날아가 있는 시간 ≈ 거리×2÷속도 + 머무는 시간")]
        [SerializeField, Min(0f)] private float _crownDistance = 12f;

        [Tooltip("끝에서 머무는 시간 (초)")]
        [SerializeField, Min(0f)] private float _crownHangTime = 0.5f;

        [Tooltip("날아가는 왕관의 판정 반경 (유닛)")]
        [SerializeField, Min(0f)] private float _crownHitRadius = 0.6f;

        [Tooltip("왕관이 날아가 있는 동안 왕이 플레이어 쪽으로 걷는 속도 (유닛/초). 0이면 제자리에 선다")]
        [SerializeField, Min(0f)] private float _crownlessWalkSpeed = 1.2f;

        [Header("사운드")]
        [SerializeField, Tooltip("붉게 달아오르기 시작하는 순간의 효과음")] private SoundData _transformChargeSound;
        [SerializeField, Tooltip("펑 하고 각성 모습으로 바뀌는 순간의 효과음")] private SoundData _transformSound;
        [SerializeField, Tooltip("구르는 순간의 효과음")] private SoundData _dodgeSound;
        [SerializeField, Tooltip("베는 순간의 효과음")] private SoundData _slashSound;
        [SerializeField, Tooltip("돌진하는 순간의 효과음")] private SoundData _lungeSound;
        [SerializeField, Tooltip("왕관을 던지는 순간의 효과음")] private SoundData _crownThrowSound;
        [SerializeField, Tooltip("왕관을 다시 받는 순간의 효과음")] private SoundData _crownCatchSound;
        [SerializeField, Tooltip("왕관 없이 맞은 순간의 효과음")] private SoundData _hurtSound;
        [SerializeField, Tooltip("그로기에 빠지는 순간의 효과음")] private SoundData _groggySound;
        [SerializeField, Tooltip("왕관에 막힌 순간의 효과음")] private SoundData _immuneSound;

        [Header("연출")]
        [SerializeField, Tooltip("펑 하고 각성 모습으로 바뀌는 순간의 연출 (흔들림, 히트스톱, 폭발). 그림 한가운데서 터진다")] private FeedbackData _transformFeedback;
        [SerializeField, Tooltip("달아오르는 동안 몸 둘레에 도는 빛 색")] private Color _transformGlowColor = new Color(1f, 0.15f, 0.1f, 1f);
        [SerializeField, Tooltip("왕관 없이 맞은 순간의 연출")] private FeedbackData _hurtFeedback;
        [SerializeField, Tooltip("왕관에 막혔을 때의 연출")] private FeedbackData _immuneFeedback;
        [SerializeField, Tooltip("쓰러뜨린 순간의 연출 (슬로모션)")] private FeedbackData _defeatFeedback;
        [SerializeField, Tooltip("왕관에 막혔을 때 번쩍이는 색")] private Color _immuneFlashColor = new Color(1f, 0.9f, 0.4f, 1f);
        [SerializeField, Min(0f), Tooltip("번쩍임 시간 (초)")] private float _immuneFlashDuration = 0.2f;

        public int CrownHits => _crownHits;
        public float CrownRegainTime => _crownRegainTime;
        public float GroggyDuration => _groggyDuration;
        public float TransformDuration => _transformDuration;
        public float CrownSummonDuration => _crownSummonDuration;
        public Color TransformGlowColor => _transformGlowColor;
        public SoundData TransformChargeSound => _transformChargeSound;
        public float PreferredDistance => _preferredDistance;
        public float PatternInterval => _patternInterval;
        public float PatternStartup => _patternStartup;
        public float DodgeMargin => _dodgeMargin;
        public float DodgeWindup => _dodgeWindup;
        public float DodgeDistance => _dodgeDistance;
        public float DodgeDuration => _dodgeDuration;
        public int SlashCount => _slashCount;
        public float SlashDashSpeed => _slashDashSpeed;
        public float SlashReach => _slashReach;
        public float SlashWindup => _slashWindup;
        public float SlashRadius => _slashRadius;
        public float SlashAngle => _slashAngle;
        public float SlashPause => _slashPause;
        public float SlashFrameTime => _slashFrameTime;
        public int LungeCount => _lungeCount;
        public float BackstepDistance => _backstepDistance;
        public float BackstepDuration => _backstepDuration;
        public float LungeAimTime => _lungeAimTime;
        public float LungeLockTime => _lungeLockTime;
        public float LungeSpeed => _lungeSpeed;
        public float LungeOvershoot => _lungeOvershoot;
        public float LungeMaxDistance => _lungeMaxDistance;
        public float LungeWidth => _lungeWidth;
        public float LungePause => _lungePause;
        public float CrownWindup => _crownWindup;
        public float CrownSpeed => _crownSpeed;
        public float CrownDistance => _crownDistance;
        public float CrownHangTime => _crownHangTime;
        public float CrownHitRadius => _crownHitRadius;
        public float CrownlessWalkSpeed => _crownlessWalkSpeed;
        public SoundData TransformSound => _transformSound;
        public SoundData DodgeSound => _dodgeSound;
        public SoundData SlashSound => _slashSound;
        public SoundData LungeSound => _lungeSound;
        public SoundData CrownThrowSound => _crownThrowSound;
        public SoundData CrownCatchSound => _crownCatchSound;
        public SoundData HurtSound => _hurtSound;
        public SoundData GroggySound => _groggySound;
        public SoundData ImmuneSound => _immuneSound;
        public FeedbackData TransformFeedback => _transformFeedback;
        public FeedbackData HurtFeedback => _hurtFeedback;
        public FeedbackData ImmuneFeedback => _immuneFeedback;
        public FeedbackData DefeatFeedback => _defeatFeedback;
        public Color ImmuneFlashColor => _immuneFlashColor;
        public float ImmuneFlashDuration => _immuneFlashDuration;
    }
}
