// 순위표 닉네임 창: 처음 켤 때 닉네임을 정하고, 설정에서 바꾼다
using System;
using DG.Tweening;
using ExplodeIt.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExplodeIt.UI
{
    // 닉네임은 기기에 바로 저장하고 창을 닫는다. 서버 이름은 기록을 올릴 때 LeaderboardService가 맞추므로 여기서 연결을 기다리지 않는다.
    // 처음 정할 때(무한 모드 첫 최고 기록)는 닫기 버튼 없이 확인만 둔다(닉네임이 있어야 기록이 순위표에 오른다).
    // 씬에서는 꺼 둔다(ConfirmDialog와 같은 이유로 Awake에서 끄지 않는다).
    public class NicknameDialog : MonoBehaviour
    {
        private const float PopTime = 0.2f;

        [SerializeField] private RectTransform _box;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _body;
        [SerializeField] private TMP_InputField _nameInput;
        [SerializeField] private TMP_Text _placeholder;
        // 이름 규칙 위반을 한 줄로 알린다.
        [SerializeField] private TMP_Text _status;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private TMP_Text _confirmLabel;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private TMP_Text _cancelLabel;

        [Header("문구 (한국어 원문, 번역 표의 키)")]
        [SerializeField] private string _firstTitle = "닉네임 설정";
        [SerializeField, TextArea] private string _firstBody = "무한모드의 최고 기록이 전 세계 리더보드에 기록됩니다.\n기록될 닉네임을 작성해주세요.\n설정에서 언제든 바꿀 수 있습니다.";
        [SerializeField] private string _renameTitle = "닉네임 바꾸기";
        [SerializeField, TextArea] private string _renameBody = "순위표에 보일 닉네임을 바꿉니다.";
        [SerializeField] private string _confirmText = "확인";
        [SerializeField] private string _cancelText = "취소";
        [SerializeField] private string _placeholderFormat = "닉네임 ({0}~{1}자)";
        [SerializeField] private string _invalidNameFormat = "{0}~{1}자의 한글, 영문, 숫자, 밑줄만 쓸 수 있습니다.";

        private Action _onClosed;

        public bool IsOpen { get; private set; }

        // 무한 모드에서 처음 최고 기록을 세운 결과 창에서 부른다. 처음 켤 때 물으면 무슨 기록인지 몰라 어색했다.
        public void ShowFirst(Action onClosed)
        {
            Open(false, onClosed);
        }

        // 설정의 닉네임 바꾸기.
        public void ShowRename(Action onClosed)
        {
            Open(true, onClosed);
        }

        private void Open(bool isRename, Action onClosed)
        {
            _onClosed = onClosed;
            _title.text = L10n.Tr(isRename ? _renameTitle : _firstTitle);
            _body.text = L10n.Tr(isRename ? _renameBody : _firstBody);
            _confirmLabel.text = L10n.Tr(_confirmText);
            _cancelLabel.text = L10n.Tr(_cancelText);
            _cancelButton.gameObject.SetActive(isRename);
            // 닫기 버튼이 없으면 확인 버튼을 가운데로 옮긴다.
            var confirmRect = (RectTransform)_confirmButton.transform;
            var cancelRect = (RectTransform)_cancelButton.transform;
            confirmRect.anchoredPosition = new Vector2(isRename ? -cancelRect.anchoredPosition.x : 0f, confirmRect.anchoredPosition.y);
            _placeholder.text = L10n.Format(_placeholderFormat, LeaderboardService.NameMinLength, LeaderboardService.NameMaxLength);
            _nameInput.characterLimit = LeaderboardService.NameMaxLength;
            _nameInput.text = SaveSystem.Data.LeaderboardName;
            _status.text = "";

            gameObject.SetActive(true);
            IsOpen = true;
            _box.DOKill();
            _box.localScale = Vector3.one * 0.8f;
            _box.DOScale(1f, PopTime).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
            _nameInput.ActivateInputField();
        }

        // 확인 버튼. 엔터로도 누를 수 있게 입력 칸의 제출에도 연결한다.
        public void Confirm()
        {
            string name = LeaderboardService.CleanName(_nameInput.text);
            if (name == null)
            {
                _status.text = L10n.Format(_invalidNameFormat, LeaderboardService.NameMinLength, LeaderboardService.NameMaxLength);
                return;
            }

            LeaderboardService.SaveName(name);
            Close();
        }

        // 취소 버튼(바꾸기에서만 보인다).
        public void Cancel()
        {
            Close();
        }

        private void Close()
        {
            IsOpen = false;
            _box.DOKill();
            gameObject.SetActive(false);
            Action onClosed = _onClosed;
            _onClosed = null;
            onClosed?.Invoke();
        }
    }
}
