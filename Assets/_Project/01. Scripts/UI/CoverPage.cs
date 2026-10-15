using UnityEngine;

namespace ExplodeIt.UI
{
    // 메뉴 첫 화면(스티커북 표지). 아무 데나 누르면 책이 펼쳐지며 메인 메뉴로 넘어간다.
    // 웹은 사용자가 한 번 눌러야 소리가 나므로, 이 클릭이 소리를 깨우는 역할도 한다.
    public class CoverPage : MonoBehaviour
    {
        // "클릭해서 시작" 표시가 위아래로 튀는 폭(UI 단위)과 빠르기.
        private const float HintBob = 12f;
        private const float HintBobRate = 1.5f;

        [SerializeField] private MenuPager _pager;
        [SerializeField] private CanvasGroup _mainPage;
        [SerializeField] private RectTransform _hint;

        private Vector2 _hintHome;

        private void Awake()
        {
            _hintHome = _hint.anchoredPosition;
        }

        private void Update()
        {
            float bob = Mathf.Abs(Mathf.Sin(Time.unscaledTime * HintBobRate * Mathf.PI)) * HintBob;
            _hint.anchoredPosition = _hintHome + Vector2.up * bob;
        }

        // 화면 전체를 덮는 버튼에 연결한다.
        public void Enter()
        {
            _pager.OpenRoot(_mainPage);
        }
    }
}
