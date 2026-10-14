using UnityEngine;

namespace ExplodeIt.Core
{
    // 전역 사건의 효과음. 폭탄 코드가 사운드를 몰라도 되게 이벤트만 듣고 재생한다.
    // 적의 공격, 블랙홀처럼 개별 행동의 소리는 그 행동의 데이터에 둔다.
    public class GameSfx : MonoBehaviour
    {
        [SerializeField] private SoundData _bombExplode;

        // 장착한 무기가 정한 폭발음 피치 범위. 무기가 알려 오기 전에는 소리 데이터의 피치를 쓴다.
        private bool _hasWeaponPitch;
        private float _pitchMin;
        private float _pitchMax;
        // 폭발음을 빼라고 알려 온 프레임. 같은 프레임의 폭발음만 뺀다.
        private int _suppressedFrame = -1;

        private void OnEnable()
        {
            GameEvents.BombExploded += OnBombExploded;
            GameEvents.ExplosionPitchChanged += OnExplosionPitchChanged;
            GameEvents.ExplosionSoundSuppressed += OnExplosionSoundSuppressed;
        }

        private void OnDisable()
        {
            GameEvents.BombExploded -= OnBombExploded;
            GameEvents.ExplosionPitchChanged -= OnExplosionPitchChanged;
            GameEvents.ExplosionSoundSuppressed -= OnExplosionSoundSuppressed;
        }

        private void OnExplosionSoundSuppressed()
        {
            _suppressedFrame = Time.frameCount;
        }

        private void OnExplosionPitchChanged(float min, float max)
        {
            _hasWeaponPitch = true;
            _pitchMin = min;
            _pitchMax = max;
        }

        private void OnBombExploded(Vector2 position, float radius, int hitCount)
        {
            if (_suppressedFrame == Time.frameCount)
            {
                return;
            }

            if (_hasWeaponPitch)
            {
                AudioManager.Play(_bombExplode, Random.Range(_pitchMin, _pitchMax));
                return;
            }

            AudioManager.Play(_bombExplode);
        }
    }
}
