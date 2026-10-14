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
        public float Elapsed { get; private set; }
        // 폭탄 하나로 동시에 맞힌 최대 수. 예측이 잘 맞았는지 보는 지표다.
        public int BestMultiKill { get; private set; }

        public event Action<int> KillsChanged;

        private void OnEnable()
        {
            GameEvents.EnemyKilled += OnEnemyKilled;
            GameEvents.BombExploded += OnBombExploded;
            GameEvents.GameStateChanged += OnGameStateChanged;
            GameEvents.StageStarted += OnStageStarted;
            GameEvents.StageTimerStopped += OnStageTimerStopped;
        }

        private void OnDisable()
        {
            GameEvents.EnemyKilled -= OnEnemyKilled;
            GameEvents.BombExploded -= OnBombExploded;
            GameEvents.GameStateChanged -= OnGameStateChanged;
            GameEvents.StageStarted -= OnStageStarted;
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
        }

        private void OnGameStateChanged(GameState previous, GameState current)
        {
            _isPlaying = current == GameState.Playing;
        }
    }
}
