using ExplodeIt.Bombs;
using ExplodeIt.Core;
using ExplodeIt.Stage;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 최종 보스(각성한 왕). 2등신 왕이 변신해 싸운다.
    // 머리 위에 왕관이 떠서 빛나는 동안은 무적이고, 근처에 폭탄이 떨어지면 굴러서 피한다(쿨타임 없음).
    // 1국면은 왕검 연속 베기 → 돌진 찌르기 → 왕관 부메랑, 2국면은 왕검 연속 베기 → 도약 내려찍기 → 왕관 투하,
    // 3국면은 유성우 → 천지 가르기 → 별의 의식(왕관을 맵 가운데 띄우고 별 끝을 순간이동, 못 잡으면 맵 전체 충격파)을 차례로 돈다.
    // 왕관이 머리에서 떠나 있는 동안만 맞는다.
    // 세 번 맞으면 그로기에 빠지고, 그로기 때 한 번 더 맞히면 쓰러진다. 맞을 때마다 다음 국면으로 넘어간다(지금은 1국면 패턴만 있다).
    // 국면이 바뀔 때는 밀려나며 왕관을 떨어뜨리고, 분노 대사를 외친 뒤 왕관이 머리로 날아 돌아오면, 각성 때처럼 쏙 줄었다가 팍 커지며
    // 몸 둘레에 가는 불꽃과 불티가 일렁이는 분노 오라가 켜진다.
    // 예측 과제: 왕관이 날아가 있는 짧은 시간 동안 걸어오는 왕의 1초 뒤 자리에 폭탄을 두고, 돌아오는 왕관도 피한다.
    public class AwakenedKingBoss : Enemy, IBoss
    {
        // 그림 전용 연출 값이라 데이터로 빼지 않는다.
        // 다 떤 뒤 2등신이 0으로 줄어드는 시간 (초).
        private const float ShrinkDuration = 0.1f;
        // 떨림이 끝날 때 떨림 소리를 줄여 끊는 시간 (초).
        private const float TransformChargeFadeOut = 0.1f;
        // 각성 모습이 0에서 팍 커지는 시간 (초)과 넘쳤다 돌아오는 세기(OutBack 계수, 1.7이면 약 10% 넘침).
        private const float PopDuration = 0.25f;
        private const float PopOvershoot = 1.7f;
        // 등장 동작에서 왕관이 내려오기 시작하는 높이 (유닛).
        private const float CrownDropHeight = 1.5f;
        // 머리 위 왕관이 위아래로 둥둥 뜨는 폭 (유닛)과 빠르기 (rad/초).
        private const float CrownBobHeight = 0.12f;
        private const float CrownBobSpeed = 3f;
        // 왕관을 다시 받았다고 보는 거리 (유닛).
        private const float CrownCatchDistance = 0.4f;
        // 그로기 직후 같은 폭발이 바로 잡지 못하게 하는 짧은 무적. 한 폭발 안의 판정은 같은 프레임에 끝난다.
        private const float GroggyGrace = 0.1f;
        // 벽을 뚫는 돌진이 멈출 자리를 찾을 때 뒤로 당겨 보는 간격 (유닛).
        private const float LungeSafeStep = 0.25f;
        // 맞아서 떨어진 왕관이 튀어 오르는 높이 (유닛), 바닥까지 떨어지는 거리 (유닛), 떨어지는 시간 (초), 바닥에 누운 기울기 (도).
        private const float CrownDropHop = 0.5f;
        private const float CrownDropFall = 0.6f;
        private const float CrownDropDuration = 0.35f;
        private const float CrownRestAngle = -25f;
        // 왕관이 머리로 돌아올 때 그리는 포물선 높이 (유닛).
        private const float CrownReturnArc = 1.5f;
        // 분노 대사를 외치는 동안 몸이 떠는 폭 (유닛).
        private const float RageJitter = 0.05f;
        // 왕관 투하에서 왕관이 솟구치는 높이 (유닛). 화면 밖으로 사라질 만큼 높게 둔다.
        private const float LeapHeight = 14f;
        // 착지 자리가 구조물에 걸리면 설 수 있는 자리를 찾는 간격과 최대 거리 (유닛), 방향 수.
        private const float LandSearchStep = 0.25f;
        private const float LandSearchMax = 3f;
        private const int LandSearchDirections = 16;
        // 공중에서 그림자가 가장 작을 때의 크기 비율. 왕이 내려올수록 원래 크기로 커진다.
        private const float LeapShadowMinScale = 0.55f;
        // 왕관 투하 예고 때 왕관이 머리 위로 올라가는 높이 (유닛), 던진 왕관이 커지는 배율, 왕관을 주웠다고 보는 거리 (유닛).
        private const float CrownRaiseHeight = 1f;
        private const float BigCrownScale = 2f;
        private const float CrownPickupDistance = 0.6f;
        // 유성우를 부르는 동안 왕 주위 보라 소용돌이 그림이 한 장씩 넘어가는 간격 (초).
        private const float MeteorAuraFrameTime = 0.08f;
        // 천지 가르기 경계선의 두께 (유닛)와 색. 웨이브(밝은 보라)와 구분되게 어두운 보라로 긋는다.
        private const float SwordLineWidth = 0.083f;
        private static readonly Color SwordLineColor = new Color(0.28f, 0.05f, 0.42f, 0.9f);
        // 첫 베기 직전 "곧 내리친다" 기를 모으는 동작: 시작 시점 (첫 베기 몇 초 전), 뒤로 젖히는 각도 (도), 위로 늘어나는 비율, 떨림 폭 (유닛).
        private const float SwordWindupTime = 0.75f;
        private const float SwordWindupLean = -14f;
        private const float SwordWindupStretch = 0.1f;
        private const float SwordWindupJitter = 0.04f;
        // 별 그림에서 가운데부터 꼭짓점까지가 그림 폭에서 차지하는 비율. 그림을 별 크기에 맞출 때 쓴다.
        private const float StarSpriteTipRatio = 0.44f;
        // 별 꼭대기에 걸친 왕관이 꼭짓점보다 위로 뜬 높이 (유닛).
        private const float CrownOnStarHeight = 0.5f;
        // 다음 끝 표시가 맥동하는 빠르기 (rad/초), 맵 밖까지 덮는 충격파 덮개의 여유 (유닛).
        private const float StarTipPulseSpeed = 10f;
        // 질주 잔상: 남기는 간격 (초), 사라지는 시간 (초), 색.
        private const float AfterimageInterval = 0.02f;
        private const float AfterimageFade = 0.3f;
        private static readonly Color AfterimageColor = new Color(0.75f, 0.45f, 1f, 0.6f);
        // 안전지대가 정해진 방향에서 흔들리는 폭 (도).
        private const float SafeZoneJitter = 20f;
        private const float ShockCoverMargin = 40f;
        // 별 다섯 끝을 한붓그리기로 잇는 순서. 순간이동도 이 순서를 따라, 별 획을 보면 다음 자리를 미리 읽을 수 있다.
        private static readonly int[] StarOrder = { 0, 2, 4, 1, 3 };

        private static readonly Collider2D[] StandBuffer = new Collider2D[4];

        private enum Pattern
        {
            Slash,
            Lunge,
            Crown,
            Leap,
            CrownDrop,
            Meteor,
            Sword,
            Star
        }

        private enum Step
        {
            Windup,
            Pause,
            Backstep,
            Aim,
            Dash,
            CrownOut,
            CrownHang,
            CrownBack,
            LeapRise,
            LeapTrack,
            LeapLock,
            LeapLand,
            DropThrow,
            DropTrack,
            DropLock,
            DropStagger,
            DropRetrieve,
            DropReturn,
            MeteorCast,
            MeteorWait,
            SwordCharge,
            SwordSlash,
            StarDraw,
            StarTeleport,
            StarShock
        }

        private enum DodgePhase
        {
            None,
            Crouch,
            Roll
        }

        // 국면별 패턴 순서. 맞을 때마다 다음 국면으로 넘어가고, 마지막 국면 순서를 그 뒤로도 쓴다.
        // 각 국면의 마지막 패턴이 왕관이 머리를 떠나는(맞을 수 있는) 유일한 기회다.
        private static readonly Pattern[][] Rotations =
        {
            new[] { Pattern.Slash, Pattern.Lunge, Pattern.Crown },
            new[] { Pattern.Slash, Pattern.Leap, Pattern.CrownDrop },
            new[] { Pattern.Meteor, Pattern.Sword, Pattern.Star }
        };

        [SerializeField] private AwakenedKingData _data;
        [SerializeField] private EnemyVisual _visual;
        // 머리 위에 떠 있는 왕관. 몸이 웅크리거나 기울어도 흔들리지 않게 그림(Body)이 아닌 루트 자식으로 둔다. 켜져 있으면 무적이다.
        [SerializeField] private SpriteRenderer _crown;
        // 변신 전 2등신 모습. 그림(Body)과 따로 두어 각성 모습과 겹쳐 바꿔 보여 준다.
        [SerializeField] private SpriteRenderer _chibi;
        // 변신하며 떠는 동안 튀는 불티.
        [SerializeField] private ParticleSystem _transformAura;
        // 각성 모습 그림. 국면 전환에서 줄었다 커질 때 그림 한가운데를 잡는 데 쓴다.
        [SerializeField] private SpriteRenderer _bodyRenderer;
        // 2국면부터 켜지는 분노 오라: 몸 둘레에서 일렁이는 가는 불꽃과 위로 날리는 불티.
        [SerializeField] private ParticleSystem _rageFlames;
        [SerializeField] private ParticleSystem _rageEmbers;
        [SerializeField] private ExplosionShape _slashRange;
        [SerializeField] private ExplosionShape _slashFill;
        [SerializeField] private TelegraphLine _aimLine;
        // 던진 왕관. 왕을 따라 움직이면 안 되므로 시작할 때 월드로 뗀다.
        [SerializeField] private Transform _flyingCrown;
        // 동작 그림. 기본 자세는 그림(Body)에 넣어 둔 그림이고, 패턴 중에만 이 그림들로 바꾼다. 비워 두면 기본 그림 그대로다.
        // 베기는 파고들며 자세를 잡는 그림 한 장과, 베는 순간 빠르게 넘기는 동작 그림들(순서대로).
        [SerializeField] private Sprite _slashReadySprite;
        [SerializeField] private Sprite[] _slashFrames;
        [SerializeField] private Sprite _thrustSprite;
        // 도약: 공중에 떠 있는 그림, 플레이어 바로 위에서 수직으로 내리꽂는 그림, 비스듬히 내리꽂는 그림. 비워 두면 기본 그림이다.
        // 그려진 방향은 오른쪽 그림 목록(_rightFacingPoseSprites)으로 판단한다. 대각 그림은 그려진 방향으로 내리꽂는다고 본다.
        [SerializeField] private Sprite _leapHoverSprite;
        [SerializeField] private Sprite _leapVerticalSlamSprite;
        [SerializeField] private Sprite _leapDiagonalSlamSprite;
        // 2국면부터(분노 오라가 켜진 순간부터) 쓰는 살짝 붉어진 그림. 비워 둔 칸은 1국면 그림을 그대로 쓴다.
        [SerializeField] private Sprite _rageIdleSprite;
        [SerializeField] private Sprite _rageSlashReadySprite;
        [SerializeField] private Sprite[] _rageSlashFrames;
        // 3국면 모습(보랏빛). 기본 자세와 패턴 자세.
        [SerializeField] private Sprite _phase3IdleSprite;
        // 그로기로 주저앉은 그림. 쓰러짐 그림은 표시 컴포넌트(EnemyVisual)의 사망 그림 칸에 둔다.
        [SerializeField] private Sprite _stunSprite;
        [SerializeField] private Sprite _meteorPoseSprite;
        // 쓰러진 뒤 그림 중 오른쪽을 보고 그려진 것(무릎 꿇기 등). 나머지(기절·쓰러짐)는 왼쪽을 보고 그려졌다.
        // 쓰러진 뒤에는 이 기준으로 뒤집어 고블린을 보게 한다.
        [SerializeField] private Sprite[] _rightFacingPoseSprites;
        [SerializeField] private Sprite _swordChargeSprite;
        [SerializeField] private Sprite _swordAttackSprite;
        // 유성우를 부르는 동안 왕 주위에 번갈아 보이는 보라 소용돌이 그림들. 그림(Body) 자식이라 몸을 따라 움직인다.
        [SerializeField] private SpriteRenderer _meteorAura;
        [SerializeField] private Sprite[] _meteorAuraFrames;
        // 유성 하나(원이 차오르다 떨어짐). 오브젝트 피커가 컴포넌트 타입 칸에 프리팹을 띄우지 않아 GameObject로 받는다.
        [SerializeField] private GameObject _meteorPrefab;
        // 유성우 동안 맵 전체를 보랏빛으로 덮는 반투명 칸(흰 사각형 그림). 맵 크기에 맞춰 월드에 깐다.
        [SerializeField] private SpriteRenderer _mapTint;
        // 각성하는 순간 왕에게서 맵 끝까지 퍼지는 파동(고리 그림)과, 그 뒤 싸우는 내내 맵을 어둡게 덮는 칸(흰 사각형 그림).
        [SerializeField] private SpriteRenderer _awakenWave;
        [SerializeField] private SpriteRenderer _mapDarken;
        // 천지 가르기 예고. 먼저 벨 쪽과 나중에 벨 쪽.
        [SerializeField] private HalfSlashWarning _halfFirst;
        [SerializeField] private HalfSlashWarning _halfSecond;
        // 줄무늬 가르기 예고. 세로 위험 칸과 가로 위험 칸을 하나씩 맡는다(칸 수의 절반씩, 넉넉히 둔다).
        [SerializeField] private HalfSlashWarning[] _stripeWarnings;
        // 두 절반을 가르는 경계선(흰 사각형 그림). 어디를 넘어야 하는지 헷갈리지 않게 패턴 내내 보인다.
        [SerializeField] private SpriteRenderer _swordLine;
        // 별의 의식: 별 낙인 선, 다섯 끝 표시, 안전지대들. 맵 가운데 고정이라 보스를 따라다니지 않게 시작할 때 월드로 뗀다.
        [SerializeField] private SpriteRenderer _starSigil;
        // 질주 잔상으로 돌려 쓰는 그림들. 몸 그림을 그 순간 그대로 베껴 보랏빛으로 흐려진다.
        [SerializeField] private SpriteRenderer[] _afterimages;
        [SerializeField] private SpriteRenderer[] _starTips;
        [SerializeField] private SpriteRenderer[] _safeZones;
        // 도약 중 바닥에 드리우는 그림자. 왕 발밑(루트 자식)에 두고 도약할 때만 켠다.
        [SerializeField] private SpriteRenderer _leapShadow;
        // 왕관 투하의 떨어질 자리 표시(큰 그림자, 금빛 범위 원과 채움). 왕이 걸어 다녀도 그 자리에 있어야 하므로 시작할 때 월드로 뗀다.
        [SerializeField] private SpriteRenderer _dropShadow;
        [SerializeField] private ExplosionShape _dropRange;
        [SerializeField] private ExplosionShape _dropFill;

        private int _bossNumber;
        private bool _isFighting;
        private bool _hasCrown;
        private bool _isGroggy;
        private int _hitsTaken;
        private int _rotationIndex;
        // 1국면에서 시작한 패턴 수와, 왕관을 노리라는 힌트를 이미 줬는지.
        private int _firstPhasePatterns;
        private bool _isCrownHintShown;
        private Pattern _pattern;
        private float _moveTimer;

        private Step _step;
        private float _stepTime;
        private int _count;
        private Vector2 _aimDirection = Vector2.right;
        private float _lineLength;
        private float _dashRemaining;
        private float _dashSpeed;

        private Vector2 _crownPosition;
        private float _crownTravel;

        private DodgePhase _dodge;
        private float _dodgeTime;
        private Vector2 _dodgeDirection;
        private float _dodgeLength;

        // 각 연출이 시작된 뒤 지난 시간. 음수면 그 연출 중이 아니다.
        private float _transformTime = -1f;
        private float _popTime = -1f;
        private float _summonTime = -1f;
        private bool _hasTransformed;
        private bool _isEntranceRequested;
        private bool _isEntranceDone;
        // 각성한 뒤 확대한 채 머문 시간. 음수면 머무는 중이 아니다.
        private float _zoomHoldTime = -1f;
        private bool _isZoomed;

        // 국면 전환·밀려남·왕관 떨어짐이 시작된 뒤 지난 시간. 음수면 그 연출 중이 아니다.
        private float _shiftTime = -1f;
        private float _knockTime = -1f;
        private float _crownDropTime = -1f;
        private bool _isShoutDone;
        private bool _isCrownReturning;
        private bool _isLockingPlayer;
        private bool _isFocusing;
        private Vector2 _knockFrom;
        private Vector2 _knockTo;
        private Vector2 _crownDropFrom;
        private Vector2 _crownGround;

        // 도약으로 그림이 떠 있는 높이 (유닛). 머리 위 왕관도 같이 띄운다.
        // 공중에 있는 동안 그림이 판정 위치에서 떨어져 보이는 거리. 0이면 땅에 있다.
        private Vector2 _leapOffset;
        // 뛰어오를 공중 자리(판정 위치 기준 거리)와, 착지 자리를 정한 순간 공중 자리까지의 거리. 내리꽂는 동안 이 값에서 0으로 줄어든다.
        private Vector2 _leapAirOffset;
        private Vector2 _leapDiveOffset;
        private bool _isLeapDiving;
        private Sprite _leapSlamPose;
        private bool _leapSlamFacesRight;
        private Vector3 _leapShadowScale;

        // 왕관 투하 예고로 머리 위 왕관을 들어 올린 높이 (유닛).
        private float _crownRaise;
        // 왕관이 떨어질(떨어진) 자리와, 아직 하늘에 있는지.
        private Vector2 _dropPoint;
        private bool _isCrownAirborne;
        private Vector3 _flyingCrownScale;
        private Vector3 _dropShadowScale;
        private Vector3 _crownReturnFromScale;

        // 국면 전환 끝에 쏙 줄어드는 정도 (1 = 원래 크기). 줄고 커질 때 기준이 되는 그림 한가운데 높이 (유닛).
        private float _shrinkSize = 1f;
        private float _popCenterHeight;
        private float _auraBaseRate;
        private Vector3 _auraBaseScale;
        private ParticleSystem.MinMaxGradient _auraBaseColor;
        // 분노 오라가 켜진 뒤 지난 시간. 음수면 꺼져 있다.
        private bool _isRaging;
        private bool _isHoldingBody;
        // 2등신으로 돌아가며 쏙 줄어든 뒤 지난 시간. 음수면 줄어드는 중이 아니다.
        private float _revertShrinkTime = -1f;
        // 각성 파동이 퍼지기 시작한 뒤 지난 시간 (음수면 없음), 파동이 닿을 가장 먼 거리, 맵 어두움 정도 (0~1)와 목표.
        private float _awakenWaveTime = -1f;
        private float _awakenWaveReach;
        private float _darken;
        private float _darkenTarget;
        private SoundHandle _transformChargeHandle = SoundHandle.None;
        // 지금 보이는 모습의 국면(1~3). 국면 전환 끝에 팍 커지는 순간 바뀐다.
        private int _lookPhase = 1;
        // 2국면 불티 색. 3국면에서 보라로 바꿨다가, 보스를 다시 쓸 때 되돌린다.
        private ParticleSystem.MinMaxGradient _emberBaseColor;

        private Meteor[] _meteors;
        private System.Action<Meteor> _onMeteorImpact;
        private int _meteorsLaunched;
        private int _meteorsLanded;
        private float _tintAlpha;

        private Vector2 _swordOrigin;
        private Vector2 _swordFirstDirection;
        // 천지 가르기 묶음: 0 = 반쪽 가르기, 1 = 축을 바꾼 반쪽 가르기, 2 = 줄무늬 가르기.
        private int _swordStage;
        private bool _isFirstSplitHorizontal;
        private bool _isSecondWaveShown;
        private bool _isFirstSlashDone;
        private bool _isSecondSlashDone;
        private float _secondSlashAt;
        // 이 묶음에서 마지막으로 벤 시각(묶음 시작 기준). 음수면 아직 안 벴다. 내리친 자세를 잠깐 유지하는 데 쓴다.
        private float _lastSlashAt = -1f;
        // 줄무늬 가르기에서 세로·가로 각각 줄무늬를 밀어 둔 정도 (위험 + 안전 한 쌍 폭 기준 0~1). 안전한 칸이 늘 같은 자리(왕 둘레)에 생기지 않게 한다.
        private float _verticalOffset;
        private float _horizontalOffset;
        // 줄무늬 가르기 경계선. 반쪽 가르기의 경계선을 복제해 앞 절반은 세로선, 뒤 절반은 가로선으로 쓴다.
        private SpriteRenderer[] _gridLines;
        private bool _isGridShown;

        private Vector2 _starCenter;
        private readonly Vector2[] _starPoints = new Vector2[5];
        private readonly Vector2[] _safeCenters = new Vector2[5];
        private int _safeCount;
        private int _teleportsDone;
        private bool _isSafeZoneShown;
        private bool _isShockCharging;
        private bool _isShockDone;
        private Vector3 _starTipScale;
        private Vector2 _dashOrigin;
        private int _dashSoundIndex;
        private float _nextAfterimageTime;
        private int _nextAfterimage;
        private readonly float[] _afterimageTimes = new float[16];
        private Vector2 _shockFrom;
        private Vector2 _shockTo;
        private bool _isShockArrived;
        // 내려찍기 때 화면이 반짝인 뒤 지난 시간. 음수면 반짝이는 중이 아니다.
        private float _screenFlashTime = -1f;

        private Vector3 _chibiPosition;
        private Vector3 _chibiScale;
        private Vector3 _crownPositionLocal;
        private Vector3 _crownScale;
        private ContactFilter2D _standFilter;

        protected override EnemyData Data => _data;
        protected override bool CanBePulled => false;
        // 처치 대화 동안 쓰러진 모습을 남겨 둔다. 대화가 끝나 작별 이벤트가 오면 사라진다.
        protected override bool HoldsBodyAfterDeath => _isHoldingBody;
        protected override bool RequiresLineOfSight => false;
        public override Vector2 AimDirection =>
            State == EnemyState.Telegraph || State == EnemyState.Attack ? _aimDirection : DirectionToTarget();
        // 유성우·천지 가르기는 예고 상태에서 자세만 잡고, 실제 예고(원, 웨이브)는 공격 단계 안에서 보인다.
        protected override float BaseTelegraphDuration =>
            _pattern == Pattern.Crown ? _data.CrownWindup : _pattern == Pattern.CrownDrop ? _data.CrownDropWindup : _data.PatternStartup;

        private bool IsDodging => _dodge != DodgePhase.None;

        protected override void Awake()
        {
            base.Awake();
            _chibiPosition = _chibi.transform.localPosition;
            _chibiScale = _chibi.transform.localScale;
            _crownPositionLocal = _crown.transform.localPosition;
            _crownScale = _crown.transform.localScale;
            _leapShadowScale = _leapShadow.transform.localScale;
            _auraBaseRate = _transformAura.emission.rateOverTimeMultiplier;
            _auraBaseScale = _transformAura.shape.scale;
            _auraBaseColor = _transformAura.main.startColor;
            _emberBaseColor = _rageEmbers.colorOverLifetime.color;
            SetRage(false);
            _leapShadow.enabled = false;
            _standFilter.useTriggers = false;
            _standFilter.SetLayerMask(ObstacleMask);
            _flyingCrown.SetParent(null);
            _flyingCrown.gameObject.SetActive(false);
            _flyingCrownScale = _flyingCrown.localScale;
            _dropShadowScale = _dropShadow.transform.localScale;
            _dropShadow.transform.SetParent(null);
            _dropRange.transform.SetParent(null);
            _dropFill.transform.SetParent(null);
            HideDropMarker();

            _meteors = new Meteor[_data.MeteorCount];
            _onMeteorImpact = OnMeteorImpact;
            for (int i = 0; i < _meteors.Length; i++)
            {
                _meteors[i] = Instantiate(_meteorPrefab).GetComponent<Meteor>();
                _meteors[i].Hide();
            }

            _mapTint.transform.SetParent(null);
            _mapTint.enabled = false;
            _awakenWave.transform.SetParent(null);
            _awakenWave.enabled = false;
            _mapDarken.transform.SetParent(null);
            _mapDarken.enabled = false;
            _halfFirst.transform.SetParent(null);
            _halfSecond.transform.SetParent(null);
            for (int i = 0; i < _stripeWarnings.Length; i++)
            {
                _stripeWarnings[i].transform.SetParent(null);
                _stripeWarnings[i].Hide();
            }
            _swordLine.transform.SetParent(null);
            _swordLine.enabled = false;
            // 전투 중 생성을 피하려고 칸 수만큼(세로·가로 각각 칸 수 + 1개) 미리 만들어 둔다.
            _gridLines = new SpriteRenderer[(StripePairCount() + 1) * 4];
            for (int i = 0; i < _gridLines.Length; i++)
            {
                _gridLines[i] = Instantiate(_swordLine);
                _gridLines[i].name = "GridLine" + i;
                _gridLines[i].enabled = false;
            }

            _starSigil.transform.SetParent(null);
            for (int i = 0; i < _afterimages.Length; i++)
            {
                _afterimages[i].transform.SetParent(null);
                _afterimages[i].enabled = false;
            }

            _starTipScale = _starTips[0].transform.localScale;
            for (int i = 0; i < _starTips.Length; i++)
            {
                _starTips[i].transform.SetParent(null);
            }

            for (int i = 0; i < _safeZones.Length; i++)
            {
                _safeZones[i].transform.SetParent(null);
            }

            HideStar();
            _halfFirst.Hide();
            _halfSecond.Hide();
            _swordLine.enabled = false;
            HideStar();
            _meteorAura.enabled = false;
        }

        protected override void OnEnable()
        {
            _isGroggy = false;
            _isFighting = false;
            base.OnEnable();
            HitReceiver.Damaged += OnDamaged;
            HitReceiver.Blocked += OnHitBlocked;
            GameEvents.BossFarewellRequested += OnFarewell;
            GameEvents.BossPoseRequested += OnPoseRequested;
            GameEvents.BossRevertRequested += OnRevertRequested;
            GameEvents.BombLanded += OnBombLanded;
            HideTelegraphs();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            HitReceiver.Damaged -= OnDamaged;
            HitReceiver.Blocked -= OnHitBlocked;
            GameEvents.BossFarewellRequested -= OnFarewell;
            GameEvents.BossPoseRequested -= OnPoseRequested;
            GameEvents.BossRevertRequested -= OnRevertRequested;
            GameEvents.BombLanded -= OnBombLanded;
            if (_flyingCrown != null)
            {
                _flyingCrown.gameObject.SetActive(false);
            }

            if (_dropShadow != null)
            {
                HideDropMarker();
            }

            if (_mapTint != null)
            {
                HidePhase3Effects();
                _awakenWaveTime = -1f;
                _awakenWave.enabled = false;
                _darken = 0f;
                _darkenTarget = 0f;
                _mapDarken.enabled = false;
            }

            // 변신 도중 사라지면(개발자 패널 전멸 등) 확대된 채 남지 않게 바로 되돌린다.
            if (_isZoomed)
            {
                _isZoomed = false;
                _zoomHoldTime = -1f;
                GameEvents.RaiseCameraZoomRequested(1f, 0f);
            }

            // 국면 전환 도중 사라져도 조작이 막히거나 카메라가 왕 자리에 묶인 채 남지 않게 한다.
            _shiftTime = -1f;
            ReleasePhaseCamera(0f);
        }

        private void OnDestroy()
        {
            if (_flyingCrown != null)
            {
                Destroy(_flyingCrown.gameObject);
            }

            // 월드로 떼어 둔 표시들은 보스와 함께 사라지지 않으므로 직접 지운다.
            if (_meteors != null)
            {
                for (int i = 0; i < _meteors.Length; i++)
                {
                    if (_meteors[i] != null)
                    {
                        Destroy(_meteors[i].gameObject);
                    }
                }
            }

            if (_mapTint != null)
            {
                Destroy(_mapTint.gameObject);
            }

            if (_awakenWave != null)
            {
                Destroy(_awakenWave.gameObject);
            }

            if (_mapDarken != null)
            {
                Destroy(_mapDarken.gameObject);
            }

            if (_halfFirst != null)
            {
                Destroy(_halfFirst.gameObject);
            }

            if (_halfSecond != null)
            {
                Destroy(_halfSecond.gameObject);
            }

            if (_stripeWarnings != null)
            {
                for (int i = 0; i < _stripeWarnings.Length; i++)
                {
                    if (_stripeWarnings[i] != null)
                    {
                        Destroy(_stripeWarnings[i].gameObject);
                    }
                }
            }

            if (_swordLine != null)
            {
                Destroy(_swordLine.gameObject);
            }

            if (_gridLines != null)
            {
                for (int i = 0; i < _gridLines.Length; i++)
                {
                    if (_gridLines[i] != null)
                    {
                        Destroy(_gridLines[i].gameObject);
                    }
                }
            }

            if (_starSigil != null)
            {
                Destroy(_starSigil.gameObject);
            }

            DestroyAll(_afterimages);

            DestroyAll(_starTips);
            DestroyAll(_safeZones);

            if (_dropShadow != null)
            {
                Destroy(_dropShadow.gameObject);
            }

            if (_dropRange != null)
            {
                Destroy(_dropRange.gameObject);
            }

            if (_dropFill != null)
            {
                Destroy(_dropFill.gameObject);
            }
        }

        // 나타난 순간은 2등신 모습이고, 곧바로 불티 속에서 떨다가 쏙 줄어든 뒤 팍 커지며 각성한다(대화 중 멈춤 시간 동안 보이게).
        // 등장 동작(왕관 강림)이 끝나야 싸운다. 그 전에도 맞지 않게 무적부터 켠다.
        public void BeginBoss(int bossNumber, int tier)
        {
            _bossNumber = bossNumber;
            _isFighting = false;
            _hitsTaken = 0;
            _rotationIndex = 0;
            _dodge = DodgePhase.None;
            _popTime = -1f;
            _summonTime = -1f;
            _hasTransformed = false;
            _isEntranceRequested = false;
            _isEntranceDone = false;
            _shiftTime = -1f;
            _knockTime = -1f;
            _crownDropTime = -1f;
            _isCrownReturning = false;
            _visual.Jitter = 0f;
            _shrinkSize = 1f;
            _lookPhase = 1;
            _isHoldingBody = false;
            _visual.HoldsDeath = false;
            _visual.SkipsDeathEnd = false;
            _visual.DeathScale = 1f;
            _revertShrinkTime = -1f;
            _bodyRenderer.enabled = true;
            SetRage(false);
            // 왕관 횟수만큼은 맞아도 버티고, 그로기 때의 한 방에 쓰러진다.
            HitReceiver.ResetHits(_data.CrownHits + 1);
            HitReceiver.IsShielded = true;
            _hasCrown = true;
            _crown.enabled = false;
            _chibi.enabled = true;
            _chibi.transform.localPosition = _chibiPosition;
            _chibi.transform.localScale = _chibiScale;
            _visual.Alpha = 0f;

            _transformTime = 0f;
            _zoomHoldTime = -1f;
            TickTransform(0f);
            ZoomIn();
            SetAuraStrength(1f, 1f);
            SetAuraColor(false);
            _transformAura.Play();
            _transformChargeHandle = AudioManager.Play(_data.TransformChargeSound);
            // 떨기 시작하면 원래 곡을 줄여, 각성하는 순간 보스 곡이 조용한 데서 터지게 한다.
            AudioManager.FadeOutBgm(_data.BgmFadeOutTime);
        }

        // 변신이 아직이면 끝나는 대로 왕관을 부른다.
        public void PlayEntrance()
        {
            _isEntranceRequested = true;
            if (_hasTransformed && _summonTime < 0f && !_isEntranceDone)
            {
                BeginSummon();
            }
        }

        public bool IsEntranceDone => _isEntranceDone;

        // 개발자 패널용. 3국면까지 다 맞은 것으로 치고 바로 그로기에 빠진다. 이때 한 방 맞히면 처치 연출로 넘어간다.
        public void DebugGroggy()
        {
            if (!_isFighting || State == EnemyState.Dead)
            {
                return;
            }

            DebugJumpToPhase(Rotations.Length);
            _hitsTaken = _data.CrownHits;
            HitReceiver.ResetHits(1);
            _isGroggy = true;
            _hasCrown = false;
            _crown.enabled = false;
            HitReceiver.IsShielded = false;
            Stun(_data.GroggyDuration);
            AudioManager.Play(_data.GroggySound);
        }

        // 개발자 패널용. 그 자리에서 바로 쓰러뜨려 처치 연출(슬로모션 → 처치 대화 → 엔딩)을 본다.
        public void DebugDefeat()
        {
            if (!_isFighting || State == EnemyState.Dead)
            {
                return;
            }

            HitReceiver.Kill();
        }

        // 개발자 패널용. 맞은 횟수를 그 국면에 맞추고 바로 그 국면 모습·패턴 순서로 넘어간다. 싸우는 중에만 동작한다.
        // 국면 전환 연출은 건너뛰고, 잠깐 멈췄다가(기절) 새 국면의 첫 패턴부터 시작한다.
        public void DebugJumpToPhase(int phase)
        {
            if (!_isFighting || State == EnemyState.Dead)
            {
                return;
            }

            int hits = Mathf.Clamp(phase - 1, 0, _data.CrownHits - 1);
            _hitsTaken = hits;
            HitReceiver.ResetHits(_data.CrownHits + 1 - hits);
            _rotationIndex = 0;
            _lookPhase = Mathf.Min(hits + 1, Rotations.Length);
            _isGroggy = false;
            _shiftTime = -1f;
            _knockTime = -1f;
            _crownDropTime = -1f;
            _isCrownReturning = false;
            _visual.Jitter = 0f;
            SetShrink(1f);
            ReleasePhaseCamera(0f);
            HidePhase3Effects();
            HideDropMarker();
            EndLeapVisual();
            _flyingCrown.gameObject.SetActive(false);
            _flyingCrown.localScale = _flyingCrownScale;
            SetCrown(true);
            SetRage(_lookPhase >= 2);
            _popCenterHeight = BodyCenterHeight();
            _popTime = 0f;
            Stun(0.3f);
        }

        public void StartFight()
        {
            _isFighting = true;
            _moveTimer = 0f;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            // 쓰러진 뒤 고블린이 반대쪽 옆자리로 걸어가도 계속 고블린 쪽을 본다.
            if (_isHoldingBody && _bodyRenderer.enabled)
            {
                FaceTargetWhileHeld();
            }

            if (_transformTime >= 0f)
            {
                TickTransform(deltaTime);
            }

            if (_zoomHoldTime >= 0f)
            {
                _zoomHoldTime += deltaTime;
                if (_zoomHoldTime >= _data.ZoomHoldTime)
                {
                    ZoomOut();
                }
            }

            if (_summonTime >= 0f)
            {
                TickSummon(deltaTime);
            }
            else if (_crown.enabled)
            {
                TickCrownFloat();
            }

            TickPop(deltaTime);
            TickRage(deltaTime);
            TickAwakenWave(deltaTime);
            TickRevertShrink(deltaTime);
            TickDarken(deltaTime);
            TickPhase3Visuals(deltaTime);
            TickAfterimages();
            TickKnockback(deltaTime);
            TickCrownFall(deltaTime);
            if (_shiftTime >= 0f)
            {
                TickPhaseShift(deltaTime);
            }

            UpdateDodgePose();
            UpdatePoseSprite();
        }

        // 베기는 파고들며 칼을 든 그림, 베는 순간부터 쉬는 동안 휘두른 그림. 돌진은 달려드는 동안만 찌르는 그림이다.
        // 조준하는 동안은 기본 자세로 서 있어야 찌르는 그림이 "지금 온다"는 신호로 읽힌다.
        private void UpdatePoseSprite()
        {
            // 그로기로 주저앉아 있는 동안은 다른 자세를 고르지 않는다.
            if (_isGroggy && State == EnemyState.Stunned && _stunSprite != null)
            {
                _visual.PoseSprite = _stunSprite;
                return;
            }

            Sprite pose = null;
            if (State == EnemyState.Attack && _pattern == Pattern.Slash)
            {
                pose = _step == Step.Pause ? SlashFrame() : RagePick(_rageSlashReadySprite, _slashReadySprite);
            }
            else if (State == EnemyState.Attack && _pattern == Pattern.Lunge && _step == Step.Dash)
            {
                pose = _thrustSprite;
            }
            else if (State == EnemyState.Attack && _pattern == Pattern.Leap && _step != Step.LeapLand)
            {
                pose = _isLeapDiving ? _leapSlamPose : _leapHoverSprite;
            }

            // 기본 자세는 그림(Body)에 넣어 둔 1국면 그림이라, 2국면에서는 붉어진 기본 그림을 자세 그림으로 덮어 쓴다.
            bool isActing = State == EnemyState.Telegraph || State == EnemyState.Attack;
            if (isActing && _pattern == Pattern.Meteor)
            {
                pose = _meteorPoseSprite;
            }
            else if (isActing && _pattern == Pattern.Sword)
            {
                pose = IsSwordStriking() ? _swordAttackSprite : _swordChargeSprite;
            }

            else if (State == EnemyState.Attack && _pattern == Pattern.Star && _step == Step.StarShock)
            {
                pose = _meteorPoseSprite;
            }

            if (pose == null && _lookPhase >= 3)
            {
                pose = _phase3IdleSprite;
            }
            else if (pose == null && _isRaging)
            {
                pose = _rageIdleSprite;
            }

            _visual.PoseSprite = pose;
        }

        private Sprite RagePick(Sprite rage, Sprite normal)
        {
            return _isRaging && rage != null ? rage : normal;
        }

        // 베는 순간부터 지난 시간(쉬는 단계의 시간)에 맞춰 동작 그림을 넘긴다. 마지막 그림에서 멈춘다.
        private Sprite SlashFrame()
        {
            Sprite[] frames = _isRaging && _rageSlashFrames != null && _rageSlashFrames.Length > 0 ? _rageSlashFrames : _slashFrames;
            if (frames == null || frames.Length == 0)
            {
                return null;
            }

            int index = Mathf.Min((int)(_stepTime / _data.SlashFrameTime), frames.Length - 1);
            return frames[index];
        }

        // ── 변신: 화난 2등신 그대로 점점 세게 떨다가, 한가운데로 쏙 줄어들어 사라지고 그 자리에서 각성 모습이 팍 커진다.
        // 줄어들고 커지는 동안 그림 한가운데를 기준으로 삼아, 발밑으로 빨려 들어가거나 발밑에서 솟는 것처럼 보이지 않게 한다.

        private void TickTransform(float deltaTime)
        {
            _transformTime += deltaTime;
            Transform chibi = _chibi.transform;
            if (_transformTime < _data.TransformDuration)
            {
                float t = _transformTime / _data.TransformDuration;
                float shake = t * t;
                float amplitude = 0.02f + 0.12f * shake;
                chibi.localPosition = _chibiPosition + new Vector3(Mathf.Sin(_transformTime * 83f), 0.5f * Mathf.Sin(_transformTime * 67f), 0f) * amplitude;
                chibi.localScale = _chibiScale * (1f + 0.12f * shake);
                return;
            }

            // 떨림이 끝나는 순간 떨림 소리도 짧게 줄여 끊는다. 소리가 떨림보다 길어 각성한 뒤에도 남지 않게 한다.
            AudioManager.FadeOut(_transformChargeHandle, TransformChargeFadeOut);
            _transformChargeHandle = SoundHandle.None;
            float shrink = Mathf.Clamp01((_transformTime - _data.TransformDuration) / ShrinkDuration);
            float size = (1f - shrink * shrink) * 1.12f;
            chibi.localScale = _chibiScale * size;
            chibi.localPosition = _chibiPosition + Vector3.up * (ChibiCenterHeight() * (1f - size));
            if (shrink >= 1f)
            {
                PopTransform();
            }
        }

        private void PopTransform()
        {
            // 그림 피벗이 발밑이라 위치 대신 그림 한가운데에서 터트린다.
            Vector2 center = (Vector2)transform.position + (Vector2)_chibiPosition + Vector2.up * ChibiCenterHeight();
            _transformTime = -1f;
            _hasTransformed = true;
            _chibi.enabled = false;
            _chibi.transform.localPosition = _chibiPosition;
            _chibi.transform.localScale = _chibiScale;
            _transformAura.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            _visual.Alpha = 1f;
            _popCenterHeight = ChibiCenterHeight();
            _popTime = 0f;
            _zoomHoldTime = 0f;
            FeedbackPlayer.Play(_data.TransformFeedback, center);
            AudioManager.Play(_data.TransformSound);
            AudioManager.PlayBgm(_data.BossBgm);
            BeginAwakenWave(center);
            _darkenTarget = 1f;

            if (_isEntranceRequested)
            {
                BeginSummon();
            }
        }

        // 처치 대화 줄이 쓰러진 모습을 바꾼다(무릎 꿇기 등). 죽은 뒤에는 표시 컴포넌트가 그림을 고르지 않아 직접 바꾼다.
        private void OnPoseRequested(Sprite sprite)
        {
            if (_isHoldingBody && sprite != null)
            {
                _bodyRenderer.sprite = sprite;
                FaceTargetWhileHeld();
            }
        }

        // 쓰러진 뒤에는 방향을 스스로 고르지 않으므로, 쓰러지는 순간과 자세가 바뀔 때마다 고블린 쪽을 보게 돌린다.
        private void FaceTargetWhileHeld()
        {
            bool artFacesRight = System.Array.IndexOf(_rightFacingPoseSprites, _bodyRenderer.sprite) >= 0;
            _visual.FaceToward(TargetPosition.x, artFacesRight);
        }

        // 각성이 풀린다. 펑 하고 사라지고, 그 자리에는 대화가 2등신 왕을 띄운다. 나중에 사라질 때 연출은 내지 않는다.
        private void OnRevertRequested()
        {
            if (!_isHoldingBody)
            {
                return;
            }

            // 파티클 없이 그 자리에서 쏙 줄어들어 사라진다. 2등신 왕은 대화가 그 자리에 뿅 띄운다.
            _revertShrinkTime = 0f;
            _visual.SkipsDeathEnd = true;
            _rageEmbers.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _rageFlames.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void TickRevertShrink(float deltaTime)
        {
            if (_revertShrinkTime < 0f)
            {
                return;
            }

            _revertShrinkTime += deltaTime;
            float t = Mathf.Clamp01(_revertShrinkTime / ShrinkDuration);
            _visual.DeathScale = 1f - t * t;
            if (t >= 1f)
            {
                _revertShrinkTime = -1f;
                _bodyRenderer.enabled = false;
            }
        }

        // 처치 대화가 끝났다. 멈춰 둔 사망 연출을 이어 가 부풀었다 쏙 사라진다(사라짐 연출·소리는 움직임 데이터에 있다).
        private void OnFarewell()
        {
            if (!_isHoldingBody)
            {
                return;
            }

            _isHoldingBody = false;
            _visual.HoldsDeath = false;
        }

        // ── 각성 파동과 어두워지는 맵: 진짜 보스로 바뀌는 순간 왕에게서 고리가 맵 끝까지 퍼지고, 그 뒤로 맵이 어둑해진다.
        // 파동은 가장 먼 맵 모서리까지 닿도록 크기를 정한다. 어두움은 쓰러질 때까지 남아 최종 보스전 분위기를 만든다.

        private void BeginAwakenWave(Vector2 center)
        {
            ArenaBounds arena = ArenaBounds.Current;
            Bounds bounds = arena != null ? arena.Bounds : new Bounds(center, new Vector3(60f, 60f, 1f));
            float reach = 0f;
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            reach = Mathf.Max(reach, Vector2.Distance(center, new Vector2(min.x, min.y)));
            reach = Mathf.Max(reach, Vector2.Distance(center, new Vector2(min.x, max.y)));
            reach = Mathf.Max(reach, Vector2.Distance(center, new Vector2(max.x, min.y)));
            reach = Mathf.Max(reach, Vector2.Distance(center, new Vector2(max.x, max.y)));
            _awakenWaveReach = reach;
            _awakenWaveTime = 0f;
            _awakenWave.transform.position = center;
            _awakenWave.enabled = true;
            TickAwakenWave(0f);

            // 어두운 칸은 화면을 다 덮도록 맵보다 넉넉히 깐다.
            Transform darken = _mapDarken.transform;
            darken.position = bounds.center;
            float size = _mapDarken.sprite != null ? _mapDarken.sprite.bounds.size.x : 1f;
            darken.localScale = new Vector3((bounds.size.x + ShockCoverMargin) / size, (bounds.size.y + ShockCoverMargin) / size, 1f);
        }

        private void TickAwakenWave(float deltaTime)
        {
            if (_awakenWaveTime < 0f)
            {
                return;
            }

            _awakenWaveTime += deltaTime;
            float t = Mathf.Clamp01(_awakenWaveTime / _data.AwakenWaveTime);
            // 처음엔 빠르게 터져 나가고 끝에서 느려진다. 고리 그림의 테두리가 그림 폭의 90% 자리라 그만큼 크게 잡는다.
            float radius = _awakenWaveReach * (1f - (1f - t) * (1f - t));
            float size = _awakenWave.sprite != null ? _awakenWave.sprite.bounds.size.x * 0.45f : 0.5f;
            float scale = Mathf.Max(radius, 0.01f) / size;
            _awakenWave.transform.localScale = new Vector3(scale, scale, 1f);
            Color color = _data.AwakenWaveColor;
            color.a *= 1f - t * t;
            _awakenWave.color = color;
            if (t >= 1f)
            {
                _awakenWaveTime = -1f;
                _awakenWave.enabled = false;
            }
        }

        private void TickDarken(float deltaTime)
        {
            _darken = Mathf.MoveTowards(_darken, _darkenTarget, deltaTime / _data.DarkenFadeTime);
            Color color = _data.DarkenColor;
            color.a *= _darken;
            _mapDarken.color = color;
            _mapDarken.enabled = _darken > 0f;
        }

        // 변신에 눈이 가도록 카메라가 왕 한가운데로 다가간다.
        private void ZoomIn()
        {
            Vector2 center = (Vector2)transform.position + (Vector2)_chibiPosition + Vector2.up * ChibiCenterHeight();
            _isZoomed = true;
            GameEvents.RaiseCameraFocusRequested(center, _data.ZoomInTime);
            GameEvents.RaiseCameraZoomRequested(_data.TransformZoom, _data.ZoomInTime);
        }

        // 보스전 등장 연출이 처음 잡아 둔 화면(플레이어와 보스 사이)으로 스윽 물러난다. 이후 조작이 돌아올 때 진행 흐름이 플레이어에게 돌려준다.
        private void ZoomOut()
        {
            _zoomHoldTime = -1f;
            _isZoomed = false;
            Vector2 middle = ((Vector2)transform.position + TargetPosition) * 0.5f;
            GameEvents.RaiseCameraFocusRequested(middle, _data.ZoomOutTime);
            GameEvents.RaiseCameraZoomRequested(1f, _data.ZoomOutTime);
        }

        // 2등신 그림 한가운데가 발밑에서 얼마나 위인지 (유닛).
        private float ChibiCenterHeight()
        {
            return _chibi.sprite != null ? _chibi.sprite.bounds.center.y * _chibiScale.y : 0f;
        }

        private void TickPop(float deltaTime)
        {
            if (_popTime < 0f)
            {
                return;
            }

            _popTime += deltaTime;
            float size = CurrentPopScale();
            // 0에서 커지는 동안 2등신이 사라진 한가운데에서 솟아나게 그림을 들어 올렸다가 내려놓는다.
            _visual.Lift = _popCenterHeight * (1f - size);
            if (_popTime >= PopDuration)
            {
                _popTime = -1f;
                _visual.Lift = 0f;
            }
        }

        // 0에서 시작해 조금 넘쳤다가 1로 자리 잡는다.
        private float CurrentPopScale()
        {
            if (_popTime < 0f)
            {
                return 1f;
            }

            float t = Mathf.Clamp01(_popTime / PopDuration) - 1f;
            return 1f + (PopOvershoot + 1f) * t * t * t + PopOvershoot * t * t;
        }

        // ── 등장 동작(왕관 강림): 머리 위로 왕관이 내려와 빛나면 조작이 돌아온다.

        private void BeginSummon()
        {
            _summonTime = 0f;
            _crown.enabled = true;
            TickSummon(0f);
        }

        private void TickSummon(float deltaTime)
        {
            _summonTime += deltaTime;
            float t = Mathf.Clamp01(_summonTime / _data.CrownSummonDuration);
            float ease = 1f - (1f - t) * (1f - t);
            _crown.transform.localPosition = _crownPositionLocal + Vector3.up * (CrownDropHeight * (1f - ease));
            _crown.transform.localScale = _crownScale;
            _crown.color = new Color(1f, 0.85f, 0.4f, t);
            if (t < 1f)
            {
                return;
            }

            _summonTime = -1f;
            _isEntranceDone = true;
            _visual.Flash(_data.ImmuneFlashColor, 0.3f);
            AudioManager.Play(_data.CrownCatchSound);
        }

        // 왕관이 빛나는 것 자체가 "지금은 무적"이라는 표시다. 머리 위에서 둥둥 뜨며 숨쉬듯 밝기를 흔들어 눈에 띄게 한다.
        private void TickCrownFloat()
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 6f);
            Color baseColor = _lookPhase >= 3 ? _data.Phase3CrownColor : new Color(1f, 0.85f, 0.4f, 1f);
            Color brightColor = _lookPhase >= 3 ? Color.Lerp(_data.Phase3CrownColor, Color.white, 0.6f) : Color.white;
            _crown.color = Color.Lerp(baseColor, brightColor, pulse);
            _crown.transform.localScale = _crownScale * (1f + 0.06f * pulse);
            _crown.transform.localPosition = _crownPositionLocal + (Vector3)_leapOffset + Vector3.up * (_crownRaise + CrownBobHeight * Mathf.Sin(Time.time * CrownBobSpeed));
        }

        // 패턴 사이에는 일정 거리까지 다가온다. 구르는 중이면 구르기만 한다.
        protected override void TickMove(float deltaTime)
        {
            if (!_isFighting)
            {
                return;
            }

            if (IsDodging)
            {
                TickDodge(deltaTime);
                return;
            }

            _moveTimer += deltaTime;
            if (!IsTargetWithin(_data.PreferredDistance))
            {
                MoveTowardTarget(_data.MoveSpeed, deltaTime);
            }
        }

        protected override bool ShouldStartAttack()
        {
            return _isFighting && !IsDodging && _moveTimer >= _data.PatternInterval;
        }

        // 왕관 부메랑은 던질 방향을 따라가다 마지막에 고정한다.
        protected override void TickTelegraph(float deltaTime)
        {
            // 왕관 투하는 왕관을 머리 위로 천천히 들어 올리는 것이 예고다.
            if (_pattern == Pattern.CrownDrop)
            {
                float t = Mathf.Clamp01(StateTime / _data.CrownDropWindup);
                _crownRaise = CrownRaiseHeight * t * t * (3f - 2f * t);
                return;
            }

            if (_pattern != Pattern.Crown)
            {
                return;
            }

            if (StateTime < _data.CrownWindup - _data.LungeLockTime)
            {
                _aimDirection = DirectionToTarget();
            }

            _aimLine.Show(Body.position, _aimDirection, _data.CrownDistance, _data.CrownHitRadius * 2f);
        }

        protected override bool TickAttack(float deltaTime)
        {
            _stepTime += deltaTime;
            switch (_pattern)
            {
                case Pattern.Slash:
                    return TickSlash(deltaTime);
                case Pattern.Lunge:
                    return TickLunge(deltaTime);
                case Pattern.Leap:
                    return TickLeap(deltaTime);
                case Pattern.CrownDrop:
                    return TickCrownDrop(deltaTime);
                case Pattern.Meteor:
                    return TickMeteor(deltaTime);
                case Pattern.Sword:
                    return TickSword(deltaTime);
                case Pattern.Star:
                    return TickStar(deltaTime);
                default:
                    return TickCrown(deltaTime);
            }
        }

        // 1국면 패턴 한 바퀴(왕관 부메랑까지)가 다 지나도록 한 대도 못 때렸으면, 다음 패턴이 시작될 때 한 번만 힌트를 준다.
        // 왕관이 없을 때만 맞는다는 규칙을 모르고 계속 피하기만 하는 경우를 돕는다.
        private void TryShowCrownHint(int rotationLength)
        {
            if (_hitsTaken != 0 || _isCrownHintShown)
            {
                return;
            }

            _firstPhasePatterns++;
            if (_firstPhasePatterns <= rotationLength)
            {
                return;
            }

            _isCrownHintShown = true;
            GameEvents.RaisePlayerHintRequested(_data.CrownHintText, _data.CrownHintHold);
        }

        protected override void OnEnterState(EnemyState state)
        {
            switch (state)
            {
                case EnemyState.Telegraph:
                    Pattern[] rotation = CurrentRotation();
                    TryShowCrownHint(rotation.Length);
                    _pattern = rotation[_rotationIndex % rotation.Length];
                    _rotationIndex = (_rotationIndex + 1) % rotation.Length;
                    _aimDirection = DirectionToTarget();
                    HideTelegraphs();
                    return;

                case EnemyState.Attack:
                    HideTelegraphs();
                    _count = 0;
                    if (_pattern == Pattern.Slash)
                    {
                        BeginSlashDash();
                    }
                    else if (_pattern == Pattern.Lunge)
                    {
                        BeginBackstep();
                    }
                    else if (_pattern == Pattern.Leap)
                    {
                        BeginLeap();
                    }
                    else if (_pattern == Pattern.CrownDrop)
                    {
                        BeginCrownDrop();
                    }
                    else if (_pattern == Pattern.Meteor)
                    {
                        BeginMeteor();
                    }
                    else if (_pattern == Pattern.Sword)
                    {
                        BeginSword();
                    }
                    else if (_pattern == Pattern.Star)
                    {
                        BeginStar();
                    }
                    else
                    {
                        ThrowCrown();
                    }

                    return;

                case EnemyState.Move:
                    _moveTimer = 0f;
                    // 기절 시간과 연출 시간은 다른 시계(물리 스텝, 프레임)로 재므로, 연출이 한 프레임 늦어도 여기서 마무리한다.
                    if (_shiftTime >= 0f)
                    {
                        FinishPhaseShift();
                    }

                    // 맞고 비틀거린 뒤, 또는 그로기 동안 못 잡았으면 왕관이 다시 생긴다.
                    _isGroggy = false;
                    if (!_hasCrown && _hasTransformed)
                    {
                        RegainCrown();
                    }

                    break;

                case EnemyState.Dead:
                    // 쓰러진 모습을 남겨 두고, 카메라가 왕에게 다가간 채 슬로모션이 걸린다. 처치 대화는 진행 흐름이 연다.
                    _isHoldingBody = true;
                    _visual.HoldsDeath = true;
                    AudioManager.TapeStopBgm(_data.BgmTapeStopTime);
                    _isZoomed = true;
                    GameEvents.RaiseCameraFocusRequested(Vector2.Lerp(Body.position, HeadCrownPosition(), 0.5f), _data.DefeatZoomTime);
                    GameEvents.RaiseCameraZoomRequested(_data.DefeatZoom, _data.DefeatZoomTime);
                    _crown.enabled = false;
                    _darkenTarget = 0f;
                    HidePhase3Effects();
                    // 불꽃만 끄고 붉어진 모습은 그대로 둔 채 쓰러진다.
                    _rageFlames.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    _rageEmbers.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    _flyingCrown.gameObject.SetActive(false);
                    FeedbackPlayer.Play(_data.DefeatFeedback, Body.position);
                    FaceTargetWhileHeld();
                    GameEvents.RaiseBossDefeated(_bossNumber);
                    break;
            }

            _dodge = DodgePhase.None;
            HideTelegraphs();
            // 별의 의식 도중 맞으면 별과 안전지대, 화면 덮개를 바로 걷는다.
            if (state == EnemyState.Stunned)
            {
                HideStar();
                _mapTint.enabled = false;
            }

            // 도약 도중 끊기면(기절·사망) 떠 있던 그림과 그림자를 바로 내려놓는다.
            EndLeapVisual();
            _crownRaise = 0f;
            _isCrownAirborne = false;
        }

        private Pattern[] CurrentRotation()
        {
            return Rotations[Mathf.Min(_hitsTaken, Rotations.Length - 1)];
        }

        // ── 왕검 연속 베기: 플레이어에게 일직선으로 파고들어 벨 거리에서 멈추고, 부채꼴이 차오르면 벤다. 이를 정해진 횟수만큼.
        // 파고드는 방향은 출발 순간 고정한다. 쫓아오며 휘는 길이면 어디로 피해야 할지 읽을 수 없다. 돌진처럼 벽을 뚫고 파고든다.

        private void BeginSlashDash()
        {
            _step = Step.Dash;
            _stepTime = 0f;
            _aimDirection = DirectionToTarget();
            float gap = Vector2.Distance(TargetPosition, Body.position) - _data.SlashReach;
            _dashRemaining = gap > 0f ? StandableLength(_aimDirection, gap) : 0f;
            _dashSpeed = _data.SlashDashSpeed;
        }

        private bool TickSlash(float deltaTime)
        {
            switch (_step)
            {
                case Step.Dash:
                    if (_dashRemaining <= 0.0001f || StepAlong(_aimDirection, deltaTime, false))
                    {
                        BeginSlashWindup();
                    }

                    return false;

                case Step.Windup:
                    float t = Mathf.Clamp01(_stepTime / _data.SlashWindup);
                    _slashFill.SetRadius(_data.SlashRadius * t);
                    if (t >= 1f)
                    {
                        HitInSector(_aimDirection, _data.SlashRadius, _data.SlashAngle);
                        _slashFill.SetColor(Color.white);
                        AudioManager.Play(_data.SlashSound);
                        _step = Step.Pause;
                        _stepTime = 0f;
                    }

                    return false;

                default:
                    if (_stepTime < _data.SlashPause)
                    {
                        return false;
                    }

                    HideTelegraphs();
                    _count++;
                    if (_count >= _data.SlashCount)
                    {
                        return true;
                    }

                    BeginSlashDash();
                    return false;
            }
        }

        // 멈추는 순간 방향을 고정한다. 차오르는 동안 방향이 바뀌면 피할 곳을 읽을 수 없다.
        private void BeginSlashWindup()
        {
            _step = Step.Windup;
            _stepTime = 0f;
            _aimDirection = DirectionToTarget();
            _slashRange.BuildSector(_aimDirection, _data.SlashRadius, _data.SlashAngle);
            _slashFill.CopyLimits(_slashRange);
            _slashFill.SetRadius(0f);
            _slashRange.ResetColor();
            _slashFill.ResetColor();
            _slashRange.Visible = true;
            _slashFill.Visible = true;
        }

        // ── 돌진 찌르기: 뒤로 물러나 거리를 벌린 뒤, 예고선을 따라 플레이어를 지나쳐 돌진. 돌진은 벽을 뚫는다. 이를 정해진 횟수만큼.

        private void BeginBackstep()
        {
            _step = Step.Backstep;
            _stepTime = 0f;
            Vector2 away = -DirectionToTarget();
            _dashRemaining = ClearDistance(away, _data.BackstepDistance, BodyRadius);
            _dashSpeed = _dashRemaining / _data.BackstepDuration;
            _aimDirection = -away;
        }

        private bool TickLunge(float deltaTime)
        {
            switch (_step)
            {
                case Step.Backstep:
                    _aimDirection = DirectionToTarget();
                    if (StepAlong(-_aimDirection, deltaTime, false))
                    {
                        BeginAim();
                    }

                    return false;

                case Step.Aim:
                    if (_stepTime < _data.LungeAimTime - _data.LungeLockTime)
                    {
                        AimLunge();
                    }

                    if (_stepTime >= _data.LungeAimTime)
                    {
                        _aimLine.Hide();
                        _step = Step.Dash;
                        _stepTime = 0f;
                        _dashRemaining = _lineLength;
                        _dashSpeed = _data.LungeSpeed;
                        AudioManager.Play(_data.LungeSound);
                    }

                    return false;

                case Step.Dash:
                    if (StepAlong(_aimDirection, deltaTime, true))
                    {
                        _step = Step.Pause;
                        _stepTime = 0f;
                    }

                    return false;

                default:
                    if (_stepTime < _data.LungePause)
                    {
                        return false;
                    }

                    _count++;
                    if (_count >= _data.LungeCount)
                    {
                        return true;
                    }

                    BeginAim();
                    return false;
            }
        }

        private void BeginAim()
        {
            _step = Step.Aim;
            _stepTime = 0f;
            AimLunge();
        }

        // 플레이어를 지나쳐 멈출 만큼 길게 벽을 뚫고 간다. 예고선 길이가 곧 돌진 거리다.
        private void AimLunge()
        {
            _aimDirection = DirectionToTarget();
            float toTarget = Vector2.Distance(TargetPosition, Body.position);
            float wanted = Mathf.Min(toTarget + _data.LungeOvershoot, _data.LungeMaxDistance);
            _lineLength = StandableLength(_aimDirection, wanted);
            _aimLine.Show(Body.position, _aimDirection, _lineLength, _data.LungeWidth);
        }

        // 벽을 뚫고 가도(돌진, 베기 전 파고들기) 벽 속이나 맵 밖에 멈추면 안 되므로, 설 수 있는 자리가 나올 때까지 끝을 당긴다.
        private float StandableLength(Vector2 direction, float wanted)
        {
            for (float length = wanted; length > 0f; length -= LungeSafeStep)
            {
                if (CanStandAt(Body.position + direction * length))
                {
                    return length;
                }
            }

            return 0f;
        }

        private bool CanStandAt(Vector2 point)
        {
            float radius = BodyRadius;
            ArenaBounds arena = ArenaBounds.Current;
            if (arena != null)
            {
                Bounds bounds = arena.Bounds;
                if (point.x < bounds.min.x + radius || point.x > bounds.max.x - radius
                    || point.y < bounds.min.y + radius || point.y > bounds.max.y - radius)
                {
                    return false;
                }
            }

            return Physics2D.OverlapCircle(point, radius, _standFilter, StandBuffer) == 0;
        }

        // 남은 거리만큼 정해진 속도로 나아간다. 돌진은 벽 너머까지 지나간 구간에 닿은 대상을 맞힌다. 다 가면 true.
        private bool StepAlong(Vector2 direction, float deltaTime, bool hits)
        {
            float step = Mathf.Min(_dashSpeed * deltaTime, _dashRemaining);
            if (hits)
            {
                HitInBox(direction, step, _data.LungeWidth, true);
            }

            Body.MovePosition(Body.position + direction * step);
            _dashRemaining -= step;
            return _dashRemaining <= 0.0001f;
        }

        // ── 도약 내려찍기: 위쪽 부채꼴 안 무작위 방향으로 화면 안 공중 자리에 뛰어올라 떠 있다가, 플레이어 자리를 착지 자리로 정하고
        // 빨간 원이 다 차는 순간 그쪽으로 비스듬히(바로 위면 수직으로) 내리꽂는다. 정한 순간 그 자리에 폭탄을 던지면 착지에 맞춰 터지는
        // "1초 뒤 예측" 시험이다(왕관을 쓰고 있어 맞지는 않는다). 화면에서 사라지지 않아 어디서 내리꽂을지 눈으로 따라갈 수 있다.
        // 공중에 있는 동안 판정 위치는 땅에 두고 그림만 옮긴다. 착지 자리를 정하는 순간 판정 위치를 착지 자리로 옮긴다.

        private bool IsAirborne => _leapOffset.sqrMagnitude > 0f;

        private void BeginLeap()
        {
            _step = Step.LeapRise;
            _stepTime = 0f;
            _isLeapDiving = false;
            _leapAirOffset = PickLeapAirPoint() - Body.position;
            AudioManager.Play(_data.LeapSound);
        }

        // 플레이어를 기준으로 위쪽을 가운데로 정한 각도 안에서 방향을 골라, 플레이어 머리 위 둘레로 날아든다.
        // 제자리 기준이면 살짝 떠오르는 정도로만 보여 뛰어오른 느낌이 약했다. 몸 전체가 화면 안에 보이도록 자리를 화면 안쪽으로 당긴다.
        private Vector2 PickLeapAirPoint()
        {
            float half = _data.LeapAirAngle * 0.5f;
            float angle = (90f + Random.Range(-half, half)) * Mathf.Deg2Rad;
            Vector2 point = TargetPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(_data.LeapAirDistanceMin, _data.LeapAirDistanceMax);

            Camera view = Camera.main;
            if (view == null)
            {
                return point;
            }

            float halfHeight = view.orthographicSize;
            float halfWidth = halfHeight * view.aspect;
            Vector2 center = view.transform.position;
            float margin = _data.LeapScreenMargin;
            // 그림은 발밑이 기준점이라 위쪽은 키만큼 더 띄운다.
            float bodyHeight = _bodyRenderer.bounds.size.y;
            point.x = Mathf.Clamp(point.x, center.x - halfWidth + margin, center.x + halfWidth - margin);
            point.y = Mathf.Clamp(point.y, center.y - halfHeight + margin, center.y + halfHeight - margin - bodyHeight);
            return point;
        }

        private bool TickLeap(float deltaTime)
        {
            switch (_step)
            {
                case Step.LeapRise:
                    float rise = Mathf.Clamp01(_stepTime / _data.LeapRiseTime);
                    SetLeapOffset(_leapAirOffset * (1f - (1f - rise) * (1f - rise)));
                    FaceLeapTarget(_leapHoverSprite);
                    if (rise >= 1f)
                    {
                        _step = Step.LeapTrack;
                        _stepTime = 0f;
                    }

                    return false;

                case Step.LeapTrack:
                    FaceLeapTarget(_leapHoverSprite);
                    if (_stepTime >= _data.LeapTrackTime)
                    {
                        LockLeap();
                    }

                    return false;

                case Step.LeapLock:
                    float fill = Mathf.Clamp01(_stepTime / _data.LeapLockTime);
                    _slashFill.SetRadius(_data.LeapRadius * fill);
                    // 원이 다 차는 순간 땅에 닿도록, 고정 시간의 마지막 구간에 떨어진다.
                    float fallStart = _data.LeapLockTime - _data.LeapFallTime;
                    float fall = Mathf.Clamp01((_stepTime - fallStart) / _data.LeapFallTime);
                    _isLeapDiving = _stepTime >= fallStart;
                    SetLeapOffset(_leapDiveOffset * (1f - fall * fall));
                    _leapShadow.transform.localScale = _leapShadowScale * Mathf.Lerp(LeapShadowMinScale, 1f, fall);
                    if (_isLeapDiving)
                    {
                        _visual.FlipOverride = _leapSlamFacesRight != IsArtFacingRight(_leapSlamPose);
                    }
                    else
                    {
                        FaceLeapTarget(_leapHoverSprite);
                    }

                    if (fill >= 1f)
                    {
                        LandLeap();
                    }

                    return false;

                default:
                    if (_stepTime < _data.LeapLandTime)
                    {
                        return false;
                    }

                    HideTelegraphs();
                    _count++;
                    if (_count >= _data.LeapCount)
                    {
                        return true;
                    }

                    BeginLeap();
                    return false;
            }
        }

        // 떠 있는 동안은 플레이어 쪽을 본다.
        private void FaceLeapTarget(Sprite pose)
        {
            float airX = Body.position.x + _leapOffset.x;
            _visual.FlipOverride = (TargetPosition.x > airX) != IsArtFacingRight(pose);
        }

        private bool IsArtFacingRight(Sprite sprite)
        {
            return sprite != null && System.Array.IndexOf(_rightFacingPoseSprites, sprite) >= 0;
        }

        // 지금 플레이어 자리를 착지 자리로 정하고 범위를 깐다. 구조물에 걸리는 자리면 가장 가까운 설 수 있는 자리로 옮긴다.
        // 판정 위치는 착지 자리로 옮기고, 그림은 공중 자리에 그대로 보이도록 거리를 다시 잡는다.
        // 왕이 플레이어 바로 위 부채꼴 안에 떠 있으면 수직으로, 아니면 착지 쪽으로 비스듬히 내리꽂는다.
        private void LockLeap()
        {
            _step = Step.LeapLock;
            _stepTime = 0f;
            Vector2 air = Body.position + _leapOffset;
            Vector2 land = FindLandingPoint(ClampToArena(TargetPosition));
            Body.position = land;
            transform.position = land;
            _leapDiveOffset = air - land;
            SetLeapOffset(_leapDiveOffset);

            Vector2 fromTarget = air - TargetPosition;
            bool isAbove = fromTarget.sqrMagnitude > 0.0001f && Vector2.Angle(Vector2.up, fromTarget) <= _data.LeapVerticalSlamAngle * 0.5f;
            _leapSlamPose = isAbove || _leapDiagonalSlamSprite == null ? _leapVerticalSlamSprite : _leapDiagonalSlamSprite;
            // 대각 그림은 그려진 방향으로 내리꽂으므로 내려갈 쪽을 보게 한다. 수직 그림은 플레이어 쪽을 본다.
            _leapSlamFacesRight = isAbove ? TargetPosition.x > air.x : land.x > air.x;
            _leapShadow.enabled = true;
            _leapShadow.transform.localScale = _leapShadowScale * LeapShadowMinScale;
            _slashRange.BuildCircle(_data.LeapRadius);
            _slashFill.CopyLimits(_slashRange);
            _slashFill.SetRadius(0f);
            _slashRange.ResetColor();
            _slashFill.ResetColor();
            _slashRange.Visible = true;
            _slashFill.Visible = true;
        }

        private void LandLeap()
        {
            _step = Step.LeapLand;
            _stepTime = 0f;
            EndLeapVisual();
            HitOverlapping(Body.position, _data.LeapRadius);
            _slashFill.SetColor(Color.white);
            FeedbackPlayer.Play(_data.LeapImpactFeedback, Body.position);
            FeedbackPlayer.Play(_data.LeapFireFeedback, Body.position);
            AudioManager.Play(_data.LeapImpactSound);
            // 화면 전체를 덮는 칸을 붉게 잠깐 반짝인다. 유성우 물들이기와 같은 칸이지만 두 패턴은 겹치지 않는다.
            PlaceShockCover();
            _screenFlashTime = 0f;
        }

        private void SetLeapOffset(Vector2 offset)
        {
            _leapOffset = offset;
            _visual.Shift = offset;
        }

        private void EndLeapVisual()
        {
            _isLeapDiving = false;
            _visual.FlipOverride = null;
            if (!IsAirborne && !_leapShadow.enabled)
            {
                return;
            }

            SetLeapOffset(Vector2.zero);
            _leapShadow.enabled = false;
            _leapShadow.transform.localScale = _leapShadowScale;
        }

        private Vector2 FindLandingPoint(Vector2 point)
        {
            if (CanStandAt(point))
            {
                return point;
            }

            for (float distance = LandSearchStep; distance <= LandSearchMax; distance += LandSearchStep)
            {
                for (int i = 0; i < LandSearchDirections; i++)
                {
                    float angle = i * (360f / LandSearchDirections) * Mathf.Deg2Rad;
                    Vector2 candidate = point + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                    if (CanStandAt(candidate))
                    {
                        return candidate;
                    }
                }
            }

            return point;
        }

        private Vector2 ClampToArena(Vector2 point)
        {
            ArenaBounds arena = ArenaBounds.Current;
            if (arena == null)
            {
                return point;
            }

            float radius = BodyRadius;
            Bounds bounds = arena.Bounds;
            point.x = Mathf.Clamp(point.x, bounds.min.x + radius, bounds.max.x - radius);
            point.y = Mathf.Clamp(point.y, bounds.min.y + radius, bounds.max.y - radius);
            return point;
        }

        // ── 왕관 투하: 왕관을 하늘로 던져 올리면 큰 그림자가 플레이어를 따라가다 멈추고, 금빛 원이 다 차는 순간 왕관이 박힌다.
        // 그 뒤 왕은 박힌 왕관으로 가장 짧은 길을 달려가 줍는다. 던진 순간부터 주울 때까지 왕관이 없어 맞는다.
        // 예측 과제: 목적지(왕관)가 보이는 달리기라, 왕이 1초 뒤 어디쯤 있을지 경로 위에 폭탄을 둔다.

        private void BeginCrownDrop()
        {
            _step = Step.DropThrow;
            _stepTime = 0f;
            Vector2 start = (Vector2)_crown.transform.position;
            _crownRaise = 0f;
            SetCrown(false);
            _isCrownAirborne = true;
            _crownPosition = start;
            _flyingCrown.position = start;
            _flyingCrown.localScale = _flyingCrownScale * BigCrownScale;
            _flyingCrown.gameObject.SetActive(true);
            AudioManager.Play(_data.CrownThrowSound);
        }

        private bool TickCrownDrop(float deltaTime)
        {
            switch (_step)
            {
                case Step.DropThrow:
                    // 왕관이 하늘에 있는 동안 왕은 천천히 다가와, 서 있는 과녁이 되지 않게 한다.
                    WalkCrownless(deltaTime);
                    float rise = Mathf.Clamp01(_stepTime / _data.CrownDropThrowTime);
                    _flyingCrown.position = _crownPosition + Vector2.up * (LeapHeight * rise * rise);
                    _flyingCrown.Rotate(0f, 0f, -900f * deltaTime);
                    if (rise >= 1f)
                    {
                        _step = Step.DropTrack;
                        _stepTime = 0f;
                        _dropPoint = ClampToArena(TargetPosition);
                        _dropShadow.enabled = true;
                        PlaceDropShadow(0f);
                    }

                    return false;

                case Step.DropTrack:
                    WalkCrownless(deltaTime);
                    Vector2 toTarget = TargetPosition - _dropPoint;
                    float step = Mathf.Min(_data.CrownDropTrackSpeed * deltaTime, toTarget.magnitude);
                    if (step > 0.0001f)
                    {
                        _dropPoint = ClampToArena(_dropPoint + toTarget.normalized * step);
                    }

                    PlaceDropShadow(0f);
                    if (_stepTime >= _data.CrownDropTrackTime)
                    {
                        LockCrownDrop();
                    }

                    return false;

                case Step.DropLock:
                    WalkCrownless(deltaTime);
                    float fill = Mathf.Clamp01(_stepTime / _data.CrownDropLockTime);
                    _dropFill.SetRadius(_data.CrownDropRadius * fill);
                    float fallStart = _data.CrownDropLockTime - _data.CrownDropFallTime;
                    float fall = Mathf.Clamp01((_stepTime - fallStart) / _data.CrownDropFallTime);
                    _flyingCrown.position = _dropPoint + Vector2.up * (LeapHeight * (1f - fall * fall));
                    _flyingCrown.rotation = Quaternion.identity;
                    PlaceDropShadow(fall);
                    if (fill >= 1f)
                    {
                        ImpactCrownDrop();
                    }

                    return false;

                case Step.DropStagger:
                    if (_stepTime >= _data.CrownDropStagger)
                    {
                        HideDropMarker();
                        _step = Step.DropRetrieve;
                        _stepTime = 0f;
                    }

                    return false;

                case Step.DropRetrieve:
                    if (RunToCrown(deltaTime))
                    {
                        PickUpCrown();
                        return true;
                    }

                    if (_stepTime >= _data.CrownRetrieveTimeout)
                    {
                        _step = Step.DropReturn;
                        _stepTime = 0f;
                        _crownReturnFromScale = _flyingCrown.localScale;
                    }

                    return false;

                default:
                    // 못 주운 왕관이 포물선을 그리며 스스로 머리로 돌아온다.
                    float t = Mathf.Clamp01(_stepTime / _data.CrownReturnTime);
                    float ease = t * t * (3f - 2f * t);
                    _flyingCrown.position = Vector2.Lerp(_dropPoint, HeadCrownPosition(), ease) + Vector2.up * (4f * CrownReturnArc * t * (1f - t));
                    _flyingCrown.localScale = Vector3.Lerp(_crownReturnFromScale, _flyingCrownScale, ease);
                    _flyingCrown.Rotate(0f, 0f, -900f * deltaTime);
                    if (t >= 1f)
                    {
                        PickUpCrown();
                        return true;
                    }

                    return false;
            }
        }

        private void WalkCrownless(float deltaTime)
        {
            if (_data.CrownlessWalkSpeed > 0f)
            {
                MoveTowardTarget(_data.CrownlessWalkSpeed, deltaTime);
            }
        }

        // 떨어질 자리를 멈춘다. 왕이 주우러 가야 하므로 구조물에 걸리는 자리면 가장 가까운 설 수 있는 자리로 옮긴다.
        private void LockCrownDrop()
        {
            _step = Step.DropLock;
            _stepTime = 0f;
            _dropPoint = FindLandingPoint(_dropPoint);
            PlaceDropShadow(0f);
            _dropRange.transform.position = _dropPoint;
            _dropFill.transform.position = _dropPoint;
            _dropRange.BuildCircle(_data.CrownDropRadius);
            _dropFill.CopyLimits(_dropRange);
            _dropFill.SetRadius(0f);
            _dropRange.ResetColor();
            _dropFill.ResetColor();
            _dropRange.Visible = true;
            _dropFill.Visible = true;
        }

        private void ImpactCrownDrop()
        {
            _step = Step.DropStagger;
            _stepTime = 0f;
            _isCrownAirborne = false;
            _dropShadow.enabled = false;
            _flyingCrown.position = _dropPoint;
            _flyingCrown.rotation = Quaternion.Euler(0f, 0f, CrownRestAngle);
            HitOverlapping(_dropPoint, _data.CrownDropRadius);
            _dropFill.SetColor(Color.white);
            FeedbackPlayer.Play(_data.CrownDropImpactFeedback, _dropPoint);
            AudioManager.Play(_data.CrownDropImpactSound);
        }

        // 가장 짧은 길로 달려간다. 왕관 자리가 직선으로 보이면 곧장 간다. 닿으면 true.
        private bool RunToCrown(float deltaTime)
        {
            Vector2 toCrown = _dropPoint - Body.position;
            float distance = toCrown.magnitude;
            if (distance <= CrownPickupDistance)
            {
                return true;
            }

            FlowField field = FlowField.Current;
            Vector2 direction = field != null ? field.GetDirectionTo(_dropPoint, Body.position) : toCrown / distance;
            float step = Mathf.Min(_data.CrownRetrieveSpeed * deltaTime, distance);
            Body.MovePosition(Body.position + direction * ClearDistance(direction, step, BodyRadius));
            return false;
        }

        private void PickUpCrown()
        {
            HideDropMarker();
            _flyingCrown.localScale = _flyingCrownScale;
            _flyingCrown.rotation = Quaternion.identity;
            CatchCrown();
            _visual.Flash(_data.ImmuneFlashColor, 0.2f);
        }

        // 그림자는 왕관이 내려올수록 커지고 진해진다.
        private void PlaceDropShadow(float fall)
        {
            Transform shadow = _dropShadow.transform;
            shadow.position = _dropPoint;
            shadow.localScale = _dropShadowScale * Mathf.Lerp(LeapShadowMinScale, 1f, fall);
        }

        // 씬이 닫힐 때는 오브젝트가 지워지는 순서가 정해져 있지 않아, 보스보다 표시가 먼저 지워져 있을 수 있다.
        private void HideDropMarker()
        {
            if (_dropShadow != null)
            {
                _dropShadow.enabled = false;
            }

            if (_dropRange != null)
            {
                _dropRange.Visible = false;
            }

            if (_dropFill != null)
            {
                _dropFill.Visible = false;
            }
        }

        // ── 유성우: 보라 소용돌이 속에서 두 손을 들면 맵이 보랏빛으로 물들고, 맵 전체에 유성이 하나씩 떨어진다.
        // 떨어질 자리마다 원이 차오르다 떨어진다. 그중 몇 개는 그 순간 플레이어 자리에 떨어져, 가만히 서서 다 피하지 못하게 한다.

        private void BeginMeteor()
        {
            _step = Step.MeteorCast;
            _stepTime = 0f;
            _meteorsLaunched = 0;
            _meteorsLanded = 0;
            AudioManager.Play(_data.MeteorCastSound);
        }

        private bool TickMeteor(float deltaTime)
        {
            int count = _meteors.Length;
            if (_step == Step.MeteorCast)
            {
                float interval = _data.MeteorCastTime / count;
                while (_meteorsLaunched < count && _stepTime >= _meteorsLaunched * interval)
                {
                    LaunchMeteor(_meteorsLaunched);
                    _meteorsLaunched++;
                }

                if (_meteorsLaunched >= count)
                {
                    _step = Step.MeteorWait;
                }

                return false;
            }

            return _meteorsLanded >= _meteorsLaunched;
        }

        private void LaunchMeteor(int index)
        {
            Vector2 point = IsTargetedMeteor(index) ? TargetPosition : RandomArenaPoint(_data.MeteorEdgeMargin);
            _meteors[index].Launch(point, _data.MeteorFallTime, _data.MeteorRadius, _onMeteorImpact);
        }

        // 조준 유성은 소환 시간 전체에 고르게 나눠, 처음부터 끝까지 한곳에 서 있을 수 없게 한다.
        private bool IsTargetedMeteor(int index)
        {
            int targeted = Mathf.Min(_data.MeteorTargetedCount, _meteors.Length);
            for (int k = 0; k < targeted; k++)
            {
                if (index == Mathf.RoundToInt((k + 1f) * _meteors.Length / (targeted + 1f)))
                {
                    return true;
                }
            }

            return false;
        }

        // 유성은 하늘에서 떨어지므로 구조물과 상관없이 맵 안 어디든 떨어진다.
        private Vector2 RandomArenaPoint(float margin)
        {
            ArenaBounds arena = ArenaBounds.Current;
            if (arena == null)
            {
                return TargetPosition + Random.insideUnitCircle * 10f;
            }

            Bounds bounds = arena.Bounds;
            return new Vector2(
                Random.Range(bounds.min.x + margin, bounds.max.x - margin),
                Random.Range(bounds.min.y + margin, bounds.max.y - margin));
        }

        private void OnMeteorImpact(Meteor meteor)
        {
            _meteorsLanded++;
            HitOverlapping(meteor.Position, meteor.Radius);
            FeedbackPlayer.Play(_data.MeteorImpactFeedback, meteor.Position);
            AudioManager.Play(_data.MeteorImpactSound);
        }

        // ── 천지 가르기: 두 손으로 검을 높이 들고 맵을 크게 세 번에 걸쳐 가른다. 한 묶음마다 예고 두 개를 먼저 보여 주고 그 순서대로 두 번 벤다.
        //   ① 왕이 선 줄을 경계로 좌·우(또는 위·아래) 절반을 차례로 벤다.
        //   ② 축을 바꿔 위·아래(또는 좌·우) 절반을 차례로 벤다.
        //   ③ 맵을 세로·가로 각각 여러 칸으로 나눠 한 칸씩 걸러 벤다(세로 줄무늬 다음 가로 줄무늬). 두 번 다 안전한 칸이 늘 남는다.
        // 한 번에 기억할 예고는 늘 두 개뿐이라, 횟수가 늘어도 헷갈리지 않는다. 벽과 상관없이 벤다.

        private void BeginSword()
        {
            _step = Step.SwordCharge;
            _isFirstSplitHorizontal = Random.value < 0.5f;
            BeginSwordStage(0);
        }

        private void BeginSwordStage(int stage)
        {
            _swordStage = stage;
            _stepTime = 0f;
            _isSecondWaveShown = false;
            _isFirstSlashDone = false;
            _isSecondSlashDone = false;
            _lastSlashAt = -1f;
            _swordOrigin = Body.position;
            if (stage < 2)
            {
                bool isHorizontal = stage == 0 ? _isFirstSplitHorizontal : !_isFirstSplitHorizontal;
                float sign = Random.value < 0.5f ? 1f : -1f;
                _swordFirstDirection = isHorizontal ? new Vector2(sign, 0f) : new Vector2(0f, sign);
                _aimDirection = _swordFirstDirection;
                PlaceSwordLine(isHorizontal);
                ShowHalf(_halfFirst, _swordFirstDirection);
                return;
            }

            _verticalOffset = Random.value;
            _horizontalOffset = Random.value;
            ShowStripes(true);
        }

        private float SwordStageWait()
        {
            return _swordStage == 0 ? _data.SwordWait : _data.SwordFollowupWait;
        }

        private float FirstSlashAt()
        {
            return _data.SwordWaveGap + _data.SwordWaveTime + SwordStageWait();
        }

        private bool TickSword(float deltaTime)
        {
            float time = _stepTime;
            if (!_isSecondWaveShown && time >= _data.SwordWaveGap)
            {
                _isSecondWaveShown = true;
                if (_swordStage < 2)
                {
                    ShowHalf(_halfSecond, -_swordFirstDirection);
                }
                else
                {
                    ShowStripes(false);
                }
            }

            if (!_isFirstSlashDone)
            {
                if (time >= FirstSlashAt())
                {
                    _isFirstSlashDone = true;
                    _lastSlashAt = time;
                    _step = Step.SwordSlash;
                    if (_swordStage < 2)
                    {
                        SlashHalf(_halfFirst, _swordFirstDirection);
                    }
                    else
                    {
                        SlashStripes(true);
                    }
                }

                return false;
            }

            if (!_isSecondSlashDone)
            {
                if (time >= FirstSlashAt() + _data.SwordSlashGap)
                {
                    _isSecondSlashDone = true;
                    _lastSlashAt = time;
                    _secondSlashAt = time;
                    if (_swordStage < 2)
                    {
                        SlashHalf(_halfSecond, -_swordFirstDirection);
                    }
                    else
                    {
                        SlashStripes(false);
                    }
                }

                return false;
            }

            if (_swordStage < 2)
            {
                if (time >= _secondSlashAt + _data.SwordStageGap)
                {
                    _step = Step.SwordCharge;
                    BeginSwordStage(_swordStage + 1);
                }

                return false;
            }

            return time >= _secondSlashAt + _data.SwordRecover;
        }

        // 각 묶음의 첫 베기 직전 0~1. 끝으로 갈수록 가파르게 뒤로 젖히며 위로 늘어나고 떨려, 곧 내리칠 것처럼 보인다.
        private float SwordWindup()
        {
            if (_pattern != Pattern.Sword || State != EnemyState.Attack || _isFirstSlashDone)
            {
                return 0f;
            }

            float t = Mathf.Clamp01(1f - (FirstSlashAt() - _stepTime) / SwordWindupTime);
            return t * t;
        }

        // 벤 직후 잠깐은 내리친 자세, 그 밖에는 검을 높이 든 자세다. 마지막 베기 뒤에는 내리친 채 끝난다.
        private bool IsSwordStriking()
        {
            if (State != EnemyState.Attack || _lastSlashAt < 0f)
            {
                return false;
            }

            if (_swordStage == 2 && _isSecondSlashDone)
            {
                return true;
            }

            return _stepTime - _lastSlashAt < _data.SwordFollowThrough;
        }

        // 좌·우로 가르면 세로선, 위·아래로 가르면 가로선을 왕이 선 자리를 지나 맵 끝까지 긋는다.
        private void PlaceSwordLine(bool isHorizontalSplit)
        {
            Bounds bounds = SwordArena();
            Transform line = _swordLine.transform;
            float size = _swordLine.sprite != null ? _swordLine.sprite.bounds.size.x : 1f;
            if (isHorizontalSplit)
            {
                line.position = new Vector3(_swordOrigin.x, bounds.center.y, 0f);
                line.localScale = new Vector3(SwordLineWidth / size, bounds.size.y / size, 1f);
            }
            else
            {
                line.position = new Vector3(bounds.center.x, _swordOrigin.y, 0f);
                line.localScale = new Vector3(bounds.size.x / size, SwordLineWidth / size, 1f);
            }

            _swordLine.color = SwordLineColor;
        }

        private Bounds SwordArena()
        {
            ArenaBounds arena = ArenaBounds.Current;
            return arena != null ? arena.Bounds : new Bounds(_swordOrigin, new Vector3(60f, 60f, 1f));
        }

        private void ShowHalf(HalfSlashWarning half, Vector2 direction)
        {
            half.Show(SwordArena(), _swordOrigin, direction, _data.SwordWaveTime);
            AudioManager.Play(_data.SwordWaveSound);
        }

        // 경계선에서 그쪽 맵 끝까지, 맵 전체 폭으로 벤다.
        private void SlashHalf(HalfSlashWarning half, Vector2 direction)
        {
            _aimDirection = direction;
            half.Slash();
            const float Reach = 200f;
            HitInBox(direction, Reach, Reach * 2f, true);
            FeedbackPlayer.Play(_data.SwordSlashFeedback, Body.position);
            AudioManager.Play(_data.SwordSlashSound);
        }

        // 줄무늬 한 벌의 위험한 칸들에 예고를 깐다. 세로 줄무늬는 아래에서 위로, 가로 줄무늬는 왼쪽에서 오른쪽으로 퍼진다.
        // 세로 줄무늬는 예고 칸 배열의 앞 절반, 가로 줄무늬는 뒤 절반을 쓴다. 밀린 줄무늬는 양 끝 칸이 잘려 한 칸 더 생길 수 있다.
        private void ShowStripes(bool isVertical)
        {
            Bounds bounds = SwordArena();
            float offset = isVertical ? _verticalOffset : _horizontalOffset;
            float size = isVertical ? bounds.size.x : bounds.size.y;
            int slot = isVertical ? 0 : _stripeWarnings.Length / 2;
            int slotEnd = isVertical ? _stripeWarnings.Length / 2 : _stripeWarnings.Length;
            int lineSlot = isVertical ? 0 : _gridLines.Length / 2;
            int lineEnd = isVertical ? _gridLines.Length / 2 : _gridLines.Length;
            for (int i = 0; i <= StripePairCount(); i++)
            {
                if (!TryGetDangerStripe(i, offset, size, out float start, out float width))
                {
                    continue;
                }

                if (slot < slotEnd)
                {
                    if (isVertical)
                    {
                        _stripeWarnings[slot].ShowRect(new Vector2(bounds.min.x + start + width * 0.5f, bounds.min.y), Vector2.up, bounds.size.y, width, _data.SwordWaveTime);
                    }
                    else
                    {
                        _stripeWarnings[slot].ShowRect(new Vector2(bounds.min.x, bounds.min.y + start + width * 0.5f), Vector2.right, bounds.size.x, width, _data.SwordWaveTime);
                    }

                    slot++;
                }

                // 위험한 칸 양쪽 가장자리에 반쪽 가르기와 같은 경계선을 맵 끝까지 긋는다.
                if (lineSlot + 1 < lineEnd)
                {
                    PlaceGridLine(_gridLines[lineSlot++], bounds, isVertical, start);
                    PlaceGridLine(_gridLines[lineSlot++], bounds, isVertical, start + width);
                }
            }

            _isGridShown = true;
            AudioManager.Play(_data.SwordWaveSound);
        }

        private void PlaceGridLine(SpriteRenderer line, Bounds bounds, bool isVertical, float at)
        {
            float size = _swordLine.sprite != null ? _swordLine.sprite.bounds.size.x : 1f;
            if (isVertical)
            {
                line.transform.position = new Vector3(bounds.min.x + at, bounds.center.y, 0f);
                line.transform.localScale = new Vector3(SwordLineWidth / size, bounds.size.y / size, 1f);
            }
            else
            {
                line.transform.position = new Vector3(bounds.center.x, bounds.min.y + at, 0f);
                line.transform.localScale = new Vector3(bounds.size.x / size, SwordLineWidth / size, 1f);
            }

            line.color = SwordLineColor;
            line.enabled = true;
        }

        private void HideGridLines()
        {
            _isGridShown = false;
            if (_gridLines == null)
            {
                return;
            }

            for (int i = 0; i < _gridLines.Length; i++)
            {
                if (_gridLines[i] != null)
                {
                    _gridLines[i].enabled = false;
                }
            }
        }

        // 위험 + 안전 한 쌍이 맵에 몇 번 들어가는지.
        private int StripePairCount()
        {
            return Mathf.Max(1, _data.StripeCount / 2);
        }

        // index번(0 ~ 쌍 수) 위험 칸의 시작 위치(맵 왼쪽·아래 끝 기준)와 폭. 줄무늬를 offset만큼 밀고 맵 밖으로 나간 부분은 잘라 낸다.
        // 맵 안에 남는 부분이 없으면 false.
        private bool TryGetDangerStripe(int index, float offset, float arenaSize, out float start, out float width)
        {
            float period = arenaSize / StripePairCount();
            float from = (offset - 1f + index) * period;
            float to = from + period * _data.StripeDangerRatio;
            start = Mathf.Max(from, 0f);
            width = Mathf.Min(to, arenaSize) - start;
            return width > 0.05f;
        }

        private void SlashStripes(bool isVertical)
        {
            Bounds bounds = SwordArena();
            float offset = isVertical ? _verticalOffset : _horizontalOffset;
            float size = isVertical ? bounds.size.x : bounds.size.y;
            for (int i = 0; i <= StripePairCount(); i++)
            {
                if (!TryGetDangerStripe(i, offset, size, out float start, out float width))
                {
                    continue;
                }

                float center = start + width * 0.5f;
                if (isVertical)
                {
                    HitInWorldRect(new Vector2(bounds.min.x + center, bounds.center.y), new Vector2(width, bounds.size.y));
                }
                else
                {
                    HitInWorldRect(new Vector2(bounds.center.x, bounds.min.y + center), new Vector2(bounds.size.x, width));
                }
            }

            int first = isVertical ? 0 : _stripeWarnings.Length / 2;
            int last = isVertical ? _stripeWarnings.Length / 2 : _stripeWarnings.Length;
            for (int i = first; i < last; i++)
            {
                if (_stripeWarnings[i].gameObject.activeSelf)
                {
                    _stripeWarnings[i].Slash();
                }
            }

            // 벤 줄무늬의 경계선도 예고와 함께 걷어 낸다. 남은 쪽 경계선만 보여 다음에 피할 곳이 헷갈리지 않는다.
            int lineFirst = isVertical ? 0 : _gridLines.Length / 2;
            int lineLast = isVertical ? _gridLines.Length / 2 : _gridLines.Length;
            for (int i = lineFirst; i < lineLast; i++)
            {
                _gridLines[i].enabled = false;
            }

            _aimDirection = isVertical ? Vector2.down : Vector2.right;
            FeedbackPlayer.Play(_data.SwordSlashFeedback, Body.position);
            AudioManager.Play(_data.SwordSlashSound);
        }

        // 유성우 동안 맵이 보랏빛으로 물들고, 왕 주위에 보라 소용돌이가 돈다.
        private void TickPhase3Visuals(float deltaTime)
        {
            bool isMeteor = _pattern == Pattern.Meteor && (State == EnemyState.Telegraph || State == EnemyState.Attack);
            _swordLine.enabled = _pattern == Pattern.Sword && State == EnemyState.Attack && _swordStage < 2;
            if (_isGridShown && !(_pattern == Pattern.Sword && State == EnemyState.Attack && _swordStage == 2))
            {
                HideGridLines();
            }
            _meteorAura.enabled = isMeteor && _meteorAuraFrames != null && _meteorAuraFrames.Length > 0;
            if (_meteorAura.enabled)
            {
                int frame = (int)(Time.time / MeteorAuraFrameTime) % _meteorAuraFrames.Length;
                _meteorAura.sprite = _meteorAuraFrames[frame];
            }

            if (_pattern == Pattern.Star && State == EnemyState.Attack && _step == Step.StarShock)
            {
                return;
            }

            float target = isMeteor ? _data.MeteorTintAlpha : 0f;
            if (isMeteor && !_mapTint.enabled)
            {
                PlaceMapTint();
            }

            _tintAlpha = Mathf.MoveTowards(_tintAlpha, target, _data.MeteorTintAlpha / _data.MeteorTintFadeTime * deltaTime);
            Color color = _data.MeteorTintColor;
            color.a = _tintAlpha;
            _mapTint.color = color;
            _mapTint.enabled = _tintAlpha > 0f;

            if (_screenFlashTime >= 0f)
            {
                _screenFlashTime += deltaTime;
                float fade = 1f - Mathf.Clamp01(_screenFlashTime / _data.LeapFlashTime);
                Color flash = _data.LeapFlashColor;
                flash.a *= fade;
                _mapTint.color = flash;
                _mapTint.enabled = fade > 0f;
                if (fade <= 0f)
                {
                    _screenFlashTime = -1f;
                }
            }
        }

        // 맵은 전투 중 움직이지 않지만, 보스가 만들어질 때는 맵 범위가 아직 안 잡혀 있을 수 있어 쓸 때 맞춘다.
        private void PlaceMapTint()
        {
            ArenaBounds arena = ArenaBounds.Current;
            if (arena == null)
            {
                return;
            }

            Bounds bounds = arena.Bounds;
            Transform tint = _mapTint.transform;
            tint.position = bounds.center;
            float size = _mapTint.sprite != null ? _mapTint.sprite.bounds.size.x : 1f;
            tint.localScale = new Vector3(bounds.size.x / size, bounds.size.y / size, 1f);
        }

        // ── 별의 의식: 왕관을 맵 가운데 공중에 띄우고 별 낙인을 그린 뒤, 별 끝을 그린 순서대로 1초마다 순간이동한다.
        // 다음에 나타날 끝이 1초 먼저 빛나서, 빛나는 순간 그 끝에 던진 폭탄이 나타나는 순간 터진다. 왕관이 없는 동안 맞는다.
        // 다 돌 때까지 못 맞히면 가운데로 돌아와 왕관을 쓰고, 안전지대를 띄운 뒤 화면을 보라색으로 덮으며 맵 전체를 한 번 친다.

        private void BeginStar()
        {
            _step = Step.StarTeleport;
            _stepTime = 0f;
            _teleportsDone = 0;
            _dashSoundIndex = -1;
            _isSafeZoneShown = false;
            _isShockCharging = false;
            _isShockDone = false;
            _dashOrigin = Body.position;
            _starCenter = ArenaCenter();
            for (int i = 0; i < 5; i++)
            {
                float angle = (90f + i * 72f) * Mathf.Deg2Rad;
                _starPoints[i] = FindLandingPoint(_starCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _data.StarRadius);
                Transform tip = _starTips[i].transform;
                tip.position = _starPoints[i];
                tip.localScale = _starTipScale;
                _starTips[i].enabled = true;
                SetTipGlow(i, 0f);
            }

            // 별 그림을 별 크기에 맞춰 가운데에 깔고, 투명에서 서서히 나타나게 한다.
            Transform sigil = _starSigil.transform;
            sigil.position = _starCenter;
            float spriteSize = _starSigil.sprite != null ? _starSigil.sprite.bounds.size.x : 1f;
            float scale = _data.StarRadius / StarSpriteTipRatio / spriteSize;
            sigil.localScale = new Vector3(scale, scale, 1f);
            SetSigilAlpha(0f);
            _starSigil.enabled = true;

            // 왕관이 머리를 떠나 별 꼭대기에 걸친다. 첫 꼭짓점에 도착하기 전까지는 무적이다.
            // 별이 나타나는 동안 가운데 서 있는 왕을 맞히면 "빛나는 끝을 노린다"는 공략을 건너뛰게 된다.
            SetCrown(false);
            HitReceiver.IsShielded = true;
            _flyingCrown.position = StarCrownPoint();
            _flyingCrown.rotation = Quaternion.identity;
            _flyingCrown.localScale = _flyingCrownScale * BigCrownScale;
            _flyingCrown.gameObject.SetActive(true);
            AudioManager.Play(_data.StarDrawSound);
        }

        // 별이 나타난 뒤, 별을 그린 순서대로 끝에서 끝으로 잔상을 남기며 질주한다. 끝 j에 도착하는 시각은 별이 다 나타난 뒤 j번째 간격이다.
        // 끝 j는 도착 1초(간격) 전부터 빛나서, 빛나는 순간 그 끝에 던진 폭탄이 도착하는 순간 터진다.
        private bool TickStar(float deltaTime)
        {
            _flyingCrown.position = StarCrownPoint() + Vector2.up * (CrownBobHeight * Mathf.Sin(Time.time * CrownBobSpeed));
            if (_step == Step.StarShock)
            {
                return TickShock();
            }

            float time = _stepTime;
            float draw = _data.StarDrawTime;
            float interval = _data.StarTeleportInterval;
            int count = _data.StarTeleportCount;
            SetSigilAlpha(Mathf.Clamp01(time / draw));

            if (time >= draw + (count - 1) * interval + interval)
            {
                BeginShock();
                return false;
            }

            // 지금까지 도착한 끝 수는 시간으로 센다. 질주가 딱 끝나는 프레임을 기다리면, 도착 시각을 넘는 순간 계산이
            // 다음 끝으로 넘어가 버려 도착(무적 해제)을 놓친다.
            int arrived = time >= draw ? Mathf.Min(count, Mathf.FloorToInt((time - draw) / interval) + 1) : 0;
            if (arrived > _teleportsDone)
            {
                _teleportsDone = arrived;
                FeedbackPlayer.Play(_data.TeleportFeedback, _starPoints[StarOrder[(arrived - 1) % 5]]);
                // 첫 꼭짓점에 도착한 순간부터 맞는다.
                HitReceiver.IsShielded = false;
            }

            // 다음에 도착할 끝(도착 시각이 아직 안 지난 첫 끝).
            int next = Mathf.Max(0, Mathf.CeilToInt((time - draw) / interval));
            bool hasNext = next < count;
            float arrival = draw + next * interval;
            UpdateTipGlow(hasNext && arrival - time <= interval ? next : -1);

            if (!hasNext)
            {
                Body.position = _starPoints[StarOrder[(count - 1) % 5]];
                return false;
            }

            Vector2 from = next == 0 ? _dashOrigin : _starPoints[StarOrder[(next - 1) % 5]];
            Vector2 to = _starPoints[StarOrder[next % 5]];
            float dashStart = arrival - _data.StarDashTime;
            if (time < dashStart)
            {
                Body.position = from;
                return false;
            }

            if (_dashSoundIndex < next)
            {
                _dashSoundIndex = next;
                _aimDirection = (to - from).sqrMagnitude > 0.0001f ? (to - from).normalized : _aimDirection;
                AudioManager.Play(_data.TeleportSound);
            }

            float t = Mathf.Clamp01((time - dashStart) / _data.StarDashTime);
            DashTo(from, to, t);
            return false;
        }

        // 아주 빠르게 미끄러지듯 옮겨 가며 지나간 자리에 잔상을 남긴다. 공중이 아니라 땅을 달리지만 너무 빨라 벽은 무시한다.
        private void DashTo(Vector2 from, Vector2 to, float progress)
        {
            float eased = progress * progress * (3f - 2f * progress);
            Vector2 point = Vector2.Lerp(from, to, eased);
            Body.position = point;
            transform.position = point;
            if (progress < 1f && Time.time >= _nextAfterimageTime)
            {
                _nextAfterimageTime = Time.time + AfterimageInterval;
                SpawnAfterimage();
            }
        }

        private Vector2 StarCrownPoint()
        {
            return _starCenter + Vector2.up * (_data.StarRadius + CrownOnStarHeight);
        }

        private void SetSigilAlpha(float alpha)
        {
            Color color = _starSigil.color;
            color.a = alpha;
            _starSigil.color = color;
        }

        // order번째로 도착할 끝을 밝게 맥동시키고 나머지는 숨긴다. -1이면 모두 숨긴다. 별 그림에 이미 끝이 그려져 있어 빛나는 끝만 표시한다.
        private void UpdateTipGlow(int order)
        {
            int lit = order >= 0 ? StarOrder[order % 5] : -1;
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * StarTipPulseSpeed);
            for (int i = 0; i < 5; i++)
            {
                SetTipGlow(i, i == lit ? pulse : 0f);
                _starTips[i].transform.localScale = _starTipScale * (i == lit ? 1.4f : 1f);
            }
        }

        private void SetTipGlow(int index, float alpha)
        {
            Color color = _starTips[index].color;
            color.a = alpha;
            _starTips[index].color = color;
        }

        // 몸 그림을 그 순간 그대로 베껴 보랏빛으로 남긴다. 잔상은 돌려 쓴다.
        private void SpawnAfterimage()
        {
            if (_afterimages.Length == 0)
            {
                return;
            }

            int index = _nextAfterimage;
            _nextAfterimage = (_nextAfterimage + 1) % _afterimages.Length;
            SpriteRenderer ghost = _afterimages[index];
            Transform body = _bodyRenderer.transform;
            ghost.sprite = _bodyRenderer.sprite;
            ghost.flipX = _bodyRenderer.flipX;
            ghost.transform.SetPositionAndRotation(body.position, body.rotation);
            ghost.transform.localScale = body.lossyScale;
            ghost.color = AfterimageColor;
            ghost.enabled = true;
            _afterimageTimes[index % _afterimageTimes.Length] = Time.time;
        }

        private void TickAfterimages()
        {
            for (int i = 0; i < _afterimages.Length; i++)
            {
                SpriteRenderer ghost = _afterimages[i];
                if (!ghost.enabled)
                {
                    continue;
                }

                float t = (Time.time - _afterimageTimes[i % _afterimageTimes.Length]) / AfterimageFade;
                if (t >= 1f)
                {
                    ghost.enabled = false;
                    continue;
                }

                Color color = AfterimageColor;
                color.a *= 1f - t;
                ghost.color = color;
            }
        }

        // 못 맞혔다. 가운데로 질주해 돌아와 왕관을 다시 쓰고 충격파를 모은다. 다시 무적이다.
        private void BeginShock()
        {
            _step = Step.StarShock;
            _stepTime = 0f;
            UpdateTipGlow(-1);
            _shockFrom = Body.position;
            _shockTo = FindLandingPoint(_starCenter);
            _isShockArrived = false;
            AudioManager.Play(_data.TeleportSound);
            PlaceShockCover();
        }

        private bool TickShock()
        {
            if (!_isShockArrived)
            {
                float dash = Mathf.Clamp01(_stepTime / _data.StarDashTime);
                DashTo(_shockFrom, _shockTo, dash);
                if (dash < 1f)
                {
                    return false;
                }

                _isShockArrived = true;
                _stepTime = 0f;
                _flyingCrown.gameObject.SetActive(false);
                _flyingCrown.localScale = _flyingCrownScale;
                SetCrown(true);
                _visual.Flash(_data.ImmuneFlashColor, 0.3f);
            }

            float t = _stepTime;
            float safeAt = _data.ShockSafeZoneDelay;
            float fadeAt = safeAt + _data.ShockFadeDelay;
            float coveredAt = fadeAt + _data.ShockFadeIn;
            float hitAt = coveredAt + _data.ShockHold;
            float endAt = hitAt + _data.ShockFadeOut;

            if (!_isSafeZoneShown && t >= safeAt)
            {
                _isSafeZoneShown = true;
                ShowSafeZones();
            }

            if (!_isShockCharging && t >= fadeAt)
            {
                _isShockCharging = true;
                AudioManager.Play(_data.ShockChargeSound);
            }

            if (!_isShockDone && t >= hitAt)
            {
                _isShockDone = true;
                HitAllOutside(_safeCenters, _safeCount, _data.SafeZoneRadius);
                FeedbackPlayer.Play(_data.ShockFeedback, Body.position);
                AudioManager.Play(_data.ShockSound);
                HideSafeZones();
            }

            float cover = 0f;
            if (t >= fadeAt && t < coveredAt)
            {
                cover = (t - fadeAt) / _data.ShockFadeIn;
            }
            else if (t >= coveredAt && t < hitAt)
            {
                cover = 1f;
            }
            else if (t >= hitAt)
            {
                cover = 1f - Mathf.Clamp01((t - hitAt) / _data.ShockFadeOut);
            }

            Color color = _data.ShockColor;
            color.a *= cover;
            _mapTint.color = color;
            _mapTint.enabled = cover > 0f;

            if (t < endAt)
            {
                return false;
            }

            _mapTint.enabled = false;
            HideStar();
            return true;
        }

        // 화면을 다 가려야 하므로 맵보다 넉넉히 크게 덮는다.
        private void PlaceShockCover()
        {
            ArenaBounds arena = ArenaBounds.Current;
            Bounds bounds = arena != null ? arena.Bounds : new Bounds(_starCenter, new Vector3(60f, 60f, 1f));
            Transform cover = _mapTint.transform;
            cover.position = bounds.center;
            float size = _mapTint.sprite != null ? _mapTint.sprite.bounds.size.x : 1f;
            cover.localScale = new Vector3((bounds.size.x + ShockCoverMargin) / size, (bounds.size.y + ShockCoverMargin) / size, 1f);
        }

        // 왕 둘레에 고르게 나눈 방향으로, 데이터에 정한 거리 범위 안에 안전지대(보호막)를 띄운다. 방향은 매번 조금씩 흔든다.
        private void ShowSafeZones()
        {
            _safeCount = Mathf.Min(Mathf.Min(_data.SafeZoneCount, _safeCenters.Length), _safeZones.Length);
            Vector2 center = Body.position;
            float baseAngle = Random.Range(0f, 360f);
            for (int i = 0; i < _safeCount; i++)
            {
                float angle = (baseAngle + i * 360f / _safeCount + Random.Range(-SafeZoneJitter, SafeZoneJitter)) * Mathf.Deg2Rad;
                float distance = Random.Range(_data.SafeZoneDistance.x, _data.SafeZoneDistance.y);
                Vector2 point = FindLandingPoint(ClampToArena(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance));
                _safeCenters[i] = point;
                Transform zone = _safeZones[i].transform;
                zone.position = point;
                float size = _safeZones[i].sprite != null ? _safeZones[i].sprite.bounds.size.x : 1f;
                float scale = _data.SafeZoneRadius * 2f / size;
                zone.localScale = new Vector3(scale, scale, 1f);
                _safeZones[i].enabled = true;
            }
        }

        private void HideSafeZones()
        {
            for (int i = 0; i < _safeZones.Length; i++)
            {
                _safeZones[i].enabled = false;
            }
        }

        private void HideStar()
        {
            _starSigil.enabled = false;
            for (int i = 0; i < _starTips.Length; i++)
            {
                _starTips[i].enabled = false;
            }

            HideSafeZones();
        }

        private Vector2 ArenaCenter()
        {
            ArenaBounds arena = ArenaBounds.Current;
            return arena != null ? (Vector2)arena.Bounds.center : Body.position;
        }

        private static void DestroyAll(SpriteRenderer[] renderers)
        {
            if (renderers == null)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    Destroy(renderers[i].gameObject);
                }
            }
        }

        // 메테오·격자선은 월드에 따로 만든 오브젝트라, 씬이 내려갈 때 보스보다 먼저 파괴될 수 있다. 그래서 남아 있는 것만 숨긴다.
        private void HidePhase3Effects()
        {
            if (_meteors != null)
            {
                for (int i = 0; i < _meteors.Length; i++)
                {
                    if (_meteors[i] != null)
                    {
                        _meteors[i].Hide();
                    }
                }
            }

            _halfFirst.Hide();
            _halfSecond.Hide();
            for (int i = 0; i < _stripeWarnings.Length; i++)
            {
                _stripeWarnings[i].Hide();
            }

            HideGridLines();
            _meteorAura.enabled = false;
            _tintAlpha = 0f;
            _mapTint.enabled = false;
        }

        // ── 왕관 부메랑: 왕관이 벽을 뚫고 날아갔다가 돌아온다. 날아가 있는 동안 왕은 무적이 아니다.

        private void ThrowCrown()
        {
            _step = Step.CrownOut;
            _stepTime = 0f;
            _crownTravel = 0f;
            // 예고선과 같은 길(몸 한가운데에서 시작)로 날아가야 한다. 머리 위에서 출발하면 왕관과 판정이 선보다 위로 떠서 지나간다.
            _crownPosition = Body.position;
            SetCrown(false);
            _flyingCrown.position = _crownPosition;
            _flyingCrown.gameObject.SetActive(true);
            AudioManager.Play(_data.CrownThrowSound);
        }

        private bool TickCrown(float deltaTime)
        {
            // 왕관이 없는 동안 천천히 다가와, 서 있는 과녁이 아니라 1초 뒤를 읽어야 하는 과녁이 되게 한다.
            if (_data.CrownlessWalkSpeed > 0f)
            {
                MoveTowardTarget(_data.CrownlessWalkSpeed, deltaTime);
            }

            float step = _data.CrownSpeed * deltaTime;
            switch (_step)
            {
                case Step.CrownOut:
                    _crownPosition += _aimDirection * step;
                    _crownTravel += step;
                    if (_crownTravel >= _data.CrownDistance)
                    {
                        _step = Step.CrownHang;
                        _stepTime = 0f;
                    }

                    break;

                case Step.CrownHang:
                    if (_stepTime >= _data.CrownHangTime)
                    {
                        _step = Step.CrownBack;
                    }

                    break;

                default:
                    // 돌아올 때도 예고선의 시작점(몸 한가운데)으로 온다.
                    Vector2 home = Body.position;
                    Vector2 toHome = home - _crownPosition;
                    if (toHome.magnitude <= Mathf.Max(step, CrownCatchDistance))
                    {
                        CatchCrown();
                        return true;
                    }

                    _crownPosition += toHome.normalized * step;
                    break;
            }

            _flyingCrown.position = _crownPosition;
            _flyingCrown.Rotate(0f, 0f, -720f * deltaTime);
            HitOverlapping(_crownPosition, _data.CrownHitRadius);
            return false;
        }

        private void CatchCrown()
        {
            _flyingCrown.gameObject.SetActive(false);
            SetCrown(true);
            AudioManager.Play(_data.CrownCatchSound);
        }

        private void RegainCrown()
        {
            _flyingCrown.gameObject.SetActive(false);
            _flyingCrown.localScale = _flyingCrownScale;
            HideDropMarker();
            SetCrown(true);
            _visual.Flash(_data.ImmuneFlashColor, 0.2f);
        }

        private void SetCrown(bool isOn)
        {
            _hasCrown = isOn;
            _crown.enabled = isOn;
            HitReceiver.IsShielded = isOn;
        }

        // 왕관 없이 맞았다. 크게 번쩍이며 밀려나고 왕관을 떨어뜨린다. 정해진 횟수를 다 맞으면 그로기에, 아니면 국면 전환에 들어간다.
        private void OnDamaged(HitInfo hit)
        {
            _hitsTaken++;
            Knockback(hit.SourcePosition);
            DropCrown();
            FeedbackPlayer.Play(_data.HurtFeedback, Body.position);
            // 맞힌 폭탄의 폭발음은 빼고 피격음만 크게 들리게 한다.
            GameEvents.RaiseExplosionSoundSuppressed();
            AudioManager.Play(_data.HurtSound);
            _visual.Flash(_data.HurtFlashColor, _data.HurtFlashDuration);

            if (_hitsTaken >= _data.CrownHits)
            {
                _isGroggy = true;
                _hasCrown = false;
                _crown.enabled = false;
                HitReceiver.IsShielded = false;
                HitReceiver.GrantInvulnerability(GroggyGrace);
                Stun(_data.GroggyDuration);
                AudioManager.Play(_data.GroggySound);
                return;
            }

            // 연발 폭탄의 다음 발에 또 맞지 않게 무적은 바로 켠다. 왕관은 연출 끝에 머리로 돌아온다.
            HitReceiver.IsShielded = true;
            BeginPhaseShift();
        }

        // ── 맞은 반응: 폭탄 반대쪽으로 밀려나고, 날아가 있던 왕관이 그 자리에 툭 떨어진다.

        private void Knockback(Vector2 source)
        {
            Vector2 away = Body.position - source;
            Vector2 direction = away.sqrMagnitude > 0.0001f ? away.normalized : -DirectionToTarget();
            _knockFrom = Body.position;
            _knockTo = _knockFrom + direction * ClearDistance(direction, _data.KnockbackDistance, BodyRadius);
            _knockTime = 0f;
        }

        // 밀려난 자리를 시간에 맞춰 정해 두고 옮기므로, 물리 스텝 사이에 여러 번 불려도 밀리는 거리가 늘지 않는다.
        private void TickKnockback(float deltaTime)
        {
            if (_knockTime < 0f)
            {
                return;
            }

            _knockTime += deltaTime;
            float t = Mathf.Clamp01(_knockTime / _data.KnockbackDuration);
            Body.MovePosition(Vector2.Lerp(_knockFrom, _knockTo, 1f - (1f - t) * (1f - t)));
            if (t >= 1f)
            {
                _knockTime = -1f;
            }
        }

        private void DropCrown()
        {
            HideDropMarker();
            // 왕관 투하로 하늘에 있던 왕관은 떨어질 자리 바로 위에서 툭 떨어진다.
            Vector2 airborneFrom = _dropPoint + Vector2.up * CrownDropFall;
            _crownDropFrom = _isCrownAirborne ? airborneFrom : _flyingCrown.gameObject.activeSelf ? (Vector2)_flyingCrown.position : HeadCrownPosition();
            _isCrownAirborne = false;
            _crownGround = _crownDropFrom + Vector2.down * CrownDropFall;
            _crownDropTime = 0f;
            _isCrownReturning = false;
            _flyingCrown.gameObject.SetActive(true);
            _flyingCrown.position = _crownDropFrom;
        }

        // 한 번 통 튀었다가 바닥에 비스듬히 눕는다. 돌던 왕관이 힘을 잃고 떨어진 느낌을 준다.
        private void TickCrownFall(float deltaTime)
        {
            if (_crownDropTime < 0f || _isCrownReturning)
            {
                return;
            }

            _crownDropTime += deltaTime;
            float t = Mathf.Clamp01(_crownDropTime / CrownDropDuration);
            _flyingCrown.position = Vector2.Lerp(_crownDropFrom, _crownGround, t * t) + Vector2.up * (4f * CrownDropHop * t * (1f - t));
            _flyingCrown.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-540f, CrownRestAngle, 1f - (1f - t) * (1f - t)));
            if (t >= 1f)
            {
                _crownDropTime = -1f;
            }
        }

        private Vector2 HeadCrownPosition()
        {
            return transform.TransformPoint(_crownPositionLocal);
        }

        // ── 국면 전환: 밀려남 → 카메라가 다가가고 몸을 떨며 분노 대사 → 떨어진 왕관이 머리로 날아 돌아와 번쩍 → 다시 싸운다.
        // 이 동안 왕은 무적이고 공격하지 않는다. 플레이어가 "한 방 먹였다"를 확실히 느끼고 숨을 고르는 구간이다.

        private void BeginPhaseShift()
        {
            _shiftTime = 0f;
            _isShoutDone = false;
            // 새 국면은 첫 패턴(베기)부터 돈다. 전환 연출 직후 바로 맞을 기회가 오지 않게 한다.
            _rotationIndex = 0;
            Stun(_data.PhaseShiftDuration);

            // 밀려난 뒤의 자리(몸과 머리 사이)를 미리 바라본다.
            Vector2 offset = _knockTo - Body.position;
            Vector2 center = Vector2.Lerp(Body.position, HeadCrownPosition(), 0.5f) + offset;
            _isFocusing = true;
            GameEvents.RaiseCameraFocusRequested(center, _data.PhaseZoomInTime);
            GameEvents.RaiseCameraZoomRequested(_data.PhaseZoom, _data.PhaseZoomInTime);
            if (_data.LocksPlayerOnPhaseShift)
            {
                _isLockingPlayer = true;
                GameEvents.RaisePlayerControlLockChanged(true);
            }
        }

        private void TickPhaseShift(float deltaTime)
        {
            _shiftTime += deltaTime;
            // 끝에서부터: 쏙 줄어드는 시간, 그 앞이 왕관이 돌아오는 시간이다. 다 줄어든 순간 팍 커지며 다시 싸운다.
            float returnEnd = _data.PhaseShiftDuration - ShrinkDuration;
            float returnStart = returnEnd - _data.CrownReturnTime;

            if (!_isShoutDone && _shiftTime >= _data.PhaseLineDelay)
            {
                _isShoutDone = true;
                _visual.Jitter = RageJitter;
                SetAuraStrength(_data.PhaseAuraRateMultiplier, _data.PhaseAuraRangeMultiplier);
                // 3국면(보랏빛)으로 넘어가는 전환이면 불티도 보라색으로 튄다.
                SetAuraColor(_hitsTaken + 1 >= 3);
                _transformAura.Play();
                GameEvents.RaiseBossLineRequested(_data.GetPhaseLine(_hitsTaken), returnStart - _data.PhaseLineDelay);
            }

            if (_shiftTime < returnStart)
            {
                return;
            }

            // 바닥의 왕관이 포물선을 그리며 빙글빙글 머리 위로 돌아온다.
            if (!_isCrownReturning)
            {
                _isCrownReturning = true;
                _crownDropTime = -1f;
                _crownReturnFromScale = _flyingCrown.localScale;
                _visual.Jitter = 0f;
            }

            // 왕관이 머리에 닿으면 각성 때처럼 그림 한가운데로 쏙 줄어든다.
            if (_shiftTime >= returnEnd)
            {
                _flyingCrown.gameObject.SetActive(false);
                float shrink = Mathf.Clamp01((_shiftTime - returnEnd) / ShrinkDuration);
                SetShrink(1f - shrink * shrink);
                if (shrink >= 1f)
                {
                    FinishPhaseShift();
                }

                return;
            }

            float t = Mathf.Clamp01((_shiftTime - returnStart) / _data.CrownReturnTime);
            float ease = t * t * (3f - 2f * t);
            _flyingCrown.position = Vector2.Lerp(_crownGround, HeadCrownPosition(), ease) + Vector2.up * (4f * CrownReturnArc * t * (1f - t));
            _flyingCrown.localScale = Vector3.Lerp(_crownReturnFromScale, _flyingCrownScale, ease);
            _flyingCrown.Rotate(0f, 0f, -900f * deltaTime);
        }

        private void FinishPhaseShift()
        {
            _shiftTime = -1f;
            _isCrownReturning = false;
            _crownDropTime = -1f;
            _visual.Jitter = 0f;
            _transformAura.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            _flyingCrown.gameObject.SetActive(false);
            _flyingCrown.localScale = _flyingCrownScale;
            SetCrown(true);
            // 쏙 줄었던 자리에서 팍 커지며(각성 때와 같은 넘침) 분노 오라가 켜진다.
            float center = BodyCenterHeight();
            SetShrink(1f);
            _popCenterHeight = center;
            _popTime = 0f;
            _lookPhase = Mathf.Min(_hitsTaken + 1, Rotations.Length);
            SetRage(true);
            FeedbackPlayer.Play(_data.PhaseShiftFeedback, Body.position + Vector2.up * center);
            AudioManager.Play(_data.PhaseShiftSound != null ? _data.PhaseShiftSound : _data.CrownCatchSound);
            ReleasePhaseCamera(_data.ZoomOutTime);
        }

        // 그림 피벗이 발밑이라, 줄어드는 만큼 들어 올려 그림 한가운데를 기준으로 줄어들게 한다.
        private void SetShrink(float size)
        {
            _shrinkSize = size;
            _visual.Lift = BodyCenterHeight() * (1f - size);
        }

        // 각성 모습 그림 한가운데가 발밑에서 얼마나 위인지 (유닛).
        private float BodyCenterHeight()
        {
            Sprite sprite = _bodyRenderer.sprite;
            return sprite != null ? sprite.bounds.center.y * _bodyRenderer.transform.localScale.y : 0f;
        }

        // 변신·국면 전환에서 떠는 동안 튀는 불티의 양과 범위. 처음 각성 기준(1배)에 곱한다.
        private void SetAuraStrength(float rateMultiplier, float rangeMultiplier)
        {
            ParticleSystem.EmissionModule emission = _transformAura.emission;
            emission.rateOverTimeMultiplier = _auraBaseRate * rateMultiplier;
            ParticleSystem.ShapeModule shape = _transformAura.shape;
            shape.scale = _auraBaseScale * rangeMultiplier;
        }

        private void SetAuraColor(bool isPurple)
        {
            ParticleSystem.MainModule main = _transformAura.main;
            main.startColor = isPurple ? new ParticleSystem.MinMaxGradient(_data.Phase3ShiftAuraMin, _data.Phase3ShiftAuraMax) : _auraBaseColor;
        }

        // ── 분노 오라: 2국면부터 몸 둘레에서 가는 불꽃이 짧게 일렁이다 꺼지고, 불티가 위로 날린다.
        // 바닥에는 아무것도 깔지 않는다. 바닥의 빨간 원은 공격 예고만의 언어로 남긴다.
        // 도약해 공중에 있는 동안은 왕이 화면 밖이므로 끈다(그림자만 남아야 착지 자리가 또렷하다).

        private void SetRage(bool isOn)
        {
            _isRaging = isOn;
            // 3국면은 보랏빛이라 붉은 불꽃은 끄고, 불티만 보라색으로 날린다.
            bool isPurple = isOn && _lookPhase >= 3;
            SetRageEmitter(_rageFlames, isOn && !isPurple, _data.RageFlameRate);
            SetRageEmitter(_rageEmbers, isOn, _data.RageEmberRate);
            if (isPurple)
            {
                SetEmberColor(_data.Phase3EmberStart, _data.Phase3EmberEnd);
            }
            else
            {
                ParticleSystem.ColorOverLifetimeModule colorOverLifetime = _rageEmbers.colorOverLifetime;
                colorOverLifetime.color = _emberBaseColor;
            }
        }

        private void SetEmberColor(Color start, Color end)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = _rageEmbers.colorOverLifetime;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        private static void SetRageEmitter(ParticleSystem particles, bool isOn, float rate)
        {
            if (isOn)
            {
                ParticleSystem.EmissionModule emission = particles.emission;
                emission.rateOverTimeMultiplier = rate;
                emission.enabled = true;
                particles.Play();
                return;
            }

            particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        private void TickRage(float deltaTime)
        {
            if (!_isRaging)
            {
                return;
            }

            bool isVisible = !IsAirborne;
            ParticleSystem.EmissionModule flames = _rageFlames.emission;
            flames.enabled = isVisible;
            ParticleSystem.EmissionModule embers = _rageEmbers.emission;
            embers.enabled = isVisible;
        }

        private void ReleasePhaseCamera(float blendDuration)
        {
            if (_isFocusing)
            {
                _isFocusing = false;
                GameEvents.RaiseCameraZoomRequested(1f, blendDuration);
                GameEvents.RaiseCameraFocusReleased(blendDuration);
            }

            if (_isLockingPlayer)
            {
                _isLockingPlayer = false;
                GameEvents.RaisePlayerControlLockChanged(false);
            }
        }

        // 왕관에 막힌 폭탄에 "팅" 하고 튕겨내는 반응을 준다. 그로기 직후의 짧은 무적은 왕관이 아니므로 반응하지 않는다.
        private void OnHitBlocked(HitInfo hit)
        {
            // 국면 전환 중에는 왕관이 머리에 없으므로 "팅"으로 튕겨 내는 반응도 없다.
            // 도약해 공중에 있을 때 그림자에 떨어진 폭탄도 반응하지 않는다(왕은 화면 밖에 있다).
            if (!HitReceiver.IsShielded || !_hasTransformed || _isGroggy || _shiftTime >= 0f || IsAirborne)
            {
                return;
            }

            FeedbackPlayer.Play(_data.ImmuneFeedback, Body.position);
            AudioManager.Play(_data.ImmuneSound);
            _visual.Flash(_data.ImmuneFlashColor, _data.ImmuneFlashDuration);
        }

        // ── 구르기: 왕관이 있고 패턴 사이에 서 있을 때, 근처에 떨어진 폭탄을 보고 반대쪽으로 구른다. 쿨타임은 없다.

        private void OnBombLanded(Vector2 position, float radius)
        {
            if (!_isFighting || !_hasCrown || IsDodging || State != EnemyState.Move)
            {
                return;
            }

            Vector2 away = Body.position - position;
            float reach = radius + HitRadius + _data.DodgeMargin;
            if (away.sqrMagnitude > reach * reach)
            {
                return;
            }

            // 폭탄이 몸 한가운데 떨어지면 방향이 없으므로 플레이어를 옆에 두고 구른다.
            Vector2 toTarget = DirectionToTarget();
            Vector2 direction = away.sqrMagnitude > 0.0001f ? away.normalized : new Vector2(-toTarget.y, toTarget.x);
            _dodge = DodgePhase.Crouch;
            _dodgeTime = 0f;
            _dodgeDirection = direction;
            _dodgeLength = ClearDistance(direction, _data.DodgeDistance, BodyRadius);
        }

        private void TickDodge(float deltaTime)
        {
            _dodgeTime += deltaTime;
            if (_dodge == DodgePhase.Crouch)
            {
                if (_dodgeTime >= _data.DodgeWindup)
                {
                    _dodge = DodgePhase.Roll;
                    _dodgeTime = 0f;
                    AudioManager.Play(_data.DodgeSound);
                }

                return;
            }

            float speed = _dodgeLength / _data.DodgeDuration;
            Body.MovePosition(Body.position + _dodgeDirection * (speed * deltaTime));
            if (_dodgeTime >= _data.DodgeDuration)
            {
                _dodge = DodgePhase.None;
            }
        }

        // 웅크릴 때 납작해지고, 구르는 동안 몸을 말아 기울인다. 각성 직후에는 부풀었다 돌아온다.
        private void UpdateDodgePose()
        {
            switch (_dodge)
            {
                case DodgePhase.Crouch:
                    _visual.ExtraScale = new Vector2(1.25f, 0.7f);
                    _visual.ExtraLean = 0f;
                    break;
                case DodgePhase.Roll:
                    _visual.ExtraScale = new Vector2(0.85f, 0.85f);
                    _visual.ExtraLean = 35f;
                    break;
                default:
                    float pop = CurrentPopScale();
                    float windup = SwordWindup();
                    Vector2 stretch = new Vector2(1f - SwordWindupStretch * 0.5f * windup, 1f + SwordWindupStretch * windup);
                    _visual.ExtraScale = new Vector2(pop, pop) * (LeapSquash() * _shrinkSize) * stretch;
                    _visual.ExtraLean = SwordWindupLean * windup;
                    if (_pattern == Pattern.Sword && State == EnemyState.Attack)
                    {
                        _visual.Jitter = SwordWindupJitter * windup;
                    }
                    break;
            }
        }

        // 도약 직전 웅크리고, 착지한 순간 납작하게 눌렸다 돌아온다.
        private Vector2 LeapSquash()
        {
            if (_pattern != Pattern.Leap)
            {
                return Vector2.one;
            }

            if (State == EnemyState.Telegraph)
            {
                return new Vector2(1.2f, 0.75f);
            }

            if (State == EnemyState.Attack && _step == Step.LeapLand)
            {
                float t = Mathf.Clamp01(_stepTime / 0.15f);
                return Vector2.Lerp(new Vector2(1.3f, 0.65f), Vector2.one, t);
            }

            return Vector2.one;
        }

        private void HideTelegraphs()
        {
            _aimLine.Hide();
            _slashRange.Visible = false;
            _slashFill.Visible = false;
        }
    }
}
