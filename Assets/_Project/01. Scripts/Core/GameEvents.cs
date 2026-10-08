using System;
using UnityEngine;

namespace ExplodeIt.Core
{
    // 시스템끼리 직접 참조하지 않도록 전투·진행 이벤트를 한곳에서 발행한다.
    // 이벤트는 필요해지는 단계에서 하나씩 추가한다.
    public static class GameEvents
    {
        public static event Action<GameState, GameState> GameStateChanged;

        // 위치, 반경, 범위 안 대상 수. 다중 처치 업적·재화, 잔불 장판 등이 폭탄 코드를 몰라도 되게 한다.
        public static event Action<Vector2, float, int> BombExploded;
        public static event Action PlayerDied;
        // 처치 위치. 재화 드롭, 처치 수 업적, 이펙트가 구독한다.
        public static event Action<Vector2> EnemyKilled;
        // 현재 충전량(처치 수, 모든 칸 합계), 한 칸에 필요한 양, 칸 수. 게이지 UI가 능력 코드를 몰라도 되게 한다.
        public static event Action<int, int, int> AbilityChargeChanged;
        // 남은 폭탄 수, 최대 보유 수. HUD가 무기 코드를 몰라도 되게 한다.
        public static event Action<int, int> BombChargesChanged;
        // 폭탄이 손을 떠난 순간. 던지는 동작 연출이 무기 코드를 몰라도 되게 한다.
        public static event Action BombThrown;

        public static void RaiseBombThrown()
        {
            BombThrown?.Invoke();
        }

        public static void RaiseBombChargesChanged(int current, int max)
        {
            BombChargesChanged?.Invoke(current, max);
        }
        // 마지막 웨이브까지 다 나오고 적이 모두 처치된 순간. 상점, 포탈, 과열 구간이 여기서 시작한다.
        public static event Action StageCleared;
        // 몇 번째 웨이브(1부터)를 다 잡았는지. 업적·승천 조건이 구독한다.
        public static event Action<int> WaveCleared;
        // 몇 번째 보스전(1부터)이 시작되는지. 보스 등장 연출 UI가 구독한다.
        public static event Action<int> BossIntroStarted;

        // 연출 동안 플레이어 조작을 막고 푼다. 입력 쪽이 진행 흐름을 몰라도 되게 한다.
        public static event Action<bool> PlayerControlLockChanged;
        // 카메라가 바라볼 지점과 옮겨 가는 시간(초). 연출이 카메라 설정을 몰라도 되게 한다.
        public static event Action<Vector2, float> CameraFocusRequested;
        // 다시 플레이어를 따라가게 하고, 돌아가는 시간(초).
        public static event Action<float> CameraFocusReleased;

        public static void RaisePlayerControlLockChanged(bool isLocked)
        {
            PlayerControlLockChanged?.Invoke(isLocked);
        }

        public static void RaiseCameraFocusRequested(Vector2 point, float blendDuration)
        {
            CameraFocusRequested?.Invoke(point, blendDuration);
        }

        public static void RaiseCameraFocusReleased(float blendDuration)
        {
            CameraFocusReleased?.Invoke(blendDuration);
        }

        // 판이 시작될 때 한 번. 스토리 모드인지 알려 준다(왕 컷인 등 스토리 전용 연출이 구독한다).
        public static event Action<bool> StageStarted;

        // 폭탄(블랙홀 포함)이 착지해 범위가 정해진 순간. 위치, 반경. 폭탄을 보고 피하는 적이 구독한다.
        public static event Action<Vector2, float> BombLanded;

        public static void RaiseBombLanded(Vector2 position, float radius)
        {
            BombLanded?.Invoke(position, radius);
        }

        // 몇 번째 웨이브(1부터)가 시작됐는지.
        public static event Action<int> WaveStarted;

        public static void RaiseStageStarted(bool isStoryMode)
        {
            StageStarted?.Invoke(isStoryMode);
        }

        public static void RaiseWaveStarted(int waveNumber)
        {
            WaveStarted?.Invoke(waveNumber);
        }

        // 몇 번째 보스(1부터)를 잡았는지. 진행 흐름이 다음 웨이브로 넘어가고, 업적이 구독한다.
        public static event Action<int> BossDefeated;

        public static void RaiseBossDefeated(int bossNumber)
        {
            BossDefeated?.Invoke(bossNumber);
        }

        public static void RaiseWaveCleared(int waveNumber)
        {
            WaveCleared?.Invoke(waveNumber);
        }

        public static void RaiseBossIntroStarted(int bossNumber)
        {
            BossIntroStarted?.Invoke(bossNumber);
        }

        public static void RaiseStageCleared()
        {
            StageCleared?.Invoke();
        }

        public static void RaiseAbilityChargeChanged(int current, int perCharge, int maxCharges)
        {
            AbilityChargeChanged?.Invoke(current, perCharge, maxCharges);
        }

        public static void RaiseGameStateChanged(GameState previous, GameState current)
        {
            GameStateChanged?.Invoke(previous, current);
        }

        public static void RaiseBombExploded(Vector2 position, float radius, int hitCount)
        {
            BombExploded?.Invoke(position, radius, hitCount);
        }

        public static void RaisePlayerDied()
        {
            PlayerDied?.Invoke();
        }

        public static void RaiseEnemyKilled(Vector2 position)
        {
            EnemyKilled?.Invoke(position);
        }

        // 도메인 리로드를 끈 플레이모드 설정에서도 이전 세션의 구독자가 남지 않게 한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSubscribers()
        {
            GameStateChanged = null;
            BombExploded = null;
            PlayerDied = null;
            EnemyKilled = null;
            AbilityChargeChanged = null;
            BombChargesChanged = null;
            BombThrown = null;
            StageCleared = null;
            WaveCleared = null;
            BossIntroStarted = null;
            BossDefeated = null;
            PlayerControlLockChanged = null;
            CameraFocusRequested = null;
            CameraFocusReleased = null;
            StageStarted = null;
            BombLanded = null;
            WaveStarted = null;
        }
    }
}
