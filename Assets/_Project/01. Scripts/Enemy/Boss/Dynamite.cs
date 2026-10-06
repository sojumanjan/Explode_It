using System;
using ExplodeIt.Bombs;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 드릴 광부의 가짜 구멍에서 튀어나와 사방으로 날아간 뒤 터지는 다이너마이트.
    // 날아가는 순간부터 떨어질 자리에 범위 원을 깔고 채워 가므로, 터지기 전까지가 곧 예고다.
    public class Dynamite : MonoBehaviour
    {
        // 전체 시간 중 날아가는 데 쓰는 비율. 나머지 동안 떨어진 자리에서 심지가 탄다.
        private const float FlightPortion = 0.55f;
        // 날아가는 동안 도는 속도 (도/초).
        private const float SpinSpeed = 720f;

        [SerializeField] private Transform _visual;
        [SerializeField] private ExplosionShape _range;
        [SerializeField] private ExplosionShape _fill;

        private Vector2 _from;
        private Vector2 _to;
        private float _duration;
        private float _height;
        private float _radius;
        private float _time;
        private Action<Dynamite> _onExplode;

        public Vector2 Position => _to;
        public float Radius => _radius;

        public void Launch(Vector2 from, Vector2 to, float duration, float height, float radius, Action<Dynamite> onExplode)
        {
            _from = from;
            _to = to;
            _duration = Mathf.Max(duration, 0.05f);
            _height = height;
            _radius = radius;
            _onExplode = onExplode;
            _time = 0f;

            // 범위 원은 떨어질 자리에 둔다. 그림만 출발점에서 날아간다.
            transform.position = to;
            gameObject.SetActive(true);
            _range.BuildCircle(radius);
            _fill.CopyLimits(_range);
            _fill.SetRadius(0f);
            _range.Visible = true;
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
                _onExplode?.Invoke(this);
            }
        }

        // 뚜잉 하고 포물선으로 튀어 올라 떨어진 뒤, 떨어진 자리에서 심지가 타며 살짝 떤다.
        private void PlaceVisual(float t)
        {
            float flight = Mathf.Clamp01(t / FlightPortion);
            float ease = 1f - (1f - flight) * (1f - flight);
            Vector2 position = Vector2.Lerp(_from, _to, ease);
            position.y += Mathf.Sin(flight * Mathf.PI) * _height;

            if (flight < 1f)
            {
                _visual.position = position;
                _visual.rotation = Quaternion.Euler(0f, 0f, -SpinSpeed * _time);
                return;
            }

            float fuse = (t - FlightPortion) / (1f - FlightPortion);
            float shake = Mathf.Sin(_time * 40f * 2f * Mathf.PI) * 0.03f * fuse;
            _visual.position = new Vector3(_to.x + shake, _to.y, 0f);
            _visual.rotation = Quaternion.Euler(0f, 0f, -20f);
        }
    }
}
