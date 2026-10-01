using UnityEngine;

namespace ExplodeIt.Core
{
    // 효과음 하나. 같은 소리가 한 프레임에 수십 번 요청되는 게임이라(블랙홀 한 번에 20마리 처치)
    // 겹침 상한과 최소 간격을 소리마다 정한다.
    [CreateAssetMenu(fileName = "Sfx_", menuName = "Explode It/Audio/Sound Data")]
    public class SoundData : ScriptableObject
    {
        [Tooltip("클립 목록. 재생할 때마다 하나를 무작위로 골라 같은 소리의 반복을 덜 거슬리게 한다")]
        [SerializeField] private AudioClip[] _clips;

        [Tooltip("볼륨 (0~1). 효과음 전체 볼륨과 곱해진다")]
        [SerializeField, Range(0f, 1f)] private float _volume = 1f;

        [Tooltip("최소 피치. 최소~최대 사이에서 무작위로 정해 반복음에 변화를 준다. 1이면 원음")]
        [SerializeField, Range(0.5f, 2f)] private float _pitchMin = 0.95f;

        [Tooltip("최대 피치")]
        [SerializeField, Range(0.5f, 2f)] private float _pitchMax = 1.05f;

        [Tooltip("동시 재생 상한 (개). 이보다 많이 울리고 있으면 새 요청을 무시한다")]
        [SerializeField, Min(1)] private int _maxInstances = 4;

        [Tooltip("최소 재생 간격 (초). 직전 재생 후 이 시간 안의 요청은 무시한다. 같은 순간 여러 번 요청돼도 한 번만 들리게 한다")]
        [SerializeField, Min(0f)] private float _minInterval = 0.03f;

        public float Volume => _volume;
        public int MaxInstances => _maxInstances;
        public float MinInterval => _minInterval;

        public bool HasClip => _clips != null && _clips.Length > 0;

        public AudioClip PickClip()
        {
            return _clips[Random.Range(0, _clips.Length)];
        }

        public float PickPitch()
        {
            return Random.Range(_pitchMin, _pitchMax);
        }

        private void OnValidate()
        {
            if (_pitchMax < _pitchMin)
            {
                _pitchMax = _pitchMin;
            }
        }
    }
}
