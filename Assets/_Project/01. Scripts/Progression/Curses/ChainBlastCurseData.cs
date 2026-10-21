using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Progression
{
    // 저주 "유폭": 적이 죽은 자리에 원이 차오른 뒤 터진다. 플레이어만 맞고 적끼리는 맞지 않는다.
    [CreateAssetMenu(fileName = "Curse_ChainBlast", menuName = "Explode It/Curses/Chain Blast")]
    public class ChainBlastCurseData : CurseData
    {
        [Header("유폭")]
        [Tooltip("적이 죽고 터지기까지 시간 (초). 원이 다 차는 순간 터진다")]
        [SerializeField, Min(0.1f)] private float _delay = 0.8f;

        [Tooltip("폭발 반경 (유닛)")]
        [SerializeField, Min(0.1f)] private float _radius = 1.4f;

        [Header("표시와 연출")]
        [SerializeField] private Color _rangeColor = new Color(1f, 0.35f, 0.1f, 0.22f);
        [SerializeField] private Color _fillColor = new Color(1f, 0.45f, 0.1f, 0.4f);

        [Tooltip("터질 때 연출 (흔들림·이펙트)")]
        [SerializeField] private FeedbackData _explodeFeedback;

        [Tooltip("터질 때 소리. 여럿이 한꺼번에 터지므로 사운드 데이터의 동시 재생 수를 작게 둔다")]
        [SerializeField] private SoundData _explodeSound;

        public override CurseKind Kind => CurseKind.ChainBlast;
        public float Delay => _delay;
        public float Radius => _radius;
        public Color RangeColor => _rangeColor;
        public Color FillColor => _fillColor;
        public FeedbackData ExplodeFeedback => _explodeFeedback;
        public SoundData ExplodeSound => _explodeSound;
    }
}
