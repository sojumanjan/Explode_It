using System;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 웨이브 안에서 군집 하나를 언제 내보낼지. 무엇이 어디서 나올지는 군집이 정한다.
    [Serializable]
    public class WaveGroupEntry
    {
        [Tooltip("시작 시간 (초). 이 웨이브가 시작된 뒤 이 군집이 나오기 시작하기까지")]
        [SerializeField, Min(0f)] private float _startTime;

        [Tooltip("군집 에셋")]
        [SerializeField] private SpawnGroupData _group;

        public float StartTime => _startTime;
        public SpawnGroupData Group => _group;
    }
}
