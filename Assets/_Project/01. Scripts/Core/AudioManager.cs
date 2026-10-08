using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace ExplodeIt.Core
{
    // 효과음은 미리 만든 AudioSource를 돌려 쓰고, 배경음은 두 소스를 교차해 바꾼다.
    // 재시작마다 씬을 다시 불러오므로, 배경음이 끊기지 않게 첫 인스턴스만 씬을 넘어 살아남는다.
    // 씬마다 하나씩 두어도 되고, 그중 처음 것만 남는다. 어느 씬에서 시작해도 소리가 난다.
    public class AudioManager : MonoBehaviour
    {
        private const string SfxVolumeKey = "Audio.SfxVolume";
        private const string BgmVolumeKey = "Audio.BgmVolume";

        private static AudioManager _current;

        // 동시에 울릴 수 있는 효과음 수. 소리마다 동시 재생 상한이 따로 있어 이 수를 넘기는 일은 드물다.
        [SerializeField, Min(1)] private int _sfxVoices = 24;

        private AudioSource[] _voices;
        private SoundData[] _voiceData;
        private int[] _voicePlayIds;
        private int _nextPlayId = 1;
        private int _nextVoice;
        private readonly Dictionary<SoundData, float> _lastPlayTimes = new Dictionary<SoundData, float>();

        private AudioSource _bgmFront;
        private AudioSource _bgmBack;
        private BgmData _currentBgm;
        private float _sfxVolume = 1f;
        private float _bgmVolume = 1f;

        public static float SfxVolume
        {
            get => _current != null ? _current._sfxVolume : 1f;
            set
            {
                if (_current == null)
                {
                    return;
                }

                _current._sfxVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(SfxVolumeKey, _current._sfxVolume);
            }
        }

        public static float BgmVolume
        {
            get => _current != null ? _current._bgmVolume : 1f;
            set
            {
                if (_current == null)
                {
                    return;
                }

                _current._bgmVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(BgmVolumeKey, _current._bgmVolume);
                _current.ApplyBgmVolume();
            }
        }

        // 도메인 리로드를 끈 플레이모드 설정에서도 이전 세션의 인스턴스를 가리키지 않게 한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            _current = null;
        }

        private void Awake()
        {
            if (_current != null && _current != this)
            {
                Destroy(gameObject);
                return;
            }

            _current = this;
            DontDestroyOnLoad(gameObject);

            _sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
            _bgmVolume = PlayerPrefs.GetFloat(BgmVolumeKey, 1f);

            _voices = new AudioSource[_sfxVoices];
            _voiceData = new SoundData[_sfxVoices];
            _voicePlayIds = new int[_sfxVoices];
            for (int i = 0; i < _sfxVoices; i++)
            {
                _voices[i] = CreateSource("Sfx");
            }

            _bgmFront = CreateSource("Bgm");
            _bgmBack = CreateSource("Bgm");
            _bgmFront.loop = true;
            _bgmBack.loop = true;
        }

        // 효과음을 낼 쪽은 매니저를 찾지 않고 이 정적 함수만 부른다. 매니저가 없거나 소리가 비어 있으면 조용히 넘어간다.
        // 도중에 끊어야 하는 소리(블랙홀 흡입음 등)는 돌려받은 표를 Stop에 넘긴다.
        public static SoundHandle Play(SoundData sound)
        {
            if (_current != null && sound != null)
            {
                return _current.PlaySfx(sound, sound.PickPitch());
            }

            return SoundHandle.None;
        }

        // 같은 소리를 쓰는 쪽마다 높낮이를 달리할 때(무기별 폭발음 등) 소리 데이터의 피치 대신 이 피치로 낸다.
        public static SoundHandle Play(SoundData sound, float pitch)
        {
            if (_current != null && sound != null)
            {
                return _current.PlaySfx(sound, pitch);
            }

            return SoundHandle.None;
        }

        public static void Stop(SoundHandle handle)
        {
            // 재생 번호는 1부터 매기므로, 0인 표는 기본값으로 만들어진 빈 표다.
            if (_current == null || handle.Voice < 0 || handle.PlayId <= 0)
            {
                return;
            }

            if (_current._voicePlayIds[handle.Voice] == handle.PlayId)
            {
                _current._voices[handle.Voice].Stop();
            }
        }

        public static void PlayBgm(BgmData bgm)
        {
            if (_current != null && bgm != null)
            {
                _current.SwitchBgm(bgm);
            }
        }

        private SoundHandle PlaySfx(SoundData sound, float pitch)
        {
            if (!sound.HasClip)
            {
                return SoundHandle.None;
            }

            // 일시정지 중 버튼 소리 등도 막히지 않게 실제 시간으로 잰다.
            float now = Time.unscaledTime;
            if (_lastPlayTimes.TryGetValue(sound, out float last) && now - last < sound.MinInterval)
            {
                return SoundHandle.None;
            }

            if (CountPlaying(sound) >= sound.MaxInstances)
            {
                return SoundHandle.None;
            }

            AudioSource voice = FindFreeVoice(out int index);
            if (voice == null)
            {
                return SoundHandle.None;
            }

            voice.clip = sound.PickClip();
            voice.volume = sound.Volume * _sfxVolume;
            voice.pitch = pitch;
            voice.Play();
            _voiceData[index] = sound;
            _lastPlayTimes[sound] = now;

            int playId = _nextPlayId++;
            _voicePlayIds[index] = playId;
            return new SoundHandle(index, playId);
        }

        private int CountPlaying(SoundData sound)
        {
            int count = 0;
            for (int i = 0; i < _voices.Length; i++)
            {
                if (_voiceData[i] == sound && _voices[i].isPlaying)
                {
                    count++;
                }
            }

            return count;
        }

        // 다 쓰고 있으면 새 소리를 버린다. 이미 울리는 소리를 끊는 것보다 덜 거슬린다.
        private AudioSource FindFreeVoice(out int index)
        {
            for (int i = 0; i < _voices.Length; i++)
            {
                index = (_nextVoice + i) % _voices.Length;
                if (!_voices[index].isPlaying)
                {
                    _nextVoice = (index + 1) % _voices.Length;
                    return _voices[index];
                }
            }

            index = -1;
            return null;
        }

        // 재시작으로 같은 씬이 다시 불러와져도 같은 곡이면 처음부터 다시 틀지 않는다.
        private void SwitchBgm(BgmData bgm)
        {
            if (bgm == _currentBgm || bgm.Clip == null)
            {
                return;
            }

            _currentBgm = bgm;
            (_bgmFront, _bgmBack) = (_bgmBack, _bgmFront);

            _bgmFront.clip = bgm.Clip;
            _bgmFront.volume = 0f;
            _bgmFront.Play();

            float duration = bgm.FadeDuration;
            AudioSource front = _bgmFront;
            AudioSource back = _bgmBack;
            front.DOKill();
            back.DOKill();
            // 일시정지 화면에서 타이틀로 가도 전환이 멈추지 않게 실제 시간으로 바꾼다.
            DOTween.To(() => front.volume, v => front.volume = v, bgm.Volume * _bgmVolume, duration)
                .SetTarget(front).SetUpdate(true);
            DOTween.To(() => back.volume, v => back.volume = v, 0f, duration)
                .SetTarget(back).SetUpdate(true)
                .OnComplete(back.Stop);
        }

        private void ApplyBgmVolume()
        {
            if (_currentBgm != null)
            {
                _bgmFront.DOKill();
                _bgmFront.volume = _currentBgm.Volume * _bgmVolume;
            }
        }

        // 탑다운 시점이라 거리에 따라 작아지지 않는 2D 소리로 낸다.
        private AudioSource CreateSource(string label)
        {
            var source = new GameObject(label).AddComponent<AudioSource>();
            source.transform.SetParent(transform, false);
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }
    }
}
