using UnityEngine;

namespace ExplodeIt.UI
{
    // 설문 버튼. 실수로 눌러 게임 밖으로 튕겨 나가지 않게 확인 창을 거친 뒤 설문 페이지를 연다.
    // 웹 빌드는 사용자가 누른 직후에만 새 창을 허락하므로, 확인 버튼을 누른 그 자리에서 바로 연다.
    public class SurveyButton : MonoBehaviour
    {
        [SerializeField] private SurveyData _data;
        [SerializeField] private ConfirmDialog _dialog;

        private void Awake()
        {
            if (_data == null || !_data.HasUrl)
            {
                gameObject.SetActive(false);
            }
        }

        // 버튼 OnClick에 연결한다.
        public void Open()
        {
            _dialog.Show(_data.ConfirmMessage, OpenSurvey);
        }

        private void OpenSurvey()
        {
            Application.OpenURL(_data.BuildUrl(Core.PlayAnalytics.UserId));
        }
    }
}
