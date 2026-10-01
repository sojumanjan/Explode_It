using UnityEngine;

namespace ExplodeIt.Core
{
    // 전역 사건의 효과음. 폭탄 코드가 사운드를 몰라도 되게 이벤트만 듣고 재생한다.
    // 적의 공격, 블랙홀처럼 개별 행동의 소리는 그 행동의 데이터에 둔다.
    public class GameSfx : MonoBehaviour
    {
        [SerializeField] private SoundData _bombExplode;

        private void OnEnable()
        {
            GameEvents.BombExploded += OnBombExploded;
        }

        private void OnDisable()
        {
            GameEvents.BombExploded -= OnBombExploded;
        }

        private void OnBombExploded(Vector2 position, float radius, int hitCount)
        {
            AudioManager.Play(_bombExplode);
        }
    }
}
