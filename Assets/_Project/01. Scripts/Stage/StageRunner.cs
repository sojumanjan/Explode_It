using System;
using System.Collections.Generic;
using ExplodeIt.Core;
using ExplodeIt.Enemies;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 처치 수 기반 진행: 대기 → 웨이브(정해진 수 스폰) → 전부 처치 → 보스전 연출 → 웨이브 사이 간격 → 다음 웨이브.
    // 웨이브가 시작될 때 그 웨이브의 스폰을 "몇 초에, 어떤 적이, 어디서"로 미리 펼쳐 두고,
    // 시간이 되면 앞에서부터 꺼낸다. 전투 중에는 목록을 읽기만 하므로 할당이 없다.
    public class StageRunner : MonoBehaviour
    {
        public enum Phase
        {
            Rest,
            Spawning,
            BossIntro
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

        private readonly List<SpawnEvent> _events = new List<SpawnEvent>(128);
        private readonly Dictionary<int, List<SpawnArea>> _areasById = new Dictionary<int, List<SpawnArea>>();

        private Phase _phase;
        private float _phaseTime;
        private float _restDuration;
        private int _waveIndex;
        private int _waveNumber;
        private int _nextEvent;
        private bool _isRunning;

        public bool IsPaused { get; set; }
        public Phase CurrentPhase => _phase;
        public float PhaseTime => _phaseTime;
        // 지금까지 시작한 웨이브 수(1부터). 마지막 웨이브를 반복하면 데이터 번호보다 커진다.
        public int WaveNumber => _waveNumber;
        public int WaveCount => _stage != null ? _stage.Waves.Count : 0;

        private void OnEnable()
        {
            GameEvents.GameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.GameStateChanged -= OnGameStateChanged;
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
                        GameEvents.RaiseBossIntroStarted(_waveNumber);
                        EnterPhase(Phase.BossIntro);
                    }
                    break;

                case Phase.BossIntro:
                    // 보스가 생기면 연출과 다음 웨이브 사이에 보스전이 들어간다.
                    if (_phaseTime >= _stage.BossIntroDuration)
                    {
                        // 무한/엔딩이 정해지기 전까지는 마지막 웨이브를 반복한다.
                        _waveIndex = Mathf.Min(_waveIndex + 1, _stage.Waves.Count - 1);
                        BeginRest(_stage.WaveInterval);
                    }
                    break;
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

        // 구역마다 기준점을 하나씩 뽑고, 그 구역의 적은 기준점 주변에서 같은 구역 간격을 두고 한 마리씩 나온다.
        // 무작위는 등장 위치에만 둔다. 등장한 뒤의 움직임은 예측 가능해야 한다.
        private void BuildWave(WaveData wave)
        {
            _events.Clear();
            int order = 0;
            float interval = _stage.SameAreaSpawnInterval;
            float radius = _stage.ClusterRadius;

            IReadOnlyList<WaveGroupEntry> entries = wave.Groups;
            for (int e = 0; e < entries.Count; e++)
            {
                SpawnGroupData group = entries[e].Group;
                if (group == null)
                {
                    continue;
                }

                IReadOnlyList<SpawnAreaSlot> slots = group.Areas;
                for (int s = 0; s < slots.Count; s++)
                {
                    if (slots[s].TotalCount == 0 || !TryPickArea(SpawnGroupData.AreaIdOf(s), out SpawnArea area))
                    {
                        continue;
                    }

                    Vector2 anchor = area.GetRandomPoint();
                    int sequence = 0;
                    IReadOnlyList<SpawnGroupUnit> units = slots[s].Units;
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
                    ValidateGroup(entries[e].Group, wave, e);
                }
            }

            return canRun;
        }

        private void ValidateGroup(SpawnGroupData group, WaveData wave, int entryIndex)
        {
            if (group == null)
            {
                Debug.LogError($"StageRunner: 웨이브 '{wave.name}'의 {entryIndex + 1}번 군집 칸이 비어 있어 건너뜁니다.", wave);
                return;
            }

            IReadOnlyList<SpawnAreaSlot> slots = group.Areas;
            for (int s = 0; s < slots.Count; s++)
            {
                if (slots[s].TotalCount == 0)
                {
                    continue;
                }

                int areaId = SpawnGroupData.AreaIdOf(s);
                if (!_areasById.ContainsKey(areaId))
                {
                    Debug.LogError($"StageRunner: 군집 '{group.DisplayName}'의 Area {areaId}가 씬에 없어 그 구역 스폰을 건너뜁니다.", group);
                }

                IReadOnlyList<SpawnGroupUnit> units = slots[s].Units;
                for (int u = 0; u < units.Count; u++)
                {
                    if (units[u].Enemy == null)
                    {
                        Debug.LogError($"StageRunner: 군집 '{group.DisplayName}'의 Area {areaId} {u + 1}번 줄에 적 프리팹이 없어 그 줄을 건너뜁니다.", group);
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
                    if (entries[e].Group == null)
                    {
                        continue;
                    }

                    IReadOnlyList<SpawnAreaSlot> slots = entries[e].Group.Areas;
                    for (int s = 0; s < slots.Count; s++)
                    {
                        IReadOnlyList<SpawnGroupUnit> units = slots[s].Units;
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
