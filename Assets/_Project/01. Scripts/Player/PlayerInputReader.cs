using ExplodeIt.Core;
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
        [SerializeField] private InputActionReference _abilityAction;
        [SerializeField] private InputActionReference _dodgeAction;

        // 보스 등장 같은 연출 동안에는 이동·투척·능력·구르기를 막는다. 조준(바라보는 방향)은 그대로 둔다.
        public bool IsLocked { get; private set; }

        public Vector2 Move => IsLocked ? Vector2.zero : _moveAction.action.ReadValue<Vector2>();
        // 화면 좌표. 월드 좌표 변환은 카메라를 아는 쪽에서 한다.
        public Vector2 Aim => _aimAction.action.ReadValue<Vector2>();
        // 한 번 누를 때 한 번만 던진다. 꾹 누르고 있어도 연사되지 않게 누른 프레임만 읽는다.
        public bool ThrowPressed => !IsLocked && !PointerBlocked && _throwAction.action.WasPressedThisFrame();
        // 누르고 있는 동안 계속 참. 순간폭탄 스킬 동안만 꾹 눌러 연사하는 데 쓴다.
        public bool ThrowHeld => !IsLocked && !PointerBlocked && _throwAction.action.IsPressed();
        // 게이지가 차는 순간 누르고 있던 버튼으로 바로 나가지 않도록, 누른 프레임만 읽는다.
        public bool AbilityPressed => !IsLocked && !PointerBlocked && !IsAbilityBlocked && _abilityAction.action.WasPressedThisFrame();
        // 키보드 입력이라 패널 위 커서와 무관하게 받는다.
        public bool DodgePressed => !IsLocked && _dodgeAction.action.WasPressedThisFrame();

        // 커서가 개발자 패널 같은 화면 UI 위에 있을 때, 슬라이더를 누르는 클릭이 투척으로 새지 않게 막는다.
        public bool PointerBlocked { get; set; }

        // 튜토리얼이 스킬 차례가 오기 전에 게이지를 써 버리지 않게 막을 때 쓴다.
        public bool IsAbilityBlocked { get; set; }

        private void OnEnable()
        {
            _moveAction.action.Enable();
            _aimAction.action.Enable();
            _throwAction.action.Enable();
            _abilityAction.action.Enable();
            _dodgeAction.action.Enable();
            GameEvents.PlayerControlLockChanged += OnControlLockChanged;
        }

        private void OnDisable()
        {
            _moveAction.action.Disable();
            _aimAction.action.Disable();
            _throwAction.action.Disable();
            _abilityAction.action.Disable();
            _dodgeAction.action.Disable();
            GameEvents.PlayerControlLockChanged -= OnControlLockChanged;
        }

        private void OnControlLockChanged(bool isLocked)
        {
            IsLocked = isLocked;
        }
    }
}
