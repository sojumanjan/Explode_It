using DG.Tweening;
using ExplodeIt.Stage;
using TMPro;
using UnityEngine;

namespace ExplodeIt.UI
{
    // 화면 위 가운데 처치 수. 잡을 때마다 숫자가 통통 튀고, 정해진 수마다 한 번 크게 튄다.
    public class KillCounterView : MonoBehaviour
    {
        [SerializeField] private StageStats _stats;
        [SerializeField] private TMP_Text _text;

        [Header("튀기")]
        [Tooltip("한 마리 잡을 때마다 커지는 정도 (배율, 0.15 = 15%)")]
        [SerializeField, Min(0f)] private float _punch = 0.15f;
        [Tooltip("이 수마다 크게 튄다 (마리)")]
        [SerializeField, Min(1)] private int _milestoneEvery = 25;
        [Tooltip("정해진 수에 닿았을 때 커지는 정도 (배율)")]
        [SerializeField, Min(0f)] private float _milestonePunch = 0.45f;
        [Tooltip("정해진 수에 닿았을 때 잠깐 바뀌는 글씨 색")]
        [SerializeField] private Color _milestoneColor = new Color(1f, 0.85f, 0.3f, 1f);
        [Tooltip("튀었다 돌아오는 시간 (초, 실제 시간)")]
        [SerializeField, Min(0.05f)] private float _punchTime = 0.25f;
        [Tooltip("정해진 수에 닿았을 때 튀고 색이 돌아오는 시간 (초, 실제 시간)")]
        [SerializeField, Min(0.05f)] private float _milestoneTime = 0.6f;

        private RectTransform _textRect;
        private Color _baseColor;
        private int _shownKills = -1;

        private void Awake()
        {
            _textRect = _text.rectTransform;
            _baseColor = _text.color;
        }

        private void OnEnable()
        {
            _stats.KillsChanged += OnKillsChanged;
        }

        private void OnDisable()
        {
            _stats.KillsChanged -= OnKillsChanged;
        }

        private void Start()
        {
            OnKillsChanged(_stats.Kills);
        }

        // SetText의 숫자 인자 버전은 문자열을 새로 만들지 않는다.
        private void OnKillsChanged(int kills)
        {
            _text.SetText("{0}", kills);
            // 처음 표시할 때와, 중간부터 시작해 처치 수가 한 번에 건너뛸 때는 튀지 않는다.
            if (_shownKills >= 0 && kills == _shownKills + 1)
            {
                Bounce(kills);
            }

            _shownKills = kills;
        }

        private void Bounce(int kills)
        {
            _textRect.DOKill(true);
            _text.DOKill(true);
            if (kills % _milestoneEvery == 0)
            {
                _textRect.DOPunchScale(Vector3.one * _milestonePunch, _milestoneTime, 6).SetUpdate(true).SetLink(_text.gameObject);
                _text.color = _milestoneColor;
                _text.DOColor(_baseColor, _milestoneTime).SetEase(Ease.InQuad).SetUpdate(true).SetLink(_text.gameObject);
                return;
            }

            _textRect.DOPunchScale(Vector3.one * _punch, _punchTime, 4).SetUpdate(true).SetLink(_text.gameObject);
        }
    }
}
