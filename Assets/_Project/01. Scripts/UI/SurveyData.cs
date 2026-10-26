using UnityEngine;

namespace ExplodeIt.UI
{
    // 플레이 테스트 설문 링크. 타이틀·결과 창 버튼이 함께 쓴다. 설문을 바꾸면 이 에셋의 주소만 고친다.
    [CreateAssetMenu(fileName = "SurveyData", menuName = "Explode It/Survey Data")]
    public class SurveyData : ScriptableObject
    {
        [Tooltip("설문 페이지 주소(구글 폼 등). 비워 두면 설문 버튼을 숨긴다")]
        [SerializeField] private string _url = "";
        [Tooltip("설문 버튼을 눌렀을 때 확인 창에 띄울 글")]
        [SerializeField, TextArea(2, 4)] private string _confirmMessage = "플레이 소감 설문 페이지를 열까요?\n(브라우저 새 창에서 열려요)";

        public string Url => _url;
        public string ConfirmMessage => _confirmMessage;
        public bool HasUrl => !string.IsNullOrWhiteSpace(_url);
    }
}
