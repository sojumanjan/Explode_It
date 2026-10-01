using System;
using System.Collections.Generic;
using ExplodeIt.Enemies;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 한 무리. 같은 곳에서 같이 출발해야 흐름장을 따라 뭉쳐 오고, 폭탄 한 방에 여럿을 잡는 상황이 생긴다.
    // 에셋으로 두어 같은 무리를 여러 웨이브·스테이지에서 재사용한다. 언제 나올지는 웨이브가 정한다.
    [CreateAssetMenu(fileName = "SpawnGroup", menuName = "Explode It/Stage/Spawn Group")]
    public class SpawnGroupData : ScriptableObject
    {
        // Enemy 타입 칸은 오브젝트 피커에 프리팹이 뜨지 않아(프리팹은 GameObject 에셋으로만 검색된다) GameObject로 받고,
        // 적이 아닌 프리팹은 OnValidate에서 걸러 낸다.
        [Tooltip("적 프리팹. Enemy 컴포넌트(돌격병, 궁수 등)가 붙은 프리팹만 넣을 수 있다")]
        [SerializeField] private GameObject _enemyPrefab;

        [Tooltip("수 (마리)")]
        [SerializeField, Min(1)] private int _count = 1;

        [Tooltip("간격 (초). 0이면 한 덩어리로 동시에 나오고, 0보다 크면 이 간격으로 한 마리씩 줄지어 나온다")]
        [SerializeField, Min(0f)] private float _interval;

        [Tooltip("뭉침 반경 (유닛). 구역 안에서 기준점 하나를 뽑고, 그 주변 이 반경 안에 모아서 생성한다")]
        [SerializeField, Min(0f)] private float _clusterRadius = 1f;

        [Tooltip("스폰 구역 번호 목록. 나올 때마다 이 중 하나를 무작위로 고른다. 비워 두면 모든 구역 중에서 고른다")]
        [SerializeField] private int[] _areaIds;

        // 스폰마다 GetComponent를 부르지 않도록 처음 읽을 때 한 번만 찾는다.
        [NonSerialized] private Enemy _enemy;

        public Enemy Enemy
        {
            get
            {
                if (_enemy == null && _enemyPrefab != null)
                {
                    _enemyPrefab.TryGetComponent(out _enemy);
                }

                return _enemy;
            }
        }
        public int Count => _count;
        public float Interval => _interval;
        public float ClusterRadius => _clusterRadius;
        public IReadOnlyList<int> AreaIds => _areaIds ?? Array.Empty<int>();

        private void OnValidate()
        {
            _enemy = null;
            if (_enemyPrefab != null && !_enemyPrefab.TryGetComponent(out Enemy _))
            {
                Debug.LogWarning($"{name}: '{_enemyPrefab.name}'에는 Enemy 컴포넌트가 없어 비웁니다.", this);
                _enemyPrefab = null;
            }
        }
    }
}
