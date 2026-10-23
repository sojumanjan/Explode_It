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

        [Tooltip("까만 화면이 걷히며 크레딧이 드러나는 시간 (초)")]
        [SerializeField, Min(0.05f)] private float _revealTime = 1f;

        [Tooltip("크레딧이 드러난 뒤 왼쪽 첫 장면이 밝아지기 시작할 때까지 (초)")]
        [SerializeField, Min(0f)] private float _firstShotDelay = 4f;

        [Tooltip("크레딧 줄. 위에서부터 차례로 화면 아래에서 올라온다")]
        [SerializeField] private CreditsEntry[] _entries;

        [Tooltip("빈 줄 높이 (UI 단위)")]
        [SerializeField, Min(0f)] private float _spaceHeight = 120f;

        [Tooltip("캐스트 줄에 세워 두는 캐릭터 그림 높이 (UI 단위). 그림 비율은 그대로 둔다")]
        [SerializeField, Min(10f)] private float _castSpriteHeight = 150f;

        [Tooltip("크레딧이 올라가는 빠르기 (UI 단위/초)")]
        [SerializeField, Min(1f)] private float _scrollSpeed = 96f;

        [Tooltip("마우스를 꾹 누르고 있을 때 빠르기 배율")]
        [SerializeField, Min(1f)] private float _holdSpeedMultiplier = 3f;

        [Tooltip("찐엔딩을 이미 본 적이 있을 때만 화면 아래에 뜨는 안내. 비우면 띄우지 않는다")]
        [SerializeField] private string _skipHint = "스페이스: 건너뛰기";

        [Header("마지막 문구")]
        [Tooltip("크레딧이 다 올라간 뒤 빈 화면으로 기다리는 시간 (초)")]
        [SerializeField, Min(0f)] private float _finalGap = 2.5f;

        [Tooltip("빈 화면 가운데에 마지막으로 떠오르는 문구")]
        [SerializeField, TextArea(2, 4)] private string _finalText = "그리고, 저주 9개와 왕관까지 짊어진 당신께 감사를.";

        [Tooltip("마지막 문구가 떠오르는 시간 (초)")]
        [SerializeField, Min(0.05f)] private float _finalFadeTime = 1.5f;

        [Tooltip("마지막 문구가 머무는 시간 (초)")]
        [SerializeField, Min(0f)] private float _finalHoldTime = 4f;

        [Tooltip("마지막 문구가 사라지는 시간 (초)")]
        [SerializeField, Min(0.05f)] private float _endFadeTime = 1.5f;

        [Tooltip("문구가 사라진 뒤 결과 창이 뜨기까지 (초). 그 사이 까만 화면 위에 찐엔딩 업적 알림이 먼저 올라온다")]
        [SerializeField, Min(0f)] private float _clearDelay = 1f;

        public Enemy King => _king != null && _king.TryGetComponent(out Enemy enemy) ? enemy : null;
        public BossDialogueData Dialogue => _dialogue;
        public float StartDelay => _startDelay;
        public float DefeatHold => _defeatHold;
        public float Zoom => _zoom;
        public float ZoomTime => _zoomTime;
        public float CoverTime => _coverTime;
        public BgmData CreditsBgm => _creditsBgm;
        public float BgmFadeTime => _bgmFadeTime;
        public float RevealTime => _revealTime;
        public float FirstShotDelay => _firstShotDelay;
        public CreditsEntry[] Entries => _entries;
        public float SpaceHeight => _spaceHeight;
        public float CastSpriteHeight => _castSpriteHeight;
        public float ScrollSpeed => _scrollSpeed;
        public float HoldSpeedMultiplier => _holdSpeedMultiplier;
        public string SkipHint => _skipHint;
        public float FinalGap => _finalGap;
        public string FinalText => _finalText;
        public float FinalFadeTime => _finalFadeTime;
        public float FinalHoldTime => _finalHoldTime;
        public float EndFadeTime => _endFadeTime;
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
