using System;
using System.Collections.Generic;
using ExplodeIt.Core;
using ExplodeIt.Enemies;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 한 판의 진행표. 웨이브 하나를 다 잡으면 보스전, 보스전이 끝나면 다음 웨이브.
    // 스토리 모드: 보스 순서를 1바퀴 돈 뒤 최종 보스를 잡으면 판이 끝난다(엔딩).
    // 무한 모드의 강화 로테이션(여러 바퀴)도 같은 진행표 구조를 쓸 수 있게 바퀴 수는 남겨 둔다.
    [CreateAssetMenu(fileName = "StageData", menuName = "Explode It/Stage/Stage Data")]
    public class StageData : ScriptableObject
    {
        [Header("모드")]
        [Tooltip("스토리 모드 진행표인지. 켜면 보스 등장 대화와 우측 하단 왕 컷인이 나온다")]
        [SerializeField] private bool _isStoryMode = true;

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

        [Header("보스")]
        // 프리팹 칸은 오브젝트 피커가 컴포넌트 타입에 프리팹을 띄우지 않아 GameObject로 받는다.
        [Tooltip("보스 순서. 1번 칸이 100마리, 2번 칸이 200마리, 3번 칸이 300마리 보스다. 바퀴 수가 2 이상이면 다 돈 뒤 1번 칸부터 한 바퀴 더 강해져서 나온다. 빈 칸은 연출만 보여주고 넘어간다")]
        [SerializeField] private GameObject[] _bossRotation = new GameObject[3];

        [Tooltip("바퀴 수. 보스 순서를 이만큼 돈 뒤 최종 보스가 나온다. 스토리 모드는 1 (3종 = 300마리, 최종 보스는 400마리)")]
        [SerializeField, Min(1)] private int _bossLaps = 3;

        [Tooltip("최종 보스. 모든 바퀴가 끝난 다음 보스전에 나오고, 잡으면 판이 끝난다(엔딩). 비워 두면 연출만 보여주고 바로 끝난다")]
        [SerializeField] private GameObject _finalBoss;

        [Tooltip("보스 등장 거리 (유닛). 플레이어에게서 맵의 넓은 쪽으로 이만큼 떨어진 곳에 내려온다")]
        [SerializeField, Min(0f)] private float _bossSpawnDistance = 5f;

        [Tooltip("보스 등장 여유 (유닛). 맵 테두리와 구조물에서 이만큼은 떨어진 곳에만 내려온다")]
        [SerializeField, Min(0f)] private float _bossSpawnClearance = 1.5f;

        [Tooltip("지정 자리(BossSpawnPoints)가 있는 보스의 최소 거리 (유닛). 플레이어와 이보다 멀리 떨어진 후보 중 가장 가까운 곳에 내려온다")]
        [SerializeField, Min(0f)] private float _fixedSpawnMinDistance = 6f;

        [Tooltip("보스 등장 후 멈춤 (초). 보스가 내려앉고 구체가 퍼진 뒤 이 시간 동안 플레이어는 움직일 수 없고 카메라는 보스 쪽을 비춘다")]
        [SerializeField, Min(0f)] private float _bossRevealHold = 1f;

        [Tooltip("보스 등장 카메라 이동 시간 (초). 카메라가 플레이어와 보스 사이로 옮겨 가고, 다시 플레이어에게 돌아오는 시간. 돌아오면 보스가 움직이기 시작한다")]
        [SerializeField, Min(0f)] private float _bossCameraBlend = 0.6f;

        [Header("스토리 대화")]
        [Tooltip("보스 등장 대화. 보스 순서와 같은 칸 순서(1번 칸 = 1번 보스). 비워 두면 대화 없이 진행한다")]
        [SerializeField] private BossDialogueData[] _bossDialogues = new BossDialogueData[3];

        [Tooltip("최종 보스 등장 대화")]
        [SerializeField] private BossDialogueData _finalBossDialogue;

        [Tooltip("보스가 내려앉는 순간의 연출 (흔들림 등). 비워 두면 연출 없이 나타난다")]
        [SerializeField] private FeedbackData _bossLandFeedback;

        public bool IsStoryMode => _isStoryMode;
        public float FirstWaveDelay => _firstWaveDelay;
        public int BossLaps => _bossLaps;
        public int BossKindCount => _bossRotation != null ? _bossRotation.Length : 0;
        public float BossSpawnDistance => _bossSpawnDistance;
        public float BossSpawnClearance => _bossSpawnClearance;
        public float FixedSpawnMinDistance => _fixedSpawnMinDistance;
        public FeedbackData BossLandFeedback => _bossLandFeedback;
        public float BossRevealHold => _bossRevealHold;
        public float BossCameraBlend => _bossCameraBlend;

        // 최종 보스는 모든 바퀴 다음 순번이다. 이 보스를 잡으면 판이 끝난다.
        public int FinalBossNumber => BossKindCount * _bossLaps + 1;

        // bossNumber(1부터)로 몇 번째 보스가 어느 바퀴로 나올지 정한다. 바퀴는 0부터.
        // 최종 보스 뒤로 가는 경우(개발자 패널 건너뛰기 등)는 마지막 바퀴의 보스 순서를 계속 돈다.
        public Enemy GetBoss(int bossNumber, out int tier)
        {
            int kinds = BossKindCount;
            int index = bossNumber - 1;
            if (kinds == 0 || index < 0)
            {
                tier = 0;
                return null;
            }

            if (index == kinds * _bossLaps)
            {
                tier = 0;
                return ToEnemy(_finalBoss);
            }

            tier = Mathf.Min(index / kinds, _bossLaps - 1);
            return ToEnemy(_bossRotation[index % kinds]);
        }

        // 스토리 대화는 첫 바퀴에만 있다. 같은 보스가 다시 나오는 바퀴(무한 모드)에는 대화를 붙이지 않는다.
        public BossDialogueData GetBossDialogue(int bossNumber)
        {
            if (bossNumber == FinalBossNumber)
            {
                return _finalBossDialogue;
            }

            int index = bossNumber - 1;
            return _bossDialogues != null && index >= 0 && index < _bossDialogues.Length && index < BossKindCount
                ? _bossDialogues[index]
                : null;
        }

        private static Enemy ToEnemy(GameObject prefab)
        {
            return prefab != null && prefab.TryGetComponent(out Enemy enemy) ? enemy : null;
        }

        private void OnValidate()
        {
            if (_bossRotation != null)
            {
                for (int i = 0; i < _bossRotation.Length; i++)
                {
                    if (_bossRotation[i] != null && !_bossRotation[i].TryGetComponent(out IBoss _))
                    {
                        Debug.LogWarning($"{name}: 보스 순서 {i + 1}번 칸 프리팹에 보스 컴포넌트가 없어 비웁니다.", this);
                        _bossRotation[i] = null;
                    }
                }
            }

            if (_finalBoss != null && !_finalBoss.TryGetComponent(out IBoss _))
            {
                Debug.LogWarning($"{name}: 최종 보스 프리팹에 보스 컴포넌트가 없어 비웁니다.", this);
                _finalBoss = null;
            }
        }
        public float WaveInterval => _waveInterval;
        public float BossIntroDuration => _bossIntroDuration;
        public int SpawnsPerWave => _spawnsPerWave;
        public float SameAreaSpawnInterval => _sameAreaSpawnInterval;
        public float ClusterRadius => _clusterRadius;
        public IReadOnlyList<WaveData> Waves => _waves ?? Array.Empty<WaveData>();
    }
}
