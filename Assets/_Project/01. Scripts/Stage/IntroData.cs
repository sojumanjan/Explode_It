using System;
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 첫 판 오프닝: 만화 컷 4장(마을에서 책 속으로 빨려 들어감) → 정원(책에서 튀어나온 고블린, 놀란 왕, 병사) → 아레나로 도망쳐 들어옴.
    [CreateAssetMenu(fileName = "IntroData", menuName = "Explode It/Story/Intro Data")]
    public class IntroData : ScriptableObject
    {
        [Serializable]
        public struct Line
        {
            [Tooltip("누가 말하는지. 왕(King) 또는 고블린(Player)")]
            [SerializeField] private DialogueSpeaker _speaker;

            [Tooltip("왕이 말할 때의 표정. 고블린 줄에서는 쓰지 않는다")]
            [SerializeField] private KingMood _mood;

            [Tooltip("대사")]
            [SerializeField, TextArea] private string _text;

            [Tooltip("이 줄이 뜨는 순간 병사들이 위쪽에서 걸어 들어온다")]
            [SerializeField] private bool _callsSoldiers;

            [Tooltip("이 줄을 띄우기 전에 책이 번쩍이고 고블린이 튀어나온다. 그 앞 줄들은 고블린 없이 왕 혼자 말한다. 어느 줄에도 없으면 첫 줄 전에 튀어나온다")]
            [SerializeField] private bool _popsGoblinBefore;

            public DialogueSpeaker Speaker => _speaker;
            public KingMood Mood => _mood;
            public string Text => _text;
            public bool CallsSoldiers => _callsSoldiers;
            public bool PopsGoblinBefore => _popsGoblinBefore;
        }

        [Header("만화 컷")]
        [Tooltip("컷 그림. 위에서부터 차례로 페이지의 1~4번 칸에 들어간다. 비워 두면 빈 칸(회색)으로 보인다")]
        [SerializeField] private Sprite[] _panelSprites = new Sprite[4];

        [Tooltip("컷마다 아래 상자에 나오는 대사. 컷 그림과 같은 순서")]
        [SerializeField, TextArea] private string[] _panelLines = new string[4];

        [Tooltip("컷이 튀어나오는 시간 (초)")]
        [SerializeField, Min(0.05f)] private float _panelPopTime = 0.35f;

        [Tooltip("컷 대사가 나오는 빠르기 (초당 글자 수)")]
        [SerializeField, Min(1f)] private float _captionCharsPerSecond = 20f;

        [Tooltip("컷이 튀어나올 때 소리")]
        [SerializeField] private SoundData _panelSound;

        [Tooltip("만화가 끝나고 하얗게 번쩍이며 정원으로 바뀔 때, 하얘지는 시간과 다시 걷히는 시간 (초, 각각)")]
        [SerializeField, Min(0.05f)] private float _whiteFadeTime = 0.35f;

        [Header("정원")]
        [Tooltip("정원을 비출 때 카메라 배율. 1이면 평소 크기, 작을수록 가깝게 비춘다")]
        [SerializeField, Range(0.3f, 1.5f)] private float _gardenZoom = 0.7f;

        [Tooltip("정원이 보인 뒤 책이 번쩍이기 시작할 때까지 (초). 왕이 한가롭게 쉬는 모습을 먼저 보여 준다")]
        [SerializeField, Min(0f)] private float _gardenStartWait = 0.8f;

        [Tooltip("책이 들썩이며 번쩍이는 시간 (초)")]
        [SerializeField, Min(0f)] private float _bookGlowTime = 0.8f;

        [Tooltip("고블린이 책에서 빙글 돌며 튀어나와 착지하기까지 (초)")]
        [SerializeField, Min(0.05f)] private float _popTime = 0.5f;

        [Tooltip("튀어나올 때 포물선 높이 (유닛)")]
        [SerializeField, Min(0f)] private float _popArc = 1.5f;

        [Tooltip("튀어나오며 도는 바퀴 수")]
        [SerializeField, Min(0f)] private float _popSpins = 2f;

        [Tooltip("고블린이 착지한 뒤 다음 대사가 뜨기까지 (초). 튀어나온 모습을 잠깐 보여 준다")]
        [SerializeField, Min(0f)] private float _afterPopWait = 0.4f;

        [Tooltip("책이 번쩍일 때 소리")]
        [SerializeField] private SoundData _bookGlowSound;

        [Tooltip("고블린이 튀어나올 때 소리")]
        [SerializeField] private SoundData _popSound;

        [Tooltip("고블린이 튀어나와 땅에 착지할 때 소리")]
        [SerializeField] private SoundData _landSound;

        [Tooltip("고블린이 착지할 때 화면 흔들림·흙먼지. 비워 두면 흔들지 않는다. 오프닝은 판 진행 중이 아니라서 히트스톱은 걸리지 않는다")]
        [SerializeField] private FeedbackData _landFeedback;

        [Tooltip("정원 대사. 클릭해서 넘긴다. 마지막 줄을 넘기면 고블린이 아래로 도망친다")]
        [SerializeField] private Line[] _lines;

        [Tooltip("대사를 넘길 때 소리")]
        [SerializeField] private SoundData _clickSound;

        [Header("병사·도망")]
        [Tooltip("병사가 화면 위 밖에서 걸어 들어오는 거리 (유닛)")]
        [SerializeField, Min(0f)] private float _soldierEnterDistance = 5f;

        [Tooltip("병사가 걸어 들어오는 속도 (유닛/초)")]
        [SerializeField, Min(0.1f)] private float _soldierEnterSpeed = 3.5f;

        [Tooltip("도망치는 고블린을 쫓는 병사 속도 (유닛/초). 고블린 이동 속도보다 느려야 따돌리는 것처럼 보인다")]
        [SerializeField, Min(0.1f)] private float _soldierChaseSpeed = 4f;

        [Tooltip("고블린이 아래로 도망치는 거리 (유닛). 화면 아래 끝을 넘을 만큼 둔다")]
        [SerializeField, Min(1f)] private float _runDistance = 14f;

        [Tooltip("도망이 이 시간(초) 안에 화면 밖으로 못 나가도 아레나로 넘어간다. 소품에 걸려 멈추는 경우를 대비한다")]
        [SerializeField, Min(0.5f)] private float _chaseTimeout = 4f;

        [Header("아레나 입장")]
        [Tooltip("화면이 까맣게 덮이는 시간과 다시 걷히는 시간 (초, 각각)")]
        [SerializeField, Min(0.05f)] private float _blackFadeTime = 0.35f;

        [Tooltip("까만 화면을 유지하는 시간 (초). 그 사이 카메라가 아레나로 옮겨 간다")]
        [SerializeField, Min(0f)] private float _blackHoldTime = 0.25f;

        [Tooltip("아레나 위쪽 입구에서 고블린이 걸어 내려오는 거리 (유닛)")]
        [SerializeField, Min(0f)] private float _arenaWalkDistance = 3f;

        [Tooltip("걸어 내려오다 이 시간(초)이 지나면 그 자리에서 조작을 돌려준다")]
        [SerializeField, Min(0.1f)] private float _arenaWalkTimeout = 1.5f;

        public int PanelCount => Mathf.Min(_panelSprites.Length, _panelLines.Length);
        public Sprite GetPanelSprite(int index) => _panelSprites[index];
        public string GetPanelLine(int index) => _panelLines[index];
        public float PanelPopTime => _panelPopTime;
        public float CaptionCharsPerSecond => _captionCharsPerSecond;
        public SoundData PanelSound => _panelSound;
        public float WhiteFadeTime => _whiteFadeTime;
        public float GardenZoom => _gardenZoom;
        public float GardenStartWait => _gardenStartWait;
        public float BookGlowTime => _bookGlowTime;
        public float PopTime => _popTime;
        public float PopArc => _popArc;
        public float PopSpins => _popSpins;
        public float AfterPopWait => _afterPopWait;
        public SoundData BookGlowSound => _bookGlowSound;
        public SoundData PopSound => _popSound;
        public SoundData LandSound => _landSound;
        public FeedbackData LandFeedback => _landFeedback;
        public int LineCount => _lines != null ? _lines.Length : 0;
        public Line GetLine(int index) => _lines[index];
        public SoundData ClickSound => _clickSound;
        public float SoldierEnterDistance => _soldierEnterDistance;
        public float SoldierEnterSpeed => _soldierEnterSpeed;
        public float SoldierChaseSpeed => _soldierChaseSpeed;
        public float RunDistance => _runDistance;
        public float ChaseTimeout => _chaseTimeout;
        public float BlackFadeTime => _blackFadeTime;
        public float BlackHoldTime => _blackHoldTime;
        public float ArenaWalkDistance => _arenaWalkDistance;
        public float ArenaWalkTimeout => _arenaWalkTimeout;
    }
}
