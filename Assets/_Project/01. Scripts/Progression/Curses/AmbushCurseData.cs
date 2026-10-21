using ExplodeIt.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace ExplodeIt.Progression
{
    // 저주 "기습": 군집 일부가 구역 대신 플레이어 근처 빈자리에서 나온다. 나오기 전에 그 자리에 표시가 뜬다.
    // 자리 고르기와 스폰은 스테이지 진행이 맡고, 표시는 게임 씬의 기습 컴포넌트가 그린다.
    [CreateAssetMenu(fileName = "Curse_Ambush", menuName = "Explode It/Curses/Ambush")]
    public class AmbushCurseData : CurseData
    {
        [Header("기습")]
        [Tooltip("군집 하나가 플레이어 근처에서 나올 확률 (0~1)")]
        [SerializeField, Range(0f, 1f)] private float _chance = 0.5f;

        [Header("플레이어와의 거리")]
        [Tooltip("플레이어에게서 최소 이만큼 떨어진 곳에 나온다 (유닛). 바로 옆에 나오면 피할 수 없다")]
        [SerializeField, Min(0f), FormerlySerializedAs("_minRadius")] private float _minDistance = 4f;

        [Tooltip("플레이어에게서 최대 이만큼 떨어진 곳까지 나온다 (유닛)")]
        [SerializeField, Min(0f), FormerlySerializedAs("_maxRadius")] private float _maxDistance = 10f;

        [Tooltip("자리가 표시된 뒤 적이 나오기까지 시간 (초)")]
        [SerializeField, Min(0f)] private float _warningTime = 2f;

        [Header("표시")]
        [Tooltip("나올 자리에 꽂히는 깃발 그림. 기준점은 깃대 밑동(땅)이다. 비워 두면 아래 색의 원으로 표시한다")]
        [SerializeField] private Sprite _flagSprite;

        [Tooltip("깃발 키 (유닛)")]
        [SerializeField, Min(0.1f)] private float _flagHeight = 1.3f;

        [Tooltip("깃발 그림이 바라보는 쪽. 켜면 오른쪽, 끄면 왼쪽. 플레이어 쪽을 보도록 좌우를 뒤집는 기준이다")]
        [SerializeField] private bool _flagFacesRight;

        [Tooltip("깃발이 떨어지기 시작하는 높이 (유닛, 꽂힐 자리 위)")]
        [SerializeField, Min(0f)] private float _flagDropHeight = 1.5f;

        [Tooltip("깃발이 떨어져 꽂히기까지 시간 (초)")]
        [SerializeField, Min(0.01f)] private float _flagDropTime = 0.22f;

        [Tooltip("꽂힌 뒤 좌우로 흔들리는 각도 (도)")]
        [SerializeField, Min(0f)] private float _flagSwayAngle = 5f;

        [Tooltip("한 번 흔들리는 데 걸리는 시간 (초, 왼쪽→오른쪽)")]
        [SerializeField, Min(0.05f)] private float _flagSwayTime = 0.45f;

        [Tooltip("적이 나올 때 깃발이 작아지며 사라지는 시간 (초)")]
        [SerializeField, Min(0.01f)] private float _flagVanishTime = 0.18f;

        [Tooltip("나올 자리 바깥 원 색 (그림이 없을 때)")]
        [SerializeField] private Color _rangeColor = new Color(0.55f, 0.1f, 0.45f, 0.25f);

        [Tooltip("남은 시간만큼 차오르는 안쪽 원 색")]
        [SerializeField] private Color _fillColor = new Color(0.75f, 0.2f, 0.65f, 0.35f);

        [Tooltip("표시가 뜰 때 소리")]
        [SerializeField] private SoundData _warnSound;

        public override CurseKind Kind => CurseKind.Ambush;
        public Sprite FlagSprite => _flagSprite;
        public float FlagHeight => _flagHeight;
        public bool FlagFacesRight => _flagFacesRight;
        public float FlagDropHeight => _flagDropHeight;
        public float FlagDropTime => _flagDropTime;
        public float FlagSwayAngle => _flagSwayAngle;
        public float FlagSwayTime => _flagSwayTime;
        public float FlagVanishTime => _flagVanishTime;
        public Color RangeColor => _rangeColor;
        public Color FillColor => _fillColor;
        public SoundData WarnSound => _warnSound;

        public override void ApplyRunModifiers()
        {
            RunCurses.AmbushChance = _chance;
            RunCurses.AmbushMinRadius = _minDistance;
            RunCurses.AmbushMaxRadius = Mathf.Max(_minDistance, _maxDistance);
            RunCurses.AmbushWarning = _warningTime;
        }
    }
}
