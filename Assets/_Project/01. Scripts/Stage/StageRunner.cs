using System;
using System.Collections.Generic;
using ExplodeIt.Core;
using ExplodeIt.Enemies;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 처치 수 기반 진행: 대기 → 웨이브(정해진 수 스폰) → 전부 처치 → 보스 등장 연출 → 보스전 → 웨이브 사이 간격 → 다음 웨이브.
    // 웨이브가 시작될 때 그 웨이브의 스폰을 "몇 초에, 어떤 적이, 어디서"로 미리 펼쳐 두고,
    // 시간이 되면 앞에서부터 꺼낸다. 전투 중에는 목록을 읽기만 하므로 할당이 없다.
    // 무한 모드는 미리 펼치지 않고, 생성기가 웨이브 도중 낼 차례가 된 군집을 같은 목록에 이어 붙인다.
    public class StageRunner : MonoBehaviour
    {
        // 처치 대화 옆자리에 다 왔다고 보는 거리, 옆자리가 비어 있는지 볼 반지름 (유닛). 연출용 값이다.
        private const float TalkArriveDistance = 0.35f;
        private const float TalkPointClearance = 0.4f;

        public enum Phase
        {
            Rest,
            Spawning,
            BossIntro,
            // 스토리 보스전: 왕이 먼저 나와 대화하고, 대화 중 표시한 줄에서 보스가 내려앉고 등장 동작을 시작한다.
            BossDialogue,
            // 보스가 내려앉은 뒤: 멈춤 → 보스 등장 동작 → 조작 복귀·카메라 복귀 → 보스 행동 시작.
            BossEntrance,
            Boss,
            // 최종 보스를 쓰러뜨린 뒤: 슬로모션 → 처치 대화 → 왕이 사라짐 → 스테이지 끝.
            BossDefeat
        }

        private readonly struct SpawnEvent
        {
            public readonly float Time;
            // 같은 시간이면 데이터에 적은 순서를 지키기 위한 번호.
            public readonly int Order;
            public readonly Enemy Prefab;
            public readonly Vector2 Position;
            // 구역에 입구가 연결돼 있으면 먼저 걸어갈 입구 안의 점.
            public readonly bool HasEntry;
            public readonly Vector2 EntryPoint;
            // 맵 안에 바로 나와 진입 유예 없이 싸움에 드는지(저주 "기습").
            public readonly bool IsAmbush;

            public SpawnEvent(float time, int order, Enemy prefab, Vector2 position, bool hasEntry, Vector2 entryPoint,
                bool isAmbush = false)
            {
                Time = time;
                Order = order;
                Prefab = prefab;
                Position = position;
                HasEntry = hasEntry;
                EntryPoint = entryPoint;
                IsAmbush = isAmbush;
            }
        }

        private static readonly Comparison<SpawnEvent> ByTime = (a, b) =>
            a.Time != b.Time ? a.Time.CompareTo(b.Time) : a.Order.CompareTo(b.Order);
        // 무한 모드는 웨이브 도중 목록 뒤쪽만 다시 정렬하는데, 구간 정렬은 IComparer만 받는다.
        private static readonly IComparer<SpawnEvent> ByTimeComparer = Comparer<SpawnEvent>.Create(ByTime);

        [SerializeField] private StageData _stage;
        // 메뉴에서 무한 모드로 시작한 판은 이 진행표로 바꿔 쓴다. 비워 두면 늘 위 진행표를 쓴다.
        [SerializeField] private StageData _endlessStage;
        [SerializeField] private EnemySpawner _spawner;
        // 보스가 내려앉을 자리를 연출 동안 미리 보여주는 원. 비워 두면 표시 없이 나타난다.
        [SerializeField] private Transform _bossSpawnMarker;
        // 보스가 구조물 안에 내려앉지 않게 피할 레이어.
        [SerializeField] private LayerMask _obstacleMask;
        // 스토리 보스전 등장 대화 연출. 보스 등장 동작을 대화가 끝날 때까지 미뤄야 해서 직접 참조한다. 비워 두면 대화 없이 진행한다.
        [SerializeField] private BossDialoguePlayer _dialoguePlayer;
        // 첫 판 오프닝 연출. 끝날 때까지 첫 웨이브를 미뤄야 해서 직접 참조한다. 비워 두면 바로 시작한다.
        [SerializeField] private IntroSequence _intro;
        // 첫 판 조작 튜토리얼. 오프닝을 본 판에만 이어서 하고, 끝나야 첫 웨이브를 연다. 비워 두면 바로 시작한다.
        [SerializeField] private TutorialSequence _tutorial;
        // 스토리 엔딩 연출. 끝날 때까지 판 끝(결과 창)을 미뤄야 해서 직접 참조한다. 비워 두면 바로 끝난다.
        [SerializeField] private OutroSequence _outro;
        // 찐엔딩(저주를 모두 걸고 왕관까지 낀 무한 모드): 웨이브 없이 왕과 대화한 뒤 각성한 왕과 싸우고, 쓰러뜨리면 크레딧으로 끝난다.
        // 둘 중 하나라도 비워 두면 왕관을 껴도 평소 무한 모드로 진행한다.
        [SerializeField] private TrueEndingData _trueEnding;
        [SerializeField] private TrueEndingSequence _trueEndingSequence;

        // 기습 자리를 찾는 횟수와 적 한 마리 둘레로 비워 둘 반경, 같은 군집 적끼리 떨어뜨릴 거리 (유닛).
        private const int AmbushAttempts = 12;
        private const float AmbushClearRadius = 0.6f;
        private const float AmbushSeparation = 1.6f;

        private readonly List<Vector2> _ambushSpots = new List<Vector2>(8);

        private readonly List<SpawnEvent> _events = new List<SpawnEvent>(128);
        private readonly Dictionary<int, List<SpawnArea>> _areasById = new Dictionary<int, List<SpawnArea>>();
        private readonly List<EndlessGroupOrder> _orders = new List<EndlessGroupOrder>(8);
        // 무한 모드에서만 쓴다. 스토리 모드면 null.
        private EndlessWaveGenerator _generator;
        private int _eventOrder;

        private Phase _phase;
        private float _phaseTime;
        private float _restDuration;
        private int _waveIndex;
        private int _waveNumber;
        private int _nextEvent;
        private bool _isRunning;
        private Enemy _bossPrefab;
        private int _bossNumber;
        private int _bossTier;
        private Vector2 _bossSpawnPoint;
        private bool _isBossDefeated;
        private IBoss _boss;
        private bool _isControlReleased;
        private bool _hasPlayedEntrance;
        private BossDialogueData _dialogue;
        private BossSpawnPoints[] _bossSpawnPointSets = Array.Empty<BossSpawnPoints>();
        private float _releaseTime;
        private Enemy _bossEnemy;
        // 최종 보스 처치 흐름: 쓰러진 실제 시각, 대화를 열었는지, 왕을 보낸 실제 시각(음수면 아직).
        private float _defeatStartTime;
        private bool _isDefeatTalking;
        private float _farewellTime = -1f;
        private bool _isPlayingOutro;
        // 고블린이 쓰러진 왕 쪽으로 걸어가기 시작한 실제 시각. 음수면 걷는 중이 아니다.
        private float _walkStartTime = -1f;
        // 처치 대화 때 고블린이 설 왕 옆자리.
        private Vector2 _talkPoint;
        private bool _isTrueEnding;
        // 오프닝·튜토리얼이 끝나기를 기다리는 중. 그동안은 웨이브 시계를 돌리지 않는다.
        private bool _isWaitingIntro;
        // 스토리를 중간 지점부터 시작했는지(시작 지점 번호, 0이면 처음부터)와, 첫 쉬는 시간 뒤 웨이브 없이 최종 보스전부터 하는지.
        private int _startCheckpoint;
        private bool _startsAtBoss;
        private System.Action _onIntroFinished;
        private System.Action _onTutorialFinished;
        private System.Action _onDialogueSpawnBoss;
        private System.Action _onDialogueEntrance;

        public bool IsPaused { get; set; }
        public Phase CurrentPhase => _phase;
        public float PhaseTime => _phaseTime;
        // 지금까지 시작한 웨이브 수(1부터). 마지막 웨이브를 반복하면 데이터 번호보다 커진다.
        public int WaveNumber => _waveNumber;
        // 무한 모드는 정해진 웨이브가 없어 0이다.
        public int WaveCount => _stage != null && !_stage.IsEndless ? _stage.Waves.Count : 0;

        private void OnEnable()
        {
            GameEvents.GameStateChanged += OnGameStateChanged;
            GameEvents.BossDefeated += OnBossDefeated;
            if (_dialoguePlayer != null)
            {
                _onDialogueSpawnBoss ??= OnDialogueSpawnBoss;
                _onDialogueEntrance ??= OnDialogueEntrance;
                _dialoguePlayer.BossSpawnRequested += _onDialogueSpawnBoss;
                _dialoguePlayer.EntranceRequested += _onDialogueEntrance;
            }
        }

        private void OnDisable()
        {
            GameEvents.GameStateChanged -= OnGameStateChanged;
            GameEvents.BossDefeated -= OnBossDefeated;
            if (_dialoguePlayer != null)
            {
                _dialoguePlayer.BossSpawnRequested -= _onDialogueSpawnBoss;
                _dialoguePlayer.EntranceRequested -= _onDialogueEntrance;
            }
        }

        private void Start()
        {
            if (RunSetup.Mode == RunMode.Endless && _endlessStage != null)
            {
                _stage = _endlessStage;
            }

            _isTrueEnding = RunSetup.Mode == RunMode.Endless && RunCurses.Has(CurseKind.King)
                && _trueEnding != null && _trueEnding.King != null && _trueEndingSequence != null && _dialoguePlayer != null;

            // 정적 값이라 다시 하기(씬 다시 불러오기)에도 지난 판 속도가 남으므로 판마다 되돌린다.
            EnemyTempo.MoveSpeedMultiplier = 1f;
            BossTempo.Reset();

            // 구역을 추가할 때마다 인스펙터에 연결하지 않도록 시작할 때 씬에서 모두 찾는다.
            SpawnArea[] areas = FindObjectsByType<SpawnArea>();
            if (areas.Length == 0 || (!_stage.IsEndless && _stage.Waves.Count == 0))
            {
                Debug.LogError("StageRunner: 스폰 구역이나 웨이브가 비어 있습니다.", this);
                enabled = false;
                return;
            }

            IndexAreas(areas);
            // 보스 지정 자리도 구역처럼 씬에서 모두 찾는다. 없으면 기존 방식(플레이어 근처 넓은 쪽)으로 내려온다.
            _bossSpawnPointSets = FindObjectsByType<BossSpawnPoints>();
            if (_stage.IsEndless)
            {
                _generator = new EndlessWaveGenerator(_stage.EndlessWaves, areas, _stage.SpawnsPerWave);
            }

            if (!(_stage.IsEndless ? ValidateEndless() : ValidateWaves()))
            {
                enabled = false;
                return;
            }

            _spawner.Prepare(CollectEnemyPrefabs());
            PrepareBosses();
            SetMarker(false, 0f);
            // 중간부터 시작하는 판은 이미 본 오프닝·기초 과제를 건너뛴다.
            if (ApplyStartCheckpoint())
            {
                BeginStage();
                return;
            }

            if (_intro != null && _stage.IsStoryMode && _intro.ShouldPlay)
            {
                _isWaitingIntro = true;
                _onIntroFinished ??= OnIntroFinished;
                _intro.Play(_onIntroFinished);
                return;
            }

            BeginStage();
        }

        private void OnIntroFinished()
        {
            StartTutorialOrStage();
        }

        private void StartTutorialOrStage()
        {
            if (_tutorial != null && _tutorial.CanPlay)
            {
                _isWaitingIntro = true;
                _onTutorialFinished ??= OnTutorialFinished;
                _tutorial.PlayBasics(_onTutorialFinished);
                return;
            }

            _isWaitingIntro = false;
            BeginStage();
        }

        private void OnTutorialFinished()
        {
            _isWaitingIntro = false;
            BeginStage();
        }

        // 스토리 시작 지점 n은 웨이브 n개(와 그 뒤 보스 n마리)를 끝낸 자리다. 웨이브 수와 같으면 마지막 웨이브까지 끝내고 최종 보스만 남은 자리다.
        // 처치 수는 건너뛴 웨이브의 스폰 수만큼 잡은 것으로 치고 이어 센다.
        private bool ApplyStartCheckpoint()
        {
            if (!_stage.IsStoryMode || _stage.IsEndless || RunSetup.StartCheckpoint <= 0)
            {
                return false;
            }

            _startCheckpoint = Mathf.Min(RunSetup.StartCheckpoint, WaveCount);
            _waveNumber = _startCheckpoint;
            _waveIndex = Mathf.Min(_startCheckpoint, WaveCount - 1);
            _startsAtBoss = _startCheckpoint >= WaveCount;
            return true;
        }

        private int SkippedKills()
        {
            int kills = 0;
            for (int i = 0; i < _startCheckpoint; i++)
            {
                kills += _stage.Waves[i].TotalSpawnCount;
            }

            return kills;
        }

        // 판 시간과 웨이브 시계는 여기서부터 센다. 오프닝은 판 시간에 들어가지 않는다.
        private void BeginStage()
        {
            if (_startCheckpoint > 0)
            {
                GameEvents.RaiseStoryResumed(_startCheckpoint, SkippedKills());
            }

            GameEvents.RaiseStageStarted(_stage.IsStoryMode);
            BeginRest(_isTrueEnding ? _trueEnding.StartDelay : _stage.FirstWaveDelay);
        }

        private void Update()
        {
            if (!_isRunning || IsPaused || _isWaitingIntro)
            {
                return;
            }

            // 웨이브 적이 나오는 동안만 시계를 빨리 돌려 스폰 간격을 줄인다(아티팩트 팬던트). 휴식·보스전 시간은 그대로다.
            bool isSpawning = _phase == Phase.Spawning;
            float spawnInterval = RunModifiers.SpawnIntervalMultiplier;
            _phaseTime += isSpawning && spawnInterval > 0f ? Time.deltaTime / spawnInterval : Time.deltaTime;
            switch (_phase)
            {
                case Phase.Rest:
                    if (_phaseTime >= _restDuration)
                    {
                        if (_isTrueEnding)
                        {
                            BeginTrueEndingBoss();
                        }
                        else if (_startsAtBoss)
                        {
                            _startsAtBoss = false;
                            BeginBossIntro(_waveNumber);
                        }
                        else
                        {
                            StartWave();
                        }
                    }
                    break;

                case Phase.Spawning:
                    if (_generator != null)
                    {
                        GenerateEndless();
                    }

                    while (_nextEvent < _events.Count && _events[_nextEvent].Time <= _phaseTime)
                    {
                        SpawnEvent spawn = _events[_nextEvent];
                        Enemy enemy = _spawner.Spawn(spawn.Prefab, spawn.Position);
                        if (enemy != null && spawn.HasEntry)
                        {
                            enemy.SetEntryPoint(spawn.EntryPoint);
                        }

                        if (enemy != null && spawn.IsAmbush)
                        {
                            enemy.SkipArenaEntryDelay();
                        }
                        _nextEvent++;
                    }

                    // 웨이브 사이는 처치로만 넘어간다. 다 나오고 다 잡혀야 보스전이다.
                    if (_nextEvent >= _events.Count && (_generator == null || _generator.IsDone) && _spawner.ActiveCount == 0)
                    {
                        GameEvents.RaiseWaveCleared(_waveNumber);
                        // 마지막 웨이브를 끝내면 최종 보스전부터 다시 할 수 있다.
                        if (_stage.IsStoryMode && !_stage.IsEndless && _waveNumber >= WaveCount)
                        {
                            GameEvents.RaiseStoryCheckpointReached(_waveNumber);
                        }

                        BeginBossIntro(_waveNumber);
                    }
                    break;

                case Phase.BossIntro:
                    // 내려앉을 자리를 원으로 키워 보여주다가, 다 차는 순간 보스가 나타난다.
                    float introDuration = _stage.GetBossIntroDuration(_bossTier);
                    float progress = introDuration > 0f ? _phaseTime / introDuration : 1f;
                    SetMarker(_bossPrefab != null, progress);
                    if (_phaseTime >= introDuration)
                    {
                        SetMarker(false, 0f);
                        if (_bossPrefab != null)
                        {
                            SpawnBoss();
                            EnterPhase(Phase.BossEntrance);
                        }
                        else
                        {
                            // 아직 만들지 않은 보스 자리는 연출만 보여주고 넘어간다.
                            FinishBoss();
                        }
                    }
                    break;

                case Phase.BossDialogue:
                    if (_dialoguePlayer.IsPlaying)
                    {
                        break;
                    }

                    // 아직 만들지 않은 보스(최종 보스 등)는 대화만 보여 주고 넘어간다.
                    if (_bossPrefab == null)
                    {
                        ReleaseEntrance();
                        FinishBoss();
                        break;
                    }

                    // 대화에서 표시하지 않은 단계는 대화가 끝난 지금 진행한다.
                    if (_bossEnemy == null)
                    {
                        SpawnBoss();
                    }

                    StartEntrance();
                    EnterPhase(Phase.BossEntrance);
                    break;

                case Phase.BossEntrance:
                    if (_isBossDefeated)
                    {
                        ReleaseEntrance();
                        FinishBoss();
                        break;
                    }

                    if (!_hasPlayedEntrance && _phaseTime >= _stage.BossRevealHold)
                    {
                        StartEntrance();
                    }

                    // 보스마다 등장 동작(예: 광부의 파고들기)이 끝나는 시점이 달라서, 끝났다고 알려 올 때 조작을 돌려준다.
                    if (!_isControlReleased && _hasPlayedEntrance && (_boss == null || _boss.IsEntranceDone))
                    {
                        ReleaseEntrance();
                        _releaseTime = _phaseTime;
                    }

                    if (_isControlReleased && _phaseTime >= _releaseTime + _stage.BossCameraBlend)
                    {
                        _boss?.StartFight();
                        EnterPhase(Phase.Boss);
                    }
                    break;

                case Phase.Boss:
                    if (_isBossDefeated)
                    {
                        if (_isTrueEnding)
                        {
                            BeginTrueEndingDefeat();
                        }
                        else if (_bossNumber >= _stage.FinalBossNumber)
                        {
                            BeginDefeat();
                        }
                        else
                        {
                            FinishBoss();
                        }
                    }
                    break;

                case Phase.BossDefeat:
                    // 찐엔딩은 처치 대화 없이 연출 감독이 끝을 알려 온다.
                    if (!_isTrueEnding)
                    {
                        TickDefeat();
                    }
                    break;
            }
        }

        public int BossKindCount => _stage != null ? _stage.BossKindCount : 0;
        public bool IsEndless => _stage != null && _stage.IsEndless;
        public int BossLaps => _stage != null ? _stage.BossLaps : 0;

        // 개발자 패널용. 남은 적과 스폰을 지우고 해당 보스의 등장 연출부터 바로 시작한다.
        public void SkipToBoss(int bossNumber)
        {
            _spawner.KillAll();
            _events.Clear();
            _nextEvent = 0;
            _generator?.Stop();
            BeginBossIntro(bossNumber);
        }

        private void BeginBossIntro(int bossNumber)
        {
            Enemy prefab = _stage.GetBoss(bossNumber, out int tier);
            BossDialogueData dialogue = _stage.IsStoryMode && _dialoguePlayer != null ? _stage.GetBossDialogue(bossNumber) : null;
            BeginBossIntro(bossNumber, prefab, tier, dialogue);
        }

        // 찐엔딩 결전. 보스 번호는 보스 순서와 상관없으므로 1로 둔다(처치 알림에만 쓰인다).
        private void BeginTrueEndingBoss()
        {
            BeginBossIntro(1, _trueEnding.King, 0, _trueEnding.Dialogue);
        }

        private void BeginBossIntro(int bossNumber, Enemy prefab, int tier, BossDialogueData dialogue)
        {
            _bossNumber = bossNumber;
            _bossPrefab = prefab;
            _bossTier = tier;
            _stage.ApplyBossTempo(_bossTier);
            _isBossDefeated = false;
            _bossEnemy = null;
            _boss = null;
            _hasPlayedEntrance = false;
            _dialogue = dialogue;
            if (_bossPrefab != null || dialogue != null)
            {
                _bossSpawnPoint = PickBossSpawnPoint();
                // 등장 연출 동안 플레이어를 세우고, 카메라를 플레이어와 보스 사이로 옮겨 둘 다 보이게 한다.
                Vector2 player = _spawner.Target != null ? (Vector2)_spawner.Target.position : _bossSpawnPoint;
                GameEvents.RaisePlayerControlLockChanged(true);
                GameEvents.RaiseCameraFocusRequested((player + _bossSpawnPoint) * 0.5f, _stage.BossCameraBlend);
                _isControlReleased = false;
            }

            GameEvents.RaiseBossIntroStarted(bossNumber);
            if (_bossPrefab != null)
            {
                GameEvents.RaiseBossAppeared(_bossPrefab.name, _bossTier);
            }
            if (dialogue != null)
            {
                Vector2 player = _spawner.Target != null ? (Vector2)_spawner.Target.position : _bossSpawnPoint;
                _dialoguePlayer.Play(dialogue, _bossSpawnPoint, player, _spawner.Target);
                EnterPhase(Phase.BossDialogue);
                return;
            }

            EnterPhase(Phase.BossIntro);
        }

        private void SpawnBoss()
        {
            Enemy boss = _spawner.Spawn(_bossPrefab, _bossSpawnPoint);
            _bossEnemy = boss;
            _boss = boss as IBoss;
            _boss?.BeginBoss(_bossNumber, _bossTier);
            // 왕이 그 자리에서 변신하는 보스는 내려앉지 않으므로 착지 연출을 뺀다.
            if (_dialogue == null || !_dialogue.KingBecomesBoss)
            {
                FeedbackPlayer.Play(_stage.BossLandFeedback, _bossSpawnPoint);
            }
        }

        private void StartEntrance()
        {
            if (_hasPlayedEntrance)
            {
                return;
            }

            _hasPlayedEntrance = true;
            _boss?.PlayEntrance();
        }

        // 대화의 "보스 등장" 줄. 보스가 내려앉고, 이후 보스 줄 말풍선이 보스 머리 위에 뜨도록 알려 준다.
        private void OnDialogueSpawnBoss()
        {
            if (_phase != Phase.BossDialogue || _bossPrefab == null || _bossEnemy != null)
            {
                return;
            }

            SpawnBoss();
            _dialoguePlayer.SetBoss(_bossEnemy != null ? _bossEnemy.transform : null);
        }

        // 대화의 "등장 동작 시작" 줄. 보스가 아직 없으면 먼저 내려앉힌다.
        private void OnDialogueEntrance()
        {
            if (_phase != Phase.BossDialogue || _bossPrefab == null)
            {
                return;
            }

            OnDialogueSpawnBoss();
            StartEntrance();
        }

        // 플레이어 조작을 돌려주고 카메라를 플레이어에게 되돌린다. 보스는 카메라가 다 돌아온 뒤 움직인다.
        private void ReleaseEntrance()
        {
            if (_isControlReleased)
            {
                return;
            }

            _isControlReleased = true;
            GameEvents.RaisePlayerControlLockChanged(false);
            GameEvents.RaiseCameraFocusReleased(_stage.BossCameraBlend);
        }

        // 최종 보스를 쓰러뜨렸다. 슬로모션이 끝나도록 실제 시간으로 기다린 뒤 처치 대화를 연다. 대화가 없으면 바로 왕을 보내고 끝낸다.
        private void BeginDefeat()
        {
            EnterPhase(Phase.BossDefeat);
            GameEvents.RaiseStageTimerStopped();
            _defeatStartTime = Time.unscaledTime;
            _isDefeatTalking = false;
            _farewellTime = -1f;
            _isPlayingOutro = false;
            _walkStartTime = -1f;
            GameEvents.RaisePlayerControlLockChanged(true);
            _isControlReleased = false;
            if (_bossEnemy != null)
            {
                GameEvents.RaisePlayerLookRequested(_bossEnemy.transform.position);
            }

            BossDialogueData dialogue = _stage.FinalBossDefeatDialogue;
            if (dialogue == null || _dialoguePlayer == null || _bossEnemy == null)
            {
                SendFarewell();
            }
        }

        // 찐엔딩 결전 승리. 대화 없이 조작을 묶고(저주도 함께 멈춘다) 연출 감독에게 넘긴다. 판 시간도 여기서 멈춘다.
        private void BeginTrueEndingDefeat()
        {
            EnterPhase(Phase.BossDefeat);
            GameEvents.RaiseStageTimerStopped();
            GameEvents.RaisePlayerControlLockChanged(true);
            _isControlReleased = false;
            _trueEndingSequence.Play(_trueEnding, EndTrueEnding);
        }

        private void EndTrueEnding()
        {
            _isRunning = false;
            GameEvents.RaiseStageCleared();
        }

        private void TickDefeat()
        {
            if (_farewellTime >= 0f)
            {
                if (_isPlayingOutro || Time.unscaledTime - _farewellTime < _stage.DefeatEndDelay)
                {
                    return;
                }

                // 엔딩 동안은 조작을 묶어 둔다. 만화를 넘기는 클릭에 폭탄이 나가면 안 된다.
                if (_outro != null && _stage.Ending != null)
                {
                    _isPlayingOutro = true;
                    Transform player = _spawner.Target;
                    Vector2 book = _dialoguePlayer != null ? _dialoguePlayer.BookPosition
                        : player != null ? (Vector2)player.position : Vector2.zero;
                    _outro.Play(_stage.Ending, book, EndAfterOutro);
                    return;
                }

                EndAfterOutro();
                return;
            }

            if (!_isDefeatTalking)
            {
                if (Time.unscaledTime - _defeatStartTime < _stage.DefeatDialogueDelay)
                {
                    return;
                }

                // 멀리 있으면 고블린이 쓰러진 왕 쪽으로 걸어가 말할 거리까지 다가간다. 막혀서 못 가면 시간이 지나 그 자리에서 대화한다.
                if (!IsCloseEnoughToTalk())
                {
                    return;
                }

                // 쓰러진 왕과 고블린이 함께 보이게 둘 사이를 비춘다.
                _isDefeatTalking = true;
                Transform player = _spawner.Target;
                Vector2 boss = _bossEnemy.transform.position;
                Vector2 middle = player != null ? (boss + (Vector2)player.position) * 0.5f : boss;
                GameEvents.RaiseCameraZoomRequested(1f, _stage.BossCameraBlend);
                GameEvents.RaiseCameraFocusRequested(middle, _stage.BossCameraBlend);
                _dialoguePlayer.PlayDefeat(_stage.FinalBossDefeatDialogue, _bossEnemy.transform, player);
                return;
            }

            if (!_dialoguePlayer.IsPlaying)
            {
                SendFarewell();
            }
        }

        // 고블린이 쓰러진 왕 옆자리에 나란히 선다. 지금 있는 쪽 옆자리를 먼저 고르고, 그쪽이 막혀 있으면 반대쪽으로 간다.
        // 막혀서 못 가면 시간이 지나 그 자리에서 대화한다.
        private bool IsCloseEnoughToTalk()
        {
            Transform player = _spawner.Target;
            if (player == null)
            {
                return true;
            }

            Vector2 position = player.position;
            if (_walkStartTime < 0f)
            {
                _talkPoint = PickTalkPoint(position);
                if ((position - _talkPoint).magnitude <= TalkArriveDistance)
                {
                    return true;
                }

                _walkStartTime = Time.unscaledTime;
                GameEvents.RaisePlayerWalkRequested(_talkPoint);
                return false;
            }

            if ((position - _talkPoint).magnitude <= TalkArriveDistance || Time.unscaledTime - _walkStartTime >= _stage.DefeatWalkTimeout)
            {
                GameEvents.RaisePlayerWalkCancelled();
                return true;
            }

            return false;
        }

        private Vector2 PickTalkPoint(Vector2 player)
        {
            Vector2 boss = _bossEnemy.transform.position;
            float side = player.x >= boss.x ? 1f : -1f;
            Vector2 near = boss + new Vector2(side * _stage.DefeatTalkDistance, _stage.DefeatTalkHeight);
            if (!IsTalkPointBlocked(boss, near))
            {
                return near;
            }

            Vector2 far = boss + new Vector2(-side * _stage.DefeatTalkDistance, _stage.DefeatTalkHeight);
            return IsTalkPointBlocked(boss, far) ? near : far;
        }

        // 맵 밖이거나, 그 자리에 구조물이 있거나, 왕과 그 자리 사이를 구조물이 가로막으면 막힌 자리다.
        private bool IsTalkPointBlocked(Vector2 boss, Vector2 point)
        {
            ArenaBounds arena = ArenaBounds.Current;
            if (arena != null)
            {
                Bounds bounds = arena.Bounds;
                if (point.x < bounds.min.x + TalkPointClearance || point.x > bounds.max.x - TalkPointClearance
                    || point.y < bounds.min.y + TalkPointClearance || point.y > bounds.max.y - TalkPointClearance)
                {
                    return true;
                }
            }

            return Physics2D.OverlapCircle(point, TalkPointClearance, _obstacleMask) || Physics2D.Linecast(boss, point, _obstacleMask);
        }

        private void EndAfterOutro()
        {
            ReleaseEntrance();
            FinishBoss();
        }

        private void SendFarewell()
        {
            _farewellTime = Time.unscaledTime;
            GameEvents.RaiseBossFarewellRequested();
        }

        private void FinishBoss()
        {
            // 최종 보스를 잡으면(아직 비어 있으면 연출만 보여준 뒤) 판이 끝난다.
            if (_bossNumber >= _stage.FinalBossNumber)
            {
                _isRunning = false;
                GameEvents.RaiseStageCleared();
                return;
            }

            // 보스를 쓰러뜨리면 다음 웨이브부터 다시 할 수 있다.
            if (_stage.IsStoryMode && !_stage.IsEndless)
            {
                GameEvents.RaiseStoryCheckpointReached(_bossNumber);
            }

            // 웨이브가 보스 수보다 적으면 마지막 웨이브를 반복한다. 무한 모드는 웨이브 목록을 쓰지 않는다.
            if (!_stage.IsEndless)
            {
                _waveIndex = Mathf.Min(_waveIndex + 1, _stage.Waves.Count - 1);
            }

            BeginRest(_stage.WaveInterval);
        }

        private void OnBossDefeated(int bossNumber)
        {
            _isBossDefeated = true;
        }

        // 플레이어 가까이, 맵에서 더 넓은 쪽(맵 중심 쪽)으로 내려온다. 구석에 몰린 플레이어 바로 옆 벽에 붙어 나오지 않게 한다.
        // 그 자리가 구조물에 걸리면 좌우로 30도씩 돌려 가며 비어 있는 자리를 찾는다.
        // 단, 정해진 자리가 있는 보스는 그 후보 중에서 고른다.
        private Vector2 PickBossSpawnPoint()
        {
            Vector2 player = _spawner.Target != null ? (Vector2)_spawner.Target.position : Vector2.zero;
            for (int i = 0; _bossPrefab != null && i < _bossSpawnPointSets.Length; i++)
            {
                BossSpawnPoints set = _bossSpawnPointSets[i];
                if (set.BossPrefab == _bossPrefab.gameObject && set.TryPick(player, _stage.FixedSpawnMinDistance, out Vector2 fixedPoint))
                {
                    return fixedPoint;
                }
            }

            ArenaBounds arena = ArenaBounds.Current;
            if (arena == null)
            {
                return player + Vector2.up * _stage.BossSpawnDistance;
            }

            Bounds bounds = arena.Bounds;
            Vector2 toCenter = (Vector2)bounds.center - player;
            Vector2 direction = toCenter.sqrMagnitude > 0.01f ? toCenter.normalized : Vector2.up;
            float clearance = _stage.BossSpawnClearance;
            Vector2 min = (Vector2)bounds.min + new Vector2(clearance, clearance);
            Vector2 max = (Vector2)bounds.max - new Vector2(clearance, clearance);

            Vector2 fallback = Vector2.zero;
            for (int i = 0; i < 12; i++)
            {
                float angle = (i % 2 == 0 ? 1f : -1f) * ((i + 1) / 2) * 30f;
                Vector2 rotated = Quaternion.Euler(0f, 0f, angle) * direction;
                Vector2 point = player + rotated * _stage.BossSpawnDistance;
                point = new Vector2(Mathf.Clamp(point.x, min.x, max.x), Mathf.Clamp(point.y, min.y, max.y));
                if (i == 0)
                {
                    fallback = point;
                }

                if (!Physics2D.OverlapCircle(point, clearance, _obstacleMask))
                {
                    return point;
                }
            }

            return fallback;
        }

        private void SetMarker(bool isVisible, float progress)
        {
            if (_bossSpawnMarker == null)
            {
                return;
            }

            _bossSpawnMarker.gameObject.SetActive(isVisible);
            if (isVisible)
            {
                _bossSpawnMarker.position = _bossSpawnPoint;
                float size = Mathf.Clamp01(progress);
                _bossSpawnMarker.localScale = new Vector3(size, size, 1f);
            }
        }

        // 보스는 한 판에 한 번씩만 나오므로 종류마다 하나만 미리 만든다. 바퀴가 달라도 같은 프리팹이다.
        private void PrepareBosses()
        {
            int kinds = _stage.BossKindCount;
            for (int n = 1; n <= kinds; n++)
            {
                PrepareBoss(n);
            }

            if (!_stage.IsEndless)
            {
                PrepareBoss(_stage.FinalBossNumber);
            }

            if (_isTrueEnding)
            {
                _spawner.Prepare(_trueEnding.King, 1);
            }
        }

        private void PrepareBoss(int bossNumber)
        {
            Enemy boss = _stage.GetBoss(bossNumber, out _);
            if (boss != null)
            {
                _spawner.Prepare(boss, 1);
            }
        }

        // 개발자 패널용. 쉬는 시간 없이 해당 웨이브를 바로 시작한다. 이미 나온 적은 그대로 둔다.
        public void SkipToWave(int waveIndex)
        {
            if (waveIndex < 0 || waveIndex >= WaveCount)
            {
                return;
            }

            _waveIndex = waveIndex;
            StartWave();
        }

        private void BeginRest(float duration)
        {
            _restDuration = duration;
            EnterPhase(Phase.Rest);
        }

        private void EnterPhase(Phase phase)
        {
            _phase = phase;
            _phaseTime = 0f;
        }

        private void StartWave()
        {
            if (_generator != null)
            {
                // 무한 모드는 적을 미리 펼치지 않고, 웨이브 도중 낼 차례가 될 때마다 만든다.
                _events.Clear();
                _eventOrder = 0;
                _generator.BeginWave(_waveNumber + 1, _waveNumber * _stage.SpawnsPerWave);
                // 이미 나와 있는 적도 다음 걸음부터 같이 빨라진다.
                EnemyTempo.MoveSpeedMultiplier = _stage.EndlessWaves.GetEnemySpeedMultiplier(_waveNumber + 1);
            }
            else
            {
                BuildWave(_stage.Waves[_waveIndex]);
            }

            _waveNumber++;
            GameEvents.RaiseWaveStarted(_waveNumber);
            _nextEvent = 0;
            EnterPhase(Phase.Spawning);
        }

        // 항목의 구역마다 기준점을 하나씩 뽑고, 그 구역에서 군집 전체가 기준점 주변에 같은 구역 간격을 두고 한 마리씩 나온다.
        // 구역끼리는 같은 시작 시간에 동시에 나오고, 군집 후보가 여럿이면 항목마다 하나를 뽑아 모든 구역에 같이 쓴다.
        // 무작위는 등장 위치와 조합에만 둔다. 등장한 뒤의 움직임은 예측 가능해야 한다.
        private void BuildWave(WaveData wave)
        {
            _events.Clear();
            _eventOrder = 0;

            IReadOnlyList<WaveGroupEntry> entries = wave.Groups;
            for (int e = 0; e < entries.Count; e++)
            {
                SpawnGroupData group = entries[e].PickGroup();
                if (group == null || group.TotalCount == 0)
                {
                    continue;
                }

                IReadOnlyList<int> areaIds = entries[e].AreaIds;
                for (int a = 0; a < areaIds.Count; a++)
                {
                    if (!TryPickArea(areaIds[a], out SpawnArea area))
                    {
                        continue;
                    }

                    Vector2 anchor = area.GetRandomPoint();
                    int sequence = 0;
                    IReadOnlyList<SpawnGroupUnit> units = group.Units;
                    for (int u = 0; u < units.Count; u++)
                    {
                        sequence = AddUnitEvents(area, anchor, units[u].Enemy, units[u].Count, entries[e].StartTime, sequence);
                    }
                }
            }

            _events.Sort(ByTime);
        }

        // 생성기가 이번에 낸 군집을 스토리 웨이브와 같은 방식(기준점 주변, 같은 구역 간격)으로 펼친다.
        // 이미 펼쳐 둔 앞 군집의 남은 스폰과 시간이 섞이므로, 아직 안 나온 부분만 다시 정렬한다.
        private void GenerateEndless()
        {
            _orders.Clear();
            Vector2 player = _spawner.Target != null ? (Vector2)_spawner.Target.position : Vector2.zero;
            _generator.Tick(_phaseTime, player, _orders);
            if (_orders.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _orders.Count; i++)
            {
                EndlessGroupOrder order = _orders[i];
                if (TryAmbush(order, player))
                {
                    continue;
                }

                AddUnitEvents(order.Area, order.Area.GetRandomPoint(), order.Prefab, order.Count, order.Time, 0);
            }

            _events.Sort(_nextEvent, _events.Count - _nextEvent, ByTimeComparer);
        }

        // 저주 "기습": 확률로 군집을 구역 대신 플레이어 근처 빈자리에 낸다. 나오기 전에 자리를 표시하고, 표시 시간만큼 늦게 나온다.
        // 여러 마리면 한데 몰아 내지 않고 한 마리씩 서로 떨어진 자리에 낸다(한곳에 뭉치면 폭탄 한 발로 끝난다).
        // 맵 안이고 구조물과 겹치지 않는 자리를 몇 번 찾아보고, 못 찾은 적은 원래 구역에서 낸다.
        private bool TryAmbush(EndlessGroupOrder order, Vector2 player)
        {
            if (RunCurses.AmbushChance <= 0f || UnityEngine.Random.value >= RunCurses.AmbushChance)
            {
                return false;
            }

            // 표시 시간은 실제 시간인데, 스폰 시계는 아티팩트·저주로 빨리 돌 수 있어 그 배율만큼 늘려 맞춘다.
            float clock = RunModifiers.SpawnIntervalMultiplier > 0f ? RunModifiers.SpawnIntervalMultiplier : 1f;
            float startTime = Mathf.Max(order.Time, _phaseTime) + RunCurses.AmbushWarning / clock;
            _ambushSpots.Clear();
            for (int k = 0; k < order.Count; k++)
            {
                if (TryPickAmbushSpot(player, out Vector2 spot))
                {
                    _ambushSpots.Add(spot);
                    GameEvents.RaiseSpawnWarned(spot, AmbushClearRadius, RunCurses.AmbushWarning);
                    _events.Add(new SpawnEvent(startTime, _eventOrder++, order.Prefab, spot, false, spot, true));
                }
                else
                {
                    AddUnitEvents(order.Area, order.Area.GetRandomPoint(), order.Prefab, 1, order.Time, 0);
                }
            }

            return true;
        }

        private bool TryPickAmbushSpot(Vector2 player, out Vector2 spot)
        {
            ArenaBounds arena = ArenaBounds.Current;
            float sqrSeparation = AmbushSeparation * AmbushSeparation;
            for (int attempt = 0; attempt < AmbushAttempts; attempt++)
            {
                Vector2 direction = UnityEngine.Random.insideUnitCircle.normalized;
                float distance = UnityEngine.Random.Range(RunCurses.AmbushMinRadius, RunCurses.AmbushMaxRadius);
                spot = player + direction * distance;
                if ((arena != null && !arena.Contains(spot)) || Physics2D.OverlapCircle(spot, AmbushClearRadius, _obstacleMask) != null)
                {
                    continue;
                }

                bool isApart = true;
                for (int i = 0; i < _ambushSpots.Count; i++)
                {
                    if ((_ambushSpots[i] - spot).sqrMagnitude < sqrSeparation)
                    {
                        isApart = false;
                        break;
                    }
                }

                if (isApart)
                {
                    return true;
                }
            }

            spot = player;
            return false;
        }

        // 한 구역에서 같은 적 count마리를 기준점 주변에 같은 구역 간격을 두고 한 마리씩 낸다. 다음 순번을 돌려준다.
        // area가 없으면(기습) 입구 없이 그 자리에서 바로 쫓는다.
        private int AddUnitEvents(SpawnArea area, Vector2 anchor, Enemy prefab, int count, float startTime, int sequence)
        {
            float interval = _stage.SameAreaSpawnInterval;
            float radius = _stage.ClusterRadius;
            for (int k = 0; k < count; k++)
            {
                if (prefab != null)
                {
                    float time = startTime + sequence * interval;
                    Vector2 position = anchor + UnityEngine.Random.insideUnitCircle * radius;
                    bool hasEntry = area != null && area.HasEntrance;
                    Vector2 entryPoint = hasEntry ? area.GetRandomEntrancePoint() : position;
                    _events.Add(new SpawnEvent(time, _eventOrder++, prefab, position, hasEntry, entryPoint));
                }

                sequence++;
            }

            return sequence;
        }

        // 같은 번호를 단 구역이 여럿이면 그중 하나를 고른다.
        private bool TryPickArea(int areaId, out SpawnArea area)
        {
            if (_areasById.TryGetValue(areaId, out List<SpawnArea> areas))
            {
                area = areas[UnityEngine.Random.Range(0, areas.Count)];
                return true;
            }

            area = null;
            return false;
        }

        private void IndexAreas(SpawnArea[] areas)
        {
            for (int i = 0; i < areas.Length; i++)
            {
                if (!_areasById.TryGetValue(areas[i].Id, out List<SpawnArea> list))
                {
                    list = new List<SpawnArea>();
                    _areasById.Add(areas[i].Id, list);
                }

                list.Add(areas[i]);
            }
        }

        // 데이터 실수는 플레이 도중이 아니라 시작할 때 한 번에 알린다.
        // 스폰이 하나도 없는 웨이브는 곧바로 끝나 보스 연출만 반복되므로 시작을 막는다.
        private bool ValidateWaves()
        {
            bool canRun = true;
            IReadOnlyList<WaveData> waves = _stage.Waves;
            for (int w = 0; w < waves.Count; w++)
            {
                WaveData wave = waves[w];
                if (wave == null)
                {
                    Debug.LogError($"StageRunner: 웨이브 목록 {w + 1}번 칸이 비어 있습니다.", _stage);
                    canRun = false;
                    continue;
                }

                int total = wave.TotalSpawnCount;
                if (total == 0)
                {
                    Debug.LogError($"StageRunner: 웨이브 '{wave.name}'에 스폰이 하나도 없습니다.", wave);
                    canRun = false;
                }
                else if (total != _stage.SpawnsPerWave)
                {
                    Debug.LogWarning($"StageRunner: 웨이브 '{wave.name}'의 스폰 합계가 {total}마리입니다. 기준은 {_stage.SpawnsPerWave}마리입니다.", wave);
                }

                IReadOnlyList<WaveGroupEntry> entries = wave.Groups;
                for (int e = 0; e < entries.Count; e++)
                {
                    ValidateEntry(entries[e], wave, e);
                }
            }

            return canRun;
        }

        private void ValidateEntry(WaveGroupEntry entry, WaveData wave, int entryIndex)
        {
            if (entry.PickGroup() == null)
            {
                Debug.LogError($"StageRunner: 웨이브 '{wave.name}'의 {entryIndex + 1}번 항목에 군집이 없어 건너뜁니다.", wave);
                return;
            }

            if (entry.HasCountMismatch())
            {
                Debug.LogWarning($"StageRunner: 웨이브 '{wave.name}'의 {entryIndex + 1}번 항목 군집 후보끼리 마리 수가 달라, 뽑히는 군집에 따라 웨이브 합계가 달라집니다.", wave);
            }

            IReadOnlyList<int> areaIds = entry.AreaIds;
            if (areaIds.Count == 0)
            {
                Debug.LogError($"StageRunner: 웨이브 '{wave.name}'의 {entryIndex + 1}번 항목에 구역 번호가 없어 건너뜁니다.", wave);
            }

            for (int a = 0; a < areaIds.Count; a++)
            {
                if (!_areasById.ContainsKey(areaIds[a]))
                {
                    Debug.LogError($"StageRunner: 웨이브 '{wave.name}'의 {entryIndex + 1}번 항목 Area {areaIds[a]}가 씬에 없어 그 구역만 건너뜁니다.", wave);
                }
            }

            IReadOnlyList<SpawnGroupData> candidates = entry.Candidates;
            for (int c = 0; c < candidates.Count; c++)
            {
                SpawnGroupData group = candidates[c];
                if (group == null)
                {
                    continue;
                }

                IReadOnlyList<SpawnGroupUnit> units = group.Units;
                for (int u = 0; u < units.Count; u++)
                {
                    if (units[u].Enemy == null)
                    {
                        Debug.LogError($"StageRunner: 군집 '{group.DisplayName}'의 {u + 1}번 줄에 적 프리팹이 없어 그 줄을 건너뜁니다.", group);
                    }
                }
            }
        }

        // 무한 모드 데이터 실수도 시작할 때 한 번에 알린다. 처음부터 낼 적이 하나도 없으면 시작을 막는다.
        private bool ValidateEndless()
        {
            EndlessWaveData data = _stage.EndlessWaves;
            int total = data.TotalSpawnCount;
            if (total == 0)
            {
                Debug.LogError($"StageRunner: 무한 모드 데이터 '{data.name}'의 구간 마리 수가 모두 0입니다.", data);
                return false;
            }

            if (total != _stage.SpawnsPerWave)
            {
                Debug.LogWarning($"StageRunner: 무한 모드 데이터 '{data.name}'의 구간 합계가 {total}마리입니다. 기준은 {_stage.SpawnsPerWave}마리입니다.", data);
            }

            IReadOnlyList<EndlessEnemyEntry> enemies = data.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null && enemies[i].GetWeight(0, _stage.SpawnsPerWave, 0) > 0f)
                {
                    return true;
                }
            }

            Debug.LogError($"StageRunner: 무한 모드 데이터 '{data.name}'에 처음부터 나올 수 있는 적(등장 시작 0, 가중치 0 초과)이 없습니다.", data);
            return false;
        }

        private List<Enemy> CollectEnemyPrefabs()
        {
            var prefabs = new List<Enemy>();
            if (_stage.IsEndless)
            {
                IReadOnlyList<EndlessEnemyEntry> enemies = _stage.EndlessWaves.Enemies;
                for (int i = 0; i < enemies.Count; i++)
                {
                    Enemy enemy = enemies[i] != null ? enemies[i].Enemy : null;
                    if (enemy != null && !prefabs.Contains(enemy))
                    {
                        prefabs.Add(enemy);
                    }
                }

                return prefabs;
            }

            IReadOnlyList<WaveData> waves = _stage.Waves;
            for (int w = 0; w < waves.Count; w++)
            {
                IReadOnlyList<WaveGroupEntry> entries = waves[w].Groups;
                for (int e = 0; e < entries.Count; e++)
                {
                    // 어떤 후보가 뽑힐지 모르므로 후보 전부의 적을 미리 풀에 준비한다.
                    IReadOnlyList<SpawnGroupData> candidates = entries[e].Candidates;
                    for (int c = 0; c < candidates.Count; c++)
                    {
                        if (candidates[c] == null)
                        {
                            continue;
                        }

                        IReadOnlyList<SpawnGroupUnit> units = candidates[c].Units;
                        for (int u = 0; u < units.Count; u++)
                        {
                            Enemy enemy = units[u].Enemy;
                            if (enemy != null && !prefabs.Contains(enemy))
                            {
                                prefabs.Add(enemy);
                            }
                        }
                    }
                }
            }

            return prefabs;
        }

        private void OnGameStateChanged(GameState previous, GameState current)
        {
            _isRunning = current == GameState.Playing;
        }
    }
}
