using System;
using System.Collections.Generic;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 군집 안에서 구역 하나에 나올 적 목록. 쓰는 구역만 칸을 추가하고 번호를 직접 적는다.
    [Serializable]
    public class SpawnAreaSlot
    {
        // 첫 문자열 필드라 인스펙터 리스트 항목 이름으로 보인다. 구역 번호에 맞춰 자동으로 채운다.
        [Tooltip("항목 이름. 구역 번호에 맞춰 자동으로 채워지므로 고치지 않아도 된다")]
        [SerializeField] private string _label;

        [Tooltip("구역 번호. 씬 뷰에서 각 구역 위에 보이는 Area 숫자와 같다")]
        [SerializeField, Min(0)] private int _areaId = 1;

        [Tooltip("이 구역에서 나올 적 목록. 위 줄부터 순서대로, 같은 구역 스폰 간격을 두고 한 마리씩 나온다")]
        [SerializeField] private SpawnGroupUnit[] _units;

        public int AreaId => _areaId;
        public IReadOnlyList<SpawnGroupUnit> Units => _units ?? Array.Empty<SpawnGroupUnit>();

        public int TotalCount
        {
            get
            {
                int total = 0;
                IReadOnlyList<SpawnGroupUnit> units = Units;
                for (int i = 0; i < units.Count; i++)
                {
                    total += units[i].Count;
                }

                return total;
            }
        }

        public void RefreshLabel()
        {
            _label = $"Area {_areaId}";
        }
    }
}
