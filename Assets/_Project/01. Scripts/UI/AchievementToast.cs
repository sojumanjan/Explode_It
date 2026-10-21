using System.Collections.Generic;
using DG.Tweening;
using ExplodeIt.Core;
using ExplodeIt.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExplodeIt.UI
{
    // 업적을 달성하면 좌측 하단에서 띠가 쓱 올라왔다가 잠시 뒤 내려간다(스팀 업적 알림처럼). 여러 개면 차례로 띄운다.
    // 업적 이름과 달성 방법만 보여 준다. 무엇을 해금했는지는 판이 끝난 뒤 해금 창에서 한 번에 알린다.
    // 슬로모션·일시정지와 상관없이 실제 시간으로 움직인다.
    public class AchievementToast : MonoBehaviour
    {
        // 올라오고 내려가는 시간(초)과 떠 있는 시간(초, 실제 시간).
        private const float SlideTime = 0.35f;
        private const float HoldTime = 4f;

        [SerializeField] private RectTransform _panel;
        [SerializeField] private Image _icon;
        // 그림 칸 테두리. 업적 스티커가 없으면 칸째 숨기고 글을 왼쪽으로 당긴다.
        [SerializeField] private GameObject _iconFrame;
        // 업적에 스티커 그림이 아직 없을 때 대신 보이는 공용 빈 스티커. 이것도 비어 있으면 그림 칸을 숨긴다.
        [SerializeField] private Sprite _fallbackSticker;
        [Tooltip("그림 칸을 숨겼을 때 글의 왼쪽 위치 (UI 단위)")]
        [SerializeField] private float _textLeftWithoutIcon = 28f;
        [SerializeField] private TMP_Text _title;
        // 달성 방법(업적 설명).
        [SerializeField] private TMP_Text _detail;
        [SerializeField] private SoundData _sound;
        [SerializeField] private string _titleFormat = "업적 달성 · {0}";

        // 띠 하나에 들어갈 내용.
        private readonly struct Toast
        {
            public readonly Sprite Icon;
            public readonly string Title;
            public readonly string Detail;

            public Toast(Sprite icon, string title, string detail)
            {
                Icon = icon;
                Title = title;
                Detail = detail;
            }
        }

        private readonly Queue<Toast> _queue = new Queue<Toast>();
        private Vector2 _shownPosition;
        private Vector2 _hiddenPosition;
        private Sequence _sequence;
        private bool _isShowing;
        private float _textLeft;

        private void Awake()
        {
            _shownPosition = _panel.anchoredPosition;
            // 패널 높이만큼 아래(화면 밖)에서 올라온다.
            _hiddenPosition = _shownPosition - new Vector2(0f, _panel.rect.height + _shownPosition.y + 10f);
            _panel.anchoredPosition = _hiddenPosition;
            _panel.gameObject.SetActive(false);
            _textLeft = _title.rectTransform.anchoredPosition.x;
        }

        private void OnEnable()
        {
            ProgressionEvents.AchievementCompleted += OnAchievementCompleted;
        }

        private void OnDisable()
        {
            ProgressionEvents.AchievementCompleted -= OnAchievementCompleted;
            _sequence?.Kill();
        }

        private void OnAchievementCompleted(AchievementData achievement)
        {
            Sprite icon = achievement.Sticker != null ? achievement.Sticker : _fallbackSticker;
            Enqueue(new Toast(icon, string.Format(_titleFormat, achievement.DisplayName), achievement.Description));
        }

        private void Enqueue(Toast toast)
        {
            _queue.Enqueue(toast);
            if (!_isShowing)
            {
                ShowNext();
            }
        }

        private void ShowNext()
        {
            if (_queue.Count == 0)
            {
                _isShowing = false;
                _panel.gameObject.SetActive(false);
                return;
            }

            _isShowing = true;
            Toast toast = _queue.Dequeue();
            Sprite icon = toast.Icon;
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _iconFrame.SetActive(icon != null);
            float left = icon != null ? _textLeft : _textLeftWithoutIcon;
            SetLeft(_title.rectTransform, left);
            SetLeft(_detail.rectTransform, left);
            _title.text = toast.Title;
            _detail.text = toast.Detail;

            _panel.gameObject.SetActive(true);
            _panel.anchoredPosition = _hiddenPosition;
            AudioManager.Play(_sound);
            _sequence?.Kill();
            _sequence = DOTween.Sequence()
                .Append(_panel.DOAnchorPos(_shownPosition, SlideTime).SetEase(Ease.OutBack))
                .AppendInterval(HoldTime)
                .Append(_panel.DOAnchorPos(_hiddenPosition, SlideTime).SetEase(Ease.InQuad))
                .OnComplete(ShowNext)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private static void SetLeft(RectTransform rect, float x)
        {
            rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
        }
    }
}
