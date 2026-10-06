using UnityEngine;

namespace ExplodeIt.Core
{
    // 카메라가 따라가는 대상. 평소엔 플레이어 위치에 붙어 있고, 연출(보스 등장 등)이 요청하면 다른 지점으로 부드럽게 옮겨 간다.
    // 카메라 설정을 연출마다 바꾸지 않고 이 대상만 움직이므로, 흔들림·범위 제한 같은 카메라 설정은 그대로 유지된다.
    public class CameraFollowTarget : MonoBehaviour
    {
        [SerializeField] private Transform _player;

        private Vector2 _from;
        private Vector2 _focusPoint;
        private bool _isFocusing;
        private float _blendTime;
        private float _blendDuration;

        private void OnEnable()
        {
            GameEvents.CameraFocusRequested += OnFocusRequested;
            GameEvents.CameraFocusReleased += OnFocusReleased;
        }

        private void OnDisable()
        {
            GameEvents.CameraFocusRequested -= OnFocusRequested;
            GameEvents.CameraFocusReleased -= OnFocusReleased;
        }

        private void Start()
        {
            transform.position = _player.position;
        }

        // 카메라는 LateUpdate에서 대상을 읽으므로, 그 전에 위치를 정해 둔다.
        private void Update()
        {
            Vector2 goal = _isFocusing ? _focusPoint : (Vector2)_player.position;
            if (_blendTime < _blendDuration)
            {
                _blendTime += Time.deltaTime;
                float t = Mathf.Clamp01(_blendTime / _blendDuration);
                t = t * t * (3f - 2f * t);
                goal = Vector2.Lerp(_from, goal, t);
            }

            transform.position = new Vector3(goal.x, goal.y, transform.position.z);
        }

        private void OnFocusRequested(Vector2 point, float blendDuration)
        {
            StartBlend(blendDuration);
            _focusPoint = point;
            _isFocusing = true;
        }

        private void OnFocusReleased(float blendDuration)
        {
            StartBlend(blendDuration);
            _isFocusing = false;
        }

        private void StartBlend(float duration)
        {
            _from = transform.position;
            _blendTime = 0f;
            _blendDuration = duration;
        }
    }
}
