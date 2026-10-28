using System;
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.UI
{
    // 플레이 테스트 설문 링크. 타이틀·결과 창 버튼이 함께 쓴다. 설문을 바꾸면 이 에셋의 주소만 고친다.
    // 플레이 기록의 익명 번호를 설문의 번호 칸에 미리 채워 열어, 설문 답과 플레이 기록을 같은 사람끼리 이어 볼 수 있게 한다.
    [CreateAssetMenu(fileName = "SurveyData", menuName = "Explode It/Survey Data")]
    public class SurveyData : ScriptableObject
    {
        [Tooltip("설문 페이지 주소(구글 폼의 .../viewform까지, 뒤의 ?는 빼고). 비워 두면 설문 버튼을 숨긴다")]
        [SerializeField] private string _url = "";
        [Tooltip("플레이어 번호를 미리 채울 칸 이름. 구글 폼 '양식 미리 작성' 링크의 entry.숫자 부분. 비워 두면 번호 없이 연다")]
        [SerializeField] private string _playerIdEntry = "";
        [Tooltip("설문 버튼을 눌렀을 때 확인 창에 띄울 글")]
        [SerializeField, TextArea(2, 4)] private string _confirmMessage = "플레이 소감 설문 페이지를 열까요?\n(브라우저 새 창에서 열려요)";

        public string ConfirmMessage => L10n.Data(_confirmMessage);
        public bool HasUrl => !string.IsNullOrWhiteSpace(_url);

        // 번호가 없으면(기록 수집을 거절한 사람 등) 빈 칸으로 연다.
        public string BuildUrl(string playerId)
        {
            if (string.IsNullOrEmpty(_playerIdEntry) || string.IsNullOrEmpty(playerId))
            {
                return _url;
            }

            string separator = _url.Contains("?") ? "&" : "?";
            return _url + separator + "usp=pp_url&" + _playerIdEntry + "=" + Uri.EscapeDataString(playerId);
        }
    }
}
