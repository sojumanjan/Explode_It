using System;
using System.Collections.Generic;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 마지막 웨이브까지 다 나오고 살아 있는 적이 없으면 스테이지 클리어다.
    [CreateAssetMenu(fileName = "StageData", menuName = "Explode It/Stage/Stage Data")]
    public class StageData : ScriptableObject
    {
        [Tooltip("웨이브 목록. 위에서부터 차례로, 쉬는 시간만큼 간격을 두고 이어진다")]
        [SerializeField] private StageWave[] _waves;

        public IReadOnlyList<StageWave> Waves => _waves ?? Array.Empty<StageWave>();
    }
}
