using System;
using System.Collections.Generic;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 한 번에 나오는 무리. 어떤 적이 몇 마리인지만 적고, 어디서 나올지는 웨이브가 정한다.
    // 구역은 맵마다 다르므로, 무리 구성만 담아 두면 같은 군집을 여러 웨이브·맵에서 다시 쓸 수 있다.
    [CreateAssetMenu(fileName = "SpawnGroup", menuName = "Explode It/Stage/Spawn Group")]
    public class SpawnGroupData : ScriptableObject
    {
        [Tooltip("대표 이름. 웨이브에서 이 군집을 알아보기 위한 이름일 뿐 동작에는 영향이 없다")]
        [SerializeField] private string _displayName;

        [Tooltip("적 목록. 위 줄부터 순서대로, 같은 구역 스폰 간격을 두고 한 마리씩 나온다")]
        [SerializeField] private SpawnGroupUnit[] _units;

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
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

        private void OnValidate()
        {
            if (_units == null)
            {
                return;
            }

            for (int i = 0; i < _units.Length; i++)
            {
                if (_units[i] != null && _units[i].Validate())
                {
                    Debug.LogWarning($"{name}: {i + 1}번 줄 프리팹에 Enemy 컴포넌트가 없어 비웁니다.", this);
                }
            }
        }
    }
}
