using UnityEngine;

namespace ExplodeIt.Core
{
    [CreateAssetMenu(fileName = "Bgm_", menuName = "Explode It/Audio/Bgm Data")]
    public class BgmData : ScriptableObject
    {
        [Tooltip("곡")]
        [SerializeField] private AudioClip _clip;

        [Tooltip("끝나면 처음부터 다시 튼다. 크레딧처럼 화면에 길이를 맞춘 곡은 끈다")]
        [SerializeField] private bool _loop = true;

        [Tooltip("볼륨 (0~1). 배경음 전체 볼륨과 곱해진다")]
        [SerializeField, Range(0f, 1f)] private float _volume = 0.6f;

        [Tooltip("전환 시간 (초). 이전 곡이 줄어들고 이 곡이 커지는 데 걸리는 시간")]
        [SerializeField, Min(0f)] private float _fadeDuration = 1f;

        public AudioClip Clip => _clip;
        public bool Loop => _loop;
        public float Volume => _volume;
        public float FadeDuration => _fadeDuration;
    }
}
