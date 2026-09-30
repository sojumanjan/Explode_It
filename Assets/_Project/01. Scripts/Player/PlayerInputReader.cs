using UnityEngine;
using UnityEngine.InputSystem;

namespace ExplodeIt.Player
{
    // 입력을 이동·투척 로직과 분리해 두면, 대시 같은 새 조작이 생겨도 읽는 곳만 늘리면 된다.
    public class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionReference _moveAction;
        [SerializeField] private InputActionReference _aimAction;
        [SerializeField] private InputActionReference _throwAction;

        public Vector2 Move => _moveAction.action.ReadValue<Vector2>();
        // 화면 좌표. 월드 좌표 변환은 카메라를 아는 쪽에서 한다.
        public Vector2 Aim => _aimAction.action.ReadValue<Vector2>();
        // 누르고 있으면 연사 간격마다 계속 던지도록 눌림 상태로 읽는다.
        public bool ThrowHeld => _throwAction.action.IsPressed();

        private void OnEnable()
        {
            _moveAction.action.Enable();
            _aimAction.action.Enable();
            _throwAction.action.Enable();
        }

        private void OnDisable()
        {
            _moveAction.action.Disable();
            _aimAction.action.Disable();
            _throwAction.action.Disable();
        }
    }
}
