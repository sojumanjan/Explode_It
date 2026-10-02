using UnityEngine;

namespace ExplodeIt.Core
{
    // 전역 사건의 연출. 폭탄·적 코드가 연출을 몰라도 되게 이벤트만 듣고 재생한다.
    public class GameFeedback : MonoBehaviour
    {
        [SerializeField] private FeedbackData _bombExplode;
        // 예측이 맞아 여러 마리를 한 번에 잡았을 때 한 단계 더 큰 반응으로 보상한다.
        [SerializeField] private FeedbackData _bombMultiKill;
        [SerializeField, Min(2)] private int _multiKillThreshold = 3;
        [SerializeField] private FeedbackData _enemyKilled;
        [SerializeField] private FeedbackData _playerDied;
        // 이벤트 대신 자기 데이터로 연출을 내는 쪽(블랙홀 등)의 이펙트도 전투 전에 풀을 채워 둔다.
        [SerializeField] private FeedbackData[] _alsoPrepare;

        private Transform _player;

        private void OnEnable()
        {
            GameEvents.BombExploded += OnBombExploded;
            GameEvents.EnemyKilled += OnEnemyKilled;
            GameEvents.PlayerDied += OnPlayerDied;
        }

        private void OnDisable()
        {
            GameEvents.BombExploded -= OnBombExploded;
            GameEvents.EnemyKilled -= OnEnemyKilled;
            GameEvents.PlayerDied -= OnPlayerDied;
        }

        private void Start()
        {
            FeedbackPlayer.Prepare(_bombExplode);
            FeedbackPlayer.Prepare(_bombMultiKill);
            FeedbackPlayer.Prepare(_enemyKilled);
            FeedbackPlayer.Prepare(_playerDied);
            if (_alsoPrepare != null)
            {
                for (int i = 0; i < _alsoPrepare.Length; i++)
                {
                    FeedbackPlayer.Prepare(_alsoPrepare[i]);
                }
            }

            GameObject player = GameObject.FindWithTag("Player");
            _player = player != null ? player.transform : null;
        }

        private void OnBombExploded(Vector2 position, float radius, int hitCount)
        {
            FeedbackData data = hitCount >= _multiKillThreshold && _bombMultiKill != null ? _bombMultiKill : _bombExplode;
            FeedbackPlayer.Play(data, position);
        }

        private void OnEnemyKilled(Vector2 position)
        {
            FeedbackPlayer.Play(_enemyKilled, position);
        }

        private void OnPlayerDied()
        {
            FeedbackPlayer.Play(_playerDied, _player != null ? (Vector2)_player.position : Vector2.zero);
        }
    }
}
