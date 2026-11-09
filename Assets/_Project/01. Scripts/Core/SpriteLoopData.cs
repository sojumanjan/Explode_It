// 맵 소품의 반복 그림(분수대 물, 깃발, 촛불)의 장면 목록과 넘기는 빠르기를 담는 데이터
using UnityEngine;

namespace ExplodeIt.Core
{
    [CreateAssetMenu(fileName = "SpriteLoop", menuName = "Explode It/Sprite Loop")]
    public class SpriteLoopData : ScriptableObject
    {
        [Tooltip("차례로 넘길 그림. 마지막 다음은 첫 장으로 돌아간다")]
        [SerializeField] private Sprite[] _frames;

        [Tooltip("한 장을 보여 주는 시간 (초)")]
        [SerializeField, Min(0.02f)] private float _frameTime = 0.15f;

        [Tooltip("한 장을 보여 주는 시간의 최댓값 (초). 프레임 시간보다 크면 장이 바뀔 때마다 그 사이에서 새로 골라, 같은 소품 여럿이 박자를 맞춰 움직이지 않게 한다. 프레임 시간 이하이면 늘 프레임 시간")]
        [SerializeField, Min(0f)] private float _frameTimeMax;

        [Tooltip("켜질 때 시작 장을 무작위로 고른다. 같은 소품 여러 개가 똑같이 움직이면 기계처럼 보인다")]
        [SerializeField] private bool _randomStart = true;

        public Sprite[] Frames => _frames;
        public float FrameTime => _frameTime;
        public float NextFrameTime() => _frameTimeMax > _frameTime ? Random.Range(_frameTime, _frameTimeMax) : _frameTime;
        public bool RandomStart => _randomStart;
    }
}
