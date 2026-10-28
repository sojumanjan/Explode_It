using DG.Tweening;
using ExplodeIt.Core;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace ExplodeIt.UI
{
    // 언어를 고른 적이 없을 때(처음 켰을 때) 동의 창보다 먼저 뜨는 언어 고르기. 동의 창을 읽을 수 있는 언어로 띄우기 위해서다.
    // 버튼 글은 각 언어 자신의 이름(한국어, English)으로 씬에 고정한다. 어느 언어로 떠 있든 자기 언어를 찾을 수 있어야 한다.
    // 반드시 하나를 골라야 닫힌다. 씬에서는 꺼 둔다(동의 창과 같은 이유).
    public class LanguagePickDialog : MonoBehaviour
    {
        private const float PopTime = 0.25f;

        [SerializeField] private RectTransform _box;

        public bool IsOpen { get; private set; }

        // 아직 고른 적이 없으면 띄우고 true를 돌려준다.
        public bool ShowIfNeeded()
        {
            if (!string.IsNullOrEmpty(SaveSystem.Data.Language))
            {
                return false;
            }

            gameObject.SetActive(true);
            IsOpen = true;
            _box.DOKill();
            _box.localScale = Vector3.one * 0.85f;
            _box.DOScale(1f, PopTime).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
            return true;
        }

        // 버튼마다 언어 코드(ko, en)를 인자로 연결한다.
        public void Pick(string code)
        {
            Locale locale = LocalizationSettings.AvailableLocales.GetLocale(code);
            if (locale != null)
            {
                LocalizationSettings.SelectedLocale = locale;
            }

            SaveSystem.Data.Language = code;
            SaveSystem.Save();
            IsOpen = false;
            _box.DOKill();
            gameObject.SetActive(false);
        }
    }
}
