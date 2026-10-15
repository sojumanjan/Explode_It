namespace ExplodeIt.UI
{
    // 메뉴 페이지가 열리고 닫힐 때 MenuPager가 알려 준다. 열릴 때마다 저장 내용으로 화면을 새로 맞춘다.
    public interface IMenuPage
    {
        void OnPageOpened();
        void OnPageClosed();
    }
}
