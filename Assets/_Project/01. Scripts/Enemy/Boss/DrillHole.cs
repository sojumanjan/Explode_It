using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 노장 광부가 뚫고 나올 구멍 하나의 예고 표시. 나타나는 순간 흙더미가 뽀잉 솟고, 분출이 다가올수록 점점 세게 떨다가 터진다.
    // 흙더미 가운데로 진짜 구멍은 드릴 끝, 가짜 구멍은 다이너마이트가 보여서 자세히 보면 구분된다.
    // 범위 원이나 남은 시간 표시는 두지 않는다. 그림을 덮어 진짜·가짜를 못 알아보게 되고, 떨림이 세지는 것만으로 터질 때를 감으로 익히게 한다.
    // 흙더미 크기가 곧 분출 범위다. 분출하면 흙더미가 쏙 빠르게 꺼지며 사라진다.
    public class DrillHole : MonoBehaviour
    {
        // 그림 전용 연출 값이라 데이터로 빼지 않는다.
        // 흙더미가 0에서 솟는 시간 (초)과 넘쳤다 돌아오는 세기(OutBack 계수), 분출할 때 0으로 꺼지는 시간 (초).
        private const float PopDuration = 0.25f;
        private const float PopOvershoot = 1.7f;
        private const float CollapseDuration = 0.12f;
        // 분출 직전 흙더미가 떠는 가장 큰 폭 (유닛)과 빠르기 (rad/초). 분출이 다가올수록 세진다.
        private const float ShakeMax = 0.1f;
        private const float ShakeSpeed = 70f;
        // 흙더미 그림에서 빽빽한 흙 몸통이 차지하는 반경 비율. 바깥의 흩어진 흙덩이는 범위에 넣지 않는다.
        private const float BodyCoverage = 0.86f;

        [SerializeField] private SpriteRenderer _mound;
        [SerializeField] private Sprite _realSprite;
        [SerializeField] private Sprite _fakeSprite;

        private float _moundScale;
        private float _popTime = -1f;
        private float _collapseTime = -1f;
        private float _shake;

        // radius: 분출 범위 (유닛). 흙더미 몸통이 이 반경에 맞게 그려진다.
        public void Show(Vector2 position, float radius, bool isReal)
        {
            transform.position = position;
            _mound.sprite = isReal ? _realSprite : _fakeSprite;
            float spriteSize = _mound.sprite != null ? _mound.sprite.bounds.size.x : 1f;
            _moundScale = radius * 2f / (spriteSize * BodyCoverage);
            _mound.enabled = true;
            _shake = 0f;
            _popTime = 0f;
            _collapseTime = -1f;
            SetMoundSize(0f);
            gameObject.SetActive(true);
        }

        // progress: 0(표시 순간) ~ 1(분출 순간). 끝으로 갈수록 가파르게 세게 떤다.
        public void SetProgress(float progress)
        {
            float t = Mathf.Clamp01(progress);
            _shake = ShakeMax * t * t;
        }

        // 분출 순간. 흙더미가 쏙 꺼진 뒤 사라진다.
        public void Collapse()
        {
            _shake = 0f;
            _popTime = -1f;
            _collapseTime = 0f;
        }

        public void Hide()
        {
            _popTime = -1f;
            _collapseTime = -1f;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            float time = Time.time;
            _mound.transform.localPosition = new Vector3(Mathf.Sin(time * ShakeSpeed), 0.5f * Mathf.Sin(time * ShakeSpeed * 0.8f), 0f) * _shake;

            if (_popTime >= 0f)
            {
                _popTime += Time.deltaTime;
                float t = Mathf.Clamp01(_popTime / PopDuration) - 1f;
                SetMoundSize(1f + (PopOvershoot + 1f) * t * t * t + PopOvershoot * t * t);
                if (_popTime >= PopDuration)
                {
                    _popTime = -1f;
                    SetMoundSize(1f);
                }
            }

            if (_collapseTime >= 0f)
            {
                _collapseTime += Time.deltaTime;
                float t = Mathf.Clamp01(_collapseTime / CollapseDuration);
                SetMoundSize(1f - t * t);
                if (t >= 1f)
                {
                    Hide();
                }
            }
        }

        private void SetMoundSize(float size)
        {
            _mound.transform.localScale = new Vector3(_moundScale * size, _moundScale * size, 1f);
        }
    }
}
