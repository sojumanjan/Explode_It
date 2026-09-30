using UnityEngine;
using UnityEngine.InputSystem;

namespace ExplodeIt.Player
{
    // 입력을 이동·투척 로직과 분리해 두면, 대시 같은 새 조작이 생겨도 읽는 곳만 늘리면 된다.
    public class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionReference _moveAction;

        public Vector2 Move => _moveAction.action.ReadValue<Vector2>();

        private void OnEnable()
        {
            _moveAction.action.Enable();
        }

        private void OnDisable()
        {
            _moveAction.action.Disable();
        }
    }
}
