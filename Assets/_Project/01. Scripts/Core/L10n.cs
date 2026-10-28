using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace ExplodeIt.Core
{
    // 코드에서 만드는 글자의 번역. 인스펙터에 적힌 한국어 문구를 그대로 키로 UI 표에서 찾는다.
    // 문구 칸을 번역 키 칸으로 바꾸면 씬에 저장된 값을 옮겨야 해서, 칸은 그대로 두고 쓰는 줄에서만 거친다.
    // 표에 없거나 지금 언어 칸이 비어 있으면 원문(한국어)을 돌려준다. 번역이 빠져도 개발용 문구가 화면에 뜨지 않는다.
    // 웹은 표를 비동기로만 불러올 수 있어, 언어 준비가 끝나기 전에는 기다리지 않고 원문을 돌려준다(표는 시작할 때 미리 불러 둔다).
    public static class L10n
    {
        private const string TableName = "UI";

        public static string Tr(string source)
        {
            if (string.IsNullOrEmpty(source) || !LocalizationSettings.InitializationOperation.IsDone)
            {
                return source;
            }

            var tableOperation = LocalizationSettings.StringDatabase.GetTableAsync(TableName);
            if (!tableOperation.IsDone || tableOperation.Result == null)
            {
                return source;
            }

            StringTableEntry entry = tableOperation.Result.GetEntry(source);
            return entry != null && !string.IsNullOrEmpty(entry.Value) ? entry.Value : source;
        }

        public static string Format(string source, object arg0)
        {
            return string.Format(Tr(source), arg0);
        }

        public static string Format(string source, object arg0, object arg1)
        {
            return string.Format(Tr(source), arg0, arg1);
        }
    }
}
