using ExplodeIt.Core;
using ExplodeIt.Stage;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 드릴 광부 보스(boss2).
    // 잠수(흙더미가 다가오다 사라짐) → 구멍 예고(진짜 1 + 가짜 N, 원이 차오름) → 모든 구멍 분출 + 진짜 구멍에서 튀어나옴
    // → 노출(회복) → 플레이어 반대쪽으로 도약 → 파고들기 → 잠수 …
    // 실드는 그로기 때만 꺼진다. 노출 중 폭탄에 막히면 죽지 않는 대신 과열이 1 오르고 곧바로 도약해 도망간다.
    // 과열이 다 차면 그 자리에서 그로기에 빠져 한 방에 잡힌다. 못 잡으면 과열이 0으로 돌아간다.
    // 예측 과제: 진짜 구멍을 알아보고 바로 던지면 1초 뒤, 광부가 튀어나온 직후에 터진다. 내 몸은 모든 구멍 원 밖에 있어야 한다.
    public class DrillMinerBoss : Enemy, IBoss
    {
        // 땅속 흙더미가 들썩이는 폭(비율)과 빠르기(회/초). 그림 전용 연출이라 데이터로 빼지 않는다.
        private const float MoundHeave = 0.08f;
        private const float MoundHeaveRate = 5f;
        // 파고들기 중 준비 동작(뛰어오르기)이 차지하는 비율. 나머지 동안 드릴로 파고든다.
        private const float DigHopEnd = 0.2f;
        // 그로기 직후 같은 폭발이 바로 잡지 못하게 하는 짧은 무적. 한 폭발 안의 판정은 같은 프레임에 끝난다.
        private const float GroggyGrace = 0.1f;
        // 구멍 자리를 고를 때 조건에 맞는 곳을 찾아보는 횟수. 못 찾으면 마지막 후보를 쓴다.
        private const int PlacementTries = 24;

        private enum MovePhase
        {
            Idle,
            Leap,
            Dig,
            Underground
        }

        private static readonly Collider2D[] ObstacleBuffer = new Collider2D[1];

        [SerializeField] private DrillMinerData _data;
        [SerializeField] private EnemyVisual _visual;
        // 땅속에 있을 때 보이는 흙더미. 보스를 따라 움직인다.
        [SerializeField] private SpriteRenderer _mound;
        // 오브젝트 피커가 컴포넌트 타입 칸에 프리팹을 띄우지 않아 GameObject로 받는다.
        [SerializeField] private GameObject _holePrefab;
        [SerializeField] private GameObject _dynamitePrefab;

        private DrillHole[] _holes;
        private Vector2[] _holePositions;
        private int _holeCount;
        private int _bossNumber;
        private int _tier;
        private bool _isFighting;
        private bool _isEntering;
        private MovePhase _phase;
        private float _phaseTime;
        private Vector2 _leapStart;
        private Vector2 _leapEnd;
        private int _heat;
        private bool _hasHeatedThisExposure;
        private Color _moundBaseColor;
        private Vector3 _moundBaseScale;
        private float _nextDirtTime;
        private bool _hasPlungedDrill;
        private ContactFilter2D _landingFilter;
        private Dynamite[] _dynamites;
        private System.Action<Dynamite> _onDynamiteExplode;
        private SoundHandle _diggingMoveHandle = SoundHandle.None;

        protected override EnemyData Data => _data;
        protected override bool CanBePulled => false;
        protected override bool RequiresLineOfSight => false;
        // 도약은 플레이어를 바라본 채 뒤로 뛰는 뒷도약이다.
        public override bool FacesTargetWhileMoving => CurrentState == EnemyState.Move && _phase == MovePhase.Leap;

        private DrillMinerData.Tier Tier => _data.GetTier(_tier);

        protected override void Awake()
        {
            base.Awake();

            // 보스는 판마다 한 번씩만 나오므로 구멍 표시도 함께 미리 만든다. 보스를 따라 움직이면 안 되므로 월드에 둔다.
            int count = _data.MaxHoleCount;
            _holes = new DrillHole[count];
            _holePositions = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                _holes[i] = Instantiate(_holePrefab).GetComponent<DrillHole>();
                _holes[i].Hide();
            }

            // 가짜 구멍마다 최대 개수만큼 미리 만든다. 진짜 구멍에서는 나오지 않는다.
            int dynamiteCount = Mathf.Max(0, count - 1) * Mathf.Max(1, _data.DynamiteCount.y);
            _dynamites = new Dynamite[dynamiteCount];
            for (int i = 0; i < dynamiteCount; i++)
            {
                _dynamites[i] = Instantiate(_dynamitePrefab).GetComponent<Dynamite>();
                _dynamites[i].Hide();
            }

            _onDynamiteExplode = OnDynamiteExplode;
            _moundBaseColor = _mound.color;
            _moundBaseScale = _mound.transform.localScale;
            _landingFilter = new ContactFilter2D();
            _landingFilter.SetLayerMask(ObstacleMask);
        }

        protected override void OnEnable()
        {
            _isFighting = false;
            _isEntering = false;
            base.OnEnable();
            HitReceiver.Blocked += OnHitBlocked;
            HideHoles();
            HideDynamites();
            SetMoundAlpha(0f);
            _phase = MovePhase.Idle;
            _heat = 0;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            HitReceiver.Blocked -= OnHitBlocked;
            HideHoles();
            HideDynamites();
            StopDiggingMoveSound();
        }

        private void OnDestroy()
        {
            if (_holes == null)
            {
                return;
            }

            for (int i = 0; i < _holes.Length; i++)
            {
                if (_holes[i] != null)
                {
                    Destroy(_holes[i].gameObject);
                }
            }

            for (int i = 0; i < _dynamites.Length; i++)
            {
                if (_dynamites[i] != null)
                {
                    Destroy(_dynamites[i].gameObject);
                }
            }
        }

        public void BeginBoss(int bossNumber, int tier)
        {
            _bossNumber = bossNumber;
            _tier = tier;
            _heat = 0;
            HitReceiver.IsShielded = true;
            ApplyHeatTint();
        }

        // 플레이어가 멈춰 있는 동안 그 자리에서 파고드는 모습까지 보여준다. 땅속으로 사라진 직후 조작이 돌아온다.
        public void PlayEntrance()
        {
            _isEntering = true;
            BeginDig();
        }

        public bool IsEntranceDone => _isEntering && _phase == MovePhase.Underground;

        // 카메라가 돌아오면 땅속 이동부터 시작한다. 등장 동작을 건너뛴 경우에만 직접 파고든다.
        public void StartFight()
        {
            _isFighting = true;
            _isEntering = false;
            if (_phase != MovePhase.Underground)
            {
                BeginDig();
            }
        }

        protected override void TickMove(float deltaTime)
        {
            // 등장 동작 중에는 파고들기까지만 진행하고, 땅속 이동은 카메라가 돌아온 뒤에 시작한다.
            if (!_isFighting && !(_isEntering && _phase == MovePhase.Dig))
            {
                return;
            }

            _phaseTime += deltaTime;
            switch (_phase)
            {
                case MovePhase.Leap:
                    TickLeap();
                    break;
                case MovePhase.Dig:
                    TickDig();
                    break;
                case MovePhase.Underground:
                    TickUnderground(deltaTime);
                    break;
            }
        }

        // 구조물을 넘어 착지점까지 곧장 날아간다. 그림만 포물선으로 떠오른다.
        private void TickLeap()
        {
            float t = Mathf.Clamp01(_phaseTime / _data.LeapDuration);
            Body.MovePosition(Vector2.Lerp(_leapStart, _leapEnd, t));
            float arc = Mathf.Sin(t * Mathf.PI);
            _visual.Lift = arc * _data.LeapHeight;
            _visual.ExtraLean = -arc * _data.LeapBackLean;
            if (t >= 1f)
            {
                _visual.ExtraLean = 0f;
                BeginDig();
            }
        }

        // 살짝 뛰어올랐다가(준비) 드릴을 내리꽂고, 진동하며 흙을 튀기면서 가라앉아 사라진다.
        private void TickDig()
        {
            float t = Mathf.Clamp01(_phaseTime / _data.DigDuration);
            Vector2 position = Body.position;

            if (t < DigHopEnd)
            {
                // 준비: 위로 늘어나며 뛰어오른다.
                float hop = t / DigHopEnd;
                _visual.Lift = Mathf.Sin(hop * Mathf.PI) * _data.DigHopHeight;
                _visual.ExtraScale = new Vector2(0.9f, 1.12f);
            }
            else
            {
                if (!_hasPlungedDrill)
                {
                    // 드릴이 땅에 박히는 순간 흙이 크게 튄다.
                    _hasPlungedDrill = true;
                    FeedbackPlayer.Play(_data.DigFeedback, position);
                    _nextDirtTime = _phaseTime;
                }

                // 파고들기: 눌린 채 떨면서 가라앉고, 뒤쪽 절반부터 흐려진다.
                float sink = (t - DigHopEnd) / (1f - DigHopEnd);
                _visual.Lift = -_data.DigSinkDepth * sink;
                _visual.ExtraScale = new Vector2(1.1f, 0.92f);
                _visual.Jitter = _data.DigJitter;
                // 바라보는 쪽으로 드릴을 꽂듯 앞으로 기운 채 떤다. 기울기는 박는 순간 빠르게 들어간다.
                float leanIn = Mathf.Clamp01(sink / 0.15f);
                _visual.ExtraLean = _data.DigLean * leanIn + Mathf.Sin(_phaseTime * 12f * 2f * Mathf.PI) * 6f;
                _visual.Alpha = 1f - Mathf.Clamp01((sink - 0.4f) / 0.6f);
                SetMoundAlpha(sink);

                if (_phaseTime >= _nextDirtTime)
                {
                    _nextDirtTime += _data.DigDirtInterval;
                    FeedbackPlayer.Play(_data.DigDirtFeedback, position);
                }
            }

            if (t >= 1f)
            {
                _phase = MovePhase.Underground;
                _phaseTime = 0f;
                _nextDirtTime = 0f;
                ResetDigPose();
                _diggingMoveHandle = AudioManager.Play(_data.DiggingMoveSound);
            }
        }

        private void ResetDigPose()
        {
            _visual.Lift = 0f;
            _visual.ExtraScale = Vector2.one;
            _visual.Jitter = 0f;
            _visual.ExtraLean = 0f;
        }

        // 흙더미가 플레이어 쪽으로 다가오다 희미해져 사라진다. 땅속이라 구조물을 통과한다.
        private void TickUnderground(float deltaTime)
        {
            Vector2 toTarget = TargetPosition - Body.position;
            float step = _data.UndergroundSpeed * deltaTime;
            if (toTarget.sqrMagnitude > step * step)
            {
                Body.MovePosition(Body.position + toTarget.normalized * step);
            }

            float fadeStart = _data.MoundVisibleTime;
            float alpha = _phaseTime <= fadeStart
                ? 1f
                : 1f - Mathf.Clamp01((_phaseTime - fadeStart) / Mathf.Max(_data.MoundFadeTime, 0.01f));
            SetMoundAlpha(alpha);

            // 땅속에서 드릴이 돌고 있다는 느낌으로 흙더미가 들썩이고, 또렷이 보이는 동안 흙이 튄다.
            float heave = Mathf.Sin(_phaseTime * MoundHeaveRate * 2f * Mathf.PI) * MoundHeave;
            _mound.transform.localScale = new Vector3(_moundBaseScale.x * (1f - heave * 0.5f), _moundBaseScale.y * (1f + heave), _moundBaseScale.z);
            if (alpha > 0.5f && _phaseTime >= _nextDirtTime)
            {
                _nextDirtTime = _phaseTime + _data.DigDirtInterval * 2f;
                FeedbackPlayer.Play(_data.DigDirtFeedback, Body.position);
            }
        }

        protected override bool ShouldStartAttack()
        {
            return _isFighting && _phase == MovePhase.Underground && _phaseTime >= _data.UndergroundDuration;
        }

        protected override void TickTelegraph(float deltaTime)
        {
            float progress = StateTime / _data.TelegraphDuration;
            for (int i = 0; i < _holeCount; i++)
            {
                _holes[i].SetProgress(progress);
            }
        }

        // 모든 구멍이 동시에 분출하고, 광부는 진짜 구멍(0번)에서 튀어나온다.
        protected override bool TickAttack(float deltaTime)
        {
            Vector2 real = _holePositions[0];
            Body.position = real;
            transform.position = real;
            _visual.Alpha = 1f;
            _visual.Lift = 0f;

            int nextDynamite = 0;
            for (int i = 0; i < _holeCount; i++)
            {
                HitOverlapping(_holePositions[i], _data.HoleRadius);
                FeedbackPlayer.Play(_data.BurstFeedback, _holePositions[i]);
                if (i > 0)
                {
                    nextDynamite = ScatterDynamites(_holePositions[i], nextDynamite);
                }
            }

            AudioManager.Play(_data.BurstSound);
            HideHoles();
            return true;
        }

        protected override void OnEnterState(EnemyState state)
        {
            switch (state)
            {
                case EnemyState.Telegraph:
                    SetMoundAlpha(0f);
                    StopDiggingMoveSound();
                    PlaceHoles();
                    break;

                case EnemyState.Recover:
                    _hasHeatedThisExposure = false;
                    break;

                case EnemyState.Move:
                    // 노출이 끝났거나 그로기를 놓쳤다. 플레이어 반대쪽으로 도약해 다시 파고든다.
                    HitReceiver.IsShielded = true;
                    ApplyHeatTint();
                    if (_isFighting)
                    {
                        BeginLeap();
                    }
                    break;

                case EnemyState.Dead:
                    HideHoles();
                    HideDynamites();
                    StopDiggingMoveSound();
                    SetMoundAlpha(0f);
                    _visual.Alpha = 1f;
                    ResetDigPose();
                    FeedbackPlayer.Play(_data.DefeatFeedback, Body.position);
                    GameEvents.RaiseBossDefeated(_bossNumber);
                    break;
            }
        }

        private void BeginLeap()
        {
            _phase = MovePhase.Leap;
            _phaseTime = 0f;
            _leapStart = Body.position;
            _leapEnd = PickLandingPoint();
            _visual.Alpha = 1f;
        }

        private void BeginDig()
        {
            _phase = MovePhase.Dig;
            _phaseTime = 0f;
            _hasPlungedDrill = false;
            ResetDigPose();
            AudioManager.Play(_data.DigSound);
        }

        // 플레이어 반대쪽으로 정해진 거리. 맵 안쪽으로 당기고, 구조물 위라면 30도씩 돌려 빈 자리를 찾는다.
        private Vector2 PickLandingPoint()
        {
            Vector2 away = Body.position - TargetPosition;
            Vector2 direction = away.sqrMagnitude > 0.0001f ? away.normalized : Random.insideUnitCircle.normalized;
            Vector2 fallback = ClampToArena(Body.position + direction * _data.LeapDistance);
            for (int i = 0; i < 12; i++)
            {
                float angle = (i % 2 == 0 ? 1f : -1f) * ((i + 1) / 2) * 30f;
                Vector2 point = ClampToArena(Body.position + (Vector2)(Quaternion.Euler(0f, 0f, angle) * direction) * _data.LeapDistance);
                if (IsClear(point))
                {
                    return point;
                }
            }

            return fallback;
        }

        // 진짜 구멍(0번)을 먼저 놓고, 가짜 구멍은 서로 최소 간격을 두고 플레이어 주변에 흩어 놓는다.
        private void PlaceHoles()
        {
            _holeCount = Mathf.Min(Tier.HoleCount, _holes.Length);
            Vector2 player = TargetPosition;
            for (int i = 0; i < _holeCount; i++)
            {
                Vector2 range = i == 0 ? _data.RealHoleDistance : _data.FakeHoleDistance;
                Vector2 point = player;
                for (int attempt = 0; attempt < PlacementTries; attempt++)
                {
                    point = ClampToArena(player + Random.insideUnitCircle.normalized * Random.Range(range.x, range.y));
                    if (IsClear(point) && IsSpaced(point, i))
                    {
                        break;
                    }
                }

                _holePositions[i] = point;
                _holes[i].Show(point, _data.HoleRadius, i == 0);
            }
        }

        private bool IsSpaced(Vector2 point, int placedCount)
        {
            float spacing = _data.HoleSpacing;
            for (int i = 0; i < placedCount; i++)
            {
                if ((_holePositions[i] - point).sqrMagnitude < spacing * spacing)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsClear(Vector2 point)
        {
            return Physics2D.OverlapCircle(point, _data.LandingClearance, _landingFilter, ObstacleBuffer) == 0;
        }

        private Vector2 ClampToArena(Vector2 point)
        {
            ArenaBounds arena = ArenaBounds.Current;
            if (arena == null)
            {
                return point;
            }

            Bounds bounds = arena.Bounds;
            float margin = _data.LandingClearance;
            return new Vector2(
                Mathf.Clamp(point.x, bounds.min.x + margin, bounds.max.x - margin),
                Mathf.Clamp(point.y, bounds.min.y + margin, bounds.max.y - margin));
        }

        // 실드는 그로기 때만 꺼지므로 모든 폭탄이 여기로 온다.
        // 노출 중이면 과열을 올리고(한 번 노출에 1회), 아니면 막힘 반응만 보여준다. 땅속에선 보이지 않으니 반응하지 않는다.
        private void OnHitBlocked(HitInfo hit)
        {
            EnemyState state = CurrentState;
            if (state == EnemyState.Recover)
            {
                if (!_hasHeatedThisExposure)
                {
                    _hasHeatedThisExposure = true;
                    AddHeat();
                }

                return;
            }

            bool isVisible = state != EnemyState.Telegraph && state != EnemyState.Dead
                && !(state == EnemyState.Move && _phase == MovePhase.Underground);
            if (isVisible && HitReceiver.IsShielded)
            {
                FeedbackPlayer.Play(_data.ImmuneFeedback, Body.position);
                AudioManager.Play(_data.ImmuneSound);
                _visual.Flash(_data.ImmuneFlashColor, _data.FlashDuration);
            }
        }

        private void AddHeat()
        {
            _heat++;
            ApplyHeatTint();
            FeedbackPlayer.Play(_data.HeatFeedback, Body.position);
            _visual.Flash(_data.HeatFlashColor, _data.FlashDuration);

            if (_heat < _data.HeatToOverheat)
            {
                AudioManager.Play(_data.HeatSound);
                // 맞는 순간 도망간다. 노출 시간을 다 채우지 않는다.
                EndRecoverEarly();
                return;
            }

            // 드릴이 멈춰 맞은 그 자리에서 주저앉는다.
            AudioManager.Play(_data.OverheatSound);
            AudioManager.Play(_data.GroggySound);
            HitReceiver.IsShielded = false;
            HitReceiver.GrantInvulnerability(GroggyGrace);
            _heat = 0;
            Stun(_data.GroggyDuration);
        }

        // 과열될수록 붉어진다. 다 과열되면 과열 수를 0으로 되돌리지만 색은 그로기가 끝나 다시 움직일 때 돌아온다.
        private void ApplyHeatTint()
        {
            _visual.Tint = Color.Lerp(Color.white, _data.OverheatTint, (float)_heat / _data.HeatToOverheat);
        }

        private void SetMoundAlpha(float alpha)
        {
            Color color = _moundBaseColor;
            color.a *= Mathf.Clamp01(alpha);
            _mound.color = color;
        }

        // 가짜 구멍에서 다이너마이트를 서로 다른 방향으로 흩뿌린다. 방향은 고르게 나누고 조금씩 흔들어 매번 모양이 다르게 한다.
        // 다음에 쓸 다이너마이트 번호를 돌려준다.
        private int ScatterDynamites(Vector2 center, int startIndex)
        {
            int count = Random.Range(_data.DynamiteCount.x, _data.DynamiteCount.y + 1);
            float baseAngle = Random.Range(0f, 360f);
            float step = 360f / Mathf.Max(1, count);
            int index = startIndex;
            for (int k = 0; k < count && index < _dynamites.Length; k++)
            {
                float angle = (baseAngle + step * k + Random.Range(-step * 0.25f, step * 0.25f)) * Mathf.Deg2Rad;
                float distance = Random.Range(_data.DynamiteDistance.x, _data.DynamiteDistance.y);
                Vector2 landing = ClampToArena(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance);
                float delay = Random.Range(_data.DynamiteDelayRange.x, _data.DynamiteDelayRange.y);
                _dynamites[index].Launch(center, landing, delay,_data.DynamiteArcHeight, _data.DynamiteRadius, _onDynamiteExplode);
                index++;
            }

            return index;
        }

        private void OnDynamiteExplode(Dynamite dynamite)
        {
            HitOverlapping(dynamite.Position, dynamite.Radius);
            FeedbackPlayer.Play(_data.DynamiteFeedback, dynamite.Position);
            AudioManager.Play(_data.DynamiteSound);
        }

        private void HideDynamites()
        {
            if (_dynamites == null)
            {
                return;
            }

            for (int i = 0; i < _dynamites.Length; i++)
            {
                if (_dynamites[i] != null)
                {
                    _dynamites[i].Hide();
                }
            }
        }

        private void StopDiggingMoveSound()
        {
            AudioManager.Stop(_diggingMoveHandle);
            _diggingMoveHandle = SoundHandle.None;
        }

        private void HideHoles()
        {
            if (_holes == null)
            {
                return;
            }

            for (int i = 0; i < _holes.Length; i++)
            {
                if (_holes[i] != null)
                {
                    _holes[i].Hide();
                }
            }
        }
    }
}
