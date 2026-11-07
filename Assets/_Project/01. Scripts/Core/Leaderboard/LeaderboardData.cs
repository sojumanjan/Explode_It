// 무한 모드 순위표(Unity Leaderboards) 설정 데이터
using UnityEngine;

namespace ExplodeIt.Core
{
    // 순위표 ID는 Unity Cloud 대시보드에서 만든 순위표와 같아야 한다. 이름 규칙과 부정 기록 거르기 수치도 여기서 조절한다.
    [CreateAssetMenu(fileName = "LeaderboardData", menuName = "Explode It/Leaderboard Data")]
    public class LeaderboardData : ScriptableObject
    {
        [Tooltip("순위표 기능 전체 켜기. 끄면 닉네임을 묻지도, 올리지도, 불러오지도 않는다")]
        [SerializeField] private bool _isEnabled = true;

        [Tooltip("대시보드에서 만든 순위표 ID. 정렬은 내림차순, 기록 방식은 최고 기록 유지(Keep Best)로 만든다")]
        [SerializeField] private string _leaderboardId = "endless_kills";

        [Tooltip("에디터에서 플레이할 때도 실제로 올릴지. 끄면 개발 중 기록이 순위표에 섞이지 않는다(불러오기는 한다)")]
        [SerializeField] private bool _submitFromEditor;

        [Tooltip("순위표 패널 한 페이지에 보여 줄 인원. 패널에 미리 놓아 둔 줄 수보다 크면 남는 순위는 안 보인다")]
        [SerializeField, Range(1, 50)] private int _pageSize = 10;

        [Header("이름")]
        [Tooltip("이름 최소 글자 수")]
        [SerializeField, Range(1, 10)] private int _nameMinLength = 2;

        [Tooltip("이름 최대 글자 수. 서버 한도(50자)보다 작게, 순위표 한 줄에 들어가게 둔다")]
        [SerializeField, Range(2, 30)] private int _nameMaxLength = 12;

        [Tooltip("이름에 들어가면 막을 말. 대소문자는 가리지 않고, 띄어쓰기를 지운 이름에서 찾는다")]
        [SerializeField] private string[] _bannedWords = new string[0];

        [Header("부정 기록 거르기")]
        [Tooltip("버틴 시간 1초당 처치할 수 있는 최대 마리 수. 이보다 많으면 조작으로 보고 올리지 않는다")]
        [SerializeField, Min(0.1f)] private float _maxKillsPerSecond = 6f;

        [Header("네트워크")]
        [Tooltip("서버 요청 하나를 기다리는 최대 시간 (초). 넘으면 실패로 보고 게임은 그대로 진행한다")]
        [SerializeField, Min(1f)] private float _requestTimeout = 8f;

        public bool IsEnabled => _isEnabled;
        public string LeaderboardId => _leaderboardId;
        public bool SubmitFromEditor => _submitFromEditor;
        public int PageSize => _pageSize;
        public int NameMinLength => _nameMinLength;
        public int NameMaxLength => _nameMaxLength;
        public string[] BannedWords => _bannedWords;
        public float MaxKillsPerSecond => _maxKillsPerSecond;
        public float RequestTimeout => _requestTimeout;
    }
}
