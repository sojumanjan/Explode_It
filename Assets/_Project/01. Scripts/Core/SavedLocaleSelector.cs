using System;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace ExplodeIt.Core
{
    // 게임을 켤 때 설정에서 고른 언어로 시작한다. 고른 적이 없으면 null을 돌려 다음 선택기(시스템 언어 → 기본 영어)에 맡긴다.
    // 언어 설정 에셋의 시작 언어 선택기 목록 맨 앞에 둔다.
    [Serializable]
    public class SavedLocaleSelector : IStartupLocaleSelector
    {
        public Locale GetStartupLocale(ILocalesProvider availableLocales)
        {
            string code = SaveSystem.Data.Language;
            return string.IsNullOrEmpty(code) ? null : availableLocales.GetLocale(code);
        }
    }
}
