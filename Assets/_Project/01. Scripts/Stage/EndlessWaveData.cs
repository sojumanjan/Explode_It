using System;
using System.Collections.Generic;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 무한 모드 웨이브 생성 규칙. 손으로 짠 웨이브 대신, 리듬 틀(구간)에 맞춰 플레이 중 실시간으로 군집을 만든다.
    // 무작위는 "어떤 적이 어디서 나오는가"에만 두고, 웨이브가 쌓일수록 양과 배치로만 어려워진다(적 속도는 건드리지 않는다).
    [CreateAssetMenu(fileName = "EndlessWaveData", menuName = "Explode It/Stage/Endless Wave Data")]
    public class EndlessWaveData : ScriptableObject
    {
        [Tooltip("구간 마리 수 합계 (마리). 자동 계산되므로 고치지 않아도 된다. 웨이브당 스폰 수(100)와 같아야 한다")]
        [SerializeField] private int _totalSpawnPreview;

        [Header("리듬")]
        [Tooltip("웨이브 하나의 리듬. 위에서부터 차례로 진행한다(예: 소개 → 섞기 → 쉼 → 압박 → 쉼 → 몰아치기 → 마무리)")]
        [SerializeField] private EndlessSegment[] _segments;

        [Header("적 풀")]
        [Tooltip("나올 수 있는 적. 처치 수에 따라 열리고, 군집마다 가중치로 하나를 뽑는다")]
        [SerializeField] private EndlessEnemyEntry[] _enemies;

        [Tooltip("적 가중치가 웨이브마다 오르기 시작하는 처치 수 (마리). 그 전까지는 모든 적이 기본 가중치로 나온다. 300이면 보스 셋을 한 번씩 잡은 뒤 4웨이브부터 오른다")]
        [SerializeField, Min(0)] private int _weightGrowthStartKills = 300;

        // 순수 무작위면 가중치가 낮은 적이 한참 안 나오거나 한 종류가 몰려 나와 운에 너무 기댄다.
        [Tooltip("박자에서 안 뽑힐 때마다 그 적의 가중치에 더하는 값. 뽑히면 0으로 돌아간다. 오래 안 나온 적이 조금씩 나오기 쉬워진다. 0이면 순수 무작위")]
        [SerializeField, Min(0f)] private float _missWeightGrowth = 0.01f;

        [Header("구역 고르기")]
        // 가까운 구역을 아예 빼면 플레이어가 움직이는 쪽에서 적이 안 나와 쉬워지므로, 양 끝을 덜 뽑는 정도로만 둔다.
        [Tooltip("플레이어와의 거리에 따른 구역 가중치. 가로축 0 = 가장 가까운 구역, 1 = 가장 먼 구역, 세로축 = 가중치")]
        [SerializeField] private AnimationCurve _areaDistanceWeight = new AnimationCurve(
            new Keyframe(0f, 0.5f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0.5f));

        [Tooltip("포위 구역 중 두 번째부터, 첫 구역과 벌어진 각도에 따른 가중치 배율. 가로축 0 = 같은 방향, 1 = 정반대(180도). 반대편일수록 크게 줘서 협공이 되게 한다")]
        [SerializeField] private AnimationCurve _areaSpreadWeight = new AnimationCurve(
            new Keyframe(0f, 0.3f), new Keyframe(1f, 1f));

        // 스토리 웨이브는 한동안 한쪽(전선)에서 보내다 옆으로 옮겨 간다. 매번 다른 방향에서 나오면 한 마리씩이어도 늘 포위당하는 느낌이 된다.
        [Tooltip("단독·뭉치 박자에서, 맵 중심에서 본 직전 구역과의 각도에 따른 가중치 배율. 가로축 0 = 같은 방향, 1 = 정반대(180도). 가까울수록 크게 줘서 전선이 이어지게 한다")]
        [SerializeField] private AnimationCurve _frontAngleWeight = new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(0.25f, 0.8f), new Keyframe(0.5f, 0.3f), new Keyframe(1f, 0.1f));

        [Tooltip("전선이 건너뛸 확률 (0~1). 박자마다 이 확률로 직전 구역을 무시하고 거리 가중치만으로 뽑아, 다른 쪽에서 새 전선이 열린다")]
        [SerializeField, Range(0f, 1f)] private float _frontJumpChance = 0.15f;

        [Header("포위 종류")]
        [Tooltip("한 종류 포위 가중치. 포위할 때 모든 구역에서 같은 적이 나온다")]
        [SerializeField, Min(0f)] private float _sameKindSurroundWeight = 6f;

        [Tooltip("따로 뽑는 포위 가중치. 포위할 때 구역마다 적을 따로 뽑는다")]
        [SerializeField, Min(0f)] private float _mixedSurroundWeight = 4f;

        [Header("난이도 상승")]
        [Tooltip("웨이브마다 포위 박자 가중치에 더하는 값. 쉬는 구간에는 더하지 않는다")]
        [SerializeField, Min(0f)] private float _surroundWeightPerWave = 3f;

        // 뭉쳐 나오면 폭탄 한 발에 여럿이 잡혀 오히려 쉬워지므로, 웨이브가 쌓일수록 줄이는 쪽(음수)으로 둔다.
        [Tooltip("웨이브마다 뭉치 박자 가중치에 더하는 값. 음수면 줄어들고, 0 밑으로는 내려가지 않는다")]
        [SerializeField] private float _clumpWeightPerWave = -1.5f;

        [Tooltip("박자 가중치가 더 이상 오르지 않는 웨이브 수 (웨이브). 10이면 11웨이브부터는 그대로다")]
        [SerializeField, Min(0)] private int _beatShiftWaveCap = 10;

        [Tooltip("웨이브마다 구간 길이에 곱하는 비율. 0.95면 웨이브마다 5%씩 짧아져 같은 마리 수가 더 빽빽하게 나온다")]
        [SerializeField, Range(0.5f, 1f)] private float _durationScalePerWave = 0.95f;

        [Tooltip("구간 길이 비율 하한. 웨이브가 아무리 쌓여도 1웨이브 길이의 이 비율보다 짧아지지 않는다")]
        [SerializeField, Range(0.1f, 1f)] private float _minDurationScale = 0.6f;

        [Tooltip("포위 구역이 하나 늘어나는 웨이브 간격 (웨이브). 3이면 4·7·10웨이브에서 하나씩 는다. 0이면 늘지 않는다")]
        [SerializeField, Min(0)] private int _wavesPerExtraArea = 3;

        [Tooltip("적 이동 속도가 오르기 시작하는 웨이브 (웨이브). 물량·배치 상승이 멈추는 쯤부터 속도로 계속 어려워진다")]
        [SerializeField, Min(1)] private int _speedUpStartWave = 11;

        [Tooltip("시작 웨이브부터 웨이브마다 적 이동 속도에 곱하는 증가율. 0.04면 웨이브마다 4%씩 빨라지고 상한은 없다")]
        [SerializeField, Min(0f)] private float _speedUpPerWave = 0.04f;

        [Tooltip("웨이브가 쌓여 늘어나는 포위 구역 수 최대 (개)")]
        [SerializeField, Min(0)] private int _maxExtraAreas = 2;

        public IReadOnlyList<EndlessSegment> Segments => _segments ?? Array.Empty<EndlessSegment>();
        public IReadOnlyList<EndlessEnemyEntry> Enemies => _enemies ?? Array.Empty<EndlessEnemyEntry>();
        public float DurationScalePerWave => _durationScalePerWave;
        public float MinDurationScale => _minDurationScale;
        public int WavesPerExtraArea => _wavesPerExtraArea;
        public int MaxExtraAreas => _maxExtraAreas;
        public float FrontJumpChance => _frontJumpChance;
        public int WeightGrowthStartKills => _weightGrowthStartKills;
        public float MissWeightGrowth => _missWeightGrowth;
        public float SameKindSurroundWeight => _sameKindSurroundWeight;
        public float MixedSurroundWeight => _mixedSurroundWeight;
        public float SurroundWeightPerWave => _surroundWeightPerWave;
        public float ClumpWeightPerWave => _clumpWeightPerWave;
        public int BeatShiftWaveCap => _beatShiftWaveCap;

        public int TotalSpawnCount
        {
            get
            {
                int total = 0;
                IReadOnlyList<EndlessSegment> segments = Segments;
                for (int i = 0; i < segments.Count; i++)
                {
                    total += segments[i] != null ? segments[i].Count : 0;
                }

                return total;
            }
        }

        public float GetAreaDistanceWeight(float normalizedDistance)
        {
            return Mathf.Max(0f, _areaDistanceWeight.Evaluate(normalizedDistance));
        }

        public float GetAreaSpreadWeight(float normalizedAngle)
        {
            return Mathf.Max(0f, _areaSpreadWeight.Evaluate(normalizedAngle));
        }

        // waveNumber는 1부터. 시작 웨이브 전에는 1.
        public float GetEnemySpeedMultiplier(int waveNumber)
        {
            int waves = waveNumber - _speedUpStartWave + 1;
            return waves > 0 ? Mathf.Pow(1f + _speedUpPerWave, waves) : 1f;
        }

        public float GetFrontAngleWeight(float normalizedAngle)
        {
            return Mathf.Max(0f, _frontAngleWeight.Evaluate(normalizedAngle));
        }

        private void OnValidate()
        {
            if (_enemies != null)
            {
                for (int i = 0; i < _enemies.Length; i++)
                {
                    if (_enemies[i] != null && _enemies[i].Validate())
                    {
                        Debug.LogWarning($"{name}: 적 풀 {i + 1}번 줄 프리팹에 Enemy 컴포넌트가 없어 비웁니다.", this);
                    }
                }
            }

            _totalSpawnPreview = TotalSpawnCount;
        }
    }
}
