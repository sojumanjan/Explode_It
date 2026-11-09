// 맵 위를 떠다니는 장식(나뭇잎, 구름 그림자)의 개수, 바람, 흔들림, 크기, 색을 담는 데이터
using UnityEngine;

namespace ExplodeIt.Core
{
    [CreateAssetMenu(fileName = "AmbientDrifter", menuName = "Explode It/Ambient Drifter")]
    public class AmbientDrifterData : ScriptableObject
    {
        [Tooltip("떠다니는 장식 그림. 여러 장이면 차례로 넘긴다(나뭇잎이 뒤집히는 모습). 한 장이면 그대로 둔다")]
        [SerializeField] private Sprite[] _frames;

        [Tooltip("한 장을 보여 주는 시간 (초). 그림이 한 장이거나 모양 종류로 쓸 때는 쓰지 않는다")]
        [SerializeField, Min(0.02f)] private float _frameTime = 0.2f;

        [Tooltip("켜면 그림을 넘기지 않고 하나마다 한 장을 무작위로 골라 고정한다(구름 모양 종류). 끄면 차례로 넘긴다(나뭇잎 뒤집힘)")]
        [SerializeField] private bool _framesAreVariants;

        [Tooltip("맵 전체에 띄울 개수")]
        [SerializeField, Min(1)] private int _count = 30;

        [Tooltip("바람 방향. 크기는 상관없다(방향만 쓴다)")]
        [SerializeField] private Vector2 _windDirection = new Vector2(1f, -0.25f);

        [Tooltip("바람 방향으로 움직이는 빠르기 범위 (유닛/초). 하나마다 이 사이에서 고른다")]
        [SerializeField] private Vector2 _speedRange = new Vector2(0.6f, 1.2f);

        [Tooltip("바람과 직각으로 흔들리는 폭 (유닛). 0이면 곧게 날아간다")]
        [SerializeField, Min(0f)] private float _wobbleAmplitude = 0.3f;

        [Tooltip("흔들림 빠르기 (초당 왕복 횟수)")]
        [SerializeField, Min(0f)] private float _wobbleFrequency = 0.5f;

        [Tooltip("크기 범위 (배). 하나마다 이 사이에서 고른다")]
        [SerializeField] private Vector2 _scaleRange = new Vector2(1f, 1f);

        [Tooltip("그림에 곱할 색. 불투명도가 진하기다")]
        [SerializeField] private Color _color = Color.white;

        [Tooltip("그리는 순서(Default 층). 소품(5)보다 위, 공격 예고(10 이상)보다 아래에 둬 예고를 가리지 않게 한다")]
        [SerializeField] private int _sortingOrder = 9;

        public Sprite[] Frames => _frames;
        public float FrameTime => _frameTime;
        public bool FramesAreVariants => _framesAreVariants;
        public int Count => _count;
        public Vector2 WindDirection => _windDirection;
        public Vector2 SpeedRange => _speedRange;
        public float WobbleAmplitude => _wobbleAmplitude;
        public float WobbleFrequency => _wobbleFrequency;
        public Vector2 ScaleRange => _scaleRange;
        public Color Color => _color;
        public int SortingOrder => _sortingOrder;
    }
}
