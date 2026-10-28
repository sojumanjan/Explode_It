using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace ExplodeIt.UI
{
    // 게임을 켜면 처음 보이는 메인 타이틀. 시작하기를 누르면 스티커북(메인 메뉴)이 펼쳐진다.
    // 첫 판 결과 창을 보기 전(처음 켠 사람)은 책 대신 곧장 첫 스토리 판(오프닝부터)으로 들어간다.
    // 설정·나가기 버튼은 페이지 넘기기와 종료 동작에 직접 연결한다.
    public class TitlePage : MonoBehaviour
    {
        // 고블린이 숨 쉬듯 위아래로 움직이는 폭(UI 단위)과 빠르기(초당 왕복 수).
        private const float GoblinBob = 10f;
        private const float GoblinBobRate = 0.6f;

        [SerializeField] private MenuPager _pager;
        [SerializeField] private CanvasGroup _bookPage;
        // 첫 판으로 바로 들어갈 때 장착 확인과 판 시작을 메인 메뉴와 똑같이 하려고 메인 메뉴 페이지를 빌려 쓴다.
        [SerializeField] private MainMenuPage _mainMenu;
        [SerializeField] private RectTransform _goblin;
        // 처음 켰을 때 타이틀 위에 띄우는 언어 고르기와 플레이 기록 수집 동의 창. 언어를 먼저 골라야 동의 창을 읽을 수 있다.
        [SerializeField] private LanguagePickDialog _language;
        [SerializeField] private AnalyticsConsentDialog _consent;

        private Vector2 _goblinHome;

        private void Awake()
        {
            _goblinHome = _goblin.anchoredPosition;
        }

        // 웹은 번역 표를 비동기로 불러오므로, 준비가 끝난 뒤에 동의 창을 띄워야 고른 언어로 나온다.
        private IEnumerator Start()
        {
            yield return LocalizationSettings.InitializationOperation;
            if (_language.ShowIfNeeded())
            {
                yield return new WaitUntil(() => !_language.IsOpen);
                yield return Core.L10n.WaitForTables();
            }

            _consent.ShowIfNeeded();
        }

        private void Update()
        {
            float bob = Mathf.Sin(Time.unscaledTime * GoblinBobRate * Mathf.PI * 2f) * GoblinBob;
            _goblin.anchoredPosition = _goblinHome + Vector2.up * bob;
        }

        // 시작하기 버튼. 책에서 뒤로가기(Esc)를 누르면 이 화면으로 돌아온다.
        public void OpenBook()
        {
            if (!Core.SaveSystem.Data.FirstRunDone)
            {
                _mainMenu.StartFirstRun();
                return;
            }

            _pager.Open(_bookPage);
        }
    }
}
