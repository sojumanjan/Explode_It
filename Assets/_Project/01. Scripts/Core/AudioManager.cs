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
        // 슬로모션이 끝나 효과음 높낮이가 되돌아가는 빠르기 (배율/초, 실제 시간).
        private const float PitchRecoverSpeed = 4f;

        private static AudioManager _current;

        // 동시에 울릴 수 있는 효과음 수. 소리마다 동시 재생 상한이 따로 있어 이 수를 넘기는 일은 드물다.
        [SerializeField, Min(1)] private int _sfxVoices = 24;

        private AudioSource[] _voices;
        private SoundData[] _voiceData;
        private int[] _voicePlayIds;
        // 소리마다 처음 정한 높낮이. 슬로모션 배율을 바꿀 때 이 값에 곱한다.
        private float[] _voiceBasePitches;
        private float _sfxPitchScale = 1f;
        // 소리마다 서서히 줄여 끊는 중인지: 시작한 실제 시각, 걸리는 시간, 줄이기 시작한 음량. 시간이 음수면 줄이는 중이 아니다.
        private float[] _voiceFadeStart;
        private float[] _voiceFadeDuration;
        private float[] _voiceFadeVolume;
        private float _sfxPitchTarget = 1f;
        private int _nextPlayId = 1;
        private int _nextVoice;
        // 소리마다 다음에 다시 울릴 수 있는 시각. 재생할 때 간격을 무작위로 뽑아 정해 둔다.
        private readonly Dictionary<SoundData, float> _nextPlayTimes = new Dictionary<SoundData, float>();

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
            _voiceBasePitches = new float[_sfxVoices];
            _voiceFadeStart = new float[_sfxVoices];
            _voiceFadeDuration = new float[_sfxVoices];
            _voiceFadeVolume = new float[_sfxVoices];
            for (int i = 0; i < _sfxVoices; i++)
            {
                _voiceFadeStart[i] = -1f;
            }
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

        // 뚝 끊으면 튀는 소리가 나는 긴 소리를 짧게 줄이며 끊는다. 이미 끝났거나 다른 소리로 바뀐 표면 아무것도 안 한다.
        public static void FadeOut(SoundHandle handle, float duration)
        {
            if (_current == null || handle.Voice < 0 || handle.PlayId <= 0 || _current._voicePlayIds[handle.Voice] != handle.PlayId)
            {
                return;
            }

            AudioSource voice = _current._voices[handle.Voice];
            if (duration <= 0f)
            {
                voice.Stop();
                return;
            }

            _current._voiceFadeStart[handle.Voice] = Time.unscaledTime;
            _current._voiceFadeDuration[handle.Voice] = duration;
            _current._voiceFadeVolume[handle.Voice] = voice.volume;
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

        // 슬로모션 동안 효과음을 낮고 느리게 늘인다(배경음은 그대로). 낮출 때는 바로, 되돌릴 때는 짧게 미끄러지듯 올라가
        // 지금 울리는 소리(보스를 잡은 마지막 폭발음 등)가 뚝 끊기듯 바뀌지 않게 한다.
        public static void SetSfxPitchScale(float scale)
        {
            if (_current == null)
            {
                return;
            }

            _current._sfxPitchTarget = scale;
            if (scale < _current._sfxPitchScale)
            {
                _current.ApplySfxPitch(scale);
            }
        }

        private void Update()
        {
            TickFades();
            if (Mathf.Approximately(_sfxPitchScale, _sfxPitchTarget))
            {
                return;
            }

            ApplySfxPitch(Mathf.MoveTowards(_sfxPitchScale, _sfxPitchTarget, PitchRecoverSpeed * Time.unscaledDeltaTime));
        }

        private void TickFades()
        {
            float now = Time.unscaledTime;
            for (int i = 0; i < _voices.Length; i++)
            {
                if (_voiceFadeStart[i] < 0f)
                {
                    continue;
                }

                float t = (now - _voiceFadeStart[i]) / _voiceFadeDuration[i];
                if (t >= 1f || !_voices[i].isPlaying)
                {
                    _voices[i].Stop();
                    _voiceFadeStart[i] = -1f;
                    continue;
                }

                _voices[i].volume = _voiceFadeVolume[i] * (1f - t);
            }
        }

        private void ApplySfxPitch(float scale)
        {
            _sfxPitchScale = scale;
            for (int i = 0; i < _voices.Length; i++)
            {
                if (_voices[i].isPlaying)
                {
                    _voices[i].pitch = _voiceBasePitches[i] * scale;
                }
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
            if (_nextPlayTimes.TryGetValue(sound, out float next) && now < next)
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
            _voiceBasePitches[index] = pitch;
            _voiceFadeStart[index] = -1f;
            voice.pitch = pitch * _sfxPitchScale;
            voice.Play();
            _voiceData[index] = sound;
            _nextPlayTimes[sound] = now + sound.PickInterval();

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
