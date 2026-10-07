using UnityEngine;

namespace ExplodeIt.Stage
{
    // 정해진 자리에만 내려와야 하는 보스(제자리에 고정되는 화염 마법사 등)의 후보 자리. 자식 트랜스폼 하나하나가 후보다.
    // 보스 프리팹은 씬 오브젝트를 참조할 수 없으므로, 이쪽에서 어느 보스의 자리인지 가리키고 진행 쪽이 씬에서 찾는다.
    public class BossSpawnPoints : MonoBehaviour
    {
        [Tooltip("이 자리들을 쓰는 보스 프리팹")]
        [SerializeField] private GameObject _bossPrefab;

        public GameObject BossPrefab => _bossPrefab;

        // 플레이어와 최소 거리 이상 떨어진 후보 중 가장 가까운 곳. 모두 너무 가까우면 가장 먼 곳을 고른다.
        public bool TryPick(Vector2 player, float minDistance, out Vector2 point)
        {
            point = Vector2.zero;
            if (transform.childCount == 0)
            {
                return false;
            }

            float minSqr = minDistance * minDistance;
            float bestNear = float.PositiveInfinity;
            float bestFar = -1f;
            Vector2 near = Vector2.zero;
            Vector2 far = Vector2.zero;
            bool hasNear = false;
            for (int i = 0; i < transform.childCount; i++)
            {
                Vector2 candidate = transform.GetChild(i).position;
                float sqr = (candidate - player).sqrMagnitude;
                if (sqr >= minSqr && sqr < bestNear)
                {
                    bestNear = sqr;
                    near = candidate;
                    hasNear = true;
                }

                if (sqr > bestFar)
                {
                    bestFar = sqr;
                    far = candidate;
                }
            }

            point = hasNear ? near : far;
            return true;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.5f, 0.1f);
            for (int i = 0; i < transform.childCount; i++)
            {
                Gizmos.DrawWireSphere(transform.GetChild(i).position, 0.5f);
            }
        }
#endif
    }
}
