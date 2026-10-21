using UnityEngine;

namespace ExplodeIt.Core
{
    // 이번 판에 건 저주. 판이 시작될 때 장비 적용기가 한 번 채우고, 각 시스템은 여기서 읽기만 한다.
    // 저주는 무한 모드에만 걸린다. 스토리 모드에서는 늘 비어 있다.
    public static class RunCurses
    {
        private static readonly bool[] _active = new bool[System.Enum.GetValues(typeof(CurseKind)).Length];

        // 걸린 저주 수. 마지막 칸(왕)은 저주가 아니라서 세지 않는다.
        public static int Count { get; private set; }

        // 0보다 크면 체력을 이 칸 수로 고정한다(아티팩트 팬던트 무시). 저주 "한 방".
        public static int HealthOverride;
        // 무한 모드 적 풀의 모든 적이 처음부터 최대 가중치로 나온다. 저주 "총공세".
        public static bool EnemiesAtMaxWeight;
        // 군집 하나가 구역 대신 플레이어 근처에서 나올 확률(0~1)과 거리 범위(유닛), 나오기 전 표시 시간(초). 저주 "기습".
        public static float AmbushChance;
        public static float AmbushMinRadius;
        public static float AmbushMaxRadius;
        public static float AmbushWarning;
        // 저주를 9개 모두 건 판인지. 기록을 따로 남긴다.
        public static bool HasAll;
        // 연출(보스 등장 대화 등)로 조작이 막힌 동안 저주 시간이 멈춘다. 예고 원·떨어지는 운석·덫·바람이 모두 그 자리에 선다.
        public static bool IsFrozen;
        // 저주 연출이 쓰는 트윈 표시. 멈출 때 이 표시가 붙은 트윈만 함께 멈춘다.
        public const string TweenId = "Curse";

        // 저주가 쓰는 프레임 시간. 멈춘 동안은 0이다.
        public static float DeltaTime => IsFrozen ? 0f : Time.deltaTime;

        // 플레이어 이동속도 배율. 늪(저주 "수렁")에 들어가 있는 동안만 1보다 작아진다. 매 프레임 늪 쪽이 갱신한다.
        public static float PlayerMoveMultiplier = 1f;

        public static bool Has(CurseKind kind)
        {
            return _active[(int)kind];
        }

        public static void Add(CurseKind kind)
        {
            if (_active[(int)kind])
            {
                return;
            }

            _active[(int)kind] = true;
            if (kind != CurseKind.King)
            {
                Count++;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            System.Array.Clear(_active, 0, _active.Length);
            Count = 0;
            HealthOverride = 0;
            EnemiesAtMaxWeight = false;
            AmbushChance = 0f;
            AmbushMinRadius = 0f;
            AmbushMaxRadius = 0f;
            AmbushWarning = 0f;
            PlayerMoveMultiplier = 1f;
            HasAll = false;
            IsFrozen = false;
        }
    }
}
