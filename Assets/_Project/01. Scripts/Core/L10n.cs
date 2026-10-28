using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace ExplodeIt.Core
{
    // 코드와 데이터에서 만드는 글자의 번역. 한국어 원문을 그대로 키로 표에서 찾는다.
    // 문구 칸을 번역 키 칸으로 바꾸면 씬·데이터 에셋에 저장된 값을 옮겨야 해서, 칸은 그대로 두고 꺼내는 곳에서만 거친다.
    // UI 표는 화면·코드 문구, Data 표는 데이터 에셋 문구(무기·업적 이름, 대사 등)다.
    // 표에 없거나 지금 언어 칸이 비어 있으면 원문(한국어)을 돌려준다. 번역이 빠져도 개발용 문구가 화면에 뜨지 않는다.
    // 웹은 표를 비동기로만 불러올 수 있어, 언어 준비가 끝나기 전에는 기다리지 않고 원문을 돌려준다(표는 시작할 때 미리 불러 둔다).
    public static class L10n
    {
        private const string UiTableName = "UI";
        private const string DataTableName = "Data";

        // 데이터 이름은 화면을 그릴 때마다 꺼내므로, 지금 언어의 표를 들고 있다가 언어가 바뀔 때만 다시 찾는다.
        private static StringTable _uiTable;
        private static StringTable _dataTable;
        private static Locale _tableLocale;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            _uiTable = null;
            _dataTable = null;
            _tableLocale = null;
        }

        public static string Tr(string source)
        {
            return Lookup(source, UiTableName, ref _uiTable);
        }

        public static string Data(string source)
        {
            return Lookup(source, DataTableName, ref _dataTable);
        }

        public static string Format(string source, object arg0)
        {
            return string.Format(Tr(source), arg0);
        }

        public static string Format(string source, object arg0, object arg1)
        {
            return string.Format(Tr(source), arg0, arg1);
        }

        // 언어를 바꾼 직후 코드 글자를 새 언어로 쓰려면 새 언어의 표가 준비될 때까지 기다린다. 웹은 표를 비동기로만 불러온다.
        public static IEnumerator WaitForTables()
        {
            yield return LocalizationSettings.StringDatabase.GetTableAsync(UiTableName);
            yield return LocalizationSettings.StringDatabase.GetTableAsync(DataTableName);
        }

        private static string Lookup(string source, string tableName, ref StringTable table)
        {
            if (string.IsNullOrEmpty(source) || !LocalizationSettings.InitializationOperation.IsDone)
            {
                return source;
            }

            Locale locale = LocalizationSettings.SelectedLocale;
            if (locale != _tableLocale)
            {
                _tableLocale = locale;
                _uiTable = null;
                _dataTable = null;
            }

            if (table == null)
            {
                var operation = LocalizationSettings.StringDatabase.GetTableAsync(tableName);
                if (!operation.IsDone || operation.Result == null)
                {
                    return source;
                }

                table = operation.Result;
            }

            StringTableEntry entry = table.GetEntry(source);
            return entry != null && !string.IsNullOrEmpty(entry.Value) ? entry.Value : source;
        }
    }
}
