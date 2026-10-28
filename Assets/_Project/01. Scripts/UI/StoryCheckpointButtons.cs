using System;
using ExplodeIt.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExplodeIt.UI
{
    // 메뉴 스토리 창의 중간부터 시작 버튼 줄. 버튼 i는 시작 지점 i+1이다(100·200·300마리부터, 최종 보스전).
    // 중간부터 할 수 있다는 것은 처음부터 알리되, 아직 닿지 못한 지점은 "???"로 가려 뒤 내용(최종 보스 등)을 미리 밝히지 않는다.
    // 스토리를 깬 저장은 이 칸이 생기기 전에 깼을 수 있어 모두 연다.
    public class StoryCheckpointButtons : MonoBehaviour
    {
        [SerializeField] private Button[] _buttons;
        // 버튼과 같은 순서. 잠긴 동안만 켜서 어떻게 여는지 알려 준다. 비워 둬도 된다.
        [SerializeField] private HoverTooltip[] _lockedTooltips;
        [Tooltip("잠긴 버튼에 대신 쓰는 글")]
        [SerializeField] private string _lockedText = "???";

        private Action<int> _onSelected;
        private TMP_Text[] _labels;
        // 열렸을 때 쓸 글. 씬에 적힌 버튼 글자를 그대로 쓴다.
        private string[] _openTexts;

        private void Awake()
        {
            _labels = new TMP_Text[_buttons.Length];
            _openTexts = new string[_buttons.Length];
            for (int i = 0; i < _buttons.Length; i++)
            {
                int checkpoint = i + 1;
                _buttons[i].onClick.AddListener(() => _onSelected?.Invoke(checkpoint));
                _labels[i] = _buttons[i].GetComponentInChildren<TMP_Text>(true);
                _openTexts[i] = _labels[i].text;
            }
        }

        private void OnEnable()
        {
            Refresh();
        }

        public void Bind(Action<int> onSelected)
        {
            _onSelected = onSelected;
        }

        public void Refresh()
        {
            // 창이 처음 켜지기 전(Awake 전)에 불리면 켜질 때 OnEnable에서 다시 맞춘다.
            if (_labels == null)
            {
                return;
            }

            int farthest = FarthestCheckpoint();
            for (int i = 0; i < _buttons.Length; i++)
            {
                bool isOpen = i + 1 <= farthest;
                _buttons[i].interactable = isOpen;
                _labels[i].text = isOpen ? L10n.Tr(_openTexts[i]) : L10n.Tr(_lockedText);
                if (_lockedTooltips != null && i < _lockedTooltips.Length && _lockedTooltips[i] != null)
                {
                    _lockedTooltips[i].enabled = !isOpen;
                }
            }
        }

        // 지금 고를 수 있는 가장 먼 시작 지점. 없으면 0.
        public static int FarthestCheckpoint(int checkpointCount = int.MaxValue)
        {
            SaveData data = SaveSystem.Data;
            return data.StoryCleared ? checkpointCount : Mathf.Min(data.StoryCheckpoint, checkpointCount);
        }
    }
}
