using UnityEditor;

namespace ShaurmaTycoon.EditorTools
{
    /// <summary>
    /// PNG из Assets/_Game/Sprites импортируются как UI-спрайты: без мипмапов, с прозрачностью, без повтора.
    /// Предел размера — по папкам: иконки (UI/Icons, Stations) показываются не крупнее ~180 px, им хватает 256;
    /// как и картинки магазина (Shop); фоны городов (Scene/City0X) и карты (Map/map_bg) — 2048; остальное — 512.
    /// Иконки, фоны, ларёк, транспорт (Ambient) и карта сжимаются (WebGL-бюджет, Docs/Visual/ART_DIRECTION.md).
    /// </summary>
    public sealed class SpriteImportSettings : AssetPostprocessor
    {
        private const string SpritesFolder = "Assets/_Game/Sprites/";
        private const string UiIconsFolder = SpritesFolder + "UI/Icons/";
        private const string StationIconsFolder = SpritesFolder + "Stations/";
        private const string SceneFolder = SpritesFolder + "Scene/";
        private const string KioskFolder = SpritesFolder + "Kiosk/";
        private const string AmbientFolder = SpritesFolder + "Ambient/";
        private const string MapFolder = SpritesFolder + "Map/";
        private const string ShopFolder = SpritesFolder + "Shop/";
        private const string MapBackground = MapFolder + "map_bg.png";

        private const string PromoFolder = SpritesFolder + "Promo/";
        // Сжатие для WebGL (V7): crunch — меньше и .data, и загрузка. Фоны крупные — quality 50, остальное идёт в атласы — 80.
        private const int LayerCrunchQuality = 50;
        private const int SpriteCrunchQuality = 80;

        // Смена версии заставляет Unity переимпортировать текстуры с новыми правилами (V2: лимиты по папкам, V3: сцена и ларёк, V5: транспорт,
        // V5b: карта и магазин, V7: WebGL-сжатие crunch).
        public override uint GetVersion()
        {
            return 6;
        }

        /// <summary>Переопределение импорта для WebGL: DXT5 + crunch (ASTC в WebGL Unity 2022.3 только для мобильного шаблона — не используем).</summary>
        public static TextureImporterPlatformSettings WebGlCrunched(int maxSize, int quality)
        {
            return new TextureImporterPlatformSettings
            {
                name = "WebGL",
                overridden = true,
                maxTextureSize = maxSize,
                format = TextureImporterFormat.DXT5Crunched,
                textureCompression = TextureImporterCompression.Compressed,
                crunchedCompression = true,
                compressionQuality = quality,
            };
        }

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(SpritesFolder))
                return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;

            bool isIcon = assetPath.StartsWith(UiIconsFolder) || assetPath.StartsWith(StationIconsFolder) || assetPath.StartsWith(ShopFolder);
            // Фоны городов и карты растягиваются на всю ширину экрана — им нужно 2048; ларёк не шире ~350 px — хватает 512.
            bool isLayer = (assetPath.StartsWith(SceneFolder) && !assetPath.Contains("/Common/")) || assetPath == MapBackground;
            importer.maxTextureSize = isIcon ? 256 : isLayer ? 2048 : 512;
            if (isIcon || isLayer || assetPath.StartsWith(KioskFolder) || assetPath.StartsWith(AmbientFolder) || assetPath.StartsWith(MapFolder))
            {
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.filterMode = UnityEngine.FilterMode.Bilinear;
            }

            // Картинка для экрана загрузки живёт в шаблоне WebGL (Images/loading_bg.jpg), здесь — исходник: в билд не попадает.
            if (assetPath.StartsWith(PromoFolder))
                return;
            importer.SetPlatformTextureSettings(WebGlCrunched(importer.maxTextureSize, isLayer ? LayerCrunchQuality : SpriteCrunchQuality));
        }
    }
}
