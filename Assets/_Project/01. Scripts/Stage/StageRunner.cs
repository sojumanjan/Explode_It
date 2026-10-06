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
    public class StageRunner : MonoBehaviour
    {
        public enum Phase
        {
            Rest,
            Spawning,
            BossIntro,
            // 보스가 내려앉은 뒤: 멈춤 → 카메라 복귀 → 보스 행동 시작.
            BossEntrance,
            Boss
        }

        private readonly struct SpawnEvent
        {
            public readonly float Time;
            // 같은 시간이면 데이터에 적은 순서를 지키기 위한 번호.
            public readonly int Order;
            public readonly Enemy Prefab;
            public readonly Vector2 Position;

            public SpawnEvent(float time, int order, Enemy prefab, Vector2 position)
            {
                Time = time;
                Order = order;
                Prefab = prefab;
                Position = position;
            }
        }

        private static readonly Comparison<SpawnEvent> ByTime = (a, b) =>
            a.Time != b.Time ? a.Time.CompareTo(b.Time) : a.Order.CompareTo(b.Order);

        [SerializeField] private StageData _stage;
        [SerializeField] private EnemySpawner _spawner;
        // 보스가 내려앉을 자리를 연출 동안 미리 보여주는 원. 비워 두면 표시 없이 나타난다.
        [SerializeField] private Transform _bossSpawnMarker;
        // 보스가 구조물 안에 내려앉지 않게 피할 레이어.
        [SerializeField] private LayerMask _obstacleMask;

        private readonly List<SpawnEvent> _events = new List<SpawnEvent>(128);
        private readonly Dictionary<int, List<SpawnArea>> _areasById = new Dictionary<int, List<SpawnArea>>();

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

        public bool IsPaused { get; set; }
        public Phase CurrentPhase => _phase;
        public float PhaseTime => _phaseTime;
        // 지금까지 시작한 웨이브 수(1부터). 마지막 웨이브를 반복하면 데이터 번호보다 커진다.
        public int WaveNumber => _waveNumber;
        public int WaveCount => _stage != null ? _stage.Waves.Count : 0;

        private void OnEnable()
        {
            GameEvents.GameStateChanged += OnGameStateChanged;
            GameEvents.BossDefeated += OnBossDefeated;
        }

        private void OnDisable()
        {
            GameEvents.GameStateChanged -= OnGameStateChanged;
            GameEvents.BossDefeated -= OnBossDefeated;
        }

        private void Start()
        {
            // 구역을 추가할 때마다 인스펙터에 연결하지 않도록 시작할 때 씬에서 모두 찾는다.
            SpawnArea[] areas = FindObjectsByType<SpawnArea>();
            if (areas.Length == 0 || _stage.Waves.Count == 0)
            {
                Debug.LogError("StageRunner: 스폰 구역이나 웨이브가 비어 있습니다.", this);
                enabled = false;
                return;
            }

            IndexAreas(areas);
            if (!ValidateWaves())
            {
                enabled = false;
                return;
            }

            _spawner.Prepare(CollectEnemyPrefabs());
            PrepareBosses();
            SetMarker(false, 0f);
            BeginRest(_stage.FirstWaveDelay);
        }

        private void Update()
        {
            if (!_isRunning || IsPaused)
            {
                return;
            }

            _phaseTime += Time.deltaTime;
            switch (_phase)
            {
                case Phase.Rest:
                    if (_phaseTime >= _restDuration)
                    {
                        StartWave();
                    }
                    break;

                case Phase.Spawning:
                    while (_nextEvent < _events.Count && _events[_nextEvent].Time <= _phaseTime)
                    {
                        SpawnEvent spawn = _events[_nextEvent];
                        _spawner.Spawn(spawn.Prefab, spawn.Position);
                        _nextEvent++;
                    }

                    // 웨이브 사이는 처치로만 넘어간다. 다 나오고 다 잡혀야 보스전이다.
                    if (_nextEvent >= _events.Count && _spawner.ActiveCount == 0)
                    {
                        GameEvents.RaiseWaveCleared(_waveNumber);
                        BeginBossIntro(_waveNumber);
                    }
                    break;

                case Phase.BossIntro:
                    // 내려앉을 자리를 원으로 키워 보여주다가, 다 차는 순간 보스가 나타난다.
                    float progress = _stage.BossIntroDuration > 0f ? _phaseTime / _stage.BossIntroDuration : 1f;
                    SetMarker(_bossPrefab != null, progress);
                    if (_phaseTime >= _stage.BossIntroDuration)
                    {
                        SetMarker(false, 0f);
                        if (_bossPrefab != null)
                        {
                            SpawnBoss();
                        }
                        else
                        {
                            // 아직 만들지 않은 보스 자리는 연출만 보여주고 넘어간다.
                            FinishBoss();
                        }
                    }
                    break;

                case Phase.BossEntrance:
                    if (_isBossDefeated)
                    {
                        ReleaseEntrance();
                        FinishBoss();
                        break;
                    }

                    if (!_isControlReleased && _phaseTime >= _stage.BossRevealHold)
                    {
                        ReleaseEntrance();
                    }

                    if (_phaseTime >= _stage.BossRevealHold + _stage.BossCameraBlend)
                    {
                        _boss?.StartFight();
                        EnterPhase(Phase.Boss);
                    }
                    break;

                case Phase.Boss:
                    if (_isBossDefeated)
                    {
                        FinishBoss();
                    }
                    break;
            }
        }

        public int BossKindCount => _stage != null ? _stage.BossKindCount : 0;
        public int BossLaps => _stage != null ? _stage.BossLaps : 0;

        // 개발자 패널용. 남은 적과 스폰을 지우고 해당 보스의 등장 연출부터 바로 시작한다.
        public void SkipToBoss(int bossNumber)
        {
            _spawner.KillAll();
            _events.Clear();
            _nextEvent = 0;
            BeginBossIntro(bossNumber);
        }

        private void BeginBossIntro(int bossNumber)
        {
            _bossNumber = bossNumber;
            _bossPrefab = _stage.GetBoss(bossNumber, out _bossTier);
            _isBossDefeated = false;
            if (_bossPrefab != null)
            {
                _bossSpawnPoint = PickBossSpawnPoint();
                // 등장 연출 동안 플레이어를 세우고, 카메라를 플레이어와 보스 사이로 옮겨 둘 다 보이게 한다.
                Vector2 player = _spawner.Target != null ? (Vector2)_spawner.Target.position : _bossSpawnPoint;
                GameEvents.RaisePlayerControlLockChanged(true);
                GameEvents.RaiseCameraFocusRequested((player + _bossSpawnPoint) * 0.5f, _stage.BossCameraBlend);
                _isControlReleased = false;
            }

            GameEvents.RaiseBossIntroStarted(bossNumber);
            EnterPhase(Phase.BossIntro);
        }

        private void SpawnBoss()
        {
            Enemy boss = _spawner.Spawn(_bossPrefab, _bossSpawnPoint);
            _boss = boss as IBoss;
            _boss?.BeginBoss(_bossNumber, _bossTier);

            FeedbackPlayer.Play(_stage.BossLandFeedback, _bossSpawnPoint);
            EnterPhase(Phase.BossEntrance);
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

        private void FinishBoss()
        {
            // 무한/엔딩이 정해지기 전까지는 마지막 웨이브를 반복한다.
            _waveIndex = Mathf.Min(_waveIndex + 1, _stage.Waves.Count - 1);
            BeginRest(_stage.WaveInterval);
        }

        private void OnBossDefeated(int bossNumber)
        {
            _isBossDefeated = true;
        }

        // 플레이어 가까이, 맵에서 더 넓은 쪽(맵 중심 쪽)으로 내려온다. 구석에 몰린 플레이어 바로 옆 벽에 붙어 나오지 않게 한다.
        // 그 자리가 구조물에 걸리면 좌우로 30도씩 돌려 가며 비어 있는 자리를 찾는다.
        private Vector2 PickBossSpawnPoint()
        {
            Vector2 player = _spawner.Target != null ? (Vector2)_spawner.Target.position : Vector2.zero;
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

            PrepareBoss(kinds * _stage.BossLaps + 1);
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
            BuildWave(_stage.Waves[_waveIndex]);
            _waveNumber++;
            _nextEvent = 0;
            EnterPhase(Phase.Spawning);
        }

        // 항목의 구역마다 기준점을 하나씩 뽑고, 그 구역에서 군집 전체가 기준점 주변에 같은 구역 간격을 두고 한 마리씩 나온다.
        // 구역끼리는 같은 시작 시간에 동시에 나오고, 군집 후보가 여럿이면 항목마다 하나를 뽑아 모든 구역에 같이 쓴다.
        // 무작위는 등장 위치와 조합에만 둔다. 등장한 뒤의 움직임은 예측 가능해야 한다.
        private void BuildWave(WaveData wave)
        {
            _events.Clear();
            int order = 0;
            float interval = _stage.SameAreaSpawnInterval;
            float radius = _stage.ClusterRadius;

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
                        Enemy prefab = units[u].Enemy;
                        for (int k = 0; k < units[u].Count; k++)
                        {
                            if (prefab != null)
                            {
                                float time = entries[e].StartTime + sequence * interval;
                                Vector2 position = anchor + UnityEngine.Random.insideUnitCircle * radius;
                                _events.Add(new SpawnEvent(time, order++, prefab, position));
                            }

                            sequence++;
                        }
                    }
                }
            }

            _events.Sort(ByTime);
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

        private List<Enemy> CollectEnemyPrefabs()
        {
            var prefabs = new List<Enemy>();
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
