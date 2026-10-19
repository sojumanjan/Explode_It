using System;
using System.Collections.Generic;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 무한 모드 웨이브를 플레이 중 실시간으로 만든다. 웨이브 시작 때 한꺼번에 짜면 30초 뒤 플레이어 위치를 모르므로,
    // 박자를 낼 차례가 될 때마다 그 순간의 플레이어 위치로 구역을, 그 웨이브의 처치 수로 적을 고른다.
    // 박자는 스토리 웨이브처럼 단독·포위·뭉치 셋 중 하나이고, 대부분 한 마리씩이라 같은 종류 덩어리로 보이지 않는다.
    // 구간 마리 수를 다 내야 다음 구간으로 넘어가므로 웨이브 합계는 늘 구간 합(100마리)과 같다.
    public class EndlessWaveGenerator
    {
        private enum Beat
        {
            Single,
            Surround,
            Clump
        }

        private readonly EndlessWaveData _data;
        private readonly SpawnArea[] _areas;
        private readonly int _spawnsPerWave;
        private readonly float[] _areaWeights;
        private readonly float[] _areaDistances;
        private readonly bool[] _isAreaUsed;
        // 이번 웨이브의 적 가중치(웨이브 동안 고정)와, 안 뽑힐 때마다 쌓이는 덤(웨이브를 넘어 이어진다). 뽑을 때는 둘을 더해 쓴다.
        private readonly float[] _enemyWeights;
        private readonly float[] _enemyMissBonus;
        private readonly float[] _enemyPickWeights;
        private readonly float[] _beatWeights = new float[3];
        // 전선 각도를 잴 기준점. 구역들의 가운데라 맵 중심과 거의 같다.
        private readonly Vector2 _center;

        private float _durationScale;
        private int _extraAreas;
        private float _surroundBonus;
        private float _clumpBonus;
        // 단독·뭉치 박자가 마지막으로 쓴 구역. 다음 박자는 이 근처에서 나와 전선이 이어진다. -1이면 아직 없다.
        private int _frontArea = -1;
        private int _segment;
        private float _segmentStart;
        private int _segmentLeft;
        private float _nextPulse;

        public EndlessWaveGenerator(EndlessWaveData data, SpawnArea[] areas, int spawnsPerWave)
        {
            _data = data;
            _areas = areas;
            _spawnsPerWave = spawnsPerWave;
            _areaWeights = new float[areas.Length];
            _areaDistances = new float[areas.Length];
            _isAreaUsed = new bool[areas.Length];
            _enemyWeights = new float[data.Enemies.Count];
            _enemyMissBonus = new float[data.Enemies.Count];
            _enemyPickWeights = new float[data.Enemies.Count];
            _segment = int.MaxValue;
            Vector2 sum = Vector2.zero;
            for (int i = 0; i < areas.Length; i++)
            {
                sum += (Vector2)areas[i].Bounds.center;
            }

            _center = areas.Length > 0 ? sum / areas.Length : Vector2.zero;
        }

        public bool IsDone => _segment >= _data.Segments.Count;

        // waveNumber는 1부터. killsBefore는 이 웨이브 전까지 나온 수로, 적 풀을 여는 기준이다.
        public void BeginWave(int waveNumber, int killsBefore)
        {
            int wavesDone = Mathf.Max(0, waveNumber - 1);
            _durationScale = Mathf.Max(_data.MinDurationScale, Mathf.Pow(_data.DurationScalePerWave, wavesDone));
            _extraAreas = _data.WavesPerExtraArea > 0 ? Mathf.Min(wavesDone / _data.WavesPerExtraArea, _data.MaxExtraAreas) : 0;
            int shiftWaves = Mathf.Min(wavesDone, _data.BeatShiftWaveCap);
            _surroundBonus = _data.SurroundWeightPerWave * shiftWaves;
            _clumpBonus = _data.ClumpWeightPerWave * shiftWaves;

            IReadOnlyList<EndlessEnemyEntry> enemies = _data.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                _enemyWeights[i] = enemies[i] != null ? enemies[i].GetWeight(killsBefore, _spawnsPerWave, _data.WeightGrowthStartKills) : 0f;
            }

            EnterSegment(0, 0f);
        }

        // 보스로 건너뛰기 등으로 웨이브를 끊을 때.
        public void Stop()
        {
            _segment = int.MaxValue;
        }

        // time은 웨이브 시작 기준 시간. 낼 차례가 된 군집을 output에 더한다.
        public void Tick(float time, Vector2 player, List<EndlessGroupOrder> output)
        {
            while (!IsDone)
            {
                EndlessSegment segment = _data.Segments[_segment];
                if (segment == null)
                {
                    EnterSegment(_segment + 1, _segmentStart);
                    continue;
                }

                float duration = segment.Duration * _durationScale;
                if (_segmentLeft > 0)
                {
                    if (time < _nextPulse)
                    {
                        return;
                    }

                    int spawned = Pulse(segment, player, output);
                    if (spawned == 0)
                    {
                        // 뽑을 적이나 구역이 없으면 이 구간은 더 낼 수 없다. 데이터 실수라 멈추지 않고 넘어간다.
                        Debug.LogWarning($"EndlessWaveGenerator: '{segment.Label}' 구간에서 낼 적이나 구역이 없어 {_segmentLeft}마리를 건너뜁니다.");
                        _segmentLeft = 0;
                        continue;
                    }

                    // 구간 마리 수를 구간 길이에 고르게 펼친다. 이번 박자가 클수록 다음 박자까지 오래 쉰다.
                    _nextPulse += duration * spawned / segment.Count;
                    continue;
                }

                if (time < _segmentStart + duration)
                {
                    return;
                }

                EnterSegment(_segment + 1, _segmentStart + duration);
            }
        }

        private void EnterSegment(int index, float start)
        {
            _segment = index;
            _segmentStart = start;
            _nextPulse = start;
            _segmentLeft = index < _data.Segments.Count && _data.Segments[index] != null ? _data.Segments[index].Count : 0;
        }

        // 박자 하나를 낸다. 낸 마리 수를 돌려준다.
        private int Pulse(EndlessSegment segment, Vector2 player, List<EndlessGroupOrder> output)
        {
            Array.Clear(_isAreaUsed, 0, _isAreaUsed.Length);
            MeasureAreas(player);

            _beatWeights[(int)Beat.Single] = segment.SingleWeight;
            _beatWeights[(int)Beat.Surround] = segment.SurroundWeight + _surroundBonus;
            _beatWeights[(int)Beat.Clump] = Mathf.Max(0f, segment.ClumpWeight + _clumpBonus);
            // 남은 마리가 하나면 포위·뭉치를 해도 한 마리라 단독과 같다.
            Beat beat = _segmentLeft > 1 ? (Beat)Mathf.Max(0, PickWeighted(_beatWeights)) : Beat.Single;

            switch (beat)
            {
                case Beat.Surround:
                    return Surround(segment, player, output);
                case Beat.Clump:
                    return Emit(PickFrontArea(player), PickEnemy(), UnityEngine.Random.Range(segment.ClumpSizeMin, segment.ClumpSizeMax + 1), output);
                default:
                    return Emit(PickFrontArea(player), PickEnemy(), 1, output);
            }
        }

        // 여러 구역에서 1마리씩 동시에. 한 종류 포위면 모든 구역이 같은 적, 따로 뽑는 포위면 구역마다 적을 새로 뽑는다.
        // 포위는 일부러 사방에서 조이는 박자라 전선을 따르지 않고, 전선도 옮기지 않는다.
        private int Surround(EndlessSegment segment, Vector2 player, List<EndlessGroupOrder> output)
        {
            int areaCount = UnityEngine.Random.Range(segment.SurroundAreasMin, segment.SurroundAreasMax + 1) + _extraAreas;
            areaCount = Mathf.Clamp(areaCount, 1, _areas.Length);
            float sameKind = _data.SameKindSurroundWeight;
            float total = sameKind + _data.MixedSurroundWeight;
            bool isSameKind = total <= 0f || UnityEngine.Random.value * total < sameKind;
            int sharedEnemy = isSameKind ? PickEnemy() : -1;

            int spawned = 0;
            int first = -1;
            for (int i = 0; i < areaCount && _segmentLeft > 0; i++)
            {
                int area = PickArea(player, first, -1);
                int enemy = isSameKind ? sharedEnemy : PickEnemy();
                int count = Emit(area, enemy, 1, output);
                if (count == 0)
                {
                    break;
                }

                spawned += count;
                if (first < 0)
                {
                    first = area;
                }
            }

            return spawned;
        }

        // 적 하나를 뽑는다. 뽑힌 적의 덤은 0으로, 나올 수 있는데 안 뽑힌 적의 덤은 조금씩 쌓인다.
        private int PickEnemy()
        {
            for (int i = 0; i < _enemyWeights.Length; i++)
            {
                _enemyPickWeights[i] = _enemyWeights[i] > 0f ? _enemyWeights[i] + _enemyMissBonus[i] : 0f;
            }

            int pick = PickWeighted(_enemyPickWeights);
            if (pick < 0)
            {
                return pick;
            }

            for (int i = 0; i < _enemyWeights.Length; i++)
            {
                if (i == pick)
                {
                    _enemyMissBonus[i] = 0f;
                }
                else if (_enemyWeights[i] > 0f)
                {
                    _enemyMissBonus[i] += _data.MissWeightGrowth;
                }
            }

            return pick;
        }

        // 단독·뭉치 박자의 구역. 직전 구역 근처를 골라 전선을 잇고, 가끔은 건너뛰어 다른 쪽에 새 전선을 연다.
        private int PickFrontArea(Vector2 player)
        {
            bool isJump = _frontArea < 0 || UnityEngine.Random.value < _data.FrontJumpChance;
            int area = PickArea(player, -1, isJump ? -1 : _frontArea);
            if (area >= 0)
            {
                _frontArea = area;
            }

            return area;
        }

        // 한 구역에서 같은 적 size마리. 적마다 정한 최대 무리 크기와 구간에 남은 수를 넘지 않는다. 낸 수를 돌려준다.
        private int Emit(int area, int enemy, int size, List<EndlessGroupOrder> output)
        {
            if (area < 0 || enemy < 0)
            {
                return 0;
            }

            EndlessEnemyEntry entry = _data.Enemies[enemy];
            size = Mathf.Min(size, entry.MaxGroupSize, _segmentLeft);
            output.Add(new EndlessGroupOrder(_nextPulse, _areas[area], entry.Enemy, size));
            _isAreaUsed[area] = true;
            _segmentLeft -= size;
            return size;
        }

        // 가장 가까운 구역을 0, 가장 먼 구역을 1로 놓은 거리.
        private void MeasureAreas(Vector2 player)
        {
            float min = float.MaxValue;
            float max = float.MinValue;
            for (int i = 0; i < _areas.Length; i++)
            {
                float distance = ((Vector2)_areas[i].Bounds.center - player).magnitude;
                _areaDistances[i] = distance;
                min = Mathf.Min(min, distance);
                max = Mathf.Max(max, distance);
            }

            float range = max - min;
            for (int i = 0; i < _areas.Length; i++)
            {
                _areaDistances[i] = range > 0.001f ? (_areaDistances[i] - min) / range : 0.5f;
            }
        }

        // 거리 가중치로 고른다. 이번 박자에서 이미 쓴 구역은 뺀다.
        // first: 포위의 두 번째 구역부터, 첫 구역과 (플레이어 기준) 벌어진 각도만큼 더 준다.
        // front: 단독·뭉치 박자에서, 맵 중심 기준으로 이 구역과 각도가 가까울수록 더 준다.
        private int PickArea(Vector2 player, int first, int front)
        {
            Vector2 firstDirection = first >= 0 ? (Vector2)_areas[first].Bounds.center - player : Vector2.zero;
            Vector2 frontDirection = front >= 0 ? (Vector2)_areas[front].Bounds.center - _center : Vector2.zero;
            for (int i = 0; i < _areas.Length; i++)
            {
                if (_isAreaUsed[i])
                {
                    _areaWeights[i] = 0f;
                    continue;
                }

                Vector2 position = _areas[i].Bounds.center;
                float weight = _data.GetAreaDistanceWeight(_areaDistances[i]);
                if (first >= 0)
                {
                    weight *= _data.GetAreaSpreadWeight(Vector2.Angle(firstDirection, position - player) / 180f);
                }

                if (front >= 0)
                {
                    weight *= _data.GetFrontAngleWeight(Vector2.Angle(frontDirection, position - _center) / 180f);
                }

                _areaWeights[i] = weight;
            }

            return PickWeighted(_areaWeights);
        }

        // 가중치 합이 0이면 -1.
        private static int PickWeighted(float[] weights)
        {
            float total = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                total += weights[i];
            }

            if (total <= 0f)
            {
                return -1;
            }

            float pick = UnityEngine.Random.value * total;
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] <= 0f)
                {
                    continue;
                }

                pick -= weights[i];
                if (pick <= 0f)
                {
                    return i;
                }
            }

            // 부동소수 오차로 끝까지 남으면 마지막 후보를 고른다.
            for (int i = weights.Length - 1; i >= 0; i--)
            {
                if (weights[i] > 0f)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
