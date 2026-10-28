using System;
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 크레딧 한 줄. 위에서부터 차례로 올라간다.
    [Serializable]
    public class CreditsEntry
    {
        [Tooltip("줄 모양. 제목 · 묶음 제목 · 한 줄 · 캐스트(그림 + 이름 + 설명) · 빈 줄")]
        [SerializeField] private CreditsEntryKind _kind = CreditsEntryKind.Line;

        [Tooltip("글. 캐스트면 캐릭터 이름. 빈 줄이면 쓰지 않는다")]
        [SerializeField, TextArea(1, 3)] private string _text;

        [Tooltip("캐스트 줄의 설명(이름 아래 작은 글). 다른 모양에서는 쓰지 않는다")]
        [SerializeField] private string _subText;

        [Tooltip("캐스트 줄에 세워 둘 캐릭터 그림. 다른 모양에서는 쓰지 않는다")]
        [SerializeField] private Sprite _sprite;

        public CreditsEntryKind Kind => _kind;
        public string Text => L10n.Data(_text);
        public string SubText => L10n.Data(_subText);
        public Sprite Sprite => _sprite;
    }
}
