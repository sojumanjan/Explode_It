using System.Collections.Generic;
using ExplodeIt.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace ExplodeIt.UI
{
    // 설정의 언어 고르기. 목록은 등록된 언어를 그 언어 자신의 이름(한국어, English)으로 보여 줘, 읽지 못하는 언어로 바뀌어도 되돌아올 수 있게 한다.
    // 고른 언어는 바로 적용하고 저장 칸에 적는다. 실제 저장은 설정 페이지를 닫을 때 한 번에 한다.
    // 웹은 언어 데이터를 비동기로 불러오므로, 준비가 끝난 뒤에 목록을 채운다.
    public class LanguageSettingsView : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown _dropdown;

        private readonly List<Locale> _locales = new List<Locale>();
        private readonly List<string> _options = new List<string>();

        private void Awake()
        {
            _dropdown.onValueChanged.AddListener(OnLanguageChanged);
        }

        private void OnEnable()
        {
            var init = LocalizationSettings.InitializationOperation;
            if (init.IsDone)
            {
                Build();
            }
            else
            {
                init.Completed += _ => Build();
            }
        }

        private void Build()
        {
            if (this == null)
            {
                return;
            }

            _locales.Clear();
            _options.Clear();
            List<Locale> all = LocalizationSettings.AvailableLocales.Locales;
            for (int i = 0; i < all.Count; i++)
            {
                _locales.Add(all[i]);
                _options.Add(all[i].Identifier.CultureInfo != null ? all[i].Identifier.CultureInfo.NativeName : all[i].LocaleName);
            }

            _dropdown.ClearOptions();
            _dropdown.AddOptions(_options);
            _dropdown.SetValueWithoutNotify(Mathf.Max(0, _locales.IndexOf(LocalizationSettings.SelectedLocale)));
            _dropdown.RefreshShownValue();
        }

        private void OnLanguageChanged(int index)
        {
            if (index < 0 || index >= _locales.Count)
            {
                return;
            }

            LocalizationSettings.SelectedLocale = _locales[index];
            SaveSystem.Data.Language = _locales[index].Identifier.Code;
        }
    }
}
