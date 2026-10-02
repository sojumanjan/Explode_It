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
        // 지금 맵의 구역 수. 새로 만든 군집에 이만큼 칸을 미리 깔아 둔다. 구역이 늘면 칸을 더 추가하면 된다.
        private const int DefaultAreaCount = 9;

        [Tooltip("대표 이름. 웨이브에서 이 군집을 알아보기 위한 이름일 뿐 동작에는 영향이 없다")]
        [SerializeField] private string _displayName;

        [Tooltip("구역별 스폰 목록. 1번 칸이 Area 1, 2번 칸이 Area 2이다. 비어 있는 칸의 구역에서는 나오지 않는다")]
        [SerializeField] private SpawnAreaSlot[] _areas = new SpawnAreaSlot[DefaultAreaCount];

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public IReadOnlyList<SpawnAreaSlot> Areas => _areas ?? Array.Empty<SpawnAreaSlot>();

        // 칸 순서가 곧 구역 번호다. 씬의 SpawnArea 번호와 맞춘다.
        public static int AreaIdOf(int slotIndex)
        {
            return slotIndex + 1;
        }

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
            if (_areas == null || _areas.Length == 0)
            {
                _areas = new SpawnAreaSlot[DefaultAreaCount];
            }

            for (int i = 0; i < _areas.Length; i++)
            {
                _areas[i] ??= new SpawnAreaSlot();
                _areas[i].SetLabel($"Area {AreaIdOf(i)}");

                IReadOnlyList<SpawnGroupUnit> units = _areas[i].Units;
                for (int u = 0; u < units.Count; u++)
                {
                    if (units[u] != null && units[u].Validate())
                    {
                        Debug.LogWarning($"{name}: Area {AreaIdOf(i)}의 {u + 1}번 줄 프리팹에 Enemy 컴포넌트가 없어 비웁니다.", this);
                    }
                }
            }
        }
    }
}
