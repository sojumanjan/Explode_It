using System;
using System.Collections.Generic;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 웨이브는 군집 묶음이다. 웨이브 사이는 시간으로만 이어지고 화면에 알리지 않아,
    // 플레이어는 웨이브 경계를 모른 채 "몰려왔다가 잠잠해지는" 흐름만 느낀다.
    [Serializable]
    public class StageWave
    {
        [Tooltip("메모. 인스펙터 리스트에 보이는 이름일 뿐 동작에는 영향이 없다")]
        [SerializeField] private string _memo;

        [Tooltip("쉬는 시간 (초). 앞 웨이브의 마지막 적이 나온 뒤 이 웨이브가 시작되기까지. 첫 웨이브는 스테이지 시작부터 잰다")]
        [SerializeField, Min(0f)] private float _restBefore = 8f;

        [Tooltip("군집 목록. 위에서부터 차례로, 각 항목의 간격만큼 앞 군집 시작 뒤에 나온다")]
        [SerializeField] private WaveGroupEntry[] _groups;

        public float RestBefore => _restBefore;
        public IReadOnlyList<WaveGroupEntry> Groups => _groups ?? Array.Empty<WaveGroupEntry>();
    }
}
