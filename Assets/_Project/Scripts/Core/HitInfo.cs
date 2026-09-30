using UnityEngine;

namespace ExplodeIt.Core
{
    public readonly struct HitInfo
    {
        public readonly Vector2 SourcePosition;

        public HitInfo(Vector2 sourcePosition)
        {
            SourcePosition = sourcePosition;
        }
    }
}
