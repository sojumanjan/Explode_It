using System;
using ExplodeIt.Enemies;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 한 무리의 구성. 어디서 언제 나올지는 웨이브가 정하므로 맵과 무관하게 어느 스테이지에서든 재사용한다.
    // 같은 곳에서 같이 출발해야 흐름장을 따라 뭉쳐 오고, 폭탄 한 방에 여럿을 잡는 상황이 생긴다.
    [CreateAssetMenu(fileName = "SpawnGroup", menuName = "Explode It/Stage/Spawn Group")]
    public class SpawnGroupData : ScriptableObject
    {
        // 한 지점에서 한 번에 나오는 무리의 상한. 더 큰 덩어리는 웨이브에서 "앞 군집 자리 이어 쓰기"로 붙인다.
        public const int MaxCount = 5;

        // Enemy 타입 칸은 오브젝트 피커에 프리팹이 뜨지 않아(프리팹은 GameObject 에셋으로만 검색된다) GameObject로 받고,
        // 적이 아닌 프리팹은 OnValidate에서 걸러 낸다.
        [Tooltip("적 프리팹 목록 (최대 5). 크기가 곧 마릿수이고, 위에서부터 순서대로 나온다. Enemy 컴포넌트가 붙은 프리팹만 넣을 수 있다")]
        [SerializeField] private GameObject[] _enemyPrefabs = new GameObject[1];

        [Tooltip("간격 (초). 0이면 한 덩어리로 동시에 나오고, 0보다 크면 목록 순서대로 이 간격을 두고 줄지어 나온다")]
        [SerializeField, Min(0f)] private float _interval;

        [Tooltip("뭉침 반경 (유닛). 구역 안 무작위 기준점 주변 이 반경 안에 모아서 생성한다")]
        [SerializeField, Min(0f)] private float _clusterRadius = 1f;

        // 스폰마다 GetComponent를 부르지 않도록 처음 읽을 때 한 번만 찾는다.
        [NonSerialized] private Enemy[] _enemies;

        public int Count => _enemyPrefabs?.Length ?? 0;
        public float Interval => _interval;
        public float ClusterRadius => _clusterRadius;

        // 비어 있는 칸이면 null을 돌려준다.
        public Enemy GetEnemy(int index)
        {
            if (_enemies == null || _enemies.Length != Count)
            {
                _enemies = new Enemy[Count];
                for (int i = 0; i < Count; i++)
                {
                    if (_enemyPrefabs[i] != null)
                    {
                        _enemyPrefabs[i].TryGetComponent(out _enemies[i]);
                    }
                }
            }

            return _enemies[index];
        }

        private void OnValidate()
        {
            _enemies = null;
            if (_enemyPrefabs == null)
            {
                return;
            }

            if (_enemyPrefabs.Length > MaxCount)
            {
                Array.Resize(ref _enemyPrefabs, MaxCount);
            }

            for (int i = 0; i < _enemyPrefabs.Length; i++)
            {
                if (_enemyPrefabs[i] != null && !_enemyPrefabs[i].TryGetComponent(out Enemy _))
                {
                    Debug.LogWarning($"{name}: '{_enemyPrefabs[i].name}'에는 Enemy 컴포넌트가 없어 비웁니다.", this);
                    _enemyPrefabs[i] = null;
                }
            }
        }
    }
}
