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

        private void Awake()
        {
            if (_nextMark != null)
            {
                _nextMarkHome = _nextMark.anchoredPosition;
            }
        }

        public bool IsTyping => _text.maxVisibleCharacters < _total;

        public void Show(string line)
        {
            gameObject.SetActive(true);
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
            _box.localScale = Vector3.one * 0.6f;
            _pop = _box.DOScale(1f, _popDuration).SetEase(Ease.OutBack).SetUpdate(_useUnscaledTime).SetLink(gameObject);
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
            if (_nextMark == null)
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
