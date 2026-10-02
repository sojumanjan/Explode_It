using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Player
{
    // 주인공 그림의 대기·걷기 전환과 좌우 방향. 판정과 무관한 표시만 맡는다.
    // 걷는 중에도 항상 커서(던질 곳) 쪽을 본다. 폭탄을 어디로 던질지가 이동 방향보다 중요한 정보다.
    public class PlayerVisual : MonoBehaviour
    {
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

        // 이보다 느리면 서 있는 것으로 본다. 멈출 때 감속 중 걷기 모션이 잠깐 남는 정도로 둔다.
        private const float MovingSpeed = 0.5f;
        private const float TurnThreshold = 0.05f;

        [SerializeField] private SpriteRenderer _sprite;
        [SerializeField] private Animator _animator;
        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private PlayerInputReader _input;
        // 시트마다 그림이 보는 방향이 달라서 동작별로 둔다.
        [SerializeField] private bool _idleArtFacesRight = true;
        [SerializeField] private bool _walkArtFacesRight;

        private Camera _camera;
        private bool _facesRight = true;

        private void Awake()
        {
            _camera = Camera.main;
        }

        private void OnEnable()
        {
            GameEvents.GameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.GameStateChanged -= OnGameStateChanged;
        }

        private void LateUpdate()
        {
            Vector2 velocity = _rigidbody.linearVelocity;
            bool isMoving = velocity.sqrMagnitude > MovingSpeed * MovingSpeed;
            _animator.SetBool(IsMovingHash, isMoving);

            float faceX = _camera.ScreenToWorldPoint(_input.Aim).x - transform.position.x;
            if (Mathf.Abs(faceX) > TurnThreshold)
            {
                _facesRight = faceX > 0f;
            }

            bool artFacesRight = isMoving ? _walkArtFacesRight : _idleArtFacesRight;
            _sprite.flipX = _facesRight != artFacesRight;
        }

        // 죽은 뒤 회색으로 멈춘 모습이 "끝났다"는 신호가 되도록 모션을 세운다. 일시정지는 timeScale이 알아서 멈춘다.
        private void OnGameStateChanged(GameState previous, GameState current)
        {
            _animator.speed = current == GameState.PlayerDead ? 0f : 1f;
            enabled = current != GameState.PlayerDead;
        }
    }
}
