using UnityEngine;

namespace ExplodeIt.UI
{
    // 웹 빌드에서는 종료가 동작하지 않으므로 종료 버튼 같은 요소를 숨긴다.
    public class HideOnWeb : MonoBehaviour
    {
        private void Awake()
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
