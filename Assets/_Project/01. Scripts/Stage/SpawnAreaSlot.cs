using System;
using System.Collections.Generic;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 군집 안에서 구역 하나에 나올 적 목록. 구역 번호는 군집의 칸 순서(1번 칸 = Area 1)로 정해진다.
    [Serializable]
    public class SpawnAreaSlot
    {
        // 첫 문자열 필드라 인스펙터 리스트 항목 이름으로 보인다. 칸 순서에 맞춰 자동으로 채운다.
        [Tooltip("구역 이름. 칸 순서에 맞춰 자동으로 채워지므로 고치지 않아도 된다")]
        [SerializeField] private string _label;

        [Tooltip("이 구역에서 나올 적 목록. 위 줄부터 순서대로, 같은 구역 스폰 간격을 두고 한 마리씩 나온다")]
        [SerializeField] private SpawnGroupUnit[] _units;

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

        public void SetLabel(string label)
        {
            _label = label;
        }
    }
}
