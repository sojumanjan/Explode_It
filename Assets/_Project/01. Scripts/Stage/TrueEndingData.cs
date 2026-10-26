// 찐엔딩: 저주 9개 + 왕관을 끼고 무한 모드에 들어가면 웨이브 없이 왕과 대화 → 각성한 왕과 결전 → 쓰러뜨리면 대화 없이
// 왕 쪽으로 천천히 다가가며 까맣게 덮이고 → 크레딧 → 마지막 문구 → 결과 창.
using ExplodeIt.Core;
using ExplodeIt.Enemies;
using UnityEngine;

namespace ExplodeIt.Stage
{
    [CreateAssetMenu(fileName = "TrueEndingData", menuName = "Explode It/Story/True Ending Data")]
    public class TrueEndingData : ScriptableObject
    {
        [Header("결전")]
        // 프리팹 칸은 오브젝트 피커가 컴포넌트 타입에 프리팹을 띄우지 않아 GameObject로 받는다.
        [Tooltip("싸울 보스(각성한 왕) 프리팹")]
        [SerializeField] private GameObject _king;

        [Tooltip("결전 전 대화. 왕이 곧 보스인 대화(Final King과 같은 방식)로 만든다")]
        [SerializeField] private BossDialogueData _dialogue;

        [Tooltip("판이 시작된 뒤 대화가 시작되기까지 (초)")]
        [SerializeField, Min(0f)] private float _startDelay = 1.5f;

        [Header("처치 후")]
        [Tooltip("왕이 쓰러진 뒤 더 깊이 다가가기 시작할 때까지 (초, 실제 시간). 0이면 쓰러질 때 왕 쪽으로 당겨지던 확대가 끊기지 않고 그대로 이어진다")]
        [SerializeField, Min(0f)] private float _defeatHold;

        [Tooltip("쓰러진 왕 쪽으로 다가가는 카메라 배율. 1이면 평소 크기, 작을수록 가깝다")]
        [SerializeField, Range(0.05f, 1f)] private float _zoom = 0.2f;

        [Tooltip("카메라가 다가가는 시간 (초, 게임 시간이라 처치 슬로모션 동안은 느리게 흐른다). 천천히 당길수록 여운이 길다")]
        [SerializeField, Min(0.1f)] private float _zoomTime = 5f;

        [Tooltip("화면이 까맣게 덮이는 시간 (초). 다가가기가 끝나는 순간 다 까매지도록 다가가는 끝무렵에 시작한다")]
        [SerializeField, Min(0.05f)] private float _coverTime = 2f;

        [Header("크레딧")]
        [Tooltip("크레딧 배경음. 까맣게 덮인 순간부터 흐른다")]
        [SerializeField] private BgmData _creditsBgm;

        [Tooltip("크레딧 배경음이 올라오는 시간 (초, 실제 시간)")]
        [SerializeField, Min(0f)] private float _bgmFadeTime = 2f;

        [Tooltip("크레딧 곡 파일 끝에 덧붙인 잔향 길이 (초). 연주가 끝나는 시각을 이만큼 앞으로 보고 크레딧·마지막 문구·까만 화면을 맞춘다. 잔향은 까만 화면 위에서 울린다")]
        [SerializeField, Min(0f)] private float _bgmTailTime = 4.5f;

        [Tooltip("까만 화면이 걷히며 크레딧이 드러나는 시간 (초)")]
        [SerializeField, Min(0.05f)] private float _revealTime = 1f;

        [Tooltip("크레딧이 드러난 뒤 왼쪽 첫 장면이 밝아지기 시작할 때까지 (초)")]
        [SerializeField, Min(0f)] private float _firstShotDelay = 4f;

        [Tooltip("왼쪽 장면이 까맣게 덮인 뒤 다음 장면이 밝아지기 시작할 때까지 까만 채로 두는 시간 (초)")]
        [SerializeField, Min(0f)] private float _shotGap = 2f;

        [Tooltip("크레딧 줄. 위에서부터 차례로 화면 아래에서 올라온다")]
        [SerializeField] private CreditsEntry[] _entries;

        [Tooltip("빈 줄 높이 (UI 단위)")]
        [SerializeField, Min(0f)] private float _spaceHeight = 120f;

        [Tooltip("캐스트 줄에 세워 두는 캐릭터 그림 높이 (UI 단위). 그림 비율은 그대로 둔다")]
        [SerializeField, Min(10f)] private float _castSpriteHeight = 150f;

        [Tooltip("크레딧이 올라가는 빠르기 (UI 단위/초). 크레딧 곡이 있으면 곡 길이에 맞춰 자동으로 정하고, 곡이 없을 때만 이 값을 쓴다")]
        [SerializeField, Min(1f)] private float _scrollSpeed = 96f;

