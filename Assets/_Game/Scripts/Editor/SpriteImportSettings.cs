using UnityEditor;

namespace ShaurmaTycoon.EditorTools
{
    /// <summary>
    /// PNG из Assets/_Game/Sprites импортируются как UI-спрайты: без мипмапов, с прозрачностью, без повтора.
    /// </summary>
    public sealed class SpriteImportSettings : AssetPostprocessor
    {
        private const string SpritesFolder = "Assets/_Game/Sprites/";

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
            importer.maxTextureSize = 512;
        }
    }
}
