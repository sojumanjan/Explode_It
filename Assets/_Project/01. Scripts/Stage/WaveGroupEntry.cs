using System;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 웨이브 안에서 군집 에셋 하나를 언제 내보낼지. 군집 에셋은 무엇을, 이 항목은 언제를 맡는다.
    [Serializable]
    public class WaveGroupEntry
    {
        [Tooltip("간격 (초). 앞 군집이 시작된 뒤 이 군집이 시작되기까지. 첫 군집은 웨이브 시작부터 잰다. 0이면 앞 군집과 동시에 나온다")]
        [SerializeField, Min(0f)] private float _delay;

        [Tooltip("군집 에셋")]
        [SerializeField] private SpawnGroupData _group;

        public float Delay => _delay;
        public SpawnGroupData Group => _group;
    }
}
