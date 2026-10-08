using ExplodeIt.Bombs;
using ExplodeIt.Core;
using ExplodeIt.Stage;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 최종 보스(각성한 왕). 2등신 왕이 변신해 싸운다.
    // 머리 위에 왕관이 떠서 빛나는 동안은 무적이고, 근처에 폭탄이 떨어지면 굴러서 피한다(쿨타임 없음).
    // 패턴은 왕검 연속 베기 → 돌진 찌르기 → 왕관 부메랑을 차례로 돈다. 왕관 부메랑으로 왕관이 날아가 있는 동안만 맞는다.
    // 세 번 맞으면 그로기에 빠지고, 그로기 때 한 번 더 맞히면 쓰러진다. 맞을 때마다 다음 국면으로 넘어간다(지금은 1국면 패턴만 있다).
    // 예측 과제: 왕관이 날아가 있는 짧은 시간 동안 걸어오는 왕의 1초 뒤 자리에 폭탄을 두고, 돌아오는 왕관도 피한다.
    public class AwakenedKingBoss : Enemy, IBoss
    {
        // 그림 전용 연출 값이라 데이터로 빼지 않는다.
        // 변신 시간 중 붉은 덧칠이 다 차오르는 구간의 비율. 나머지는 다 달아오른 채 점점 세게 떤다.
        private const float TransformHeatPortion = 0.35f;
        // 펑 하고 바뀐 순간 각성 모습이 부풀었다 돌아오는 시간 (초)과 크기, 하얗게 번쩍이는 시간 (초).
        private const float PopDuration = 0.3f;
        private const float PopScale = 1.35f;
        private const float PopFlashDuration = 0.35f;
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

        private static readonly Collider2D[] StandBuffer = new Collider2D[4];

        private enum Pattern
        {
            Slash,
            Lunge,
            Crown
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
            CrownBack
        }

        private enum DodgePhase
        {
            None,
            Crouch,
            Roll
        }

        private static readonly Pattern[] Rotation = { Pattern.Slash, Pattern.Lunge, Pattern.Crown };

        [SerializeField] private AwakenedKingData _data;
        [SerializeField] private EnemyVisual _visual;
        // 머리 위에 떠 있는 왕관. 몸이 웅크리거나 기울어도 흔들리지 않게 그림(Body)이 아닌 루트 자식으로 둔다. 켜져 있으면 무적이다.
        [SerializeField] private SpriteRenderer _crown;
        // 변신 전 2등신 모습. 그림(Body)과 따로 두어 각성 모습과 겹쳐 바꿔 보여 준다.
        [SerializeField] private SpriteRenderer _chibi;
        // 변신 연출 층. 2등신 그림과 같은 모양을 붉게 덮어 씌우는 층, 뒤에서 번지는 빛 층(둘 다 Chibi 자식, 실루엣 재질),
        // 펑 하는 순간 각성 모습을 하얗게 덮는 층(Body 자식, 실루엣 재질), 달아오르는 동안 튀는 불티.
        [SerializeField] private SpriteRenderer _chibiOverlay;
        [SerializeField] private SpriteRenderer _chibiGlow;
        [SerializeField] private SpriteRenderer _bodyFlash;
        [SerializeField] private ParticleSystem _transformAura;
        [SerializeField] private ExplosionShape _slashRange;
        [SerializeField] private ExplosionShape _slashFill;
        [SerializeField] private TelegraphLine _aimLine;
        // 던진 왕관. 왕을 따라 움직이면 안 되므로 시작할 때 월드로 뗀다.
        [SerializeField] private Transform _flyingCrown;
        // 동작 그림. 기본 자세는 그림(Body)에 넣어 둔 그림이고, 패턴 중에만 이 그림들로 바꾼다. 비워 두면 기본 그림 그대로다.
        [SerializeField] private Sprite _slashReadySprite;
        [SerializeField] private Sprite _slashStrikeSprite;
        [SerializeField] private Sprite _thrustSprite;

        private int _bossNumber;
        private bool _isFighting;
        private bool _hasCrown;
        private bool _isGroggy;
        private int _hitsTaken;
        private int _rotationIndex;
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

        private SpriteRenderer _bodySprite;
        private Vector3 _chibiPosition;
        private Vector3 _chibiScale;
        private Vector3 _crownPositionLocal;
        private Vector3 _crownScale;
        private ContactFilter2D _standFilter;

        protected override EnemyData Data => _data;
        protected override bool CanBePulled => false;
        protected override bool RequiresLineOfSight => false;
        public override Vector2 AimDirection =>
            State == EnemyState.Telegraph || State == EnemyState.Attack ? _aimDirection : DirectionToTarget();
        public override float TelegraphDuration => _pattern == Pattern.Crown ? _data.CrownWindup : _data.PatternStartup;

        private bool IsDodging => _dodge != DodgePhase.None;

        protected override void Awake()
        {
            base.Awake();
            _bodySprite = _bodyFlash.transform.parent.GetComponent<SpriteRenderer>();
            _chibiPosition = _chibi.transform.localPosition;
            _chibiScale = _chibi.transform.localScale;
            _crownPositionLocal = _crown.transform.localPosition;
            _crownScale = _crown.transform.localScale;
            _standFilter.useTriggers = false;
            _standFilter.SetLayerMask(ObstacleMask);
            _flyingCrown.SetParent(null);
            _flyingCrown.gameObject.SetActive(false);
        }

        protected override void OnEnable()
        {
            _isGroggy = false;
            _isFighting = false;
            base.OnEnable();
            HitReceiver.Damaged += OnDamaged;
            HitReceiver.Blocked += OnHitBlocked;
            GameEvents.BombLanded += OnBombLanded;
            HideTelegraphs();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            HitReceiver.Damaged -= OnDamaged;
            HitReceiver.Blocked -= OnHitBlocked;
            GameEvents.BombLanded -= OnBombLanded;
            if (_flyingCrown != null)
            {
                _flyingCrown.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_flyingCrown != null)
            {
                Destroy(_flyingCrown.gameObject);
            }
        }

        // 나타난 순간은 2등신 모습이고, 곧바로 붉게 달아올라 떨다가 펑 하고 각성한다(대화 중 멈춤 시간 동안 보이게).
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
            // 왕관 횟수만큼은 맞아도 버티고, 그로기 때의 한 방에 쓰러진다.
            HitReceiver.ResetHits(_data.CrownHits + 1);
            HitReceiver.IsShielded = true;
            _hasCrown = true;
            _crown.enabled = false;
            _bodyFlash.enabled = false;
            _chibi.enabled = true;
            _chibi.transform.localPosition = _chibiPosition;
            _chibi.transform.localScale = _chibiScale;
            _visual.Alpha = 0f;

            _transformTime = 0f;
            _chibiOverlay.enabled = true;
            _chibiGlow.enabled = true;
            TickTransform(0f);
            _transformAura.Play();
            AudioManager.Play(_data.TransformChargeSound);
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

        public void StartFight()
        {
            _isFighting = true;
            _moveTimer = 0f;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (_transformTime >= 0f)
            {
                TickTransform(deltaTime);
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
            UpdateDodgePose();
            UpdatePoseSprite();
        }

        // 베기는 파고들며 칼을 든 그림, 베는 순간부터 쉬는 동안 휘두른 그림. 돌진은 달려드는 동안만 찌르는 그림이다.
        // 조준하는 동안은 기본 자세로 서 있어야 찌르는 그림이 "지금 온다"는 신호로 읽힌다.
        private void UpdatePoseSprite()
        {
            Sprite pose = null;
            if (State == EnemyState.Attack && _pattern == Pattern.Slash)
            {
                pose = _step == Step.Pause ? _slashStrikeSprite : _slashReadySprite;
            }
            else if (State == EnemyState.Attack && _pattern == Pattern.Lunge && _step == Step.Dash)
            {
                pose = _thrustSprite;
            }

            _visual.PoseSprite = pose;
        }

        // ── 변신: 2등신 그림 위에 같은 모양을 붉게 덮고 둘레에 빛이 번지며, 점점 세게 떨다가 펑 하고 각성 모습으로 바뀐다.

        private void TickTransform(float deltaTime)
        {
            _transformTime += deltaTime;
            float t = Mathf.Clamp01(_transformTime / _data.TransformDuration);
            float heat = Mathf.Clamp01(t / TransformHeatPortion);
            float shake = t * t;
            Color glow = _data.TransformGlowColor;

            // 표정이 바뀌어도 같은 모양으로 덮이게 매번 그림을 따라 맞춘다.
            _chibiOverlay.sprite = _chibi.sprite;
            _chibiGlow.sprite = _chibi.sprite;
            _chibiOverlay.color = new Color(glow.r, glow.g, glow.b, heat * 0.85f);

            // 빛 층은 그림 한가운데를 기준으로 부풀어, 발밑이 아니라 몸 둘레 전체에 테두리처럼 번진다.
            float pulse = 0.5f + 0.5f * Mathf.Sin(_transformTime * Mathf.Lerp(10f, 40f, t));
            float glowScale = 1.08f + 0.1f * pulse + 0.12f * t;
            float centerY = _chibi.sprite != null ? _chibi.sprite.bounds.center.y : 0f;
            Transform glowTransform = _chibiGlow.transform;
            glowTransform.localScale = new Vector3(glowScale, glowScale, 1f);
            glowTransform.localPosition = new Vector3(0f, centerY * (1f - glowScale), 0f);
            _chibiGlow.color = new Color(glow.r, glow.g, glow.b, heat * (0.35f + 0.4f * pulse));

            Transform chibi = _chibi.transform;
            float amplitude = 0.02f + 0.12f * shake;
            chibi.localPosition = _chibiPosition + new Vector3(Mathf.Sin(_transformTime * 83f), 0.5f * Mathf.Sin(_transformTime * 67f), 0f) * amplitude;
            chibi.localScale = _chibiScale * (1f + 0.12f * shake);

            if (t >= 1f)
            {
                PopTransform();
            }
        }

        private void PopTransform()
        {
            // 그림 피벗이 발밑이라 위치 대신 그림 한가운데에서 터트린다.
            Vector2 center = _chibi.bounds.center;
            _transformTime = -1f;
            _hasTransformed = true;
            _chibi.enabled = false;
            _chibiOverlay.enabled = false;
            _chibiGlow.enabled = false;
            _chibi.transform.localPosition = _chibiPosition;
            _chibi.transform.localScale = _chibiScale;
            _transformAura.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            _visual.Alpha = 1f;
            _popTime = 0f;
            _bodyFlash.enabled = true;
            TickPop(0f);
            FeedbackPlayer.Play(_data.TransformFeedback, center);
            AudioManager.Play(_data.TransformSound);

            if (_isEntranceRequested)
            {
                BeginSummon();
            }
        }

        // 각성 모습이 하얗게 번쩍이며 부풀었다가 돌아온다.
        private void TickPop(float deltaTime)
        {
            if (_popTime < 0f)
            {
                return;
            }

            _popTime += deltaTime;
            _bodyFlash.sprite = _bodySprite.sprite;
            _bodyFlash.flipX = _bodySprite.flipX;
            _bodyFlash.color = new Color(1f, 1f, 1f, 1f - Mathf.Clamp01(_popTime / PopFlashDuration));
            if (_popTime >= Mathf.Max(PopDuration, PopFlashDuration))
            {
                _popTime = -1f;
                _bodyFlash.enabled = false;
            }
        }

        private float CurrentPopScale()
        {
            if (_popTime < 0f)
            {
                return 1f;
            }

            float t = Mathf.Clamp01(_popTime / PopDuration);
            return Mathf.Lerp(PopScale, 1f, 1f - (1f - t) * (1f - t));
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
            _crown.color = Color.Lerp(new Color(1f, 0.85f, 0.4f, 1f), Color.white, pulse);
            _crown.transform.localScale = _crownScale * (1f + 0.06f * pulse);
            _crown.transform.localPosition = _crownPositionLocal + Vector3.up * (CrownBobHeight * Mathf.Sin(Time.time * CrownBobSpeed));
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
                default:
                    return TickCrown(deltaTime);
            }
        }

        protected override void OnEnterState(EnemyState state)
        {
            switch (state)
            {
                case EnemyState.Telegraph:
                    _pattern = Rotation[_rotationIndex];
                    _rotationIndex = (_rotationIndex + 1) % Rotation.Length;
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
                    else
                    {
                        ThrowCrown();
                    }

                    return;

                case EnemyState.Move:
                    _moveTimer = 0f;
                    // 맞고 비틀거린 뒤, 또는 그로기 동안 못 잡았으면 왕관이 다시 생긴다.
                    _isGroggy = false;
                    if (!_hasCrown && _hasTransformed)
                    {
                        RegainCrown();
                    }

                    break;

                case EnemyState.Dead:
                    _crown.enabled = false;
                    _flyingCrown.gameObject.SetActive(false);
                    FeedbackPlayer.Play(_data.DefeatFeedback, Body.position);
                    GameEvents.RaiseBossDefeated(_bossNumber);
                    break;
            }

            _dodge = DodgePhase.None;
            HideTelegraphs();
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

        // ── 왕관 부메랑: 왕관이 벽을 뚫고 날아갔다가 돌아온다. 날아가 있는 동안 왕은 무적이 아니다.

        private void ThrowCrown()
        {
            _step = Step.CrownOut;
            _stepTime = 0f;
            _crownTravel = 0f;
            _crownPosition = _crown.transform.position;
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
                    Vector2 home = _crown.transform.position;
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
            SetCrown(true);
            _visual.Flash(_data.ImmuneFlashColor, 0.2f);
        }

        private void SetCrown(bool isOn)
        {
            _hasCrown = isOn;
            _crown.enabled = isOn;
            HitReceiver.IsShielded = isOn;
        }

        // 왕관 없이 맞았다. 다음 국면으로 넘어가고, 정해진 횟수를 다 맞으면 그로기에 빠진다.
        private void OnDamaged(HitInfo hit)
        {
            _hitsTaken++;
            _flyingCrown.gameObject.SetActive(false);
            FeedbackPlayer.Play(_data.HurtFeedback, Body.position);
            AudioManager.Play(_data.HurtSound);

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

            // 왕관은 잠깐 뒤 머리 위에 다시 생기지만, 연발 폭탄의 다음 발에 또 맞지 않게 무적은 바로 켠다.
            HitReceiver.IsShielded = true;
            Stun(_data.CrownRegainTime);
        }

        // 왕관에 막힌 폭탄에 "팅" 하고 튕겨내는 반응을 준다. 그로기 직후의 짧은 무적은 왕관이 아니므로 반응하지 않는다.
        private void OnHitBlocked(HitInfo hit)
        {
            if (!HitReceiver.IsShielded || !_hasTransformed || _isGroggy)
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
            float reach = radius + BodyRadius + _data.DodgeMargin;
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
                    _visual.ExtraScale = new Vector2(pop, pop);
                    _visual.ExtraLean = 0f;
                    break;
            }
        }

        private void HideTelegraphs()
        {
            _aimLine.Hide();
            _slashRange.Visible = false;
            _slashFill.Visible = false;
        }
    }
}
