using System;
using System.Collections.Generic;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 한 번에 나오는 무리. 어느 구역에서 어떤 적이 몇 마리 나오는지를 구역별로 적는다.
    // 같은 구역에서 같이 출발해야 흐름장을 따라 뭉쳐 오고, 폭탄 한 방에 여럿을 잡는 상황이 생긴다.
    [CreateAssetMenu(fileName = "SpawnGroup", menuName = "Explode It/Stage/Spawn Group")]
    public class SpawnGroupData : ScriptableObject
    {
        [Tooltip("대표 이름. 웨이브에서 이 군집을 알아보기 위한 이름일 뿐 동작에는 영향이 없다")]
        [SerializeField] private string _displayName;

        [Tooltip("구역별 스폰 목록. 쓰는 구역만 칸을 추가하고 구역 번호를 적는다. 같은 번호를 두 칸에 적으면 그 구역에서 두 덩어리가 따로 나온다")]
        [SerializeField] private SpawnAreaSlot[] _areas;

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public IReadOnlyList<SpawnAreaSlot> Areas => _areas ?? Array.Empty<SpawnAreaSlot>();

        public int TotalCount
        {
            get
            {
                int total = 0;
                IReadOnlyList<SpawnAreaSlot> areas = Areas;
                for (int i = 0; i < areas.Count; i++)
                {
                    total += areas[i].TotalCount;
                }

                return total;
            }
        }

        private void OnValidate()
        {
            if (_areas == null)
            {
                return;
            }

            for (int i = 0; i < _areas.Length; i++)
            {
                _areas[i] ??= new SpawnAreaSlot();
                _areas[i].RefreshLabel();

                IReadOnlyList<SpawnGroupUnit> units = _areas[i].Units;
                for (int u = 0; u < units.Count; u++)
                {
                    if (units[u] != null && units[u].Validate())
                    {
                        Debug.LogWarning($"{name}: Area {_areas[i].AreaId}의 {u + 1}번 줄 프리팹에 Enemy 컴포넌트가 없어 비웁니다.", this);
                    }
                }
            }
        }
    }
}
