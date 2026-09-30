using System.Collections.Generic;
using ExplodeIt.Core;
using ExplodeIt.Enemies;
using UnityEngine;

namespace ExplodeIt.Stage
{
    public class WaveRunner : MonoBehaviour
    {
        [SerializeField] private WaveData _wave;
        [SerializeField] private EnemySpawner _spawner;
        [SerializeField] private SpawnArea[] _areas;

        private float _timer;
        private bool _isRunning;

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
            if (_wave.Enemies.Count == 0 || _areas.Length == 0)
            {
                Debug.LogError("WaveRunner: 적 목록이나 스폰 구역이 비어 있습니다.", this);
                enabled = false;
                return;
            }

            _spawner.Prepare(_wave.Enemies);
        }

        private void Update()
        {
            if (!_isRunning)
            {
                return;
            }

            // 시작 직후 한 박자 쉬고 첫 적이 나오도록 간격이 찬 뒤에 스폰한다.
            _timer += Time.deltaTime;
            if (_timer < _wave.SpawnInterval)
            {
                return;
            }

            _timer -= _wave.SpawnInterval;
            SpawnRandom();
        }

        // 무작위는 종류와 등장 위치에만 둔다. 등장한 뒤의 움직임은 예측 가능해야 한다.
        private void SpawnRandom()
        {
            IReadOnlyList<Enemy> enemies = _wave.Enemies;
            Enemy prefab = enemies[Random.Range(0, enemies.Count)];
            SpawnArea area = _areas[Random.Range(0, _areas.Length)];
            _spawner.Spawn(prefab, area.GetRandomPoint());
        }

        private void OnGameStateChanged(GameState previous, GameState current)
        {
            _isRunning = current == GameState.Playing;
        }
    }
}
