using System;
using ExplodeIt.Bombs;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 화염 마법사의 메테오 하나. 떨어질 자리에 범위 원을 깔고 채워 가다가, 다 차는 순간 하늘에서 떨어져 터진다.
    // 원이 차오르는 시간이 곧 예고다.
    public class Meteor : MonoBehaviour
    {
        // 전체 시간 중 마지막에 떨어지는 구간의 비율과 떨어지기 시작하는 높이(유닛). 그림 전용 연출이라 데이터로 빼지 않는다.
        private const float FallPortion = 0.3f;
        private const float FallHeight = 6f;

        [SerializeField] private Transform _visual;
        [SerializeField] private ExplosionShape _range;
        // 범위 안에서 차오르며 남은 시간을 보여준다.
        [SerializeField] private ExplosionShape _fill;

        private Vector2 _position;
        private float _duration;
        private float _radius;
        private float _time;
        private Action<Meteor> _onImpact;

        public Vector2 Position => _position;
        public float Radius => _radius;

        public void Launch(Vector2 position, float duration, float radius, Action<Meteor> onImpact)
        {
            _position = position;
            _duration = Mathf.Max(duration, 0.05f);
            _radius = radius;
            _onImpact = onImpact;
            _time = 0f;

            transform.position = position;
            gameObject.SetActive(true);
            _range.BuildCircle(radius);
            _range.Visible = true;
            _fill.CopyLimits(_range);
            _fill.SetRadius(0f);
            _fill.Visible = true;
            PlaceVisual(0f);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void Update()
        {
            _time += Time.deltaTime;
            float t = _time / _duration;
            _fill.SetRadius(_radius * Mathf.Clamp01(t));
            PlaceVisual(t);

            if (t >= 1f)
            {
                Hide();
                _onImpact?.Invoke(this);
            }
        }

        // 원이 차는 동안은 보이지 않다가, 마지막 구간에 위에서 빠르게 내리꽂힌다.
        private void PlaceVisual(float t)
        {
            float fallStart = 1f - FallPortion;
            if (t < fallStart)
            {
                _visual.gameObject.SetActive(false);
                return;
            }

            _visual.gameObject.SetActive(true);
            float fall = Mathf.Clamp01((t - fallStart) / FallPortion);
            _visual.position = new Vector3(_position.x, _position.y + FallHeight * (1f - fall * fall), 0f);
        }
    }
}
