// 스토리 엔딩: 책으로 확대·하얀 화면 → 낮 컷 2장 → "그날 밤…" → 밤 컷 2장(한밤중 → 청구서) → 편지 확대 → THE END...?
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Stage
{
    [CreateAssetMenu(fileName = "OutroData", menuName = "Explode It/Story/Outro Data")]
    public class OutroData : ScriptableObject, IComicData
    {
        [Header("책으로 넘어가기")]
        [Tooltip("확대하기 전에 카메라가 책을 화면 가운데에 놓도록 옮겨 가는 시간 (초). 가운데에 오기 전에 확대하면 책 위쪽 엉뚱한 곳이 확대된다")]
        [SerializeField, Min(0.05f)] private float _bookMoveTime = 0.5f;

        [Tooltip("고블린이 빨려 들어간 책 쪽으로 카메라를 당기는 배율. 1이면 평소 크기, 작을수록 가깝게 비춘다. 책이 화면을 가득 채울 만큼 작게 둔다")]
        [SerializeField, Range(0.02f, 1f)] private float _bookZoom = 0.06f;

        [Tooltip("책 쪽으로 다가가며 확대되는 시간 (초)")]
        [SerializeField, Min(0.05f)] private float _bookZoomTime = 1.2f;

        [Tooltip("화면이 하얗게 덮이는 시간 (초). 확대가 끝나는 순간 다 하얘지도록 확대 끝무렵에 시작한다. 길면 확대가 다 보이기 전에 하얘진다")]
        [SerializeField, Min(0.05f)] private float _whiteInTime = 0.4f;

        [Tooltip("다 하얘진 채로 머무는 시간 (초). 그 사이 만화 페이지로 바뀐다")]
        [SerializeField, Min(0f)] private float _whiteHoldTime = 0.25f;

        [Tooltip("하얀 화면이 걷히며 만화 페이지가 드러나는 시간 (초)")]
        [SerializeField, Min(0.05f)] private float _whiteOutTime = 0.7f;

        [Header("만화 컷")]
        [Tooltip("컷 그림. 위에서부터 차례로 페이지의 1~4번 칸에 들어간다. 비워 두면 빈 칸(회색)으로 보인다")]
        [SerializeField] private Sprite[] _panelSprites = new Sprite[4];

        [Tooltip("컷마다 아래 상자에 나오는 대사. 컷 그림과 같은 순서")]
        [SerializeField, TextArea] private string[] _panelLines = new string[4];

        [Tooltip("컷이 튀어나오는 시간 (초)")]
        [SerializeField, Min(0.05f)] private float _panelPopTime = 0.35f;

        [Tooltip("컷 대사가 나오는 빠르기 (초당 글자 수)")]
        [SerializeField, Min(1f)] private float _captionCharsPerSecond = 20f;

        [Tooltip("컷이 튀어나올 때 소리. 비워 두면 소리 없이 나온다")]
        [SerializeField] private SoundData _panelSound;

        [Tooltip("클릭해서 넘길 때 소리")]
        [SerializeField] private SoundData _clickSound;

        [Header("시간 넘김 (낮 → 밤)")]
        [Tooltip("첫 페이지에 보여 줄 컷 수. 컷은 한 페이지에 가로 한 줄(세로 가운데)로 놓이고, 나머지 컷은 까만 화면 뒤 다음 페이지에 나온다")]
        [SerializeField, Range(1, 2)] private int _firstPageCount = 2;

        [Tooltip("두 페이지 사이 까만 화면에 뜨는 글")]
        [SerializeField] private string _nightText = "그날 밤…";

        [Tooltip("까맣게 덮이는 시간이자 글이 나타나는 시간, 다시 걷히는 시간 (초, 각각)")]
        [SerializeField, Min(0.05f)] private float _nightFadeTime = 0.6f;

        [Tooltip("글이 다 나타난 뒤 다음 페이지로 넘어가기까지 (초)")]
        [SerializeField, Min(0f)] private float _nightHoldTime = 1f;

        [Tooltip("까맣게 덮이기 시작할 때 바꿀 밤 곡. 덮이고 글이 나타나는 동안 바뀐다. 비워 두면 곡을 바꾸지 않는다")]
        [SerializeField] private BgmData _nightBgm;

        [Header("편지")]
        [Tooltip("마지막 컷을 넘기면 편지가 펼쳐지는 시간 (초). 짧을수록 탁 뜬다")]
        [SerializeField, Min(0.05f)] private float _letterOpenTime = 0.25f;

        [Tooltip("편지 내용. 서식 태그(<b>, <size=..>, <align=..>)를 쓸 수 있다")]
        [SerializeField, TextArea(6, 16)] private string _letterText;

        [Tooltip("편지가 펼쳐질 때 소리")]
        [SerializeField] private SoundData _letterSound;

        [Header("끝")]
        [Tooltip("편지를 넘기면 까만 화면에 뜨는 글")]
        [SerializeField] private string _endText = "THE END...?";

        [Tooltip("까맣게 덮이는 시간이자 끝 글이 나타나는 시간 (초)")]
        [SerializeField, Min(0.05f)] private float _endFadeTime = 0.8f;

        [Tooltip("끝 글이 다 나타난 뒤 클릭을 받기 시작할 때까지 (초). 편지를 넘기던 연타에 바로 지나가지 않게 한다")]
        [SerializeField, Min(0f)] private float _endHoldTime = 0.3f;

        [Tooltip("끝 글이 다 나타난 뒤 클릭하지 않아도 저절로 넘어갈 때까지 (초)")]
        [SerializeField, Min(0f)] private float _endAutoTime = 2.5f;

        [Tooltip("끝 글을 넘긴 뒤 결과 창이 뜨기까지 (초). 그 사이 까만 화면 위에 스토리 클리어 업적 알림이 먼저 올라온다")]
        [SerializeField, Min(0f)] private float _clearDelay = 1f;

        public int PanelCount => Mathf.Min(_panelSprites.Length, _panelLines.Length);
        public Sprite GetPanelSprite(int index) => _panelSprites[index];
        public string GetPanelLine(int index) => _panelLines[index];
        public float BookMoveTime => _bookMoveTime;
        public float BookZoom => _bookZoom;
        public float BookZoomTime => _bookZoomTime;
        public float WhiteInTime => _whiteInTime;
        public float WhiteHoldTime => _whiteHoldTime;
        public float WhiteOutTime => _whiteOutTime;
        public float PanelPopTime => _panelPopTime;
        public float CaptionCharsPerSecond => _captionCharsPerSecond;
        public SoundData PanelSound => _panelSound;
        public SoundData ClickSound => _clickSound;
        public int FirstPageCount => _firstPageCount;
        public string NightText => _nightText;
        public float NightFadeTime => _nightFadeTime;
        public float NightHoldTime => _nightHoldTime;
        public BgmData NightBgm => _nightBgm;
        public float LetterOpenTime => _letterOpenTime;
        public string LetterText => _letterText;
        public SoundData LetterSound => _letterSound;
        public string EndText => _endText;
        public float EndFadeTime => _endFadeTime;
        public float EndHoldTime => _endHoldTime;
        public float EndAutoTime => _endAutoTime;
        public float ClearDelay => _clearDelay;
    }
}
