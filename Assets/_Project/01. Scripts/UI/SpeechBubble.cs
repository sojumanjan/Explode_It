using DG.Tweening;
using TMPro;
using UnityEngine;

namespace ExplodeIt.UI
{
    // 말풍선 하나. 대사 길이에 맞춰 상자 크기를 정하고 글자를 타자 치듯 보여 준다.
    // 맵 위 보스 등장 대화(월드 캔버스)와 우측 하단 왕 컷인(화면 캔버스)이 같이 쓴다.
    public class SpeechBubble : MonoBehaviour
    {
        [SerializeField] private RectTransform _box;
        // 꼬리 테두리. 상자 뒤에 깔린 검은 꼬리라 상자 밖으로 나온 부분만 보인다. 상자 위의 흰 꼬리가 이음매를 덮는다.
        // 위치·크기는 씬에 배치한 그대로 쓰고, 상자가 뽀잉 커지는 비율만 따라간다.
        [SerializeField] private RectTransform _tailBack;
        [SerializeField] private TMP_Text _text;
        // 한 줄이 이 폭(캔버스 단위)을 넘으면 줄을 바꾼다.
        [SerializeField, Min(10f)] private float _maxTextWidth = 420f;
        [SerializeField] private Vector2 _padding = new Vector2(28f, 18f);
        [SerializeField, Min(1f)] private float _charsPerSecond = 28f;
        [SerializeField, Min(0f)] private float _popDuration = 0.18f;
        // 사망 슬로모션 중에도 컷인 대사가 제 속도로 나오게 하려면 켠다.
        [SerializeField] private bool _useUnscaledTime;
        // 글자가 다 나오면 위아래로 통통 튀는 ▼. 클릭해서 넘기는 대화에만 연결하고, 자동으로 넘어가는 컷인은 비워 둔다.
        [SerializeField] private RectTransform _nextMark;
        [SerializeField, Min(0f)] private float _nextMarkBob = 6f;
        [SerializeField, Min(0f)] private float _nextMarkRate = 2.5f;

        private float _shown;
        private int _total;
        private Tween _pop;
        private Vector2 _nextMarkHome;
        private float _nextMarkTime;
        private bool _waitsForClick = true;
        // 씬에 배치한 상자 크기·위치와 꼬리 테두리 크기·위치. 뽀잉 연출은 이 크기까지 커지고, 꼬리 테두리는 같은 비율로 따라간다.
        private Vector3 _boxScale;
        private Vector3 _boxPosition;
        private Vector3 _tailBackPosition;
        private Vector3 _tailBackScale;

        private void Awake()
        {
            _boxScale = _box.localScale;
            _boxPosition = _box.localPosition;
            if (_tailBack != null)
            {
                _tailBackPosition = _tailBack.localPosition;
                _tailBackScale = _tailBack.localScale;
            }

            if (_nextMark != null)
            {
                _nextMarkHome = _nextMark.anchoredPosition;
            }
        }

        public bool IsTyping => _text.maxVisibleCharacters < _total;

        // waitsForClick: 클릭해야 넘어가는 줄인지. 저절로 사라지는 줄에 ▼가 뜨면 눌러야 할 것처럼 읽히므로 끈다.
        public void Show(string line, bool waitsForClick = true)
        {
            gameObject.SetActive(true);
            _waitsForClick = waitsForClick;
            _text.text = line;
            Vector2 preferred = _text.GetPreferredValues(line, _maxTextWidth, 0f);
            float width = Mathf.Min(preferred.x, _maxTextWidth);
            _text.rectTransform.sizeDelta = new Vector2(width, preferred.y);
            _box.sizeDelta = new Vector2(width + _padding.x * 2f, preferred.y + _padding.y * 2f);

            _text.ForceMeshUpdate();
            _total = _text.textInfo.characterCount;
            _shown = 0f;
            _text.maxVisibleCharacters = 0;
            SetNextMark(false);

            _pop?.Kill();
            _box.localScale = _boxScale * 0.6f;
            _pop = _box.DOScale(_boxScale, _popDuration).SetEase(Ease.OutBack).SetUpdate(_useUnscaledTime).SetLink(gameObject);
        }

        public void CompleteTyping()
        {
            _shown = _total;
            _text.maxVisibleCharacters = _total;
        }

        public void Hide()
        {
            _pop?.Kill();
            gameObject.SetActive(false);
        }

        // 상자가 뽀잉 커지는 동안에도 뒤에 깐 꼬리 테두리가 흰 꼬리와 정확히 겹치게 맞춘다.
        private void LateUpdate()
        {
            if (_tailBack == null || _boxScale.x == 0f)
            {
                return;
            }

            float ratio = _box.localScale.x / _boxScale.x;
            _tailBack.localPosition = _boxPosition + (_tailBackPosition - _boxPosition) * ratio;
            _tailBack.localScale = _tailBackScale * ratio;
        }

        private void Update()
        {
            float deltaTime = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            if (!IsTyping)
            {
                BobNextMark(deltaTime);
                return;
            }

            _shown += _charsPerSecond * deltaTime;
            _text.maxVisibleCharacters = Mathf.Min(_total, (int)_shown);
        }

        private void BobNextMark(float deltaTime)
        {
            if (_nextMark == null || !_waitsForClick)
            {
                return;
            }

            if (!_nextMark.gameObject.activeSelf)
            {
                SetNextMark(true);
            }

            _nextMarkTime += deltaTime;
            float bob = Mathf.Abs(Mathf.Sin(_nextMarkTime * _nextMarkRate * Mathf.PI)) * _nextMarkBob;
            _nextMark.anchoredPosition = _nextMarkHome - new Vector2(0f, bob);
        }

        private void SetNextMark(bool isVisible)
        {
            if (_nextMark == null)
            {
                return;
            }

            _nextMark.gameObject.SetActive(isVisible);
            _nextMarkTime = 0f;
            _nextMark.anchoredPosition = _nextMarkHome;
        }
    }
}
