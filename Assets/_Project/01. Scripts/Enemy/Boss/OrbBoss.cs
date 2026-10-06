using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 구체 보스(boss3): 플레이어 둘레 작은 원의 좌우 반원을 번갈아 찍어 짧게 달려들고 멈추기를 반복
    // → 할퀴기 간격이 차면 정면 사각형 예고(범위가 차오름) → 할퀴며 범위 끝까지 돌진 → 회복 → (반복).
    // 맵을 튕겨 다니는 구체가 하나라도 남아 있으면 실드로 무적이고, 모두 터트리면 그로기에 빠져 한 방에 잡힌다.
    // 그로기 동안 못 잡으면 구체가 다시 생긴다.
    // 예측 과제: 구체는 직선으로만 움직이므로 1초 뒤 지나갈 자리에 폭탄을 두고, 할퀴기는 범위가 다 차는 순간 구르기로 피한다.
    public class OrbBoss : Enemy, IBoss
    {
        // 한 폭발 안의 판정은 같은 프레임에 끝나므로 아주 짧아도 된다. 플레이어가 느낄 길이가 아니라 데이터로 빼지 않는다.
        private const float ShieldBreakGrace = 0.1f;

        [SerializeField] private OrbBossData _data;
        [SerializeField] private RectTelegraph _clawTelegraph;
        // 오브젝트 피커가 컴포넌트 타입 칸에 프리팹을 띄우지 않아 GameObject로 받는다.
        [SerializeField] private GameObject _orbPrefab;
        [SerializeField] private EnemyVisual _visual;

        private BossOrb[] _orbs;
        private int _aliveOrbs;
        private int _bossNumber;
        private int _tier;
        private float _clawTimer;
        private Vector2 _clawDirection = Vector2.right;
        private bool _isGroggy;
        private Vector2 _lungePoint;
        private float _lungeSpeed;
        private float _lungeTime;
        private float _lungeDuration;
        private float _pauseDuration;
        private bool _lungeLeft;
        private bool _hasClawed;
        private float _dashRemaining;
        private SoundHandle _readySoundHandle = SoundHandle.None;
        // 등장 연출 동안은 구체만 퍼지고 보스는 제자리에 서 있는다.
        private bool _isFighting;

        protected override EnemyData Data => _data;
        protected override bool CanBePulled => false;
        public override Vector2 AimDirection => _clawDirection;

        private OrbBossData.Tier Tier => _data.GetTier(_tier);

        protected override void Awake()
        {
            base.Awake();

            // 보스는 판마다 한 번씩만 나오므로, 보스를 미리 만들 때 구체도 함께 만들어 둔다. 전투 중 생성은 없다.
            int count = _data.MaxOrbCount;
            _orbs = new BossOrb[count];
            for (int i = 0; i < count; i++)
            {
                // 보스를 따라 움직이면 안 되므로 부모 없이 월드에 둔다.
                _orbs[i] = Instantiate(_orbPrefab).GetComponent<BossOrb>();
                _orbs[i].gameObject.SetActive(false);
                _orbs[i].Popped += OnOrbPopped;
            }
        }

        // 기반 클래스가 이동 상태로 들어가며 OnEnterState를 부르므로, 지난 판의 그로기 표시를 먼저 지운다.
        protected override void OnEnable()
        {
            _isGroggy = false;
            base.OnEnable();
            _clawTelegraph.Hide();
            HitReceiver.Blocked += OnHitBlocked;
        }

        // 풀로 돌아가거나 재시작으로 꺼질 때 씬을 넘어 살아 있는 오디오 매니저에서 소리가 남지 않게 한다.
        protected override void OnDisable()
        {
            base.OnDisable();
            HitReceiver.Blocked -= OnHitBlocked;
            RetractOrbs();
            StopReadySound();
        }

        private void OnDestroy()
        {
            if (_orbs == null)
            {
                return;
            }

            for (int i = 0; i < _orbs.Length; i++)
            {
                if (_orbs[i] != null)
                {
                    Destroy(_orbs[i].gameObject);
                }
            }
        }

        public void BeginBoss(int bossNumber, int tier)
        {
            _bossNumber = bossNumber;
            _tier = tier;
            _clawTimer = 0f;
            _isFighting = false;
            LaunchOrbs();
        }

        public void StartFight()
        {
            _isFighting = true;
            _clawTimer = 0f;
            _lungeLeft = Random.value < 0.5f;
            PickLungePoint();
        }

        // 달려가는 시간 → 멈춤 → 다음 지점. 달려가는 속도는 찍은 순간의 거리를 달려갈 시간으로 나눠 정한다.
        protected override void TickMove(float deltaTime)
        {
            if (!_isFighting)
            {
                return;
            }

            _clawTimer += deltaTime;
            _lungeTime += deltaTime;
            if (_lungeTime < _lungeDuration)
            {
                MoveTowardPoint(_lungePoint, _lungeSpeed, deltaTime);
            }
            else if (_lungeTime >= _lungeDuration + _pauseDuration)
            {
                PickLungePoint();
            }
        }

        // 지금 플레이어 위치 기준으로 한 번 찍고 그 자리로 달려간다. 반원은 매번 번갈아 바꾼다.
        private void PickLungePoint()
        {
            float side = _lungeLeft ? -1f : 1f;
            _lungeLeft = !_lungeLeft;
            float angle = Random.Range(-90f, 90f) * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(angle) * side, Mathf.Sin(angle)) * _data.LungeCircleRadius;
            _lungePoint = TargetPosition + offset;

            _lungeDuration = Random.Range(_data.LungeDuration.x, _data.LungeDuration.y);
            _pauseDuration = Random.Range(_data.LungePause.x, _data.LungePause.y);
            _lungeSpeed = Vector2.Distance(Body.position, _lungePoint) / Mathf.Max(_lungeDuration, 0.01f);
            _lungeTime = 0f;
        }

        // 간격은 최소 대기일 뿐이다. 간격이 찬 뒤에는 배회하다 공격 시작 거리 안에 들어오는 순간 준비해 돌진한다.
        protected override bool ShouldStartAttack()
        {
            return _isFighting && _clawTimer >= _data.ClawInterval && IsTargetWithin(_data.AttackTriggerRange);
        }

        // 준비 동안 제자리에 서서 방향을 바꾸지 않는다. 보이는 사각형이 곧 맞는 사각형이다.
        protected override void TickTelegraph(float deltaTime)
        {
            float progress = StateTime / _data.TelegraphDuration;
            _clawTelegraph.Show(Body.position, _clawDirection, _data.ClawLength, _data.ClawWidth, progress);
        }

        // 할퀴는 순간 사각형 전체를 판정하고, 그 범위 끝까지 빠르게 돌진한다. 벽이 있으면 벽 앞에서 멈춘다.
        protected override bool TickAttack(float deltaTime)
        {
            if (!_hasClawed)
            {
                _hasClawed = true;
                HitInBox(_clawDirection, _data.ClawLength, _data.ClawWidth);
                AudioManager.Play(_data.ClawSound);
                _clawTimer = 0f;
                _dashRemaining = _data.ClawLength;
            }

            float step = Mathf.Min(_data.ClawDashSpeed * deltaTime, _dashRemaining);
            float clear = ClearDistance(_clawDirection, step, BodyRadius);
            Body.MovePosition(Body.position + _clawDirection * clear);
            _dashRemaining -= step;
            return _dashRemaining <= 0f || clear < step;
        }

        protected override void OnEnterState(EnemyState state)
        {
            // 준비 소리는 할퀴기 준비 동안만 낸다. 할퀴든 그로기로 끊기든 준비를 벗어나면 멈춘다.
            StopReadySound();

            switch (state)
            {
                case EnemyState.Telegraph:
                    // 걸어서는 못 빠져나가는 폭이므로, 방향은 준비 시작 순간에 고정해 구르기 타이밍만 보게 한다.
                    _clawDirection = DirectionToTarget();
                    _clawTelegraph.Show(Body.position, _clawDirection, _data.ClawLength, _data.ClawWidth, 0f);
                    _readySoundHandle = AudioManager.Play(_data.ClawReadySound);
                    return;

                case EnemyState.Attack:
                    _hasClawed = false;
                    break;

                case EnemyState.Move:
                    // 할퀴기·그로기 뒤에는 플레이어가 움직였으므로 새 자리를 찍는다.
                    PickLungePoint();
                    // 그로기 동안 못 잡았으면 처음부터 다시: 구체가 생기고 실드가 켜진다.
                    if (_isGroggy)
                    {
                        _isGroggy = false;
                        LaunchOrbs();
                    }
                    break;

                case EnemyState.Dead:
                    RetractOrbs();
                    FeedbackPlayer.Play(_data.DefeatFeedback, Body.position);
                    GameEvents.RaiseBossDefeated(_bossNumber);
                    break;
            }

            _clawTelegraph.Hide();
        }

        private void LaunchOrbs()
        {
            int count = Mathf.Min(Tier.OrbCount, _orbs.Length);
            Vector2 center = Body.position;
            // 보스 몸에서 각자 무작위 방향으로 퍼져 나간다. 퍼지는 순간을 보면 어느 구체가 어디로 가는지 읽힌다.
            for (int i = 0; i < count; i++)
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                _orbs[i].Launch(center + direction * _data.OrbSpawnRadius, direction, Tier.OrbSpeed, _data, transform);
            }

            _aliveOrbs = count;
            SetShield(true);
        }

        private void RetractOrbs()
        {
            if (_orbs == null)
            {
                return;
            }

            for (int i = 0; i < _orbs.Length; i++)
            {
                if (_orbs[i] != null)
                {
                    _orbs[i].Retract();
                }
            }

            _aliveOrbs = 0;
        }

        private void OnOrbPopped(BossOrb orb)
        {
            _aliveOrbs--;
            if (_aliveOrbs > 0 || CurrentState == EnemyState.Dead)
            {
                return;
            }

            SetShield(false);
            // 마지막 구체와 보스가 한 폭발에 같이 휘말리면, 실드가 풀린 직후 같은 폭발에 바로 죽어 그로기를 건너뛴다.
            // 실드를 푼 폭발이 보스까지 치지 못하게 아주 잠깐 무적을 준다. 그로기는 다음 폭탄으로 잡는 구간이다.
            HitReceiver.GrantInvulnerability(ShieldBreakGrace);
            _isGroggy = true;
            Stun(_data.GroggyDuration);
            AudioManager.Play(_data.GroggySound);
        }

        // 실드에 막힌 폭탄에 "팅" 하고 튕겨내는 반응을 준다. 반응이 없으면 빗나간 건지 막힌 건지 구분되지 않는다.
        // 실드가 풀린 직후의 짧은 무적에 막힌 경우는 실드가 아니므로 반응하지 않는다.
        private void OnHitBlocked(HitInfo hit)
        {
            if (!HitReceiver.IsShielded)
            {
                return;
            }

            Vector2 center = (Vector2)transform.position + _data.TetherAnchorOffset;
            Vector2 toSource = hit.SourcePosition - center;
            Vector2 sparkPosition = center + (toSource.sqrMagnitude > 0.0001f ? toSource.normalized * 0.6f : Vector2.zero);
            FeedbackPlayer.Play(_data.ImmuneFeedback, sparkPosition);
            AudioManager.Play(_data.ImmuneSound);
            if (_visual != null)
            {
                _visual.Flash(_data.ImmuneFlashColor, _data.ImmuneFlashDuration);
            }
        }

        private void StopReadySound()
        {
            AudioManager.Stop(_readySoundHandle);
            _readySoundHandle = SoundHandle.None;
        }

        // 실드는 따로 그리지 않는다. 구체가 남아 있다는 것 자체가 실드 표시이고, 풀리면 그로기 그림으로 바뀐다.
        private void SetShield(bool isOn)
        {
            HitReceiver.IsShielded = isOn;
        }
    }
}
