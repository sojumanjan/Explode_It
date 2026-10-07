using System;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 스토리 보스전 등장 대화 한 편. 보스가 내려앉은 뒤 왕이 옆에 붙어 나와 보스와 주고받고, 끝나면 보스 등장 동작이 이어진다.
    [CreateAssetMenu(fileName = "Dialogue_", menuName = "Explode It/Story/Boss Dialogue")]
    public class BossDialogueData : ScriptableObject
    {
        [Serializable]
        public class Line
        {
            [Tooltip("누가 말하는지. 말풍선이 그 머리 위에 뜬다")]
            [SerializeField] private DialogueSpeaker _speaker;

            [Tooltip("왕이 말할 때의 표정. 줄이 바뀌면 왕 그림이 뽀잉 하며 이 표정으로 바뀐다. 보스 줄에서는 쓰지 않는다")]
            [SerializeField] private KingMood _mood;

            [Tooltip("대사")]
            [SerializeField, TextArea(1, 3)] private string _text;

            [Tooltip("글자가 다 나온 뒤 다음 줄로 넘어가기까지 (초). 클릭하면 바로 넘어간다")]
            [SerializeField, Min(0f)] private float _hold = 1.2f;

            public DialogueSpeaker Speaker => _speaker;
            public KingMood Mood => _mood;
            public string Text => _text;
            public float Hold => _hold;
        }

        [Tooltip("위에서부터 차례로 말한다")]
        [SerializeField] private Line[] _lines;

        public int LineCount => _lines != null ? _lines.Length : 0;

        public Line GetLine(int index)
        {
            return _lines[index];
        }
    }
}
