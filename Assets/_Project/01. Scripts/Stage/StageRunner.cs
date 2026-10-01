using System.Collections.Generic;
using ExplodeIt.Core;
using ExplodeIt.Enemies;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 스테이지 데이터를 시작할 때 "몇 초에 어느 군집의 적 하나"라는 스폰 목록으로 펼쳐 두고,
    // 시간이 되면 앞에서부터 꺼내 스폰한다. 전투 중에는 목록을 읽기만 하므로 할당이 없다.
    public class StageRunner : MonoBehaviour
    {
        private readonly struct SpawnEvent
        {
            public readonly float Time;
            public readonly int GroupIndex;

            public SpawnEvent(float time, int groupIndex)
            {
                Time = time;
                GroupIndex = groupIndex;
            }
        }

        [SerializeField] private StageData _stage;
        [SerializeField] private EnemySpawner _spawner;

        private readonly List<SpawnEvent> _events = new List<SpawnEvent>();
        // 펼친 군집 목록. 스폰 이벤트는 인덱스로 군집을 가리킨다.
        private readonly List<SpawnGroup> _groups = new List<SpawnGroup>();
        private readonly Dictionary<int, List<SpawnArea>> _areasById = new Dictionary<int, List<SpawnArea>>();
        private SpawnArea[] _allAreas;
        private float[] _waveStartTimes;
        // 군집마다 기준점을 한 번만 뽑아, 줄지어 나오는 군집도 같은 자리에서 출발하게 한다.
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
                Spawn(_events[_nextEvent].GroupIndex);
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

                IReadOnlyList<SpawnGroup> groups = waves[w].Groups;
                for (int g = 0; g < groups.Count; g++)
                {
                    SpawnGroup group = groups[g];
                    if (!IsValid(group, w, g))
                    {
                        continue;
                    }

                    int groupIndex = _groups.Count;
                    _groups.Add(group);
                    for (int k = 0; k < group.Count; k++)
                    {
                        float time = waveStart + group.StartTime + k * group.Interval;
                        _events.Add(new SpawnEvent(time, groupIndex));
                        waveEnd = Mathf.Max(waveEnd, time);
                    }
                }

                previousEnd = waveEnd;
            }

            // 시간이 같으면 데이터에 적은 순서를 지키도록 군집 인덱스로 한 번 더 정렬한다.
            _events.Sort((a, b) => a.Time != b.Time ? a.Time.CompareTo(b.Time) : a.GroupIndex.CompareTo(b.GroupIndex));

            _anchors = new Vector2[_groups.Count];
            _hasAnchor = new bool[_groups.Count];
        }

        // 데이터 실수는 플레이 도중이 아니라 시작할 때 한 번에 알린다.
        private bool IsValid(SpawnGroup group, int waveIndex, int groupIndex)
        {
            if (group.Enemy == null)
            {
                Debug.LogError($"StageRunner: 웨이브 {waveIndex + 1} 군집 {groupIndex + 1}에 적 프리팹이 없어 건너뜁니다.", _stage);
                return false;
            }

            IReadOnlyList<int> ids = group.AreaIds;
            for (int i = 0; i < ids.Count; i++)
            {
                if (!_areasById.ContainsKey(ids[i]))
                {
                    Debug.LogError($"StageRunner: 웨이브 {waveIndex + 1} 군집 {groupIndex + 1}의 구역 번호 {ids[i]}가 씬에 없습니다.", _stage);
                }
            }

            return true;
        }

        private List<Enemy> CollectEnemyPrefabs()
        {
            var prefabs = new List<Enemy>();
            for (int i = 0; i < _groups.Count; i++)
            {
                if (!prefabs.Contains(_groups[i].Enemy))
                {
                    prefabs.Add(_groups[i].Enemy);
                }
            }

            return prefabs;
        }

        // 무작위는 등장 위치에만 둔다. 등장한 뒤의 움직임은 예측 가능해야 한다.
        private void Spawn(int groupIndex)
        {
            SpawnGroup group = _groups[groupIndex];
            if (!_hasAnchor[groupIndex])
            {
                _anchors[groupIndex] = PickArea(group).GetRandomPoint();
                _hasAnchor[groupIndex] = true;
            }

            Vector2 position = _anchors[groupIndex] + Random.insideUnitCircle * group.ClusterRadius;
            _spawner.Spawn(group.Enemy, position);
        }

        private SpawnArea PickArea(SpawnGroup group)
        {
            IReadOnlyList<int> ids = group.AreaIds;
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
