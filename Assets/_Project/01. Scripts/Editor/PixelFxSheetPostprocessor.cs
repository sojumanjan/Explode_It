using System;
using System.Collections.Generic;
using UnityEditor;

namespace ExplodeIt.EditorTools
{
    // Tools/pixelfx-export.ps1이 PixelFx 폴더에 시트를 복사하면 메뉴를 누르지 않아도 바로 가져오게 한다.
    // 가져오기가 텍스처를 다시 임포트하므로, 임포트 도중이 아니라 끝난 뒤(delayCall)에 부른다.
    public class PixelFxSheetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            List<string> sheets = null;
            foreach (string path in imported)
            {
                if (path.StartsWith(PixelFxImporter.SheetFolder + "/", StringComparison.Ordinal)
                    && path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    (sheets ??= new List<string>()).Add(path);
                }
            }

            if (sheets == null)
            {
                return;
            }

            EditorApplication.delayCall += () =>
            {
                foreach (string sheet in sheets)
                {
                    PixelFxImporter.Import(sheet);
                }
            };
        }
    }
}
