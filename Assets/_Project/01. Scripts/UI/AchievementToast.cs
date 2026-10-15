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
    // 슬로모션·일시정지와 상관없이 실제 시간으로 움직인다.
    public class AchievementToast : MonoBehaviour
    {
        // 올라오고 내려가는 시간(초)과 떠 있는 시간(초, 실제 시간).
        private const float SlideTime = 0.35f;
        private const float HoldTime = 3f;

        [SerializeField] private RectTransform _panel;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _detail;
        [SerializeField] private SoundData _sound;
        [SerializeField] private string _titleFormat = "업적 달성 · {0}";
        [SerializeField] private string _weaponFormat = "새 무기 해금: {0}";
        [SerializeField] private string _artifactFormat = "새 아티팩트 해금: {0}";

        private readonly Queue<AchievementData> _queue = new Queue<AchievementData>();
        private Vector2 _shownPosition;
        private Vector2 _hiddenPosition;
        private Sequence _sequence;
        private bool _isShowing;

        private void Awake()
        {
            _shownPosition = _panel.anchoredPosition;
            // 패널 높이만큼 아래(화면 밖)에서 올라온다.
            _hiddenPosition = _shownPosition - new Vector2(0f, _panel.rect.height + _shownPosition.y + 10f);
            _panel.anchoredPosition = _hiddenPosition;
            _panel.gameObject.SetActive(false);
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
            _queue.Enqueue(achievement);
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
            AchievementData achievement = _queue.Dequeue();
            Sprite icon = achievement.RewardWeapon != null ? achievement.RewardWeapon.Icon
                : achievement.RewardArtifact != null ? achievement.RewardArtifact.Icon : achievement.Sticker;
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _title.text = string.Format(_titleFormat, achievement.DisplayName);
            _detail.text = achievement.RewardWeapon != null ? string.Format(_weaponFormat, achievement.RewardWeapon.DisplayName)
                : achievement.RewardArtifact != null ? string.Format(_artifactFormat, achievement.RewardArtifact.DisplayName) : achievement.Description;

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
    }
}
