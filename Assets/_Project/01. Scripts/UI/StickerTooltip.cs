using System.Collections.Generic;
using System.Text;
using ExplodeIt.Core;
using ExplodeIt.Progression;
using TMPro;
using UnityEngine;

namespace ExplodeIt.UI
{
    // 도감 스티커에 마우스를 올리면 옆에 뜨는 설명 창. 이름, 땄는지, 어떻게 따는지, 무엇을 주는지를 보여 준다.
    // 창 높이는 글 길이에 맞춰 늘어나고(레이아웃 그룹), 스티커 옆 책 안쪽으로 붙되 화면 밖으로 나가지 않게 막는다.
    // 창이 마우스를 가로채면 스티커에서 벗어난 것으로 처리돼 깜빡이므로, 창 안의 모든 그림은 레이캐스트를 끈다.
    public class StickerTooltip : MonoBehaviour
    {
        [SerializeField] private RectTransform _panel;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _conditionText;
        [SerializeField] private TMP_Text _rewardText;

        [Header("글")]
        [SerializeField] private string _lockedStatus = "아직 해금되지 않음";
        [SerializeField] private string _completedStatus = "획득!";
        [SerializeField] private Color _lockedColor = new Color(0.55f, 0.45f, 0.38f, 1f);
        [SerializeField] private Color _completedColor = new Color(0.85f, 0.45f, 0.1f, 1f);
        [SerializeField] private string _conditionFormat = "<b>해금 조건</b>\n{0}";
        [SerializeField] private string _rewardFormat = "<b>보상</b>  {0}";
        [SerializeField] private string _rewardSeparator = ", ";

        [Header("배치")]
        [Tooltip("스티커 가장자리와 창 사이 간격 (UI 단위)")]
        [SerializeField, Min(0f)] private float _gap = 16f;
        [Tooltip("창이 화면 가장자리에서 떨어질 최소 거리 (UI 단위)")]
        [SerializeField, Min(0f)] private float _screenMargin = 20f;

        private readonly List<IUnlockable> _rewards = new List<IUnlockable>(4);
        private readonly StringBuilder _builder = new StringBuilder(64);

        public void Show(StickerSlot slot)
        {
            AchievementData achievement = slot.Achievement;
            bool isCompleted = slot.IsCompleted;
            _nameText.text = achievement.DisplayName;
            _statusText.text = isCompleted ? _completedStatus : _lockedStatus;
            _statusText.color = isCompleted ? _completedColor : _lockedColor;
            _conditionText.text = string.Format(_conditionFormat, achievement.Description);

            _rewards.Clear();
            achievement.CollectRewards(_rewards);
            _rewardText.gameObject.SetActive(_rewards.Count > 0);
            if (_rewards.Count > 0)
            {
                _builder.Clear();
                for (int i = 0; i < _rewards.Count; i++)
                {
                    if (i > 0)
                    {
                        _builder.Append(_rewardSeparator);
                    }

                    _builder.Append(_rewards[i].DisplayName);
                }

                _rewardText.text = string.Format(_rewardFormat, _builder);
            }

            _panel.gameObject.SetActive(true);
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_panel);
            Place((RectTransform)slot.transform);
        }

        public void Hide()
        {
            _panel.gameObject.SetActive(false);
        }

        // 왼쪽 페이지 스티커는 오른쪽에, 오른쪽 페이지 스티커는 왼쪽에 띄워 책 가운데 쪽으로 펼친다.
        private void Place(RectTransform slot)
        {
            var parent = (RectTransform)_panel.parent;
            Vector3 center = parent.InverseTransformPoint(slot.TransformPoint(slot.rect.center));
            Vector3 corner = parent.InverseTransformPoint(slot.TransformPoint(new Vector3(slot.rect.xMax, slot.rect.yMax, 0f)));
            float halfWidth = Mathf.Abs(corner.x - center.x);
            bool toRight = center.x < 0f;
            _panel.pivot = new Vector2(toRight ? 0f : 1f, 0.5f);
            float x = center.x + (toRight ? halfWidth + _gap : -halfWidth - _gap);

            Rect bounds = parent.rect;
            float halfHeight = _panel.rect.height * 0.5f;
            float y = Mathf.Clamp(center.y, bounds.yMin + _screenMargin + halfHeight, bounds.yMax - _screenMargin - halfHeight);
            float width = _panel.rect.width;
            x = toRight
                ? Mathf.Min(x, bounds.xMax - _screenMargin - width)
                : Mathf.Max(x, bounds.xMin + _screenMargin + width);
            _panel.anchoredPosition = new Vector2(x, y);
        }
    }
}
