using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 각성한 왕의 천지 가르기 예고. 왕이 선 줄에서 맵 끝까지, 한쪽 절반에 보라 웨이브가 쫘악 퍼진 뒤 반투명하게 남아 "여기를 벤다"를 알린다.
    // 베는 순간 그 절반 전체가 번쩍였다가 사라진다. 판정은 보스가 하고, 이 컴포넌트는 그림만 맡는다.
    // 맵 전체를 덮는 큰 표시라 보스를 따라다니지 않게, 보스가 시작할 때 월드로 떼어 둔다.
    public class HalfSlashWarning : MonoBehaviour
    {
        // 그림 전용 연출 값이라 데이터로 빼지 않는다.
        // 웨이브 앞머리 두께 (유닛), 다 퍼진 뒤 앞머리가 사라지는 시간 (초), 베는 순간 번쩍였다 사라지는 시간 (초).
        private const float FrontThickness = 2.5f;
        private const float FrontFadeTime = 0.3f;
        private const float FlashTime = 0.35f;
        // 맵 가장자리 밖까지 조금 더 덮어 끝이 비어 보이지 않게 한다 (유닛).
        private const float EdgeMargin = 2f;

        // 절반을 덮는 반투명 칸(흰 사각형 그림)과 퍼지는 웨이브 앞머리(한쪽으로 밝아지는 띠 그림).
        [SerializeField] private SpriteRenderer _fill;
        [SerializeField] private SpriteRenderer _front;
        [SerializeField] private Color _color = new Color(0.7f, 0.3f, 1f, 1f);
        // 다 퍼진 뒤 남는 칸의 불투명도, 웨이브 앞머리 불투명도, 베는 순간 번쩍임 불투명도.
        [SerializeField, Range(0f, 1f)] private float _fillAlpha = 0.22f;
        [SerializeField, Range(0f, 1f)] private float _frontAlpha = 0.9f;
        [SerializeField, Range(0f, 1f)] private float _flashAlpha = 0.75f;

        private Vector2 _origin;
        private Vector2 _direction;
        private float _length;
        private float _span;
        private float _sweepTime;
        private float _time;
        private float _flashStart = -1f;

        // origin: 가르는 줄 위의 한 점(왕 위치). direction: 벨 쪽(좌·우·위·아래 중 하나). sweepTime: 웨이브가 맵 끝까지 퍼지는 시간 (초).
        public void Show(Bounds arena, Vector2 origin, Vector2 direction, float sweepTime)
        {
            _origin = origin;
            _direction = direction;
            _sweepTime = Mathf.Max(sweepTime, 0.01f);
            bool isHorizontal = Mathf.Abs(direction.x) > 0.5f;
            if (isHorizontal)
            {
                _length = (direction.x > 0f ? arena.max.x - origin.x : origin.x - arena.min.x) + EdgeMargin;
                _span = arena.size.y + EdgeMargin * 2f;
            }
            else
            {
                _length = (direction.y > 0f ? arena.max.y - origin.y : origin.y - arena.min.y) + EdgeMargin;
                _span = arena.size.x + EdgeMargin * 2f;
            }

            _length = Mathf.Max(_length, 0f);
            _time = 0f;
            _flashStart = -1f;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _fill.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            _front.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            _fill.enabled = true;
            _front.enabled = true;
            gameObject.SetActive(true);
            Place(0f);
        }

        // 정해진 직사각형 한 칸(줄무늬 가르기의 한 줄). origin은 칸의 한쪽 끝 가운데, direction은 웨이브가 퍼지는 쪽,
        // length는 퍼지는 거리, span은 칸의 폭이다.
        public void ShowRect(Vector2 origin, Vector2 direction, float length, float span, float sweepTime)
        {
            _origin = origin;
            _direction = direction;
            _sweepTime = Mathf.Max(sweepTime, 0.01f);
            _length = Mathf.Max(length, 0f);
            _span = span;
            _time = 0f;
            _flashStart = -1f;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _fill.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            _front.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            _fill.enabled = true;
            _front.enabled = true;
            gameObject.SetActive(true);
            Place(0f);
        }

        // 그 절반을 벤 순간. 칸이 밝게 번쩍였다가 사라진다.
        public void Slash()
        {
            _flashStart = _time;
            _front.enabled = false;
            Place(1f);
        }

        public void Hide()
        {
            _fill.enabled = false;
            _front.enabled = false;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            _time += Time.deltaTime;
            if (_flashStart >= 0f)
            {
                float flash = Mathf.Clamp01((_time - _flashStart) / FlashTime);
                SetAlpha(_fill, _flashAlpha * (1f - flash));
                if (flash >= 1f)
                {
                    Hide();
                }

                return;
            }

            float t = Mathf.Clamp01(_time / _sweepTime);
            // 처음엔 빠르게 뻗고 끝에서 느려져 "쫘악" 퍼지는 느낌을 준다.
            Place(1f - (1f - t) * (1f - t));
            float frontFade = Mathf.Clamp01((_time - _sweepTime) / FrontFadeTime);
            SetAlpha(_front, _frontAlpha * (1f - frontFade));
            SetAlpha(_fill, _fillAlpha);
        }

        private void Place(float progress)
        {
            float length = _length * progress;
            Transform fill = _fill.transform;
            fill.position = _origin + _direction * (length * 0.5f);
            fill.localScale = new Vector3(Mathf.Max(length, 0.001f) / SpriteSize(_fill), _span / SpriteSize(_fill), 1f);

            Transform front = _front.transform;
            front.position = _origin + _direction * (length - FrontThickness * 0.5f);
            front.localScale = new Vector3(FrontThickness / SpriteWidth(_front), _span / SpriteHeight(_front), 1f);
        }

        private void SetAlpha(SpriteRenderer renderer, float alpha)
        {
            Color color = _color;
            color.a = alpha;
            renderer.color = color;
        }

        private static float SpriteSize(SpriteRenderer renderer)
        {
            return renderer.sprite != null ? renderer.sprite.bounds.size.x : 1f;
        }

        private static float SpriteWidth(SpriteRenderer renderer)
        {
            return renderer.sprite != null ? renderer.sprite.bounds.size.x : 1f;
        }

        private static float SpriteHeight(SpriteRenderer renderer)
        {
            return renderer.sprite != null ? renderer.sprite.bounds.size.y : 1f;
        }
    }
}
