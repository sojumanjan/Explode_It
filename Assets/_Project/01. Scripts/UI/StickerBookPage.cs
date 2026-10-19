using ExplodeIt.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExplodeIt.UI
{
    // 도감 페이지. 책 양면을 한 면으로 보고 여러 면을 넘긴다(1면 해금 업적, 2면 수집 업적).
    // 스티커 자리는 씬에 미리 놓아 두고, 열릴 때마다 딴 것과 못 딴 것을 다시 그린다. 업적은 메뉴 밖(게임 씬)에서만 늘어나므로 열 때 한 번이면 된다.
    public class StickerBookPage : MonoBehaviour
    {
        [Tooltip("넘길 면들. 각 면 아래에 스티커 자리(StickerSlot)를 놓는다")]
        [SerializeField] private GameObject[] _spreads;
        [SerializeField] private StickerTooltip _tooltip;
        [SerializeField] private Button _prevButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private TMP_Text _spreadText;
        [SerializeField] private string _spreadFormat = "{0} / {1}";
        [Tooltip("모은 수. 모든 면의 스티커를 센다")]
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private string _progressFormat = "모은 스티커 {0} / {1}";
        [SerializeField] private SoundData _flipSound;

        private StickerSlot[] _slots;
        private StickerSlot _hovered;
        private int _spread;

        private void Awake()
        {
            _slots = GetComponentsInChildren<StickerSlot>(true);
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].Bind(this);
            }

            _prevButton.onClick.AddListener(Prev);
            _nextButton.onClick.AddListener(Next);
        }

        private void OnEnable()
        {
            int completed = 0;
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].Refresh();
                if (_slots[i].IsCompleted)
                {
                    completed++;
                }
            }

            _progressText.text = string.Format(_progressFormat, completed, _slots.Length);
            ShowSpread(0);
        }

        public void ShowTooltip(StickerSlot slot)
        {
            _hovered = slot;
            _tooltip.Show(slot);
        }

        // 다른 스티커로 바로 옮겨 갔으면 그 스티커의 창을 닫지 않는다.
        public void HideTooltip(StickerSlot slot)
        {
            if (_hovered != slot)
            {
                return;
            }

            _hovered = null;
            _tooltip.Hide();
        }

        private void Prev()
        {
            Flip(-1);
        }

        private void Next()
        {
            Flip(1);
        }

        private void Flip(int step)
        {
            int next = Mathf.Clamp(_spread + step, 0, _spreads.Length - 1);
            if (next == _spread)
            {
                return;
            }

            AudioManager.Play(_flipSound);
            ShowSpread(next);
        }

        private void ShowSpread(int index)
        {
            _spread = index;
            for (int i = 0; i < _spreads.Length; i++)
            {
                _spreads[i].SetActive(i == index);
            }

            _hovered = null;
            _tooltip.Hide();
            _prevButton.interactable = index > 0;
            _nextButton.interactable = index < _spreads.Length - 1;
            _spreadText.text = string.Format(_spreadFormat, index + 1, _spreads.Length);
        }
    }
}
