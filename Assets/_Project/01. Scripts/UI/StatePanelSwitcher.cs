using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.UI
{
    // 꺼진 오브젝트는 이벤트를 구독할 수 없으므로, 항상 켜져 있는 Canvas에서 상태에 맞는 패널을 켜고 끈다.
    // 자식에서 꺼진 패널까지 모두 찾으므로 패널을 따로 연결하지 않아도 된다.
    public class StatePanelSwitcher : MonoBehaviour
    {
        private StatePanel[] _panels;

        private void Awake()
        {
            _panels = GetComponentsInChildren<StatePanel>(true);
            // 에디터에서 켜 둔 채 저장했더라도 시작할 때는 모두 숨긴다.
            for (int i = 0; i < _panels.Length; i++)
            {
                _panels[i].Hide();
            }
        }

        private void OnEnable()
        {
            GameEvents.GameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.GameStateChanged -= OnGameStateChanged;
        }

        private void OnGameStateChanged(GameState previous, GameState current)
        {
            for (int i = 0; i < _panels.Length; i++)
            {
                if (_panels[i].ShowOn == current)
                {
                    _panels[i].Show();
                }
                else
                {
                    _panels[i].Hide();
                }
            }
        }
    }
}
