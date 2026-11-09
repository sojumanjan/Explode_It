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
        private int _index = -1;

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
            if (_data == null)
            {
                return;
            }

            _time = _data.RandomStart ? Random.value * _data.Frames.Length * _data.FrameTime : 0f;
            _index = -1;
        }

        private void Update()
        {
            Sprite[] frames = _data.Frames;
            _time += Time.deltaTime;
            float cycle = frames.Length * _data.FrameTime;
            if (_time >= cycle)
            {
                _time -= cycle;
            }

            int index = Mathf.Min((int)(_time / _data.FrameTime), frames.Length - 1);
            if (index != _index)
            {
                _index = index;
                _renderer.sprite = frames[index];
            }
        }
    }
}
