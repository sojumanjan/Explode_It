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
        // 적이 직접 때린 공격인지와 그 적의 부류. 플레이어가 무엇에 쓰러졌는지 업적이 알 수 있게 한다.
        public readonly bool IsFromEnemy;
        public readonly EnemyKind Attacker;

        public HitInfo(Vector2 sourcePosition, bool ignoresGuard = false, DamageSource source = DamageSource.None)
        {
            SourcePosition = sourcePosition;
            IgnoresGuard = ignoresGuard;
            Source = source;
            IsFromEnemy = false;
            Attacker = EnemyKind.Normal;
        }

        private HitInfo(Vector2 sourcePosition, EnemyKind attacker)
        {
            SourcePosition = sourcePosition;
            IgnoresGuard = false;
            Source = DamageSource.None;
            IsFromEnemy = true;
            Attacker = attacker;
        }

        public static HitInfo FromEnemy(Vector2 sourcePosition, EnemyKind attacker)
        {
            return new HitInfo(sourcePosition, attacker);
        }
    }
}