        [Tooltip("마우스를 꾹 누르고 있을 때 빠르기 배율")]
        [SerializeField, Min(1f)] private float _holdSpeedMultiplier = 3f;

        [Tooltip("찐엔딩을 이미 본 적이 있을 때만 화면 아래에 뜨는 안내. 비우면 띄우지 않는다")]
        [SerializeField] private string _skipHint = "스페이스를 길게 눌러 건너뛰기";

        [Tooltip("건너뛰려면 스페이스를 누르고 있어야 하는 시간 (초)")]
        [SerializeField, Min(0.05f)] private float _skipHoldTime = 1f;

        [Header("마지막 문구")]
        [Tooltip("마지막 장면이 끝나면 화면 전체를 덮는 색. 보스 3국면 필살기(충격파)와 같은 보라")]
        [SerializeField] private Color _finalTintColor = new Color(0.82f, 0.6f, 1f, 1f);

        [Tooltip("화면이 보라로 다 덮이는 시간 (초). 다 덮인 뒤 그 위에 마지막 문구가 떠오른다")]
        [SerializeField, Min(0.05f)] private float _finalTintTime = 1.5f;
        [Tooltip("크레딧이 다 올라간 뒤 빈 화면으로 기다리는 시간 (초)")]
        [SerializeField, Min(0f)] private float _finalGap = 2.5f;

        [Tooltip("빈 화면 가운데에 마지막으로 떠오르는 문구")]
        [SerializeField, TextArea(2, 4)] private string _finalText = "그리고, 저주 9개와 왕관까지 짊어진 당신께 감사를.";

        [Tooltip("마지막 문구가 떠오르는 시간 (초)")]
        [SerializeField, Min(0.05f)] private float _finalFadeTime = 1.5f;

        [Tooltip("마지막 문구가 머무는 시간 (초). 문구가 다 떠오른 뒤 크레딧 곡이 이만큼 남게 맞춘다(빨리 감았으면 곡을 앞으로 옮김). 곡이 끝나기 직전에 까만 화면으로 바뀐다")]
        [SerializeField, Min(0f)] private float _finalHoldTime = 4f;

        [Tooltip("크레딧 곡이 끝나기 이만큼 전(초)에 문구·보라를 걷고 까만 화면으로 바꾼다. 곡의 마지막 소리가 까만 화면 위에서 울리게 한다")]
        [SerializeField, Min(0f)] private float _blackLeadTime = 0.3f;

        [Tooltip("크레딧 곡이 끝나기 이만큼 전(초)부터 줄여 끝의 딸깍 소리를 막는다. 길면 마지막 소리가 묻힌다")]
        [SerializeField, Min(0.01f)] private float _bgmEndFadeTime = 0.1f;

        [Tooltip("문구가 사라지고 까매진 뒤 결과 창이 뜨기까지 (초). 그 사이 까만 화면 위에 찐엔딩 업적 알림이 먼저 올라온다. 곡 잔향이 덜 끝났으면 결과 창 위에서 마저 울린다")]
        [SerializeField, Min(0f)] private float _clearDelay = 3.25f;

        public Enemy King => _king != null && _king.TryGetComponent(out Enemy enemy) ? enemy : null;
        public BossDialogueData Dialogue => _dialogue;
        public float StartDelay => _startDelay;
        public float DefeatHold => _defeatHold;
        public float Zoom => _zoom;
        public float ZoomTime => _zoomTime;
        public float CoverTime => _coverTime;
        public BgmData CreditsBgm => _creditsBgm;
        public float BgmFadeTime => _bgmFadeTime;
        public float BgmTailTime => _bgmTailTime;
        public float RevealTime => _revealTime;
        public float FirstShotDelay => _firstShotDelay;
        public float ShotGap => _shotGap;
        public CreditsEntry[] Entries => _entries;
        public float SpaceHeight => _spaceHeight;
        public float CastSpriteHeight => _castSpriteHeight;
        public float ScrollSpeed => _scrollSpeed;
        public float HoldSpeedMultiplier => _holdSpeedMultiplier;
        public string SkipHint => _skipHint;
        public float SkipHoldTime => _skipHoldTime;
        public Color FinalTintColor => _finalTintColor;
        public float FinalTintTime => _finalTintTime;        public float FinalGap => _finalGap;
        public string FinalText => _finalText;
        public float FinalFadeTime => _finalFadeTime;
        public float FinalHoldTime => _finalHoldTime;
        public float BlackLeadTime => _blackLeadTime;
        public float BgmEndFadeTime => _bgmEndFadeTime;
        public float ClearDelay => _clearDelay;

        private void OnValidate()
        {
            if (_king != null && !_king.TryGetComponent(out IBoss _))
            {
                Debug.LogWarning($"{name}: 보스 프리팹에 보스 컴포넌트가 없어 비웁니다.", this);
                _king = null;
            }
        }
    }
}
