using System.Collections.Generic;
using ExplodeIt.Core;
using ExplodeIt.Enemies;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 스테이지 데이터를 시작할 때 "몇 초에 어느 항목의 몇 번째 적"이라는 스폰 목록으로 펼쳐 두고,
    // 시간이 되면 앞에서부터 꺼내 스폰한다. 전투 중에는 목록을 읽기만 하므로 할당이 없다.
    public class StageRunner : MonoBehaviour
    {
        private readonly struct SpawnEvent
        {
            public readonly float Time;
            public readonly int EntryIndex;
            public readonly int Slot;

            public SpawnEvent(float time, int entryIndex, int slot)
            {
                Time = time;
                EntryIndex = entryIndex;
                Slot = slot;
            }
        }

        [SerializeField] private StageData _stage;
        [SerializeField] private EnemySpawner _spawner;

        private readonly List<SpawnEvent> _events = new List<SpawnEvent>();
        // 모든 웨이브의 유효한 항목을 순서대로 펼친 목록. 스폰 이벤트는 인덱스로 항목을 가리킨다.
        private readonly List<WaveGroupEntry> _entries = new List<WaveGroupEntry>();
        // 같은 웨이브 안 바로 앞 유효 항목의 인덱스. 첫 항목이면 -1. "앞 군집 자리 이어 쓰기"에 쓴다.
        private readonly List<int> _previousEntries = new List<int>();
        private readonly Dictionary<int, List<SpawnArea>> _areasById = new Dictionary<int, List<SpawnArea>>();
        private SpawnArea[] _allAreas;
        private float[] _waveStartTimes;
        // 항목마다 기준점을 한 번만 뽑아, 줄지어 나오는 군집도 같은 자리에서 출발하게 한다.
        private Vector2[] _anchors;
        private bool[] _hasAnchor;

        private float _elapsed;
        private int _nextEvent;
        private bool _isRunning;
        private bool _isCleared;

        public bool IsPaused { get; set; }
        public float Elapsed => _elapsed;
        public int WaveCount => _waveStartTimes?.Length ?? 0;

        // 웨이브 경계는 플레이어에게 보이지 않으므로, 개발자 패널에서 확인하는 용도로만 쓴다.
        public int CurrentWave
        {
            get
            {
                int current = 0;
                for (int i = 0; i < WaveCount; i++)
                {
                    if (_elapsed >= _waveStartTimes[i])
                    {
                        current = i;
                    }
                }

                return current;
            }
        }

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
            _allAreas = FindObjectsByType<SpawnArea>();
            if (_allAreas.Length == 0)
            {
                Debug.LogError("StageRunner: 씬에 스폰 구역이 없습니다.", this);
                enabled = false;
                return;
            }

            IndexAreas();
            BuildTimeline();
            _spawner.Prepare(CollectEnemyPrefabs());
        }

        private void Update()
        {
            if (!_isRunning || IsPaused || _isCleared)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            while (_nextEvent < _events.Count && _events[_nextEvent].Time <= _elapsed)
            {
                Spawn(_events[_nextEvent]);
                _nextEvent++;
            }

            // 웨이브 사이는 시간으로만 이어지지만, 스테이지 끝은 남은 적까지 다 잡아야 한다.
            if (_nextEvent >= _events.Count && _spawner.ActiveCount == 0)
            {
                _isCleared = true;
                GameEvents.RaiseStageCleared();
            }
        }

        // 개발자 패널용. 앞 웨이브의 남은 스폰은 건너뛰고 해당 웨이브 시작 시점부터 진행한다.
        public void SkipToWave(int waveIndex)
        {
            if (waveIndex < 0 || waveIndex >= WaveCount)
            {
                return;
            }

            _elapsed = _waveStartTimes[waveIndex];
            _nextEvent = 0;
            while (_nextEvent < _events.Count && _events[_nextEvent].Time < _elapsed)
            {
                _nextEvent++;
            }
        }

        private void IndexAreas()
        {
            for (int i = 0; i < _allAreas.Length; i++)
            {
                SpawnArea area = _allAreas[i];
                if (!_areasById.TryGetValue(area.Id, out List<SpawnArea> list))
                {
                    list = new List<SpawnArea>();
                    _areasById.Add(area.Id, list);
                }

                list.Add(area);
            }
        }

        private void BuildTimeline()
        {
            IReadOnlyList<StageWave> waves = _stage.Waves;
            _waveStartTimes = new float[waves.Count];

            // 첫 웨이브는 스테이지 시작부터, 이후 웨이브는 앞 웨이브의 마지막 스폰부터 쉬는 시간을 잰다.
            float previousEnd = 0f;
            for (int w = 0; w < waves.Count; w++)
            {
                float waveStart = previousEnd + waves[w].RestBefore;
                float waveEnd = waveStart;
                _waveStartTimes[w] = waveStart;

                // 각 군집은 앞 군집이 시작된 시점부터 간격을 잰다. 간격이 0이면 앞 군집과 동시에 출발한다.
                float groupStart = waveStart;
                int previousEntry = -1;
                IReadOnlyList<WaveGroupEntry> entries = waves[w].Groups;
                for (int g = 0; g < entries.Count; g++)
                {
                    WaveGroupEntry entry = entries[g];
                    groupStart += entry.Delay;
                    if (!IsValid(entry, w, g))
                    {
                        continue;
                    }

                    int entryIndex = _entries.Count;
                    _entries.Add(entry);
                    _previousEntries.Add(previousEntry);
                    previousEntry = entryIndex;

                    SpawnGroupData group = entry.Group;
                    for (int k = 0; k < group.Count; k++)
                    {
                        if (group.GetEnemy(k) == null)
                        {
                            continue;
                        }

                        float time = groupStart + k * group.Interval;
                        _events.Add(new SpawnEvent(time, entryIndex, k));
                        waveEnd = Mathf.Max(waveEnd, time);
                    }
                }

                previousEnd = waveEnd;
            }

            // 시간이 같으면 데이터에 적은 순서를 지키도록 항목, 칸 순서로 한 번 더 정렬한다.
            _events.Sort((a, b) =>
            {
                if (a.Time != b.Time)
                {
                    return a.Time.CompareTo(b.Time);
                }

                return a.EntryIndex != b.EntryIndex ? a.EntryIndex.CompareTo(b.EntryIndex) : a.Slot.CompareTo(b.Slot);
            });

            _anchors = new Vector2[_entries.Count];
            _hasAnchor = new bool[_entries.Count];
        }

        // 데이터 실수는 플레이 도중이 아니라 시작할 때 한 번에 알린다.
        private bool IsValid(WaveGroupEntry entry, int waveIndex, int entryIndex)
        {
            string where = $"웨이브 {waveIndex + 1} 군집 {entryIndex + 1}";
            SpawnGroupData group = entry.Group;
            if (group == null)
            {
                Debug.LogError($"StageRunner: {where}에 군집 에셋이 없어 건너뜁니다.", _stage);
                return false;
            }

            for (int i = 0; i < group.Count; i++)
            {
                if (group.GetEnemy(i) == null)
                {
                    Debug.LogError($"StageRunner: 군집 '{group.name}'의 {i + 1}번 칸이 비어 있어 그 칸만 건너뜁니다.", group);
                }
            }

            IReadOnlyList<int> ids = entry.AreaIds;
            for (int i = 0; i < ids.Count; i++)
            {
                if (!_areasById.ContainsKey(ids[i]))
                {
                    Debug.LogError($"StageRunner: {where}의 구역 번호 {ids[i]}가 씬에 없습니다.", _stage);
                }
            }

            return true;
        }

        private List<Enemy> CollectEnemyPrefabs()
        {
            var prefabs = new List<Enemy>();
            for (int i = 0; i < _entries.Count; i++)
            {
                SpawnGroupData group = _entries[i].Group;
                for (int k = 0; k < group.Count; k++)
                {
                    Enemy enemy = group.GetEnemy(k);
                    if (enemy != null && !prefabs.Contains(enemy))
                    {
                        prefabs.Add(enemy);
                    }
                }
            }

            return prefabs;
        }

        // 무작위는 등장 위치에만 둔다. 등장한 뒤의 움직임은 예측 가능해야 한다.
        private void Spawn(SpawnEvent spawnEvent)
        {
            SpawnGroupData group = _entries[spawnEvent.EntryIndex].Group;
            Vector2 position = GetAnchor(spawnEvent.EntryIndex) + Random.insideUnitCircle * group.ClusterRadius;
            _spawner.Spawn(group.GetEnemy(spawnEvent.Slot), position);
        }

        // 기준점은 항목의 첫 스폰 때 정한다. 이어 쓰기 항목은 앞 항목의 기준점을 그대로 받는다.
        // 웨이브 건너뛰기로 앞 항목이 스폰되지 않았더라도 여기서 앞 항목의 기준점을 먼저 정해 넘겨준다.
        private Vector2 GetAnchor(int entryIndex)
        {
            if (_hasAnchor[entryIndex])
            {
                return _anchors[entryIndex];
            }

            int previous = _previousEntries[entryIndex];
            _anchors[entryIndex] = _entries[entryIndex].UseAnchorOfPrevious && previous >= 0
                ? GetAnchor(previous)
                : PickArea(_entries[entryIndex]).GetRandomPoint();
            _hasAnchor[entryIndex] = true;
            return _anchors[entryIndex];
        }

        private SpawnArea PickArea(WaveGroupEntry entry)
        {
            IReadOnlyList<int> ids = entry.AreaIds;
            if (ids.Count > 0 && _areasById.TryGetValue(ids[Random.Range(0, ids.Count)], out List<SpawnArea> areas))
            {
                return areas[Random.Range(0, areas.Count)];
            }

            // 번호가 비었거나 씬에 없는 번호면 모든 구역 중에서 고른다. 없는 번호는 시작할 때 에러로 알렸다.
            return _allAreas[Random.Range(0, _allAreas.Length)];
        }

        private void OnGameStateChanged(GameState previous, GameState current)
        {
            _isRunning = current == GameState.Playing;
        }
    }
}
