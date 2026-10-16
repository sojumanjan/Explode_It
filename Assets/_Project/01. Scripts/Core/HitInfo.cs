using UnityEngine;

namespace ExplodeIt.Core
{
    public readonly struct HitInfo
    {
        public readonly Vector2 SourcePosition;
        // 방패 같은 피격 가드를 무시하는 공격인지. 블랙홀은 적을 가운데로 모아 터지므로 방패 정면이 되기 쉬워 관통시킨다.
        public readonly bool IgnoresGuard;
        // 무엇에 맞았는지. 처치 이벤트가 출처를 함께 알린다.
        public readonly DamageSource Source;

        public HitInfo(Vector2 sourcePosition, bool ignoresGuard = false, DamageSource source = DamageSource.None)
        {
            SourcePosition = sourcePosition;
            IgnoresGuard = ignoresGuard;
            Source = source;
        }
    }
}
