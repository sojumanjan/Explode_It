// 설정 페이지의 닉네임 줄(지금 닉네임과 바꾸기 버튼)
using ExplodeIt.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace ExplodeIt.UI
{
    // 타이틀 설정 페이지와 ESC 설정 페이지에 각각 둔다. 바꾸기를 누르면 닉네임 창을 띄운다.
    public class LeaderboardSettingsView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Button _renameButton;
        [SerializeField] private TMP_Text _renameLabel;
        [SerializeField] private NicknameDialog _nicknameDialog;

        [Header("문구 (한국어 원문, 번역 표의 키)")]
        [SerializeField] private string _labelFormat = "닉네임: {0}";
        [SerializeField] private string _noNameText = "닉네임 없음";
        [SerializeField] private string _renameText = "바꾸기";

        private void Awake()
        {
            _renameButton.onClick.AddListener(OnRenameClicked);
        }

        private void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            Refresh();
        }

        private void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        }

        private void OnLocaleChanged(Locale locale)
        {
            Refresh();
        }

        private void Refresh()
        {
            string name = SaveSystem.Data.LeaderboardName;
            _label.text = name.Length > 0 ? L10n.Format(_labelFormat, name) : L10n.Tr(_noNameText);
            _renameLabel.text = L10n.Tr(_renameText);
            // 순위표 기능을 꺼 둔 빌드에서는 줄은 남기고 누를 수만 없게 한다(설정 칸 배치가 흐트러지지 않게).
            _renameButton.interactable = LeaderboardService.IsAvailable;
        }

        private void OnRenameClicked()
        {
            _nicknameDialog.ShowRename(OnDialogClosed);
        }

        private void OnDialogClosed()
        {
            if (this != null)
            {
                Refresh();
            }
        }
    }
}
