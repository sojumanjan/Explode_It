using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace ExplodeIt.UI
{
    // "정말요?" 확인 창. 예를 누르면 넘겨받은 일을 하고, 아니요·뒤로가기면 그냥 닫는다.
    // 씬에서는 꺼 둔다. Awake에서 끄면 Show로 켠 직후 다시 꺼지기 때문이다.
    public class ConfirmDialog : MonoBehaviour
    {
        private const float PopTime = 0.2f;

        [SerializeField] private RectTransform _box;
        [SerializeField] private TMP_Text _message;

        private Action _onConfirm;

        public bool IsOpen { get; private set; }

        public void Show(string message, Action onConfirm)
        {
            _onConfirm = onConfirm;
            _message.text = message;
            gameObject.SetActive(true);
            IsOpen = true;
            _box.DOKill();
            _box.localScale = Vector3.one * 0.8f;
            _box.DOScale(1f, PopTime).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
        }

        // 예 버튼. 넘겨받은 일이 또 확인 창을 띄울 수 있도록 먼저 닫고 부른다.
        public void Confirm()
        {
            Action onConfirm = _onConfirm;
            Close();
            onConfirm?.Invoke();
        }

        public void Cancel()
        {
            Close();
        }

        private void Close()
        {
            IsOpen = false;
            _onConfirm = null;
            _box.DOKill();
            gameObject.SetActive(false);
        }
    }
}
