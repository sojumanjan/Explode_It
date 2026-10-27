using System;
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 스토리 보스전 등장 대화 한 편. 왕이 먼저 나와 말하다가, 표시한 줄이 시작될 때 보스를 불러내고, 표시한 줄을 넘길 때 보스 등장 동작을 시작한다.
    // 줄은 클릭해야 넘어간다.
    [CreateAssetMenu(fileName = "Dialogue_", menuName = "Explode It/Story/Boss Dialogue")]
    public class BossDialogueData : ScriptableObject
    {
        [Serializable]
        public class Line
        {
            [Tooltip("누가 말하는지. 말풍선이 그 머리 위에 뜬다. 보스가 나오기 전 줄을 보스에게 주면 보스가 나올 자리 위에 뜬다")]
            [SerializeField] private DialogueSpeaker _speaker;

            [Tooltip("왕이 말할 때의 표정. 줄이 바뀌면 왕 그림이 뽀잉 하며 이 표정으로 바뀐다. 보스 줄에서는 쓰지 않는다")]
            [SerializeField] private KingMood _mood;

            [Tooltip("대사")]
            [SerializeField, TextArea(1, 3)] private string _text;

            [Tooltip("이 줄이 시작될 때 보스가 내려앉는다. 어느 줄에도 체크하지 않으면 왕이 나온 직후 첫 줄에서 내려앉는다")]
            [SerializeField] private bool _spawnBoss;

            [Tooltip("이 줄을 다 읽고 클릭해 넘기는 순간 보스 등장 동작(화염 영역 전개, 땅 파고들기 등)을 시작한다. 마지막 줄에 체크하면 대화가 끝나며 시작한다. 어느 줄에도 체크하지 않아도 대화가 끝난 뒤 시작한다")]
            [SerializeField] private bool _startEntrance;

            [Tooltip("처치 대화용. 이 줄이 시작될 때 쓰러진 보스의 그림을 이 그림으로 바꾼다(무릎 꿇기 등). 비우면 그대로 둔다")]
            [SerializeField] private Sprite _bossSprite;

            [Tooltip("처치 대화용. 이 줄이 시작될 때 쓰러진 각성 왕이 펑 하고 2등신 왕으로 돌아간다. 그 뒤 왕 줄은 2등신 왕이 표정과 함께 말하고, 대화가 끝나면 스티커처럼 떼어지며 퇴장한다")]
            [SerializeField] private bool _revertToKing;

            [Tooltip("이 줄이 시작될 때 배경음을 이 곡으로 바꾼다. 비우면 지금 곡(또는 무음)을 그대로 둔다")]
            [SerializeField] private BgmData _bgm;

            [Tooltip("이 줄이 시작될 때 먼저 보여 줄 연출. 연출이 끝난 뒤 이 줄의 말풍선이 뜬다")]
            [SerializeField] private DialogueAction _startAction;

            [Tooltip("이 줄을 다 읽고 넘길 때 보여 줄 연출. 연출이 끝난 뒤 다음 줄로 넘어간다. 마지막 줄이면 연출 뒤 대화가 끝난다")]
            [SerializeField] private DialogueAction _endAction;

            public DialogueSpeaker Speaker => _speaker;
            public KingMood Mood => _mood;
            public string Text => _text;
            public bool SpawnBoss => _spawnBoss;
            public bool StartEntrance => _startEntrance;
            public Sprite BossSprite => _bossSprite;
            public bool RevertToKing => _revertToKing;
            public DialogueAction StartAction => _startAction;
            public BgmData Bgm => _bgm;
            public DialogueAction EndAction => _endAction;
        }

        [Tooltip("위에서부터 차례로 말한다")]
        [SerializeField] private Line[] _lines;

        [Tooltip("보스가 내려앉는 줄에서 대사를 띄우기 전 기다리는 시간 (초). 착지 연출에 눈이 가도록 그동안 말풍선을 숨기고 클릭도 받지 않는다")]
        [SerializeField, Min(0f)] private float _spawnPause = 2f;

        [Tooltip("왕이 곧 보스인 대화(최종 보스). 켜면 왕이 보스 자리에 서서 말하고, 보스 등장을 체크한 줄에서 그 자리에서 보스로 바뀐다(변신 연출은 멈춤 시간 동안). 이후 왕 대사는 보스 머리 위에 뜬다. 체크한 줄이 없으면 대화가 끝난 뒤 바뀐다")]
        [SerializeField] private bool _kingBecomesBoss;

        [Tooltip("왕이 곧 보스일 때 왕이 서는 높이 (유닛, 보스 자리 기준). 보스 프리팹의 2등신 그림(Chibi) 높이와 같게 두어야 보스로 바뀌는 순간 발 위치가 튀지 않는다")]
        [SerializeField] private float _kingStandHeight;

        [Tooltip("처치 대화에서 2등신으로 돌아가는 줄이 시작된 뒤 그 줄을 띄우기까지 (초). 펑 하고 바뀌는 연출에 눈이 가도록 잠깐 말없이 둔다")]
        [SerializeField, Min(0f)] private float _revertPause = 0.6f;

        [Tooltip("켜면 등장 대화 동안 카메라가 플레이어와 보스 사이 대신 보스 자리를 비추고 아래 배율로 확대한다. 플레이어가 구석에 있어 보스·왕 말풍선이 화면 밖으로 잘리는 보스에 쓴다")]
        [SerializeField] private bool _focusOnBoss;

        [Tooltip("보스 자리를 비출 때 카메라 화면 크기 배율 (1 = 그대로, 작을수록 확대). 보스 자리 비추기를 켰을 때만 쓴다")]
        [SerializeField, Range(0.3f, 1f)] private float _focusZoom = 0.8f;

        public float SpawnPause => _spawnPause;
        public bool FocusOnBoss => _focusOnBoss;
        public float FocusZoom => _focusZoom;
        public bool KingBecomesBoss => _kingBecomesBoss;
        public float KingStandHeight => _kingStandHeight;
        public float RevertPause => _revertPause;

        public int LineCount => _lines != null ? _lines.Length : 0;

        public Line GetLine(int index)
        {
            return _lines[index];
        }

        public bool HasSpawnLine
        {
            get
            {
                for (int i = 0; i < LineCount; i++)
                {
                    if (_lines[i].SpawnBoss)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
