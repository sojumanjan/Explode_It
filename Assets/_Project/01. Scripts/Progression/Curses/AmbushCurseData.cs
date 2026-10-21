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
        [Tooltip("나올 자리 바깥 원 색")]
        [SerializeField] private Color _rangeColor = new Color(0.55f, 0.1f, 0.45f, 0.25f);

        [Tooltip("남은 시간만큼 차오르는 안쪽 원 색")]
        [SerializeField] private Color _fillColor = new Color(0.75f, 0.2f, 0.65f, 0.35f);

        [Tooltip("표시가 뜰 때 소리")]
        [SerializeField] private SoundData _warnSound;

        public override CurseKind Kind => CurseKind.Ambush;
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
