using DG.Tweening;
using ExplodeIt.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace ExplodeIt.Progression
{
    // 저주 "먼지바람"(내부 이름 수렁): 맵 곳곳에 플레이어만 느려지는 먼지바람이 생기고, 하나씩 자리를 옮긴다.
    // 적까지 느려지면 예측이 쉬워져 오히려 도움이 되므로 플레이어만 느려진다. 대시는 느려지지 않아 빠져나갈 수 있다.
    // 그림이 가로로 길어서 판정도 가로로 긴 타원이다. 바람끼리는 겹치지 않게 자리를 고른다.
    [CreateAssetMenu(fileName = "Curse_Swamp", menuName = "Explode It/Curses/Swamp")]
    public class SwampCurseData : CurseData
    {
        [Header("먼지바람")]
        [Tooltip("맵에 동시에 있는 먼지바람 수 (개)")]
        [SerializeField, Min(1)] private int _count = 3;

        [Tooltip("먼지바람 가로 반지름 (유닛). 판정과 그림 크기가 이 값을 따른다")]
        [SerializeField, Min(0.3f)] private float _radius = 2.5f;

        [Tooltip("세로 반지름 = 가로 반지름 × 이 비율. 그림의 세로/가로 비율(0.85)에 맞춰 둔다")]
        [SerializeField, Range(0.2f, 1f)] private float _heightRatio = 0.85f;

        [Tooltip("먼지바람 안에서 플레이어 이동속도 배율 (0.7 = 30% 느려짐)")]
        [SerializeField, Range(0.1f, 1f)] private float _moveMultiplier = 0.7f;

        [Tooltip("먼지바람 하나가 자리를 옮기는 간격 (초). 바람마다 돌아가며 하나씩 옮긴다")]
        [SerializeField, Min(1f)] private float _moveInterval = 12f;

        [Tooltip("먼지바람이 나타나거나 사라지는 시간 (초). 나타나는 동안은 느려지지 않는다")]
        [SerializeField, Min(0.05f)] private float _fadeTime = 1f;

        [Tooltip("먼지바람끼리 떨어뜨릴 최소 간격 (유닛). 0이면 가장자리끼리 닿을 수 있다")]
        [SerializeField, Min(0f)] private float _gap = 0.5f;

        [Header("플레이어와의 거리")]
        [Tooltip("새 먼지바람은 플레이어에게서 최소 이만큼 떨어진 곳에 생긴다 (유닛). 발밑에 갑자기 생기면 억울하다")]
        [SerializeField, Min(0f), FormerlySerializedAs("_minPlayerDistance")] private float _minDistance = 3f;

        [Tooltip("새 먼지바람이 생길 수 있는 플레이어로부터 최대 거리 (유닛). 화면 밖 먼 곳에만 생기면 의미가 없다")]
        [SerializeField, Min(0f), FormerlySerializedAs("_maxPlayerDistance")] private float _maxDistance = 12f;

        [Header("표시")]
        [Tooltip("먼지바람 그림 (차례로 넘기는 애니메이션 프레임)")]
        [SerializeField] private Sprite[] _frames = new Sprite[0];

        [Tooltip("프레임 하나가 보이는 시간 (초)")]
        [SerializeField, Min(0.02f)] private float _frameTime = 0.12f;

        [Tooltip("그림 투명도 (0~1). 1이면 불투명")]
        [SerializeField, Range(0f, 1f)] private float _spriteAlpha = 0.6f;

        [Tooltip("그림 가로 = 판정 가로 지름 × 이 배율. 그림 가장자리의 흩어진 먼지까지 판정에 맞추려면 1보다 조금 크게 둔다")]
        [SerializeField, Min(0.1f)] private float _spriteSizeRatio = 1.1f;

        [Tooltip("나타날 때 크기 곡선. OutBack이면 살짝 커졌다가 제 크기로 돌아온다")]
        [SerializeField] private Ease _appearEase = Ease.OutBack;

        [Tooltip("사라질 때 크기 곡선. InBack이면 살짝 부풀었다가 작아진다")]
        [SerializeField] private Ease _disappearEase = Ease.InBack;

        public override CurseKind Kind => CurseKind.Swamp;
        public int Count => _count;
        public float RadiusX => _radius;
        public float RadiusY => _radius * _heightRatio;
        public float MoveMultiplier => _moveMultiplier;
        public float MoveInterval => _moveInterval;
        public float FadeTime => _fadeTime;
        public float Gap => _gap;
        public float MinDistance => _minDistance;
        public float MaxDistance => Mathf.Max(_minDistance, _maxDistance);
        public Sprite[] Frames => _frames;
        public float FrameTime => _frameTime;
        public float SpriteAlpha => _spriteAlpha;
        public float SpriteSizeRatio => _spriteSizeRatio;
        public Ease AppearEase => _appearEase;
        public Ease DisappearEase => _disappearEase;
    }
}
