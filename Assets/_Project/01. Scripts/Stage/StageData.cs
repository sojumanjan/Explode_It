using System;
using System.Collections.Generic;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 한 판의 진행표. 웨이브 하나를 다 잡으면 보스전, 보스전이 끝나면 다음 웨이브.
    // 정의된 마지막 웨이브 뒤에는 마지막 웨이브를 반복한다. 무한/엔딩은 아직 정하지 않았다.
    [CreateAssetMenu(fileName = "StageData", menuName = "Explode It/Stage/Stage Data")]
    public class StageData : ScriptableObject
    {
        [Header("흐름")]
        [Tooltip("첫 웨이브 전 대기 (초). 판 시작 후 첫 적이 나오기까지")]
        [SerializeField, Min(0f)] private float _firstWaveDelay = 2f;

        [Tooltip("웨이브 사이 간격 (초). 보스전 연출이 끝난 뒤 다음 웨이브가 시작되기까지의 쉬는 시간")]
        [SerializeField, Min(0f)] private float _waveInterval = 5f;

        [Tooltip("보스전 연출 시간 (초). 웨이브를 다 잡으면 이 시간 동안 보스 등장 연출을 보여준다. 보스가 생기기 전까지는 연출 뒤 바로 웨이브 사이 간격으로 넘어간다")]
        [SerializeField, Min(0f)] private float _bossIntroDuration = 2f;

        [Header("스폰")]
        [Tooltip("웨이브당 스폰 수 (마리). 웨이브의 군집 합계가 이 수와 다르면 시작할 때 경고한다")]
        [SerializeField, Min(1)] private int _spawnsPerWave = 100;

        [Tooltip("같은 구역 스폰 간격 (초). 한 군집에서 같은 구역에 2마리 이상 나올 때 한 마리씩 이 간격을 둔다")]
        [SerializeField, Min(0f)] private float _sameAreaSpawnInterval = 0.2f;

        // 적끼리는 서로 밀어내지 않아서, 같은 점에 나오면 한 몸처럼 겹쳐 보인다.
        [Tooltip("뭉침 반경 (유닛). 같은 구역에서 나오는 적은 구역 안 무작위 기준점 주변 이 반경 안에 나온다")]
        [SerializeField, Min(0f)] private float _clusterRadius = 1f;

        [Header("웨이브")]
        [Tooltip("웨이브 목록. 위에서부터 차례로 진행하고, 마지막 웨이브는 계속 반복한다")]
        [SerializeField] private WaveData[] _waves;

        public float FirstWaveDelay => _firstWaveDelay;
        public float WaveInterval => _waveInterval;
        public float BossIntroDuration => _bossIntroDuration;
        public int SpawnsPerWave => _spawnsPerWave;
        public float SameAreaSpawnInterval => _sameAreaSpawnInterval;
        public float ClusterRadius => _clusterRadius;
        public IReadOnlyList<WaveData> Waves => _waves ?? Array.Empty<WaveData>();
    }
}
