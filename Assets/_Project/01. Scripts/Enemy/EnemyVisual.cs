using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 적 그림의 방향과 걷기 모션. 판정·상태와 무관한 표시만 맡는다.
    // 걷기 모션은 이동 중에만 재생하고, 멈춰 조준하는 동안은 프레임을 세운다. 제자리걸음은 "멈췄다"는 예고를 흐리기 때문이다.
    public class EnemyVisual : MonoBehaviour
    {
        // 그림이 원래 어느 쪽을 보고 그려졌는지. 반대쪽으로 갈 때 뒤집는다.
        [SerializeField] private bool _artFacesRight;
        [SerializeField] private Enemy _enemy;
        [SerializeField] private SpriteRenderer _sprite;
        // 비워 두면 방향만 바꾼다.
        [SerializeField] private Animator _animator;

        // 이 이하로 움직이면 방향을 바꾸지 않는다. 벽에 붙어 미세하게 흔들릴 때 좌우로 깜빡이지 않게 한다.
        private const float TurnThreshold = 0.002f;

        private Vector2 _lastPosition;

        private void OnEnable()
        {
            _lastPosition = transform.position;
        }

        private void LateUpdate()
        {
            Vector2 position = transform.position;
            float deltaX = position.x - _lastPosition.x;
            _lastPosition = position;

            EnemyState state = _enemy.CurrentState;
            bool isMoving = state == EnemyState.Move;

            // 걸을 때는 가는 방향을, 멈춰 있을 때는 플레이어 쪽을 본다. 벽을 돌아가는 중에도 걸음 방향이 자연스럽다.
            float faceX = isMoving && Mathf.Abs(deltaX) > TurnThreshold
                ? deltaX
                : _enemy.TargetPosition.x - position.x;
            if (Mathf.Abs(faceX) > TurnThreshold)
            {
                bool facesRight = faceX > 0f;
                _sprite.flipX = facesRight != _artFacesRight;
            }

            if (_animator != null)
            {
                _animator.speed = isMoving ? 1f : 0f;
            }
        }
    }
}
