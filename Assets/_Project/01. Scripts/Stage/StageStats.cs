using System;
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 이번 판 기록. HUD와 결과 화면이 읽고, 재미 검증 때 감을 숫자로 남기는 데도 쓴다.
    public class StageStats : MonoBehaviour
    {
        private bool _isPlaying;
        // 판이 시작됐는지. 첫 판 오프닝 동안은 상태가 Playing이어도 시간을 세지 않는다.
        private bool _hasStarted;

        public int Kills { get; private set; }
        // 스토리를 중간부터 시작했을 때 건너뛴 웨이브만큼 잡은 것으로 친 수. 이번 판에 실제로 잡은 수는 Kills에서 이 값을 뺀 것이다.
        public int StartKills { get; private set; }
        public bool IsResumed { get; private set; }
        public float Elapsed { get; private set; }
        // 폭탄 하나로 동시에 맞힌 최대 수. 예측이 잘 맞았는지 보는 지표다.
        public int BestMultiKill { get; private set; }
        // 직접 던진 폭탄(일반·순간폭탄) 수와 그중 한 마리라도 맞힌 수. 스킬 폭탄은 세지 않는다(빗나감 업적과 같은 기준).
        public int Throws { get; private set; }
        public int HitThrows { get; private set; }
        // 맞힌 비율 (0~1). 던진 적이 없으면 0.
        public float Accuracy => Throws > 0 ? (float)HitThrows / Throws : 0f;
        // 플레이어를 쓰러뜨린 적. 모르면 null.
        public IKillerProfile Killer { get; private set; }

        // 직접 던진 폭탄이 터졌다는 알림이 바로 뒤 폭발 알림과 짝을 이룬다.
        private bool _isThrownExplosion;

        public event Action<int> KillsChanged;

        private void OnEnable()
        {
            GameEvents.EnemyKilled += OnEnemyKilled;
            GameEvents.BombExploded += OnBombExploded;
            GameEvents.ThrownBombExploded += OnThrownBombExploded;
            GameEvents.PlayerKillerKnown += OnPlayerKillerKnown;
            GameEvents.GameStateChanged += OnGameStateChanged;
            GameEvents.StageStarted += OnStageStarted;
            GameEvents.StoryResumed += OnStoryResumed;
            GameEvents.StageTimerStopped += OnStageTimerStopped;
        }

        private void OnDisable()
        {
            GameEvents.EnemyKilled -= OnEnemyKilled;
            GameEvents.BombExploded -= OnBombExploded;
            GameEvents.ThrownBombExploded -= OnThrownBombExploded;
            GameEvents.PlayerKillerKnown -= OnPlayerKillerKnown;
            GameEvents.GameStateChanged -= OnGameStateChanged;
            GameEvents.StageStarted -= OnStageStarted;
            GameEvents.StoryResumed -= OnStoryResumed;
            GameEvents.StageTimerStopped -= OnStageTimerStopped;
        }

        // 최종 보스를 쓰러뜨린 순간의 시간이 이번 판 기록이다.
        private void OnStageTimerStopped()
        {
            _hasStarted = false;
        }

        private void OnStageStarted(bool isStoryMode)
        {
            _hasStarted = true;
        }

        private void OnStoryResumed(int checkpoint, int startKills)
        {
            IsResumed = true;
            StartKills = startKills;
            Kills = startKills;
            KillsChanged?.Invoke(Kills);
        }

        // 일시정지·사망·클리어 동안에는 시간을 세지 않는다.
        private void Update()
        {
            if (_isPlaying && _hasStarted)
            {
                Elapsed += Time.deltaTime;
            }
        }

        private void OnEnemyKilled(Vector2 position)
        {
            Kills++;
            KillsChanged?.Invoke(Kills);
        }

        private void OnBombExploded(Vector2 position, float radius, int hitCount)
        {
            BestMultiKill = Mathf.Max(BestMultiKill, hitCount);
            if (_isThrownExplosion)
            {
                _isThrownExplosion = false;
                Throws++;
                if (hitCount > 0)
                {
                    HitThrows++;
                }
            }
        }

        private void OnThrownBombExploded()
        {
            _isThrownExplosion = true;
        }

        private void OnPlayerKillerKnown(IKillerProfile killer)
        {
            Killer = killer;
        }

        private void OnGameStateChanged(GameState previous, GameState current)
        {
            _isPlaying = current == GameState.Playing;
        }
    }
}
