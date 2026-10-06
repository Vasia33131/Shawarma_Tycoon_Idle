using UnityEditor;
using UnityEngine;

namespace ShaurmaTycoon.EditorTools
{
    /// <summary>
    /// Импорт звуков пакета G (V6) для WebGL: всё — распакованное при загрузке (Decompress On Load), эффекты и эмбиент — моно.
    /// Короткие эффекты — моно, quality 0.6. Остальные звуки (корень Audio/) не трогаем (они меньше 128 КБ — всегда Web Audio).
    /// Этап 12 (размер билда): музыка — quality 0.35 (lo-fi это прощает), эмбиент — моно и quality 0.3:
    /// в WebGL звук кодируется в AAC и с этим качеством, а браузер распаковывает клип целиком в память.
    /// </summary>
    public sealed class AudioImportSettings : AssetPostprocessor
    {
        private const string AudioFolder = "Assets/_Game/Audio/";
        private const string MusicFolder = AudioFolder + "Music/";
        private const string AmbientFolder = AudioFolder + "Ambient/";
        private const string SfxFolder = AudioFolder + "Sfx/";

        private const float MusicQuality = 0.35f;
        private const float AmbientQuality = 0.3f;
        private const float SfxQuality = 0.6f;

        // V6: 1; этап 12: 2 — ниже качество музыки, эмбиент в моно; 3 — музыка не Streaming; 4 — музыка и эмбиент Decompress On Load (системный плеер).
        public override uint GetVersion()
        {
            return 4;
        }

        private void OnPreprocessAudio()
        {
            var importer = (AudioImporter)assetImporter;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;

            if (assetPath.StartsWith(MusicFolder))
            {
                // Только Decompress On Load: Unity WebGL (Audio.js, JS_Sound_Load) играет Streaming и Compressed In Memory клипы
                // больше 128 КБ через HTML5 <audio> — браузер показывает системный плеер на ПК и в шторке телефона (1.6.1.6, 1.6.2.5).
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.quality = MusicQuality;
                importer.forceToMono = false;
            }
            else if (assetPath.StartsWith(AmbientFolder))
            {
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.quality = AmbientQuality;
                importer.forceToMono = true;
            }
            else if (assetPath.StartsWith(SfxFolder))
            {
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.quality = SfxQuality;
                importer.forceToMono = true;
            }
            else
            {
                return;
            }

            importer.defaultSampleSettings = settings;
            // Своё переопределение WebGL сбило бы эти правила — снимаем, если его выставили руками.
            importer.ClearSampleSettingOverride("WebGL");
        }
    }
}
