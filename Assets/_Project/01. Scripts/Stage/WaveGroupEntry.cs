using System;
using System.Collections.Generic;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 웨이브 안에서 군집 하나를 언제, 어디서 내보낼지. 군집 에셋은 무엇을, 이 항목은 언제·어디서를 맡는다.
    // 구역 번호는 맵마다 다르므로 맵에 묶인 스테이지 데이터인 여기에 둔다.
    [Serializable]
    public class WaveGroupEntry
    {
        [Tooltip("간격 (초). 앞 군집이 시작된 뒤 이 군집이 시작되기까지. 첫 군집은 웨이브 시작부터 잰다. 0이면 앞 군집과 동시에 나온다")]
        [SerializeField, Min(0f)] private float _delay;

        [Tooltip("군집 에셋")]
        [SerializeField] private SpawnGroupData _group;

        // 5마리를 넘는 덩어리나, 같은 길로 따라오는 후속 부대를 만들 때 쓴다.
        [Tooltip("앞 군집 자리 이어 쓰기. 켜면 구역을 새로 뽑지 않고 바로 앞 군집과 같은 지점에서 나온다. 웨이브의 첫 군집이면 무시한다")]
        [SerializeField] private bool _useAnchorOfPrevious;

        [Tooltip("스폰 구역 번호 목록. 이 중 하나를 무작위로 고르고, 그 구역 안 무작위 지점에서 나온다. 비워 두면 모든 구역 중에서 고른다")]
        [SerializeField] private int[] _areaIds;

        public float Delay => _delay;
        public SpawnGroupData Group => _group;
        public bool UseAnchorOfPrevious => _useAnchorOfPrevious;
        public IReadOnlyList<int> AreaIds => _areaIds ?? Array.Empty<int>();
    }
}
