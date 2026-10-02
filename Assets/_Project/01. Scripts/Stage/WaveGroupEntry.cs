using System;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 웨이브 안에서 군집 하나를 언제, 어느 구역에서 내보낼지. 무엇이 몇 마리 나올지는 군집이 정한다.
    // 같은 군집을 여러 구역에서 내려면 항목을 구역 수만큼 추가한다.
    [Serializable]
    public class WaveGroupEntry
    {
        // 첫 문자열 필드라 인스펙터 리스트 항목 이름으로 보인다. 시간·구역·군집에 맞춰 자동으로 채운다.
        [Tooltip("항목 이름. 시작 시간, 구역, 군집에 맞춰 자동으로 채워지므로 고치지 않아도 된다")]
        [SerializeField] private string _label;

        [Tooltip("시작 시간 (초). 이 웨이브가 시작된 뒤 이 군집이 나오기 시작하기까지")]
        [SerializeField, Min(0f)] private float _startTime;

        [Tooltip("구역 번호. 씬 뷰에서 각 구역 위에 보이는 Area 숫자와 같다. 같은 번호의 구역이 여럿이면 그중 하나에서 나온다")]
        [SerializeField, Min(0)] private int _areaId = 1;

        [Tooltip("군집 에셋")]
        [SerializeField] private SpawnGroupData _group;

        public float StartTime => _startTime;
        public int AreaId => _areaId;
        public SpawnGroupData Group => _group;

        public void RefreshLabel()
        {
            string groupName = _group != null ? _group.DisplayName : "(비어 있음)";
            _label = $"{_startTime}s · Area {_areaId} · {groupName}";
        }
    }
}
