using ExplodeIt.Bombs;
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 화염 마법사(boss1): 내려앉은 자리에 고정된 채 화염 영역으로 몸을 감싸고, 패턴(메테오 ↔ 화염구)을 번갈아 쓴다.
    // 영역에 닿은 폭탄은 터지지 않고 타 버리고, 플레이어는 영역에 닿으면 죽는다. 영역 둘레를 도는 요정을 모두 잡으면
    // 영역이 풀리며 그로기에 빠져 한 방에 잡힌다. 그로기 동안 못 잡으면 영역과 요정이 다시 생긴다.
    // 예측 과제: 일정한 속도로 도는 요정의 1초 뒤 자리에 폭탄을 두고, 메테오는 일자로 달려 피하고, 화염구는 벽이나 구르기로 피한다.
    public class FireMageBoss : Enemy, IBoss, IBombBarrier
    {
        // 한 폭발 안의 판정은 같은 프레임에 끝나므로 아주 짧아도 된다. 플레이어가 느낄 길이가 아니라 데이터로 빼지 않는다.
        private const float ShieldBreakGrace = 0.1f;

        private enum Pattern
        {
            Meteor,
            Fireball
        }

        [SerializeField] private FireMageData _data;
        [SerializeField] private EnemyVisual _visual;
        // 화염 영역 그림. 그림 폭이 곧 영역 지름이 되도록 크기를 맞추고, 살아 있는 불처럼 천천히 돌린다.
        [SerializeField] private SpriteRenderer _field;
        [SerializeField] private TelegraphLine _aimLine;
        // 오브젝트 피커가 컴포넌트 타입 칸에 프리팹을 띄우지 않아 GameObject로 받는다.
        [SerializeField] private GameObject _fairyPrefab;
        [SerializeField] private GameObject _meteorPrefab;
        [SerializeField] private GameObject _fireballPrefab;

        private FireFairy[] _fairies;
        private Meteor[] _meteors;
        private EnemyProjectile _fireball;
        private System.Action<Meteor> _onMeteorImpact;
        private System.Action<EnemyProjectile> _releaseFireball;

        private int _aliveFairies;
        private int _bossNumber;
        private int _tier;
        private bool _isFighting;
        private bool _isGroggy;
        private float _patternTimer;
        private Pattern _nextPattern;
        private Pattern _currentPattern;
        private int _meteorsFired;
        private float _meteorTimer;
        private Vector2 _aimDirection = Vector2.right;
        private float _shotLength;
        private SoundHandle _chargeHandle = SoundHandle.None;

        // 지금 영역 반경. 펼쳐지는 동안 0에서 커지고, 플레이어 판정과 폭탄 차단 모두 이 값을 쓴다.
        private float _fieldRadius;
        // 영역이 펼쳐지기 시작한 뒤 지난 시간. 음수면 펼쳐지는 중이 아니다.
        private float _fieldGrowTime = -1f;

        protected override EnemyData Data => _data;
        protected override bool CanBePulled => false;
        // 벽 뒤에 숨은 플레이어에게도 메테오를 떨어뜨리고, 화염구는 벽에 막혀 사라지는 것이 피하는 방법이다.
        protected override bool RequiresLineOfSight => false;
        public override Vector2 AimDirection => _aimDirection;
        public override float TelegraphDuration =>
            _currentPattern == Pattern.Fireball ? _data.FireballChargeTime : _data.MeteorCastTime;

        private FireMageData.Tier Tier => _data.GetTier(_tier);

        protected override void Awake()
        {
            base.Awake();

            // 보스는 판마다 한 번씩만 나오므로 요정·메테오·화염구도 미리 만든다. 보스를 따라 움직이면 안 되므로 월드에 둔다.
            _fairies = new FireFairy[_data.FairyCount];
            for (int i = 0; i < _fairies.Length; i++)
            {
                _fairies[i] = Instantiate(_fairyPrefab).GetComponent<FireFairy>();
                _fairies[i].gameObject.SetActive(false);
                _fairies[i].Popped += OnFairyPopped;
            }

            _meteors = new Meteor[_data.MaxMeteorCount];
            for (int i = 0; i < _meteors.Length; i++)
            {
                _meteors[i] = Instantiate(_meteorPrefab).GetComponent<Meteor>();
                _meteors[i].Hide();
            }

            _fireball = Instantiate(_fireballPrefab).GetComponent<EnemyProjectile>();
            _fireball.gameObject.SetActive(false);
            _onMeteorImpact = OnMeteorImpact;
            _releaseFireball = ReleaseFireball;
        }

        // 기반 클래스가 이동 상태로 들어가며 OnEnterState를 부르므로, 지난 판의 그로기 표시를 먼저 지운다.
        protected override void OnEnable()
        {
            _isGroggy = false;
            _isFighting = false;
            base.OnEnable();
            HitReceiver.Blocked += OnHitBlocked;
            BombBarriers.Register(this);
            HideField();
            _aimLine.Hide();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            HitReceiver.Blocked -= OnHitBlocked;
            BombBarriers.Unregister(this);
            RetractFairies();
            HideAttacks();
            StopChargeSound();
        }

        private void OnDestroy()
        {
            if (_fairies == null)
            {
                return;
            }

            for (int i = 0; i < _fairies.Length; i++)
            {
                if (_fairies[i] != null)
                {
                    Destroy(_fairies[i].gameObject);
                }
            }

            for (int i = 0; i < _meteors.Length; i++)
            {
                if (_meteors[i] != null)
                {
                    Destroy(_meteors[i].gameObject);
                }
            }

            if (_fireball != null)
            {
                Destroy(_fireball.gameObject);
            }
        }

        public void BeginBoss(int bossNumber, int tier)
        {
            _bossNumber = bossNumber;
            _tier = tier;
            _isFighting = false;
            _nextPattern = Pattern.Meteor;
            LaunchFairies();
        }

        // 플레이어가 멈춰 있는 동안 영역이 펼쳐지는 모습을 보여준다. 다 펼쳐지면 조작이 돌아온다.
        public void PlayEntrance()
        {
            BeginFieldGrow();
        }

        public bool IsEntranceDone => _fieldGrowTime < 0f && _fieldRadius > 0f;

        public void StartFight()
        {
            _isFighting = true;
            _patternTimer = 0f;
        }

        // 영역 안이면 폭탄을 막는다. 펼쳐지는 중이면 지금 보이는 크기까지만 막는다.
        public bool Blocks(Vector2 point)
        {
            return _fieldRadius > 0f && (point - Center).sqrMagnitude <= _fieldRadius * _fieldRadius;
        }

        public void OnBombBurned(Vector2 position)
        {
            AudioManager.Play(_data.BombBurnSound);
            FeedbackPlayer.Play(_data.BombBurnFeedback, position);
        }

        // 마법사는 움직이지 않으므로 트랜스폼 위치를 쓴다. 스폰 직후엔 물리 위치가 아직 반영되지 않았을 수 있다.
        private Vector2 Center => transform.position;

        private void Update()
        {
            TickField(Time.deltaTime);
            if (_field.enabled)
            {
                _field.transform.Rotate(0f, 0f, _data.FieldSpinSpeed * Time.deltaTime);
            }

            // 영역은 늘 보이는 위험이라 따로 예고하지 않는다. 다시 펼쳐질 때는 퍼지는 모습이 예고다.
            if (_isFighting && _fieldRadius > 0f && CurrentState != EnemyState.Dead)
            {
                HitOverlapping(Center, _fieldRadius);
            }
        }

        // 서 있는 동안 다음 패턴까지 시간을 센다. 마법사는 움직이지 않는다.
        protected override void TickMove(float deltaTime)
        {
            if (_isFighting)
            {
                _patternTimer += deltaTime;
            }
        }

        protected override bool ShouldStartAttack()
        {
            return _isFighting && _patternTimer >= _data.PatternInterval;
        }

        protected override void TickTelegraph(float deltaTime)
        {
            if (_currentPattern == Pattern.Fireball && StateTime < _data.FireballChargeTime - _data.FireballAimLockTime)
            {
                Aim();
            }
        }

        protected override bool TickAttack(float deltaTime)
        {
            if (_currentPattern == Pattern.Fireball)
            {
                FireFireball();
                return true;
            }

            // 첫 메테오는 시전이 끝나는 순간 바로, 이후 간격마다 그 순간의 플레이어 위치에 떨어뜨린다.
            _meteorTimer -= deltaTime;
            if (_meteorTimer <= 0f && _meteorsFired < _meteors.Length)
            {
                _meteors[_meteorsFired].Launch(TargetPosition, _data.MeteorFallTime, _data.MeteorRadius, _onMeteorImpact);
                _meteorsFired++;
                _meteorTimer += _data.MeteorInterval;
            }

            return _meteorsFired >= Mathf.Min(Tier.MeteorCount, _meteors.Length);
        }

        protected override void OnEnterState(EnemyState state)
        {
            StopChargeSound();

            switch (state)
            {
                case EnemyState.Telegraph:
                    _currentPattern = _nextPattern;
                    _nextPattern = _currentPattern == Pattern.Meteor ? Pattern.Fireball : Pattern.Meteor;
                    if (_currentPattern == Pattern.Fireball)
                    {
                        Aim();
                        _chargeHandle = AudioManager.Play(_data.FireballChargeSound);
                    }
                    else
                    {
                        AudioManager.Play(_data.MeteorCastSound);
                    }

                    return;

                case EnemyState.Attack:
                    _meteorsFired = 0;
                    _meteorTimer = 0f;
                    break;

                case EnemyState.Move:
                    _patternTimer = 0f;
                    // 그로기 동안 못 잡았으면 처음부터 다시: 요정이 생기고 영역이 다시 펼쳐진다.
                    if (_isGroggy)
                    {
                        _isGroggy = false;
                        LaunchFairies();
                        BeginFieldGrow();
                    }
                    break;

                case EnemyState.Dead:
                    RetractFairies();
                    HideField();
                    HideAttacks();
                    FeedbackPlayer.Play(_data.DefeatFeedback, Body.position);
                    GameEvents.RaiseBossDefeated(_bossNumber);
                    break;
            }

            _aimLine.Hide();
        }

        // 화염구는 구조물에 막힌다. 조준선 끝이 곧 화염구가 사라지는 곳이고, 굵기가 곧 판정 폭이다.
        private void Aim()
        {
            _aimDirection = DirectionToTarget();
            _shotLength = ClearDistance(_aimDirection, _data.FireballRange, _data.FireballHitRadius);
            _aimLine.Show(Body.position, _aimDirection, _shotLength, _data.FireballHitRadius * 2f);
        }

        private void FireFireball()
        {
            _fireball.transform.position = Body.position;
            _fireball.gameObject.SetActive(true);
            _fireball.Launch(_aimDirection, _data.FireballSpeed, _shotLength, _data.FireballHitRadius, _releaseFireball);
            AudioManager.Play(_data.FireballShotSound);
        }

        private void ReleaseFireball(EnemyProjectile fireball)
        {
            fireball.gameObject.SetActive(false);
        }

        // 구조물과 상관없이 원 안이면 맞는다. 하늘에서 떨어지므로 벽 뒤도 안전하지 않다.
        private void OnMeteorImpact(Meteor meteor)
        {
            HitOverlapping(meteor.Position, meteor.Radius);
            FeedbackPlayer.Play(_data.MeteorImpactFeedback, meteor.Position);
            AudioManager.Play(_data.MeteorImpactSound);
        }

        private void LaunchFairies()
        {
            Vector2 center = Center;
            float step = 360f / _fairies.Length;
            float start = Random.Range(0f, 360f);
            for (int i = 0; i < _fairies.Length; i++)
            {
                _fairies[i].Launch(center, _data.FairyOrbitRadius, start + step * i, Tier.FairyOrbitSpeed);
            }

            _aliveFairies = _fairies.Length;
            HitReceiver.IsShielded = true;
        }

        private void RetractFairies()
        {
            if (_fairies == null)
            {
                return;
            }

            for (int i = 0; i < _fairies.Length; i++)
            {
                if (_fairies[i] != null)
                {
                    _fairies[i].Retract();
                }
            }

            _aliveFairies = 0;
        }

        private void OnFairyPopped(FireFairy fairy)
        {
            _aliveFairies--;
            if (_aliveFairies > 0 || CurrentState == EnemyState.Dead)
            {
                return;
            }

            // 마지막 요정이 터지면 영역이 싸악 풀린다.
            HideField();
            FeedbackPlayer.Play(_data.FieldBreakFeedback, Body.position);
            AudioManager.Play(_data.FieldBreakSound);

            HitReceiver.IsShielded = false;
            // 마지막 요정과 마법사가 한 폭발에 같이 휘말려도 그로기를 건너뛰고 바로 죽지 않게 아주 잠깐 무적을 준다.
            HitReceiver.GrantInvulnerability(ShieldBreakGrace);
            _isGroggy = true;
            Stun(_data.GroggyDuration);
            AudioManager.Play(_data.GroggySound);
        }

        private void BeginFieldGrow()
        {
            _fieldGrowTime = 0f;
            SetFieldRadius(0f);
            _field.enabled = true;
        }

        private void TickField(float deltaTime)
        {
            if (_fieldGrowTime < 0f)
            {
                return;
            }

            _fieldGrowTime += deltaTime;
            float t = Mathf.Clamp01(_fieldGrowTime / _data.FieldGrowTime);
            // 빠르게 퍼지다 끝에서 부드럽게 멈춘다.
            float ease = 1f - (1f - t) * (1f - t);
            SetFieldRadius(_data.FieldRadius * ease);
            if (t >= 1f)
            {
                _fieldGrowTime = -1f;
            }
        }

        private void HideField()
        {
            _fieldGrowTime = -1f;
            _fieldRadius = 0f;
            _field.enabled = false;
        }

        private void SetFieldRadius(float radius)
        {
            _fieldRadius = radius;
            float spriteWidth = _field.sprite != null ? _field.sprite.bounds.size.x : 1f;
            float scale = radius * 2f / Mathf.Max(spriteWidth, 0.01f);
            _field.transform.localScale = new Vector3(scale, scale, 1f);
        }

        private void HideAttacks()
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

            if (_fireball != null)
            {
                _fireball.gameObject.SetActive(false);
            }
        }

        // 영역 밖에서 블랙홀 등이 닿아 막혔을 때 "팅" 하고 튕겨내는 반응을 준다.
        // 실드가 풀린 직후의 짧은 무적에 막힌 경우는 실드가 아니므로 반응하지 않는다.
        private void OnHitBlocked(HitInfo hit)
        {
            if (!HitReceiver.IsShielded)
            {
                return;
            }

            FeedbackPlayer.Play(_data.ImmuneFeedback, Body.position);
            AudioManager.Play(_data.ImmuneSound);
            if (_visual != null)
            {
                _visual.Flash(_data.ImmuneFlashColor, _data.ImmuneFlashDuration);
            }
        }

        private void StopChargeSound()
        {
            AudioManager.Stop(_chargeHandle);
            _chargeHandle = SoundHandle.None;
        }
    }
}
