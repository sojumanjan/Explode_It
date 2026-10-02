using System;
using System.Collections.Generic;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 웨이브 안에서 군집 하나를 언제, 어느 구역에서 내보낼지. 무엇이 몇 마리 나올지는 군집이 정한다.
    // 구역을 여러 개 적으면 구역마다 군집 전체가 하나씩 동시에 나온다. 동서남북 동시 등장을 항목 하나로 적기 위해서다.
    // 군집을 여러 개 꽂으면 웨이브가 시작될 때 그중 하나를 뽑는다. 하나만 꽂으면 늘 그 군집이라,
    // 저격수 등장처럼 타이밍이 중요한 항목은 고정해 두고 원하는 항목에만 무작위를 줄 수 있다.
    [Serializable]
    public class WaveGroupEntry
    {
        private static readonly char[] Separators = { ',', ' ' };

        // 첫 문자열 필드라 인스펙터 리스트 항목 이름으로 보인다. 시간·구역·군집에 맞춰 자동으로 채운다.
        [Tooltip("항목 이름. 시작 시간, 구역, 군집에 맞춰 자동으로 채워지므로 고치지 않아도 된다")]
        [SerializeField] private string _label;

        [Tooltip("시작 시간 (초). 이 웨이브가 시작된 뒤 이 군집이 나오기 시작하기까지")]
        [SerializeField, Min(0f)] private float _startTime;

        [Tooltip("구역 번호. 씬 뷰에서 각 구역 위에 보이는 Area 숫자와 같다. 쉼표로 여러 개 적으면(예: 1,2,3,4) 구역마다 군집 전체가 동시에 하나씩 나온다. 같은 번호를 두 번 적으면 그 구역에서 두 덩어리가 나온다")]
        [SerializeField] private string _areaIds = "1";

        [Tooltip("군집 후보. 하나만 넣으면 늘 그 군집이 나오고, 여러 개 넣으면 웨이브가 시작될 때 그중 하나를 무작위로 뽑는다. 웨이브 합계(100마리)를 지키려면 후보끼리 마리 수가 같아야 한다")]
        [SerializeField] private SpawnGroupData[] _candidates;

        // 웨이브가 시작될 때마다 문자열을 다시 쪼개지 않도록, 바뀐 경우에만 다시 읽는다.
        [NonSerialized] private int[] _parsedIds;
        [NonSerialized] private string _parsedSource;

        public float StartTime => _startTime;
        public IReadOnlyList<SpawnGroupData> Candidates => _candidates ?? Array.Empty<SpawnGroupData>();

        public IReadOnlyList<int> AreaIds
        {
            get
            {
                if (_parsedIds == null || _parsedSource != _areaIds)
                {
                    Parse(out _);
                }

                return _parsedIds;
            }
        }

        // 합계는 첫 후보 기준이다. 후보끼리 마리 수가 다르면 확인 단계에서 경고한다.
        public int SpawnCount
        {
            get
            {
                SpawnGroupData first = FirstCandidate();
                return first != null ? first.TotalCount * AreaIds.Count : 0;
            }
        }

        // 빈 칸은 후보에서 뺀다. 후보가 없으면 null.
        public SpawnGroupData PickGroup()
        {
            IReadOnlyList<SpawnGroupData> candidates = Candidates;
            int validCount = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i] != null)
                {
                    validCount++;
                }
            }

            if (validCount == 0)
            {
                return null;
            }

            // 무작위는 조합에만 둔다. 등장한 뒤의 움직임은 군집과 무관하게 예측 가능하다.
            int pick = validCount > 1 ? UnityEngine.Random.Range(0, validCount) : 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i] == null)
                {
                    continue;
                }

                if (pick == 0)
                {
                    return candidates[i];
                }

                pick--;
            }

            return null;
        }

        // 후보끼리 마리 수가 다르면, 어떤 군집이 뽑히느냐에 따라 웨이브 합계가 달라진다.
        public bool HasCountMismatch()
        {
            SpawnGroupData first = FirstCandidate();
            if (first == null)
            {
                return false;
            }

            IReadOnlyList<SpawnGroupData> candidates = Candidates;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i] != null && candidates[i].TotalCount != first.TotalCount)
                {
                    return true;
                }
            }

            return false;
        }

        // 숫자가 아닌 칸은 건너뛰고, 건너뛴 글자를 돌려준다. 데이터 확인용이다.
        public string Parse(out int validCount)
        {
            _parsedSource = _areaIds;
            string invalid = null;
            var ids = new List<int>();
            if (!string.IsNullOrEmpty(_areaIds))
            {
                string[] tokens = _areaIds.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < tokens.Length; i++)
                {
                    if (int.TryParse(tokens[i], out int id) && id >= 0)
                    {
                        ids.Add(id);
                    }
                    else
                    {
                        invalid = invalid == null ? tokens[i] : $"{invalid}, {tokens[i]}";
                    }
                }
            }

            _parsedIds = ids.ToArray();
            validCount = _parsedIds.Length;
            return invalid;
        }

        public void RefreshLabel()
        {
            var names = new List<string>();
            IReadOnlyList<SpawnGroupData> candidates = Candidates;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i] != null)
                {
                    names.Add(candidates[i].DisplayName);
                }
            }

            string groupName = names.Count > 0 ? string.Join(" / ", names) : "(비어 있음)";
            int areaCount = AreaIds.Count;
            string areas = areaCount > 0 ? string.Join(",", _parsedIds) : "?";
            string repeat = areaCount > 1 ? $" ×{areaCount}" : string.Empty;
            _label = $"{_startTime}s · Area {areas} · {groupName}{repeat}";
        }

        private SpawnGroupData FirstCandidate()
        {
            IReadOnlyList<SpawnGroupData> candidates = Candidates;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i] != null)
                {
                    return candidates[i];
                }
            }

            return null;
        }
    }
}
