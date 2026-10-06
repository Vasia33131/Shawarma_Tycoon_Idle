using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using ShaurmaTycoon.Core.Audio;
using ShaurmaTycoon.Prestige;
using UnityEditor;
using UnityEngine;

namespace ShaurmaTycoon.EditorTools
{
    /// <summary>
    /// Звуки пакета G (V6) по путям из Docs/Visual/ASSET_PROMPTS.md: музыка и основа эмбиента — в города,
    /// гриль/дождь/ветер — в AmbientAudio.asset, эффекты — в Audio.asset. Вызывается из Art/Assign Icons.
    /// Нет файла — остаётся старый звук (фишки) или слой молчит. Запасной трек Music.asset — music_cozy_01 (этап 12: HappyClappyLoop.wav в билд не идёт).
    /// </summary>
    public static class AudioAssigner
    {
        public const string MusicFolder = "Assets/_Game/Audio/Music";
        public const string AmbientFolder = "Assets/_Game/Audio/Ambient";
        public const string SfxFolder = "Assets/_Game/Audio/Sfx";
        private const string OldAudioFolder = "Assets/_Game/Audio";
        private const string CityConfigsFolder = "Assets/_Game/Configs/Cities";
        private static readonly string[] Extensions = { ".ogg", ".wav", ".mp3" };

        public static readonly string[] Files =
        {
            "Music/music_cozy_01", "Music/music_cozy_02", "Music/music_cozy_03",
            "Ambient/amb_street", "Ambient/amb_sea", "Ambient/amb_rain", "Ambient/amb_wind_soft", "Ambient/amb_sizzle",
            "Sfx/cat_purr", "Sfx/cat_meow", "Sfx/car_pass", "Sfx/coin_soft", "Sfx/pop_soft",
        };

        // Музыка по номеру города: Родной город и Казань — 01, Сочи — 02, Питер и Москва — 03.
        private static readonly Dictionary<int, string> CityMusic = new Dictionary<int, string>
        {
            { 1, "music_cozy_01" }, { 2, "music_cozy_01" }, { 3, "music_cozy_02" }, { 4, "music_cozy_03" }, { 5, "music_cozy_03" },
        };

        private const int SeaCity = 3;
        private const float MusicVolume = 0.35f;
        private const float MusicCrossfade = 2f;

        /// <returns>Сколько файлов пакета G найдено.</returns>
        public static int AssignAll(List<string> missing)
        {
            int found = 0;
            foreach (string file in Files)
            {
                if (Load("Assets/_Game/Audio/" + file) != null)
                    found++;
                else
                    missing.Add($"Assets/_Game/Audio/{file}.ogg");
            }

            AssignMusicConfig();
            AssignCities();
            AssignAmbientConfig();
            AssignSounds();
            return found;
        }

