using System;
using UnityEngine;

namespace ExplodeIt.Core
{
    // PixelFxClip의 프레임을 스프라이트 렌더러에 차례로 끼워 재생한다. Animator 없이 프레임만 바꾸므로 풀에서 꺼내 바로 다시 틀 수 있다.
    // 풀에서 쓸 때는 Play(scale, release)로 틀고, 한 번 재생이 끝나면 release로 돌아간다. 씬에 그냥 놓으면 켜질 때 스스로 튼다.
    [RequireComponent(typeof(SpriteRenderer))]
    public class PixelFxPlayer : MonoBehaviour
    {
        [Tooltip("재생할 클립")]
        [SerializeField] private PixelFxClip _clip;
        [Tooltip("켜질 때 저절로 재생한다. 풀에서 Play로 틀 때는 꺼 둔다")]
        [SerializeField] private bool _playOnEnable = true;
        [Tooltip("재생 속도 배율. 1이면 클립의 fps 그대로")]
        [SerializeField, Min(0.01f)] private float _speed = 1f;
        [Tooltip("켜면 히트스톱(게임 속도 저하)과 상관없이 실제 시간으로 돈다")]
        [SerializeField] private bool _useUnscaledTime;
        [Tooltip("한 번 재생 이펙트를 반복해서 보고 싶을 때 켠다 (확인용 씬). 이때 끝 콜백은 오지 않는다")]
        [SerializeField] private bool _forceLoop;

        private SpriteRenderer _renderer;
        private Action<PixelFxPlayer> _release;
        private Vector3 _baseScale;
        private float _time;
        private int _shownFrame = -1;
        private bool _isPlaying;

        public PixelFxClip Clip => _clip;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _baseScale = transform.localScale;
        }

        private void OnEnable()
        {
            if (_playOnEnable)
            {
                Restart();
            }
        }

        private void OnDisable()
        {
            _isPlaying = false;
        }

        public void Play(float scale, Action<PixelFxPlayer> release)
        {
            _release = release;
            transform.localScale = _baseScale * scale;
            Restart();
        }

        private void Restart()
        {
            _time = 0f;
            _shownFrame = -1;
            _isPlaying = _clip != null && _clip.Frames != null && _clip.Frames.Length > 0;
            _renderer.enabled = _isPlaying;
            if (_isPlaying)
            {
                ShowFrame(0);
            }
        }

        private void Update()
        {
            if (!_isPlaying)
            {
                return;
            }

            _time += (_useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) * _speed;
            int count = _clip.Frames.Length;
            int frame = Mathf.FloorToInt(_time * _clip.Fps);
            if (frame >= count)
            {
                if (_clip.Loop || _forceLoop)
                {
                    frame %= count;
                    _time %= _clip.Duration;
                }
                else
                {
                    Finish();
                    return;
                }
            }

            ShowFrame(frame);
        }

        private void ShowFrame(int frame)
        {
            if (frame == _shownFrame)
            {
                return;
            }

            _shownFrame = frame;
            _renderer.sprite = _clip.Frames[frame];
        }

        // pixelfx 한 번 재생 이펙트는 마지막 프레임이 비어 있게 끝나므로, 그림을 끄고 돌려보내도 튀지 않는다.
        private void Finish()
        {
            _isPlaying = false;
            _renderer.enabled = false;
            if (_release != null)
            {
                Action<PixelFxPlayer> release = _release;
                _release = null;
                release(this);
            }
        }
    }
}
