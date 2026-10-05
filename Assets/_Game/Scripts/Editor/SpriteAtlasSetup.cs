using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;

namespace ShaurmaTycoon.EditorTools
{
    /// <summary>
    /// Меню ShaurmaTycoon/Art/Build Sprite Atlases (V7): атласы по папкам спрайтов — UI (Generated + Icons + Shop),
    /// станции по городам, ларёк, персонажи, живой мир. Фоны Scene/ и карта в атлас не идут (крупные, по одному на экран).
    /// Атласы — Sprite Atlas V2 (Project Settings → Editor → Sprite Packer: Sprite Atlas V2 Enabled), лежат в Assets/_Game/Atlases.
    /// Повторный запуск пересоздаёт атласы с теми же настройками.
    /// </summary>
    public static class SpriteAtlasSetup
    {
        private const string SpritesFolder = "Assets/_Game/Sprites";
        private const string AtlasFolder = "Assets/_Game/Atlases";
        private const int MaxAtlasSize = 2048;
        private const int WebGlCrunchQuality = 80;

        [MenuItem("ShaurmaTycoon/Art/Build Sprite Atlases")]
        public static void BuildMenu()
        {
            if (EditorSettings.spritePackerMode != SpritePackerMode.SpriteAtlasV2)
                Debug.LogWarning("[Sprite Atlases] Sprite Packer is not 'Sprite Atlas V2 Enabled' — atlases will not be packed in Play/build.");

            if (!AssetDatabase.IsValidFolder(AtlasFolder))
                AssetDatabase.CreateFolder("Assets/_Game", "Atlases");

            var built = new List<string>();
            Build("UI", built, "UI/Generated", "UI/Icons", "Shop");
            for (int city = 1; city <= 5; city++)
                Build($"Stations_City{city:00}", built, $"Stations/City{city:00}");
            Build("Kiosk", built, "Kiosk");
            Build("Characters", built, "Characters");
            Build("Ambient", built, "Ambient");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget);
            Debug.Log($"[Sprite Atlases] Built {built.Count}: {string.Join(", ", built)}. Check draw calls in Frame Debugger.");
        }

        private static void Build(string name, List<string> built, params string[] folders)
        {
            var objects = new List<Object>();
            foreach (string folder in folders)
            {
                string path = $"{SpritesFolder}/{folder}";
                if (!AssetDatabase.IsValidFolder(path))
                    continue;
                objects.Add(AssetDatabase.LoadAssetAtPath<DefaultAsset>(path));
            }
            if (objects.Count == 0)
                return;

            string atlasPath = $"{AtlasFolder}/{name}.spriteatlasv2";
            if (File.Exists(atlasPath))
                AssetDatabase.DeleteAsset(atlasPath);

            var atlas = new SpriteAtlasAsset();
            atlas.Add(objects.ToArray());
            SpriteAtlasAsset.Save(atlas, atlasPath);
            AssetDatabase.ImportAsset(atlasPath);

            var importer = (SpriteAtlasImporter)AssetImporter.GetAtPath(atlasPath);
            importer.includeInBuild = true;
            // Без поворота и плотной упаковки: UI-картинки рисуются прямоугольниками, иначе соседи «залезают» в кадр.
            importer.packingSettings = new SpriteAtlasPackingSettings
            {
                blockOffset = 1,
                padding = 4,
                enableRotation = false,
                enableTightPacking = false,
                enableAlphaDilation = true,
            };
            importer.textureSettings = new SpriteAtlasTextureSettings
            {
                readable = false,
                generateMipMaps = false,
                sRGB = true,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 0,
            };
            importer.SetPlatformSettings(new TextureImporterPlatformSettings
            {
                name = "DefaultTexturePlatform",
                maxTextureSize = MaxAtlasSize,
                textureCompression = TextureImporterCompression.Compressed,
                crunchedCompression = true,
                compressionQuality = WebGlCrunchQuality,
            });
            importer.SetPlatformSettings(SpriteImportSettings.WebGlCrunched(MaxAtlasSize, WebGlCrunchQuality));
            importer.SaveAndReimport();
            built.Add(name);
        }
    }
}
