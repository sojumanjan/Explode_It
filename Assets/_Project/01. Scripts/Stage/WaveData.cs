using System.Collections.Generic;
using ExplodeIt.Enemies;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 지금은 종류·위치를 모두 무작위로 뽑는 단일 웨이브다.
    // 웨이브별 적 구성과 수는 웨이브 편집기를 만들 때 이 데이터를 확장한다.
    [CreateAssetMenu(fileName = "WaveData", menuName = "Explode It/Stage/Wave Data")]
    public class WaveData : ScriptableObject
    {
        [Tooltip("스폰 간격 (초). 이 시간마다 적 한 마리가 나온다")]
        [SerializeField, Min(0.1f)] private float _spawnInterval = 3f;

        [Tooltip("등장 가능한 적 프리팹. 스폰할 때마다 이 중 하나를 무작위로 고른다")]
        [SerializeField] private Enemy[] _enemies;

        public float SpawnInterval => _spawnInterval;
        public IReadOnlyList<Enemy> Enemies => _enemies;
    }
}
