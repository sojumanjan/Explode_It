using System;
using ExplodeIt.Enemies;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 군집 안의 한 줄: 어떤 적을 몇 마리.
    [Serializable]
    public class SpawnGroupUnit
    {
        // Enemy 타입 칸은 오브젝트 피커에 프리팹이 뜨지 않아(프리팹은 GameObject 에셋으로만 검색된다) GameObject로 받는다.
        [Tooltip("적 프리팹. Enemy 컴포넌트(돌격병, 마법사 등)가 붙은 프리팹만 넣을 수 있다")]
        [SerializeField] private GameObject _enemyPrefab;

        [Tooltip("수 (마리)")]
        [SerializeField, Min(1)] private int _count = 1;

        // 스폰마다 GetComponent를 부르지 않도록 처음 읽을 때 한 번만 찾는다.
        [NonSerialized] private Enemy _enemy;
        [NonSerialized] private bool _hasEnemy;

        public int Count => _count;
        public GameObject EnemyPrefab => _enemyPrefab;

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
