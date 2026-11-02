using System;
using System.Collections.Generic;
using System.IO;
using ExplodeIt.Core;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace ExplodeIt.EditorTools
{
    // pixelfx 스프라이트 시트(PNG + Aseprite식 JSON)를 게임에 쓸 수 있게 만든다.
    // 텍스처 설정(점 필터, 무압축) → 프레임 자르기(pivot은 이펙트 원점) → PixelFxClip → 재생 프리팹 순서.
    // 렌더와 복사는 Tools/pixelfx-export.ps1이 하고, PixelFx 폴더에 JSON이 들어오면 PixelFxSheetPostprocessor가 이걸 부른다.
    public static class PixelFxImporter
    {
        public const string SheetFolder = "Assets/_Project/05. Art/PixelFx";

        // pixelfx의 기본 캐릭터(38픽셀)가 고블린(약 1.9유닛)과 같은 키가 되는 값. 처음 가져올 때만 쓰고,
        // 그 뒤 텍스처 인스펙터에서 바꾼 값은 다시 가져와도 유지한다.
        private const float DefaultPixelsPerUnit = 20f;
        // 웹 빌드는 4096보다 큰 텍스처를 못 쓰는 기기가 있어 경고한다.
        private const int WebSafeTextureSize = 4096;
        private const string ImportedMark = "pixelfx";

        [MenuItem("Tools/pixelfx/Import Sheet...")]
        private static void ImportFromMenu()
        {
            string start = Path.GetFullPath(SheetFolder);
            string picked = EditorUtility.OpenFilePanel("pixelfx 시트 JSON", Directory.Exists(start) ? start : Application.dataPath, "json");
            if (string.IsNullOrEmpty(picked))
            {
                return;
            }

            string assetPath = ToAssetPath(picked);
            if (assetPath == null)
            {
                EditorUtility.DisplayDialog("pixelfx", "프로젝트 Assets 폴더 안의 JSON을 골라야 한다.", "확인");
                return;
            }

            Import(assetPath);
        }

        [MenuItem("Assets/pixelfx/Import Sheet", true)]
        private static bool CanImportSelected()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return path.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
        }

        [MenuItem("Assets/pixelfx/Import Sheet")]
        private static void ImportSelected()
        {
            Import(AssetDatabase.GetAssetPath(Selection.activeObject));
        }

        public static PixelFxClip Import(string jsonPath)
        {
            // 자동 가져오기는 다음 에디터 틱에 돌아서 그 사이 파일이 지워졌을 수 있다.
            if (!File.Exists(jsonPath))
            {
                return null;
            }

            Sheet sheet = JsonUtility.FromJson<Sheet>(File.ReadAllText(jsonPath));
            if (sheet == null || sheet.frames == null || sheet.frames.Length == 0 || sheet.meta == null || sheet.meta.pixelfx == null)
            {
                Debug.LogError($"[pixelfx] pixelfx 시트 JSON이 아니다: {jsonPath}");
                return null;
            }

            string folder = Path.GetDirectoryName(jsonPath).Replace('\\', '/');
            string pngPath = $"{folder}/{sheet.meta.image}";
            var importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[pixelfx] 시트 그림이 없다: {pngPath}");
                return null;
            }

            string name = Path.GetFileNameWithoutExtension(jsonPath);
            ConfigureTexture(importer, sheet);
            SliceFrames(importer, sheet, name);
            importer.SaveAndReimport();

            Sprite[] frames = LoadFrames(pngPath, name, sheet.frames.Length);
            PixelFxClip clip = SaveClip(folder, name, sheet, frames);
            SavePrefab(folder, name, clip, frames[0]);
            Debug.Log($"[pixelfx] {sheet.meta.pixelfx.id}: 프레임 {frames.Length}개, {sheet.meta.pixelfx.fps}fps → {folder}/PxFx_{name}");
            return clip;
        }

        private static void ConfigureTexture(TextureImporter importer, Sheet sheet)
        {
            // 유니티가 PNG를 처음 볼 때 이미 Multiple로 잡기도 해서, 가져오기 모드 대신 표시를 남겨 처음인지 가린다.
            if (importer.userData != ImportedMark)
            {
                importer.spritePixelsPerUnit = DefaultPixelsPerUnit;
                importer.userData = ImportedMark;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;

            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;

            int largest = Mathf.Max(sheet.meta.size.w, sheet.meta.size.h);
            importer.maxTextureSize = Mathf.Clamp(Mathf.NextPowerOfTwo(largest), 32, 16384);
            if (largest > WebSafeTextureSize)
            {
                Debug.LogWarning($"[pixelfx] 시트가 {largest}px라 웹에서 깨질 수 있다. --fps를 낮추거나 더 작은 이펙트를 쓴다.");
            }
        }

        // 이름이 같은 칸은 기존 spriteID를 이어 써서, 다시 가져와도 프레임을 참조하던 곳이 끊기지 않게 한다.
        private static void SliceFrames(TextureImporter importer, Sheet sheet, string name)
        {
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            var existing = new Dictionary<string, GUID>();
            foreach (SpriteRect old in provider.GetSpriteRects())
            {
                existing[old.name] = old.spriteID;
            }

            int textureHeight = sheet.meta.size.h;
            float[] origin = sheet.meta.pixelfx.pivot;
            var rects = new SpriteRect[sheet.frames.Length];
            var pairs = new List<SpriteNameFileIdPair>(rects.Length);
            for (int i = 0; i < rects.Length; i++)
            {
                Cell cell = sheet.frames[i].frame;
                string frameName = FrameName(name, i);
                GUID id = existing.TryGetValue(frameName, out GUID kept) ? kept : GUID.Generate();
                // JSON은 왼쪽 위가 0, 유니티는 왼쪽 아래가 0이다.
                rects[i] = new SpriteRect
                {
                    name = frameName,
                    spriteID = id,
                    rect = new Rect(cell.x, textureHeight - cell.y - cell.h, cell.w, cell.h),
                    alignment = SpriteAlignment.Custom,
                    pivot = origin != null && origin.Length == 2
                        ? new Vector2(origin[0] / cell.w, 1f - origin[1] / cell.h)
                        : new Vector2(0.5f, 0.5f),
                };
                pairs.Add(new SpriteNameFileIdPair(frameName, id));
            }

            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(pairs);
            provider.Apply();
        }

        private static Sprite[] LoadFrames(string pngPath, string name, int count)
        {
            var byName = new Dictionary<string, Sprite>();
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetRepresentationsAtPath(pngPath))
            {
                if (asset is Sprite sprite)
                {
                    byName[sprite.name] = sprite;
                }
            }

            var frames = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                byName.TryGetValue(FrameName(name, i), out frames[i]);
            }

            return frames;
        }

        private static PixelFxClip SaveClip(string folder, string name, Sheet sheet, Sprite[] frames)
        {
            string path = $"{folder}/PxFx_{name}.asset";
            var clip = AssetDatabase.LoadAssetAtPath<PixelFxClip>(path);
            if (clip == null)
            {
                clip = ScriptableObject.CreateInstance<PixelFxClip>();
                AssetDatabase.CreateAsset(clip, path);
            }

            clip.EditorSet(sheet.meta.pixelfx.id, frames, sheet.meta.pixelfx.fps, sheet.meta.pixelfx.loop);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        // 프리팹은 처음 한 번만 만든다. 그 뒤 프리팹에서 바꾼 정렬 순서나 크기는 다시 가져와도 건드리지 않는다.
        private static void SavePrefab(string folder, string name, PixelFxClip clip, Sprite firstFrame)
        {
            string path = $"{folder}/PxFx_{name}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                return;
            }

            var go = new GameObject($"PxFx_{name}");
            try
            {
                go.AddComponent<SpriteRenderer>().sprite = firstFrame;
                var player = go.AddComponent<PixelFxPlayer>();
                var so = new SerializedObject(player);
                so.FindProperty("_clip").objectReferenceValue = clip;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(go, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static string FrameName(string name, int index) => $"{name}_{index:D3}";

        private static string ToAssetPath(string fullPath)
        {
            string data = Application.dataPath.Replace('\\', '/');
            fullPath = fullPath.Replace('\\', '/');
            return fullPath.StartsWith(data, StringComparison.OrdinalIgnoreCase) ? "Assets" + fullPath.Substring(data.Length) : null;
        }

        // JsonUtility로 읽는 pixelfx 시트 JSON 모양. 쓰는 칸만 둔다.
        [Serializable] private class Sheet { public Frame[] frames; public Meta meta; }
        [Serializable] private class Frame { public Cell frame; }
        [Serializable] private class Cell { public int x; public int y; public int w; public int h; }
        [Serializable] private class Meta { public string image; public Size size; public Info pixelfx; }
        [Serializable] private class Size { public int w; public int h; }
        [Serializable] private class Info { public string id; public float fps; public bool loop; public float[] pivot; }
    }
}
