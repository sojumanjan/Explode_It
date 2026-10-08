using Unity.Cinemachine;
using UnityEngine;

namespace ExplodeIt.Core
{
    // 연출이 요청하면 카메라 화면 크기를 부드럽게 줄이고 늘린다(보스 변신 때 확대 등). 플레이어 카메라에 붙인다.
    // 연출은 카메라 설정을 모르고 배율만 요청하므로, 평소 화면 크기는 카메라에 정해 둔 값 그대로 기준이 된다.
    public class CameraZoom : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera _camera;
        // 화면 크기가 바뀌면 맵 밖이 보이지 않게 막는 범위도 다시 계산해야 한다. 없으면 비워 둔다.
        [SerializeField] private CinemachineConfiner2D _confiner;

        private float _baseSize;
        private float _fromZoom = 1f;
        private float _toZoom = 1f;
        private float _zoom = 1f;
        private float _blendTime;
        private float _blendDuration;

        private void Awake()
        {
            _baseSize = _camera.Lens.OrthographicSize;
        }

        private void OnEnable()
        {
            GameEvents.CameraZoomRequested += OnZoomRequested;
        }

        private void OnDisable()
        {
            GameEvents.CameraZoomRequested -= OnZoomRequested;
        }

        private void Update()
        {
            if (Mathf.Approximately(_zoom, _toZoom))
            {
                return;
            }

            _blendTime += Time.deltaTime;
            float t = _blendDuration > 0f ? Mathf.Clamp01(_blendTime / _blendDuration) : 1f;
            t = t * t * (3f - 2f * t);
            SetZoom(Mathf.Lerp(_fromZoom, _toZoom, t));
        }

        private void OnZoomRequested(float zoom, float blendDuration)
        {
            _fromZoom = _zoom;
            _toZoom = zoom;
            _blendTime = 0f;
            _blendDuration = blendDuration;
            if (blendDuration <= 0f)
            {
                SetZoom(zoom);
            }
        }

        private void SetZoom(float zoom)
        {
            _zoom = zoom;
            LensSettings lens = _camera.Lens;
            lens.OrthographicSize = _baseSize * zoom;
            _camera.Lens = lens;
            if (_confiner != null)
            {
                _confiner.InvalidateLensCache();
            }
        }
    }
}
