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
        // 몰아서 크게 잡았을 때 확 멈춰서 "몰아죽였다"를 손맛으로 준다. 맞은 수가 아니라 처치 수라 연쇄 처치 숫자 팝업과 같이 뜬다.
        [Tooltip("한 폭탄 처치 수가 이 이상이면 쓰는 연출. 비워 두면 위 단계까지만 쓴다")]
        [SerializeField] private FeedbackData _bombBigKill;
        [Tooltip("큰 연출을 쓸 한 폭탄 처치 수 (마리)")]
        [SerializeField, Min(2)] private int _bigKillThreshold = 5;
        [SerializeField] private FeedbackData _enemyKilled;
        [SerializeField] private FeedbackData _playerDied;
        // 이벤트 대신 자기 데이터로 연출을 내는 쪽(블랙홀 등)의 이펙트도 전투 전에 풀을 채워 둔다.
        [SerializeField] private FeedbackData[] _alsoPrepare;

        private Transform _player;
        // 처치 알림은 같은 프레임 폭발 알림보다 먼저 온다. 그 사이 처치 수를 모아 두었다가 폭발 알림에서 꺼낸다.
        private int _pendingKills;
        private int _pendingFrame = -1;

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
            FeedbackPlayer.Prepare(_bombBigKill);
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
            int kills = _pendingFrame == Time.frameCount ? _pendingKills : 0;
            _pendingKills = 0;

            FeedbackData data = hitCount >= _multiKillThreshold && _bombMultiKill != null ? _bombMultiKill : _bombExplode;
            if (kills >= _bigKillThreshold && _bombBigKill != null)
            {
                data = _bombBigKill;
            }

            FeedbackPlayer.Play(data, position);
        }

        // 프레임이 바뀌면 지난 처치는 이 폭발과 상관없다(저주 낙뢰 등).
        private void OnEnemyKilled(Vector2 position)
        {
            if (_pendingFrame != Time.frameCount)
            {
                _pendingFrame = Time.frameCount;
                _pendingKills = 0;
            }

            _pendingKills++;
            FeedbackPlayer.Play(_enemyKilled, position);
        }

        private void OnPlayerDied()
        {
            FeedbackPlayer.Play(_playerDied, _player != null ? (Vector2)_player.position : Vector2.zero);
        }
    }
}
