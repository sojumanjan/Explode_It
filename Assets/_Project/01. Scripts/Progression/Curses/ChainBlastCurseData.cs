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

        [Tooltip("터지는 시간을 흩뜨리는 폭 (초). 한꺼번에 죽은 적이 동시에 터지지 않게, 자리마다 터지기까지 시간을 ±이 값 안에서 무작위로 정한다. 원이 차오르는 속도도 같이 달라져 예고는 그대로 맞는다")]
        [SerializeField, Min(0f)] private float _delayJitter = 0.1f;

        [Tooltip("폭발 반경 (유닛)")]
        [SerializeField, Min(0.1f)] private float _radius = 1.4f;

        [Header("표시와 연출")]
        [SerializeField] private Color _rangeColor = new Color(1f, 0.35f, 0.1f, 0.22f);
        [SerializeField] private Color _fillColor = new Color(1f, 0.45f, 0.1f, 0.4f);

        [Tooltip("터질 때 연출 (흔들림·이펙트)")]
        [SerializeField] private FeedbackData _explodeFeedback;

        [Tooltip("터질 때 트는 폭발 pixelfx (PxFx_chain-pop-a, b, c). 터질 때마다 이 중 하나를 무작위로 골라 틀어 화면이 정신없어 보이게 한다. 비워 두면 틀지 않는다")]
        [SerializeField] private PixelFxPlayer[] _explodeFx;

        [Tooltip("배율 1일 때 폭발 그림이 덮는 반경 (유닛). chain-pop은 40px ÷ PPU 20 = 2")]
        [SerializeField, Min(0.01f)] private float _explodeFxRadius = 2f;

        [Tooltip("폭발 그림을 폭발 반경보다 얼마나 크게 틀지 (배). 1이면 반경에 딱 맞는다")]
        [SerializeField, Min(0.1f)] private float _explodeFxScale = 1.15f;

        [Tooltip("터질 때 소리. 여럿이 한꺼번에 터지므로 사운드 데이터의 동시 재생 수를 작게 둔다")]
        [SerializeField] private SoundData _explodeSound;

        public override CurseKind Kind => CurseKind.ChainBlast;
        public float Delay => _delay;
        public float DelayJitter => _delayJitter;
        public float Radius => _radius;
        public Color RangeColor => _rangeColor;
        public Color FillColor => _fillColor;
        public FeedbackData ExplodeFeedback => _explodeFeedback;
        public PixelFxPlayer[] ExplodeFx => _explodeFx;
        public float ExplodeFxRadius => _explodeFxRadius;
        public float ExplodeFxScale => _explodeFxScale;
        public SoundData ExplodeSound => _explodeSound;
    }
}
