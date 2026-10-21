using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Progression
{
    // 저주 "한 방": 체력을 정해진 칸 수로 고정한다. 아티팩트 팬던트의 추가 칸도 무시한다.
    // 체력 규칙(스토리·무한 2칸, 팬던트만 +1)의 유일한 예외다.
    [CreateAssetMenu(fileName = "Curse_OneHit", menuName = "Explode It/Curses/One Hit")]
    public class OneHitCurseData : CurseData
    {
        [Header("한 방")]
        [Tooltip("이 저주가 걸린 판의 체력 (칸)")]
        [SerializeField, Min(1)] private int _health = 1;

        public override CurseKind Kind => CurseKind.OneHit;

        public override void ApplyRunModifiers()
        {
            RunCurses.HealthOverride = _health;
        }
    }
}
