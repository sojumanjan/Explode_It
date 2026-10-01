using System;
using System.Collections.Generic;
using ExplodeIt.Enemies;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 한 무리. 같은 곳에서 같이 출발해야 흐름장을 따라 뭉쳐 오고, 폭탄 한 방에 여럿을 잡는 상황이 생긴다.
    [Serializable]
    public class SpawnGroup
    {
        // 첫 문자열 필드라 인스펙터 리스트 항목 이름으로 보인다.
        [Tooltip("메모. 인스펙터 리스트에 보이는 이름일 뿐 동작에는 영향이 없다")]
        [SerializeField] private string _memo;

        [Tooltip("시작 시간 (초). 이 군집이 속한 웨이브가 시작된 뒤 첫 적이 나오기까지")]
        [SerializeField, Min(0f)] private float _startTime;

        [Tooltip("적 프리팹")]
        [SerializeField] private Enemy _enemy;

        [Tooltip("수 (마리)")]
        [SerializeField, Min(1)] private int _count = 1;

        [Tooltip("간격 (초). 0이면 한 덩어리로 동시에 나오고, 0보다 크면 이 간격으로 한 마리씩 줄지어 나온다")]
        [SerializeField, Min(0f)] private float _interval;

        [Tooltip("뭉침 반경 (유닛). 구역 안에서 기준점 하나를 뽑고, 그 주변 이 반경 안에 모아서 생성한다")]
        [SerializeField, Min(0f)] private float _clusterRadius = 1f;

        [Tooltip("스폰 구역 번호 목록. 군집마다 이 중 하나를 무작위로 고른다. 비워 두면 모든 구역 중에서 고른다")]
        [SerializeField] private int[] _areaIds;

        public float StartTime => _startTime;
        public Enemy Enemy => _enemy;
        public int Count => _count;
        public float Interval => _interval;
        public float ClusterRadius => _clusterRadius;
        public IReadOnlyList<int> AreaIds => _areaIds ?? Array.Empty<int>();
    }
}
