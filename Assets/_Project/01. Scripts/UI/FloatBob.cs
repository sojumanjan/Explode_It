// 트레일러 엔드 카드의 캐릭터를 위아래로 둥둥 떠다니게 하는 연출용 MonoBehaviour
using UnityEngine;

namespace ExplodeIt.UI
{
    public class FloatBob : MonoBehaviour
    {
        [Tooltip("위아래로 움직이는 폭(유닛)")]
        [SerializeField] private float _height = 0.15f;
        [Tooltip("한 번 오르내리는 데 걸리는 시간(초)")]
        [SerializeField] private float _period = 2.4f;
        [Tooltip("좌우로 기우는 각도(도). 0이면 기울지 않는다")]
        [SerializeField] private float _tilt = 2f;
        [Tooltip("시작 위상(0~1). 둘이 같은 박자로 움직이지 않게 엇갈린다")]
        [SerializeField, Range(0f, 1f)] private float _phase;

        private Vector3 _basePosition;
        private Quaternion _baseRotation;

        private void Awake()
        {
            _basePosition = transform.localPosition;
            _baseRotation = transform.localRotation;
        }

        private void Update()
        {
            float t = (Time.time / _period + _phase) * 2f * Mathf.PI;
            transform.localPosition = _basePosition + Vector3.up * (Mathf.Sin(t) * _height);
            // 기울기는 위치보다 4분의 1박자 늦게 따라가 흔들리는 느낌을 낸다
            transform.localRotation = _baseRotation * Quaternion.Euler(0f, 0f, Mathf.Cos(t) * _tilt);
        }
    }
}
