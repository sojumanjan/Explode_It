using UnityEngine;

namespace ExplodeIt.Stage
{
    // 우측 하단 왕 컷인 대사 하나. 같은 상황의 대사가 여럿이면 그중 하나를 무작위로 고른다.
    [CreateAssetMenu(fileName = "Remark_", menuName = "Explode It/Story/King Remark")]
    public class KingRemarkData : ScriptableObject
    {
        [Tooltip("언제 나오는지")]
        [SerializeField] private KingRemarkTrigger _trigger;

        [Tooltip("상황별 값. 웨이브 시작: 웨이브 번호(0=아무 웨이브), 다중 처치: 최소 처치 수, 누적 처치: 정확한 처치 수, 보스 처치: 보스 번호(0=아무 보스)")]
        [SerializeField, Min(0)] private int _value;

        [Tooltip("왕 표정")]
        [SerializeField] private KingMood _mood;

        [Tooltip("대사")]
        [SerializeField, TextArea(1, 3)] private string _text;

        [Tooltip("글자가 다 나온 뒤 컷인이 들어가기까지 (초)")]
        [SerializeField, Min(0f)] private float _hold = 1.8f;

        public KingRemarkTrigger Trigger => _trigger;
        public int Value => _value;
        public KingMood Mood => _mood;
        public string Text => _text;
        public float Hold => _hold;
    }
}
