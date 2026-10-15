using UnityEngine;

namespace ExplodeIt.UI
{
    // 게임을 켜면 처음 보이는 메인 타이틀. 시작하기를 누르면 스티커북(메인 메뉴)이 펼쳐진다.
    // 설정·나가기 버튼은 페이지 넘기기와 종료 동작에 직접 연결한다.
    public class TitlePage : MonoBehaviour
    {
        // 고블린이 숨 쉬듯 위아래로 움직이는 폭(UI 단위)과 빠르기(초당 왕복 수).
        private const float GoblinBob = 10f;
        private const float GoblinBobRate = 0.6f;

        [SerializeField] private MenuPager _pager;
        [SerializeField] private CanvasGroup _bookPage;
        [SerializeField] private RectTransform _goblin;

        private Vector2 _goblinHome;

        private void Awake()
        {
            _goblinHome = _goblin.anchoredPosition;
        }

        private void Update()
        {
            float bob = Mathf.Sin(Time.unscaledTime * GoblinBobRate * Mathf.PI * 2f) * GoblinBob;
            _goblin.anchoredPosition = _goblinHome + Vector2.up * bob;
        }

        // 시작하기 버튼. 책에서 뒤로가기(Esc)를 누르면 이 화면으로 돌아온다.
        public void OpenBook()
        {
            _pager.Open(_bookPage);
        }
    }
}
