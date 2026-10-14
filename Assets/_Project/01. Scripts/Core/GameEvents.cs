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
        // 장착한 무기의 폭발음 피치 범위(최소, 최대). 무기를 바꿀 때마다 발행해, 효과음 쪽이 무기 코드를 몰라도 되게 한다.
        public static event Action<float, float> ExplosionPitchChanged;
        // 이번 프레임의 폭탄 폭발음을 내지 않는다. 맞은 쪽이 더 큰 자기 소리로 대신할 때(최종 보스 피격) 쓴다.
        // 폭발 판정이 폭발 이벤트보다 먼저 일어나므로, 맞는 순간 이걸 알리면 같은 프레임의 폭발음이 빠진다.
        public static event Action ExplosionSoundSuppressed;

        public static void RaiseExplosionSoundSuppressed()
        {
            ExplosionSoundSuppressed?.Invoke();
        }

        public static void RaiseExplosionPitchChanged(float min, float max)
        {
            ExplosionPitchChanged?.Invoke(min, max);
        }

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
        // 카메라 확대 배율(1 = 평소, 0.7 = 화면이 0.7배 크기로 좁아져 가까이 보임)과 바뀌는 시간(초).
        public static event Action<float, float> CameraZoomRequested;
        // 싸우는 도중 보스가 외치는 대사와 띄워 둘 시간(초). 클릭 없이 시간이 지나면 사라진다.
        public static event Action<string, float> BossLineRequested;
        // 처치 대화가 끝나 쓰러진 채 남아 있던 보스가 사라질 차례. 대화 동안 몸을 남겨 두는 보스(최종 보스)가 듣는다.
        public static event Action BossFarewellRequested;
        // 연출 중 플레이어를 정한 자리까지 저절로 걷게 한다(조작이 잠긴 동안). 도착하면 저절로 멈춘다.
        public static event Action<Vector2> PlayerWalkRequested;
        // 저절로 걷던 플레이어를 그 자리에 세운다(막혀서 못 가는 경우 등).
        public static event Action PlayerWalkCancelled;

        public static void RaisePlayerWalkRequested(Vector2 target)
        {
            PlayerWalkRequested?.Invoke(target);
        }

        public static void RaisePlayerWalkCancelled()
        {
            PlayerWalkCancelled?.Invoke();
        }

        // 판 시간을 멈춘다. 최종 보스를 쓰러뜨린 순간 기록 시간이 처치 대화·엔딩 동안 늘지 않게 한다.
        public static event Action StageTimerStopped;

        public static void RaiseStageTimerStopped()
        {
            StageTimerStopped?.Invoke();
        }

        // 우측 하단 컷인에 왕 대신 고블린이 나와 혼잣말로 힌트를 준다. 글과 떠 있는 시간(초, 실제 시간).
        public static event Action<string, float> PlayerHintRequested;

        public static void RaisePlayerHintRequested(string text, float hold)
        {
            PlayerHintRequested?.Invoke(text, hold);
        }

        // 플레이어가 대시(구르기)를 시작했다. 튜토리얼이 대시 과제를 확인하는 데 쓴다.
        public static event Action PlayerDodged;

        public static void RaisePlayerDodged()
        {
            PlayerDodged?.Invoke();
        }

        // 플레이어가 특수 능력(블랙홀)을 썼다. 튜토리얼이 스킬 안내를 거두는 데 쓴다.
        public static event Action AbilityUsed;

        public static void RaiseAbilityUsed()
        {
            AbilityUsed?.Invoke();
        }

        // 던진 특수 능력(블랙홀)이 터지고 사라졌다. 튜토리얼이 다음 설명으로 넘어가는 데 쓴다.
        public static event Action AbilityFinished;

        public static void RaiseAbilityFinished()
        {
            AbilityFinished?.Invoke();
        }

        // 플레이어 그림이 커서 대신 이 지점을 바라보게 한다(처치 대화 동안 쓰러진 보스 쪽). 조작이 돌아오면 다시 커서를 본다.
        public static event Action<Vector2> PlayerLookRequested;

        public static void RaisePlayerLookRequested(Vector2 point)
        {
            PlayerLookRequested?.Invoke(point);
        }
        // 처치 대화 줄이 쓰러진 보스의 그림을 바꾸라고 알린다(무릎 꿇기 등).
        public static event Action<Sprite> BossPoseRequested;
        // 처치 대화 줄이 쓰러진 각성 왕을 2등신으로 되돌리라고 알린다. 보스는 펑 하고 사라지고, 대화가 2등신 왕을 그 자리에 띄운다.
        public static event Action BossRevertRequested;

        public static void RaiseBossPoseRequested(Sprite sprite)
        {
            BossPoseRequested?.Invoke(sprite);
        }

        public static void RaiseBossRevertRequested()
        {
            BossRevertRequested?.Invoke();
        }

        public static void RaiseBossFarewellRequested()
        {
            BossFarewellRequested?.Invoke();
        }

        public static void RaiseBossLineRequested(string text, float duration)
        {
            BossLineRequested?.Invoke(text, duration);
        }

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

        public static void RaiseCameraZoomRequested(float zoom, float blendDuration)
        {
            CameraZoomRequested?.Invoke(zoom, blendDuration);
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
            ExplosionPitchChanged = null;
            BombThrown = null;
            StageCleared = null;
            WaveCleared = null;
            BossIntroStarted = null;
            BossDefeated = null;
            PlayerControlLockChanged = null;
            CameraFocusRequested = null;
            CameraFocusReleased = null;
            CameraZoomRequested = null;
            StageStarted = null;
            BombLanded = null;
            WaveStarted = null;
        }
    }
}
