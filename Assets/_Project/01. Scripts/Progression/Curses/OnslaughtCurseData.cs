using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Progression
{
    // 저주 "총공세": 적이 더 빨리 몰려오고, 무한 모드 적 풀의 모든 적이 처음부터 가장 자주 나온다.
    [CreateAssetMenu(fileName = "Curse_Onslaught", menuName = "Explode It/Curses/Onslaught")]
    public class OnslaughtCurseData : CurseData
    {
        [Header("총공세")]
        [Tooltip("웨이브 스폰 간격 배율 (0.75 = 25% 빨리). 아티팩트 팬던트와 곱해진다")]
        [SerializeField, Range(0.1f, 1f)] private float _spawnIntervalMultiplier = 0.75f;

        [Tooltip("켜면 처치 수와 상관없이 모든 적이 처음부터 적 풀의 가중치 최대로 나온다")]
        [SerializeField] private bool _allEnemiesAtMaxWeight = true;

        public override CurseKind Kind => CurseKind.Onslaught;

        public override void ApplyRunModifiers()
        {
            RunModifiers.SpawnIntervalMultiplier *= _spawnIntervalMultiplier;
            RunCurses.EnemiesAtMaxWeight = _allEnemiesAtMaxWeight;
        }
    }
}
