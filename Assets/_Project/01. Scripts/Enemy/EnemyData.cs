using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 모든 적이 공유하는 수치. 적 고유 수치는 하위 클래스에 둔다.
    public abstract class EnemyData : ScriptableObject
    {
        [Header("공통")]
        [Tooltip("이동 상태에서의 속도 (유닛/초)")]
        [SerializeField, Min(0f)] private float _moveSpeed = 2f;

        [Tooltip("죽기까지 필요한 피격 횟수. 기본 1, 골렘처럼 단단한 적만 올린다")]
        [SerializeField, Min(1)] private int _hitsToDie = 1;

        [Tooltip("공격 시작 거리 (유닛). 플레이어가 이 안에 있고 보이면 예고를 시작한다")]
        [SerializeField, Min(0f)] private float _attackTriggerRange = 4f;

        // 맵 밖(화면 밖)에서 스폰되므로, 보이지 않는 곳에서 공격하지 못하게 한다.
        [Tooltip("등장 유예 (초). 맵 안에 들어온 뒤 이 시간만큼 걸어야 공격할 수 있다")]
        [SerializeField, Min(0f)] private float _arenaEntryDelay = 1f;

        // 처치 수는 웨이브 100마리 주기와 승천 조건의 기준이라, 보스처럼 그 주기 밖의 적은 세지 않는다.
        [Tooltip("처치 수 포함. 끄면 잡아도 처치 수·능력 게이지에 들어가지 않는다 (보스용)")]
        [SerializeField] private bool _countsAsKill = true;

        [Header("적끼리 밀어내기")]
        [Tooltip("밀어내기 시작 거리 (반경 합 대비 비율). 두 적 중심이 서로 반경 합 × 이 값보다 가까우면 밀어낸다. 0.6이면 반경 합의 40%까지는 겹칠 수 있다")]
        [SerializeField, Range(0f, 1f)] private float _separationDistanceRatio = 0.6f;

        [Tooltip("밀어내는 최대 속도 (유닛/초). 낮을수록 살짝살짝 밀린다. 0이면 밀어내지 않는다")]
        [SerializeField, Min(0f)] private float _separationSpeed = 1.5f;

        [Header("예고 / 회복")]
        // 예고 없는 공격은 버그로 취급하므로 0이 되지 않게 막는다. 하드모드에서도 이 아래로는 줄이지 않는다.
        [Tooltip("예고 시간 (초). 멈춰서 예고하는 시간. 궁수류는 이 동안 조준이 플레이어를 따라가고, 돌격병·전사는 예고 시작 방향으로 고정된다. 최소 0.1")]
        [SerializeField, Min(0.1f)] private float _telegraphDuration = 0.6f;

        [Tooltip("회복 시간 (초). 공격 후 멈춰 있는 시간. 플레이어가 반격할 틈이 된다")]
        [SerializeField, Min(0f)] private float _recoverDuration = 0.6f;

        [Header("사망")]
        // 연출이 끝날 때까지 풀로 돌려보내지 않는다. 판정은 죽는 순간 바로 꺼지므로 길어도 플레이에는 영향이 없다.
        [Tooltip("사망 연출 시간 (초). 밀려남 → 부풀기 → 줄어들기가 이 시간 안에 끝난다")]
        [SerializeField, Min(0.05f)] private float _deathDuration = 0.5f;

        public float MoveSpeed => _moveSpeed;
        public int HitsToDie => _hitsToDie;
        public float AttackTriggerRange => _attackTriggerRange;
        public float ArenaEntryDelay => _arenaEntryDelay;
        public bool CountsAsKill => _countsAsKill;
        public float TelegraphDuration => _telegraphDuration;
        public float RecoverDuration => _recoverDuration;
        public float DeathDuration => _deathDuration;
        public float SeparationDistanceRatio => _separationDistanceRatio;
        public float SeparationSpeed => _separationSpeed;
    }
}
