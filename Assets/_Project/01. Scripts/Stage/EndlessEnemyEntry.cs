using System;
using ExplodeIt.Enemies;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 무한 모드 적 풀의 한 줄. 처치 수가 등장 시작 수를 넘으면 뽑히기 시작하고, 웨이브가 지날수록 자주 뽑힌다.
    [Serializable]
    public class EndlessEnemyEntry
    {
        // Enemy 타입 칸은 오브젝트 피커에 프리팹이 뜨지 않아 GameObject로 받는다(군집 데이터와 같은 이유).
        [Tooltip("적 프리팹. Enemy 컴포넌트가 붙은 프리팹만 넣을 수 있다")]
        [SerializeField] private GameObject _enemyPrefab;

        [Tooltip("등장 시작 처치 수 (마리). 이번 판 처치 수가 이만큼이 된 다음 웨이브부터 나온다. 100이면 2웨이브부터")]
        [SerializeField, Min(0)] private int _unlockKills;

        [Tooltip("처음 등장할 때 가중치. 다른 적 가중치와의 비율로 뽑힌다")]
        [SerializeField, Min(0f)] private float _baseWeight = 1f;

        [Tooltip("등장한 뒤 웨이브마다 더해지는 가중치. 강한 적일수록 크게 줘서 점점 자주 나오게 한다. 무한 모드 데이터의 가중치 상승 시작 처치 수 전에는 더하지 않는다")]
        [SerializeField, Min(0f)] private float _weightPerWave;

        [Tooltip("가중치 최대. 한 적만 계속 커져 다른 적이 안 나오는 것을 막는다")]
        [SerializeField, Min(0f)] private float _maxWeight = 5f;

        [Tooltip("한 구역에서 한 번에 나오는 최대 수 (마리). 저격수처럼 여럿이 겹치면 피할 수 없는 적은 작게 둔다")]
        [SerializeField, Min(1)] private int _maxGroupSize = 5;

        // 스폰마다 GetComponent를 부르지 않도록 처음 읽을 때 한 번만 찾는다.
        [NonSerialized] private Enemy _enemy;
        [NonSerialized] private bool _hasEnemy;

        public int UnlockKills => _unlockKills;
        public int MaxGroupSize => _maxGroupSize;

        public Enemy Enemy
        {
            get
            {
                if (!_hasEnemy)
                {
                    _enemy = null;
                    if (_enemyPrefab != null)
                    {
                        _enemyPrefab.TryGetComponent(out _enemy);
                    }

                    _hasEnemy = true;
                }

                return _enemy;
            }
        }

        // 아직 등장 전이면 0. growthStartKills가 된 웨이브부터 웨이브마다 오른다(보스를 다 잡은 직후 웨이브부터 바로 어려워지게).
        // 그보다 늦게 처음 나오는 적은 등장한 웨이브에는 기본 가중치로 나오고 다음 웨이브부터 오른다.
        public float GetWeight(int kills, int spawnsPerWave, int growthStartKills)
        {
            if (Enemy == null || kills < _unlockKills)
            {
                return 0f;
            }

            int waves = 0;
            if (spawnsPerWave > 0 && kills >= growthStartKills)
            {
                waves = _unlockKills >= growthStartKills
                    ? (kills - _unlockKills) / spawnsPerWave
                    : (kills - growthStartKills) / spawnsPerWave + 1;
            }

            return Mathf.Min(_baseWeight + _weightPerWave * waves, _maxWeight);
        }

        // 인스펙터에서 바뀌면 다시 찾고, 적이 아닌 프리팹은 비운다. 비웠으면 true.
        public bool Validate()
        {
            _hasEnemy = false;
            if (_enemyPrefab != null && !_enemyPrefab.TryGetComponent(out Enemy _))
            {
                _enemyPrefab = null;
                return true;
            }

            return false;
        }
    }
}
