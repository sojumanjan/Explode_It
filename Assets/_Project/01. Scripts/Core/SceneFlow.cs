using UnityEngine;
using UnityEngine.SceneManagement;

namespace ExplodeIt.Core
{
    // 씬 이동을 한곳에 모은다. 씬 이름이 바뀌면 여기만 고친다.
    // 메뉴 씬(Title)에는 타이틀 화면과 메인 메뉴(책)가 함께 있다. 게임에서 돌아오면 타이틀을 건너뛰고 메인 메뉴부터 연다.
    // 재시작도 게임 씬을 통째로 다시 불러온다. 풀, 이벤트 구독, 트윈, 상태를 하나씩 되돌리면 빠뜨리는 곳이 생기기 때문이다.
    public static class SceneFlow
    {
        private const string MenuScene = "Title";
        private const string GameScene = "Game";

        // 게임 씬에서 메뉴로 넘어왔는지. 메뉴는 이 값으로 타이틀을 건너뛰고 메인 메뉴(책)부터 연다.
        // 메뉴에서 페이지를 넘긴 기록만 보면, 게임 씬에서 바로 시작한 판에서 나갈 때 타이틀로 돌아가 버린다.
        public static bool IsReturningFromGame { get; private set; }
        // 게임에서 "타이틀로"를 눌러 나왔는지. 메뉴는 메인 메뉴 대신 타이틀 화면부터 연다.
        public static bool OpensTitle { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            IsReturningFromGame = false;
            OpensTitle = false;
        }

        public static void LoadMainMenu()
        {
            IsReturningFromGame = true;
            OpensTitle = false;
            Load(MenuScene);
        }

        public static void LoadTitle()
        {
            IsReturningFromGame = true;
            OpensTitle = true;
            Load(MenuScene);
        }

        public static void LoadGame()
        {
            IsReturningFromGame = false;
            OpensTitle = false;
            Load(GameScene);
        }

        // timeScale은 씬을 넘어 유지되므로, 일시정지·배속 상태로 다음 씬이 시작되지 않게 되돌린다.
        private static void Load(string sceneName)
        {
            Time.timeScale = 1f;
            AudioManager.SetWorldMuted(false);
            SceneManager.LoadScene(sceneName);
        }
    }
}
