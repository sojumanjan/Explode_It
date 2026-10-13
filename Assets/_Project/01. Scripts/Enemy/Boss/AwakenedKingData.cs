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


        [Tooltip("그로기 시간 (초). 왕관 없이 쓰러져 있어 한 방에 잡힌다. 못 잡으면 왕관이 다시 생긴다")]
        [SerializeField, Min(0.1f)] private float _groggyDuration = 5f;

        [Header("피격·국면 전환")]
        [Tooltip("왕관 없이 맞은 순간 몸이 번쩍이는 색")]
        [SerializeField] private Color _hurtFlashColor = Color.white;

        [Tooltip("맞은 순간 번쩍이는 시간 (초)")]
        [SerializeField, Min(0f)] private float _hurtFlashDuration = 0.15f;

        [Tooltip("맞은 순간 폭탄 반대쪽으로 밀려나는 거리 (유닛). 벽이 있으면 그 앞에서 멈춘다")]
        [SerializeField, Min(0f)] private float _knockbackDistance = 1.2f;

        [Tooltip("밀려나는 시간 (초)")]
        [SerializeField, Min(0.01f)] private float _knockbackDuration = 0.25f;

        [Tooltip("국면 전환 전체 시간 (초). 맞은 순간부터 왕관이 머리로 돌아와 다시 싸우기까지. 이 동안 왕은 무적이고 공격하지 않는다")]
        [SerializeField, Min(0.1f)] private float _phaseShiftDuration = 2.2f;

        [Tooltip("맞은 뒤 분노 대사가 뜨기까지 (초). 밀려나는 시간보다 길게 두어야 말풍선이 제자리에 뜬다")]
        [SerializeField, Min(0f)] private float _phaseLineDelay = 0.35f;

        [Tooltip("국면이 바뀔 때 왕이 외치는 대사. 첫 번째 칸은 처음 맞았을 때, 두 번째 칸은 두 번째로 맞았을 때. 비워 두면 대사 없이 넘어간다")]
        [SerializeField, TextArea(1, 2)] private string[] _phaseLines = { "감히 짐의 왕관을…!", "이 무엄한 고블린 녀석이!!" };

        [Tooltip("국면 전환 끝에 바닥에 떨어진 왕관이 머리 위로 날아 돌아오는 시간 (초)")]
        [SerializeField, Min(0.05f)] private float _crownReturnTime = 0.45f;

        [Tooltip("국면 전환 동안 카메라 확대 배율. 1이면 평소, 작을수록 왕에게 가까이 다가간다")]
        [SerializeField, Range(0.3f, 1f)] private float _phaseZoom = 0.8f;

        [Tooltip("국면 전환이 시작될 때 카메라가 왕에게 다가가는 시간 (초). 끝나고 돌아갈 때는 '확대를 풀고 돌아가는 시간'을 쓴다")]
        [SerializeField, Min(0f)] private float _phaseZoomInTime = 0.3f;

        [Tooltip("국면 전환 동안 플레이어 조작을 막는다. 끄면 연출 중에도 움직일 수 있다(왕은 그래도 무적)")]
        [SerializeField] private bool _locksPlayerOnPhaseShift = true;

        [Tooltip("국면 전환에서 분노하며 떠는 동안 튀는 불티 개수 배율. 처음 각성할 때(1배)보다 많게 둔다")]
        [SerializeField, Min(0f)] private float _phaseAuraRateMultiplier = 2f;

        [Tooltip("국면 전환에서 불티가 튀는 범위 배율. 처음 각성할 때(1배) 범위에 곱한다")]
        [SerializeField, Min(0.1f)] private float _phaseAuraRangeMultiplier = 1.5f;

        [Header("분노 오라 (2국면부터)")]
        [Tooltip("몸 둘레에서 일렁이며 피어오르는 가는 불꽃 수 (개/초)")]
        [SerializeField, Min(0f)] private float _rageFlameRate = 18f;

        [Tooltip("몸 둘레에서 위로 날리는 불티 수 (개/초)")]
        [SerializeField, Min(0f)] private float _rageEmberRate = 30f;

        [Header("등장")]
        [Tooltip("2등신 왕이 불티 속에서 떠는 시간 (초). 끝나면 쏙 줄어들었다가 팍 커지며 각성 모습으로 바뀐다. 보스가 나타나는 대화 줄의 멈춤 시간보다 짧게 둔다")]
        [SerializeField, Min(0.1f)] private float _transformDuration = 1.4f;

        [Tooltip("변신하는 동안 카메라 확대 배율. 1이면 평소, 작을수록 왕에게 가까이 다가간다 (0.65 = 화면이 0.65배로 좁아짐)")]
        [SerializeField, Range(0.3f, 1f)] private float _transformZoom = 0.65f;

        [Tooltip("변신이 시작될 때 카메라가 왕에게 다가가는 시간 (초)")]
        [SerializeField, Min(0f)] private float _zoomInTime = 0.8f;

        [Tooltip("각성 모습으로 바뀐 뒤 확대한 채 머무는 시간 (초)")]
        [SerializeField, Min(0f)] private float _zoomHoldTime = 0.4f;

        [Tooltip("확대를 풀고 원래 화면으로 스윽 돌아가는 시간 (초)")]
        [SerializeField, Min(0f)] private float _zoomOutTime = 1f;

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

        [Header("도약 내려찍기 (2국면부터, 왕관을 쓴 채라 무적)")]
        [Tooltip("한 번 패턴에 연달아 뛰는 횟수 (번)")]
        [SerializeField, Min(1)] private int _leapCount = 2;

        [Tooltip("땅을 박차고 화면 위로 솟구치는 시간 (초). 이때 바닥에 그림자가 생긴다")]
        [SerializeField, Min(0.05f)] private float _leapRiseTime = 0.35f;

        [Tooltip("공중에서 그림자가 플레이어를 따라가는 시간 (초)")]
        [SerializeField, Min(0f)] private float _leapTrackTime = 0.8f;

        [Tooltip("그림자가 플레이어를 따라가는 속도 (유닛/초). 플레이어 이동 속도보다 느리면 계속 걸어 따돌릴 수 있다")]
        [SerializeField, Min(0f)] private float _leapTrackSpeed = 9f;

        [Tooltip("그림자가 멈춘 뒤 착지까지 (초). 이 동안 빨간 원이 차오르고, 다 차는 순간 내려찍는다. 폭탄 지연(1초)과 같게 두면 멈춘 순간 그 자리에 던진 폭탄이 착지에 맞춰 터진다")]
        [SerializeField, Min(0.1f)] private float _leapLockTime = 1f;

        [Tooltip("원이 다 차기 직전 하늘에서 떨어지는 시간 (초). 고정 시간 안에 포함된다")]
        [SerializeField, Min(0.01f)] private float _leapFallTime = 0.12f;

        [Tooltip("내려찍기 판정 반경 (유닛)")]
        [SerializeField, Min(0.1f)] private float _leapRadius = 2.2f;

        [Tooltip("착지한 뒤 멈춰 있는 시간 (초). 지나면 다음 도약을 하거나 패턴을 마친다")]
        [SerializeField, Min(0f)] private float _leapLandTime = 0.5f;

        [Header("왕관 투하 (2국면부터, 유일한 공격 기회)")]
        [Tooltip("왕관을 머리 위로 높이 들어 올리는 예고 시간 (초)")]
        [SerializeField, Min(0.05f)] private float _crownDropWindup = 0.5f;

        [Tooltip("왕관을 하늘로 던져 올리는 시간 (초). 던지는 순간부터 왕관이 없어 맞는다")]
        [SerializeField, Min(0.05f)] private float _crownDropThrowTime = 0.3f;

        [Tooltip("떨어질 자리(큰 그림자)가 플레이어를 따라가는 시간 (초)")]
        [SerializeField, Min(0f)] private float _crownDropTrackTime = 0.5f;

        [Tooltip("떨어질 자리가 플레이어를 따라가는 속도 (유닛/초)")]
        [SerializeField, Min(0f)] private float _crownDropTrackSpeed = 10f;

        [Tooltip("떨어질 자리가 멈춘 뒤 왕관이 박히기까지 (초). 이 동안 금빛 원이 차오른다")]
        [SerializeField, Min(0.1f)] private float _crownDropLockTime = 1f;

        [Tooltip("원이 다 차기 직전 왕관이 하늘에서 떨어지는 시간 (초). 고정 시간 안에 포함된다")]
        [SerializeField, Min(0.01f)] private float _crownDropFallTime = 0.2f;

        [Tooltip("왕관이 박히는 판정 반경 (유닛)")]
        [SerializeField, Min(0.1f)] private float _crownDropRadius = 2.5f;

        [Tooltip("왕관이 박힌 뒤 왕이 휘청이다가 주우러 달리기 시작하기까지 (초)")]
        [SerializeField, Min(0f)] private float _crownDropStagger = 0.3f;

        [Tooltip("왕관을 주우러 달리는 속도 (유닛/초). 가장 짧은 길로 달려가므로, 느릴수록 맞힐 시간이 길다")]
        [SerializeField, Min(0.1f)] private float _crownRetrieveSpeed = 6f;

        [Tooltip("달리기 시작한 뒤 이 시간 (초) 안에 못 주우면 왕관이 스스로 날아 돌아온다")]
        [SerializeField, Min(0.1f)] private float _crownRetrieveTimeout = 4f;

        [Header("유성우 (3국면부터, 왕관을 쓴 채라 무적)")]
        [Tooltip("한 번에 떨어지는 유성 수 (개). 맵 전체에 흩어진다")]
        [SerializeField, Min(1)] private int _meteorCount = 45;

        [Tooltip("유성을 다 불러내는 데 걸리는 시간 (초). 이 동안 일정한 간격으로 하나씩 떨어질 자리가 생긴다")]
        [SerializeField, Min(0.1f)] private float _meteorCastTime = 3f;

        [Tooltip("떨어질 자리가 생긴 뒤 유성이 떨어지기까지 (초). 이 동안 보라 원이 차오른다")]
        [SerializeField, Min(0.2f)] private float _meteorFallTime = 1f;

        [Tooltip("유성 하나의 판정 반경 (유닛)")]
        [SerializeField, Min(0.1f)] private float _meteorRadius = 1.5f;

        [Tooltip("유성 중 그 순간 플레이어 자리에 떨어지는 수 (개). 가만히 서서 다 피하지 못하게 한다. 소환 시간에 고르게 나눠 떨어진다")]
        [SerializeField, Min(0)] private int _meteorTargetedCount = 3;

        [Tooltip("유성이 맵 가장자리에서 떨어지는 거리 (유닛). 원이 맵 밖으로 잘리지 않게 한다")]
        [SerializeField, Min(0f)] private float _meteorEdgeMargin = 1f;

        [Tooltip("유성우 동안 맵이 물드는 색 (불투명도는 아래 칸으로 정한다)")]
        [SerializeField] private Color _meteorTintColor = new Color(0.45f, 0.15f, 0.75f, 1f);

        [Tooltip("유성우 동안 맵이 물드는 진하기 (0~1). 진하면 예고 원이 묻힌다")]
        [SerializeField, Range(0f, 1f)] private float _meteorTintAlpha = 0.25f;

        [Tooltip("맵이 물들고 돌아오는 시간 (초)")]
        [SerializeField, Min(0.01f)] private float _meteorTintFadeTime = 0.5f;

        [Header("천지 가르기 (3국면부터, 왕관을 쓴 채라 무적)")]
        [Tooltip("벨 쪽으로 보라 웨이브가 맵 끝까지 퍼지는 시간 (초)")]
        [SerializeField, Min(0.05f)] private float _swordWaveTime = 0.35f;

        [Tooltip("첫 번째 웨이브 뒤 반대쪽 웨이브가 시작되기까지 (초). 웨이브 순서가 곧 베는 순서다")]
        [SerializeField, Min(0f)] private float _swordWaveGap = 0.6f;

        [Tooltip("두 웨이브가 다 퍼진 뒤 첫 베기까지 (초)")]
        [SerializeField, Min(0f)] private float _swordWait = 2f;

        [Tooltip("첫 베기와 두 번째 베기 사이 (초). 이 안에 왕이 선 경계선을 넘어 이미 벤 쪽으로 건너가야 한다")]
        [SerializeField, Min(0.1f)] private float _swordSlashGap = 0.8f;

        [Tooltip("두 번째 베기 뒤 멈춰 있는 시간 (초)")]
        [SerializeField, Min(0f)] private float _swordRecover = 0.6f;

        [Header("3국면 모습")]
        [Tooltip("3국면 머리 위 왕관 색. 이 색과 밝은 색 사이를 맥동한다")]
        [SerializeField] private Color _phase3CrownColor = new Color(0.75f, 0.45f, 1f, 1f);

        [Tooltip("3국면 불티 색 (처음 → 사라질 때)")]
        [SerializeField] private Color _phase3EmberStart = new Color(0.9f, 0.6f, 1f, 1f);
        [SerializeField] private Color _phase3EmberEnd = new Color(0.55f, 0.15f, 0.9f, 1f);

        [Tooltip("3국면으로 넘어가며 분노할 때 튀는 불티 색 (둘 사이에서 무작위). 1·2국면 전환은 원래 붉은색 그대로다")]
        [SerializeField] private Color _phase3ShiftAuraMin = new Color(0.55f, 0.15f, 0.95f, 1f);
        [SerializeField] private Color _phase3ShiftAuraMax = new Color(0.9f, 0.6f, 1f, 1f);

        [Header("사운드")]
        [SerializeField, Tooltip("떨기 시작하는 순간의 효과음")] private SoundData _transformChargeSound;
        [SerializeField, Tooltip("각성 모습으로 바뀌는 순간의 효과음")] private SoundData _transformSound;
        [SerializeField, Tooltip("구르는 순간의 효과음")] private SoundData _dodgeSound;
        [SerializeField, Tooltip("베는 순간의 효과음")] private SoundData _slashSound;
        [SerializeField, Tooltip("돌진하는 순간의 효과음")] private SoundData _lungeSound;
        [SerializeField, Tooltip("왕관을 던지는 순간의 효과음")] private SoundData _crownThrowSound;
        [SerializeField, Tooltip("왕관을 다시 받는 순간의 효과음")] private SoundData _crownCatchSound;
        [SerializeField, Tooltip("왕관 없이 맞은 순간의 효과음")] private SoundData _hurtSound;
        [SerializeField, Tooltip("그로기에 빠지는 순간의 효과음")] private SoundData _groggySound;
        [SerializeField, Tooltip("왕관에 막힌 순간의 효과음")] private SoundData _immuneSound;
        [SerializeField, Tooltip("도약해 솟구치는 순간의 효과음")] private SoundData _leapSound;
        [SerializeField, Tooltip("내려찍는 순간의 효과음")] private SoundData _leapImpactSound;
        [SerializeField, Tooltip("던져 올린 왕관이 땅에 박히는 순간의 효과음")] private SoundData _crownDropImpactSound;
        [SerializeField, Tooltip("유성우를 부르기 시작하는 순간의 효과음")] private SoundData _meteorCastSound;
        [SerializeField, Tooltip("유성 하나가 떨어지는 순간의 효과음")] private SoundData _meteorImpactSound;
        [SerializeField, Tooltip("천지 가르기 예고 웨이브가 퍼지는 순간의 효과음")] private SoundData _swordWaveSound;
        [SerializeField, Tooltip("천지 가르기로 절반을 베는 순간의 효과음")] private SoundData _swordSlashSound;
        [SerializeField, Tooltip("국면 전환 끝에 왕관이 머리로 돌아오는 순간의 효과음. 비우면 왕관을 받는 효과음을 쓴다")] private SoundData _phaseShiftSound;

        [Header("연출")]
        [SerializeField, Tooltip("각성 모습으로 바뀌는 순간의 연출 (흔들림, 히트스톱, 폭발). 그림 한가운데서 터진다")] private FeedbackData _transformFeedback;
        [SerializeField, Tooltip("왕관 없이 맞은 순간의 연출")] private FeedbackData _hurtFeedback;
        [SerializeField, Tooltip("왕관에 막혔을 때의 연출")] private FeedbackData _immuneFeedback;
        [SerializeField, Tooltip("도약 후 내려찍는 순간의 연출 (흔들림, 먼지). 착지 자리에서 터진다")] private FeedbackData _leapImpactFeedback;
        [SerializeField, Tooltip("왕관이 땅에 박히는 순간의 연출 (흔들림, 먼지). 박힌 자리에서 터진다")] private FeedbackData _crownDropImpactFeedback;
        [SerializeField, Tooltip("유성 하나가 떨어지는 순간의 연출. 떨어진 자리에서 터진다")] private FeedbackData _meteorImpactFeedback;
        [SerializeField, Tooltip("천지 가르기로 절반을 베는 순간의 연출 (흔들림). 왕 자리에서 터진다")] private FeedbackData _swordSlashFeedback;
        [SerializeField, Tooltip("국면 전환 끝에 왕관이 머리로 돌아오는 순간의 연출 (흔들림, 파티클). 왕 한가운데서 터진다")] private FeedbackData _phaseShiftFeedback;
        [SerializeField, Tooltip("쓰러뜨린 순간의 연출 (슬로모션)")] private FeedbackData _defeatFeedback;
        [SerializeField, Tooltip("왕관에 막혔을 때 번쩍이는 색")] private Color _immuneFlashColor = new Color(1f, 0.9f, 0.4f, 1f);
        [SerializeField, Min(0f), Tooltip("번쩍임 시간 (초)")] private float _immuneFlashDuration = 0.2f;

        public int CrownHits => _crownHits;
        public Color HurtFlashColor => _hurtFlashColor;
        public float HurtFlashDuration => _hurtFlashDuration;
        public float KnockbackDistance => _knockbackDistance;
        public float KnockbackDuration => _knockbackDuration;
        public float PhaseShiftDuration => _phaseShiftDuration;
        public float PhaseLineDelay => _phaseLineDelay;
        public float CrownReturnTime => _crownReturnTime;
        public float PhaseZoom => _phaseZoom;
        public float PhaseZoomInTime => _phaseZoomInTime;
        public bool LocksPlayerOnPhaseShift => _locksPlayerOnPhaseShift;
        public float PhaseAuraRateMultiplier => _phaseAuraRateMultiplier;
        public float PhaseAuraRangeMultiplier => _phaseAuraRangeMultiplier;
        public float RageFlameRate => _rageFlameRate;
        public float RageEmberRate => _rageEmberRate;
        public SoundData PhaseShiftSound => _phaseShiftSound;
        public FeedbackData PhaseShiftFeedback => _phaseShiftFeedback;

        // 맞은 횟수(1부터)에 맞는 국면 전환 대사. 없으면 null.
        public string GetPhaseLine(int hitsTaken)
        {
            int index = hitsTaken - 1;
            return _phaseLines != null && index >= 0 && index < _phaseLines.Length && !string.IsNullOrEmpty(_phaseLines[index]) ? _phaseLines[index] : null;
        }
        public float GroggyDuration => _groggyDuration;
        public float TransformDuration => _transformDuration;
        public float CrownSummonDuration => _crownSummonDuration;
        public float TransformZoom => _transformZoom;
        public float ZoomInTime => _zoomInTime;
        public float ZoomHoldTime => _zoomHoldTime;
        public float ZoomOutTime => _zoomOutTime;
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
        public int LeapCount => _leapCount;
        public float LeapRiseTime => _leapRiseTime;
        public float LeapTrackTime => _leapTrackTime;
        public float LeapTrackSpeed => _leapTrackSpeed;
        public float LeapLockTime => _leapLockTime;
        public float LeapFallTime => _leapFallTime;
        public float LeapRadius => _leapRadius;
        public float LeapLandTime => _leapLandTime;
        public SoundData LeapSound => _leapSound;
        public SoundData LeapImpactSound => _leapImpactSound;
        public FeedbackData LeapImpactFeedback => _leapImpactFeedback;
        public float CrownDropWindup => _crownDropWindup;
        public float CrownDropThrowTime => _crownDropThrowTime;
        public float CrownDropTrackTime => _crownDropTrackTime;
        public float CrownDropTrackSpeed => _crownDropTrackSpeed;
        public float CrownDropLockTime => _crownDropLockTime;
        public float CrownDropFallTime => _crownDropFallTime;
        public float CrownDropRadius => _crownDropRadius;
        public float CrownDropStagger => _crownDropStagger;
        public float CrownRetrieveSpeed => _crownRetrieveSpeed;
        public float CrownRetrieveTimeout => _crownRetrieveTimeout;
        public SoundData CrownDropImpactSound => _crownDropImpactSound;
        public FeedbackData CrownDropImpactFeedback => _crownDropImpactFeedback;
        public int MeteorCount => _meteorCount;
        public float MeteorCastTime => _meteorCastTime;
        public float MeteorFallTime => _meteorFallTime;
        public float MeteorRadius => _meteorRadius;
        public int MeteorTargetedCount => _meteorTargetedCount;
        public float MeteorEdgeMargin => _meteorEdgeMargin;
        public Color MeteorTintColor => _meteorTintColor;
        public float MeteorTintAlpha => _meteorTintAlpha;
        public float MeteorTintFadeTime => _meteorTintFadeTime;
        public float SwordWaveTime => _swordWaveTime;
        public float SwordWaveGap => _swordWaveGap;
        public float SwordWait => _swordWait;
        public float SwordSlashGap => _swordSlashGap;
        public float SwordRecover => _swordRecover;
        public Color Phase3CrownColor => _phase3CrownColor;
        public Color Phase3EmberStart => _phase3EmberStart;
        public Color Phase3EmberEnd => _phase3EmberEnd;
        public Color Phase3ShiftAuraMin => _phase3ShiftAuraMin;
        public Color Phase3ShiftAuraMax => _phase3ShiftAuraMax;
        public SoundData MeteorCastSound => _meteorCastSound;
        public SoundData MeteorImpactSound => _meteorImpactSound;
        public SoundData SwordWaveSound => _swordWaveSound;
        public SoundData SwordSlashSound => _swordSlashSound;
        public FeedbackData MeteorImpactFeedback => _meteorImpactFeedback;
        public FeedbackData SwordSlashFeedback => _swordSlashFeedback;
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
