using System;
using System.Collections.Generic;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 웨이브 하나. 정해진 수(기본 100마리)를 군집들로 나눠 시간에 맞춰 내보낸다.
    // 다 잡으면 보스전으로 넘어가며, 웨이브가 넘어가는 것 자체는 화면에 알리지 않는다.
    [CreateAssetMenu(fileName = "Wave_", menuName = "Explode It/Stage/Wave Data")]
    public class WaveData : ScriptableObject
    {
        [Tooltip("메모. 이 웨이브의 의도를 적어 두는 칸일 뿐 동작에는 영향이 없다")]
        [SerializeField, TextArea] private string _memo;

        [Tooltip("군집 목록. 각 군집의 시작 시간은 이 웨이브가 시작된 시점 기준이다")]
        [SerializeField] private WaveGroupEntry[] _groups;

        public IReadOnlyList<WaveGroupEntry> Groups => _groups ?? Array.Empty<WaveGroupEntry>();

        public int TotalSpawnCount
        {
            get
            {
                int total = 0;
                IReadOnlyList<WaveGroupEntry> groups = Groups;
                for (int i = 0; i < groups.Count; i++)
                {
                    if (groups[i].Group != null)
                    {
                        total += groups[i].Group.TotalCount;
                    }
                }

                return total;
            }
        }

        private void OnValidate()
        {
            if (_groups == null)
            {
                return;
            }

            for (int i = 0; i < _groups.Length; i++)
            {
                _groups[i] ??= new WaveGroupEntry();
                _groups[i].RefreshLabel();
            }
        }
    }
}
