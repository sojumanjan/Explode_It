// 버튼을 누르면 클릭 소리를 낸다. 소리가 따로 없는 결과·해금·일시정지 창 버튼에 붙인다.
using ExplodeIt.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ExplodeIt.UI
{
    [RequireComponent(typeof(Button))]
    public class ButtonClickSound : MonoBehaviour
    {
        // 결과 화면에서도 들려야 하므로 인터페이스 소리(SoundData의 IsInterface)를 넣는다.
        [SerializeField] private SoundData _sound;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Play);
        }

        private void Play()
        {
            AudioManager.Play(_sound);
        }
    }
}
