// 맵 소품 그림을 데이터의 장면 목록대로 계속 넘겨 물, 깃발, 촛불이 움직이는 것처럼 보이게 하는 컴포넌트
using UnityEngine;

namespace ExplodeIt.Core
{
    // 소품 몇십 개에 붙으므로 Animator 대신 시간만 세고, 장이 바뀔 때만 그림을 바꾼다.
    // 일시 정지 중에는 맵도 멈춰 있어야 자연스러워서 Time.deltaTime을 쓴다.
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteLoop : MonoBehaviour
    {
        [SerializeField] private SpriteLoopData _data;

        private SpriteRenderer _renderer;
        private float _time;
        private float _duration;
        private int _index;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (_data == null || _data.Frames == null || _data.Frames.Length == 0)
            {
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_data == null || _data.Frames == null || _data.Frames.Length == 0)
            {
                return;
            }

            // 장마다 보여 주는 시간이 다를 수 있어 전체 시간 대신 지금 장과 그 장에서 지난 시간을 센다.
            _duration = _data.NextFrameTime();
            if (_data.RandomStart)
            {
                _index = Random.Range(0, _data.Frames.Length);
                _time = Random.value * _duration;
            }
            else
            {
                _index = 0;
                _time = 0f;
            }

            _renderer.sprite = _data.Frames[_index];
        }

        private void Update()
        {
            _time += Time.deltaTime;
            if (_time < _duration)
            {
                return;
            }

            Sprite[] frames = _data.Frames;
            _time -= _duration;
            _duration = _data.NextFrameTime();
            _index = (_index + 1) % frames.Length;
            _renderer.sprite = frames[_index];
        }
    }
}