        private static void AssignMusicConfig()
        {
            var music = AssetDatabase.LoadAssetAtPath<MusicConfig>(ContentSetup.MusicConfigPath);
            if (music == null)
                return;
            var so = new SerializedObject(music);
            so.FindProperty("_volume").floatValue = MusicVolume;
            so.FindProperty("_fadeDuration").floatValue = MusicCrossfade;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignCities()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(CityConfig), new[] { CityConfigsFolder }))
            {
                string configPath = AssetDatabase.GUIDToAssetPath(guid);
                Match match = Regex.Match(Path.GetFileName(configPath), @"^City_(\d+)_");
                if (!match.Success)
                    continue;

                int number = int.Parse(match.Groups[1].Value);
                var so = new SerializedObject(AssetDatabase.LoadAssetAtPath<CityConfig>(configPath));
                so.FindProperty("_musicName").stringValue =
                    CityMusic.TryGetValue(number, out string track) ? ClipName($"{MusicFolder}/{track}") : string.Empty;
                so.FindProperty("_ambientBaseName").stringValue =
                    ClipName($"{AmbientFolder}/{(number == SeaCity ? "amb_sea" : "amb_street")}");
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void AssignAmbientConfig()
        {
            var so = new SerializedObject(ContentSetup.EnsureAmbientAudio());
            so.FindProperty("_sizzleName").stringValue = ClipName($"{AmbientFolder}/amb_sizzle");
            so.FindProperty("_rainName").stringValue = ClipName($"{AmbientFolder}/amb_rain");
            so.FindProperty("_windName").stringValue = ClipName($"{AmbientFolder}/amb_wind_soft");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignSounds()
        {
            var audio = AssetDatabase.LoadAssetAtPath<AudioConfig>(ContentSetup.AudioConfigPath);
            if (audio == null)
                return;

            var so = new SerializedObject(audio);
            SerializedProperty sounds = so.FindProperty("_sounds");

            // Монета: мягкий звон (питч 0.95–1.08, не больше 3 голосов); без файла — прежние фишки.
            AudioClip coin = Load($"{SfxFolder}/coin_soft");
            if (coin != null)
                SetSound(sounds, SoundId.Coin, new[] { coin }, 0.5f, 0.015f, 0.065f, 0.08f, 3);
            else
                SetSound(sounds, SoundId.Coin, LoadAll(OldAudioFolder, "chips-collide-1", "chips-collide-2", "chips-collide-3"),
                    0.55f, 0f, 0.06f, 0.08f, 3);

            SetSound(sounds, SoundId.PopupOpen, LoadAll(SfxFolder, "pop_soft"), 0.5f, 0f, 0.04f, 0.1f, 0);
            SetSound(sounds, SoundId.CatPurr, LoadAll(SfxFolder, "cat_purr"), 0.6f, 0f, 0.03f, 1f, 1);
            SetSound(sounds, SoundId.CatMeow, LoadAll(SfxFolder, "cat_meow"), 0.5f, 0f, 0.06f, 0.5f, 1);
            SetSound(sounds, SoundId.CarPass, LoadAll(SfxFolder, "car_pass"), 0.2f, 0f, 0.05f, 1f, 0);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSound(SerializedProperty sounds, SoundId id, AudioClip[] clips, float volume, float pitchOffset,
            float pitchJitter, float minInterval, int maxVoices)
        {
            SerializedProperty entry = FindOrAdd(sounds, id);
            entry.FindPropertyRelative("Volume").floatValue = volume;
            entry.FindPropertyRelative("PitchOffset").floatValue = pitchOffset;
            entry.FindPropertyRelative("PitchJitter").floatValue = pitchJitter;
            entry.FindPropertyRelative("MinInterval").floatValue = minInterval;
            entry.FindPropertyRelative("MaxVoices").intValue = maxVoices;

            SerializedProperty list = entry.FindPropertyRelative("Clips");
            list.arraySize = clips.Length;
            for (int i = 0; i < clips.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
        }

        private static SerializedProperty FindOrAdd(SerializedProperty sounds, SoundId id)
        {
            for (int i = 0; i < sounds.arraySize; i++)
            {
                SerializedProperty entry = sounds.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("Id").enumValueIndex == (int)id)
                    return entry;
            }

            sounds.arraySize++;
            SerializedProperty added = sounds.GetArrayElementAtIndex(sounds.arraySize - 1);
            added.FindPropertyRelative("Id").enumValueIndex = (int)id;
            return added;
        }

        private static AudioClip[] LoadAll(string folder, params string[] names)
        {
            var clips = new List<AudioClip>(names.Length);
            foreach (string name in names)
            {
                AudioClip clip = Load($"{folder}/{name}");
                if (clip != null)
                    clips.Add(clip);
            }
            return clips.ToArray();
        }

        /// <summary>
        /// Имя клипа для конфига (музыка и эмбиент идут в билд отдельными MP3 — AudioClipStore, поэтому в конфиге имя, а не ссылка).
        /// Файла нет — пустая строка: слой молчит.
        /// </summary>
        private static string ClipName(string pathWithoutExtension)
        {
            AudioClip clip = Load(pathWithoutExtension);
            return clip != null ? clip.name : string.Empty;
        }

        /// <param name="pathWithoutExtension">Путь без расширения: подходит .ogg, .wav или .mp3.</param>
        private static AudioClip Load(string pathWithoutExtension)
        {
            foreach (string extension in Extensions)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(pathWithoutExtension + extension);
                if (clip != null)
                    return clip;
            }
            return null;
        }
    }
}
