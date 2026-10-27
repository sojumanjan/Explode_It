using System;
using ExplodeIt.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExplodeIt.UI
{
    // 결과 창의 "중간부터 다시하기" 버튼 하나. 지금까지 연 가장 먼 시작 지점에서 다시 한다.
    // 300마리까지 간 사람이 100마리부터 다시 할 일은 드물고, 그러고 싶으면 메뉴 스토리 창에서 고르면 되므로 버튼을 늘어놓지 않는다.
    // 스토리가 아니거나 아직 연 지점이 없으면 숨긴다.
    public class CheckpointRetryButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _label;
        [Tooltip("시작 지점별 버튼 글. 순서대로 시작 지점 1, 2, 3, 4(최종 보스전)")]
        [SerializeField] private string[] _texts =
        {
            "100마리부터 다시하기", "200마리부터 다시하기", "300마리부터 다시하기", "최종 보스전부터 다시하기"
        };

        private Action<int> _onSelected;
        private int _checkpoint;

        private void Awake()
        {
            _button.onClick.AddListener(OnClicked);
        }

        // 결과 창이 켜질 때 맞춘다. 이번 판에서 방금 연 지점도 바로 반영된다.
        private void OnEnable()
        {
            _checkpoint = RunSetup.Mode == RunMode.Story ? StoryCheckpointButtons.FarthestCheckpoint(_texts.Length) : 0;
            _button.gameObject.SetActive(_checkpoint > 0);
            if (_checkpoint > 0)
            {
                _label.text = _texts[_checkpoint - 1];
            }
        }

        public void Bind(Action<int> onSelected)
        {
            _onSelected = onSelected;
        }

        private void OnClicked()
        {
            _onSelected?.Invoke(_checkpoint);
        }
    }
}
