using UnityEditor;
using UnityEngine;

namespace ShaurmaTycoon.EditorTools
{
    /// <summary>
    /// Импорт звуков пакета G (V6) для WebGL: музыка — Streaming, петли эмбиента — сжатые в памяти,
    /// короткие эффекты — распакованные при загрузке и моно. Остальные звуки (корень Audio/) не трогаем.
    /// </summary>
    public sealed class AudioImportSettings : AssetPostprocessor
    {
        private const string AudioFolder = "Assets/_Game/Audio/";
        private const string MusicFolder = AudioFolder + "Music/";
        private const string AmbientFolder = AudioFolder + "Ambient/";
        private const string SfxFolder = AudioFolder + "Sfx/";

        public override uint GetVersion()
        {
            return 1;
        }

        private void OnPreprocessAudio()
        {
            var importer = (AudioImporter)assetImporter;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;

            if (assetPath.StartsWith(MusicFolder))
            {
                settings.loadType = AudioClipLoadType.Streaming;
                settings.quality = 0.5f;
                importer.forceToMono = false;
            }
            else if (assetPath.StartsWith(AmbientFolder))
            {
                settings.loadType = AudioClipLoadType.CompressedInMemory;
                settings.quality = 0.45f;
                importer.forceToMono = false;
            }
            else if (assetPath.StartsWith(SfxFolder))
            {
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.quality = 0.6f;
                importer.forceToMono = true;
            }
            else
            {
                return;
            }

            importer.defaultSampleSettings = settings;
        }
    }
}
