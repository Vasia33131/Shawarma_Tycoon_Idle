using System.Collections.Generic;
using ShaurmaTycoon.Core.Audio;
using ShaurmaTycoon.Customers;
using ShaurmaTycoon.Prestige;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace ShaurmaTycoon.EditorTools
{
    /// <summary>
    /// Готовит контент для сборщика UI: шрифт Rubik → TMP-ассет (шрифт по умолчанию),
    /// импорт спрайтов клиентов, конфиги звука и клиентов. Уже существующие ассеты не перезаписываются —
    /// ручные правки в Audio.asset и Customers.asset сохраняются.
    /// </summary>
    public static class ContentSetup
    {
        private const string FontTtfPath = "Assets/_Game/Fonts/Rubik-Variable.ttf";
        private const string FontAssetPath = "Assets/_Game/Fonts/Rubik SDF.asset";
        private const string FallbackFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        private const string AudioConfigPath = "Assets/_Game/Configs/Audio.asset";
        private const string CustomerConfigPath = "Assets/_Game/Configs/Customers.asset";
        private const string AudioFolder = "Assets/_Game/Audio/";
        private const string CustomersFolder = "Assets/_Game/Sprites/Customers";
        private const string ReputationShopPath = "Assets/_Game/Configs/ReputationShop.asset";

        private static readonly string[] CustomerCharacters = { "malePerson", "femalePerson", "femaleAdventurer" };
        private const string SellerCharacter = "maleAdventurer";
        private const int WalkFrames = 8;
        private const int PhraseCount = 10;

        public static void EnsureAll(out AudioConfig audio, out CustomerConfig customers, out ReputationShopConfig shop)
        {
            EnsureFont();
            EnsureSpriteImport();
            audio = EnsureAudioConfig();
            customers = EnsureCustomerConfig();
            shop = EnsureReputationShop();
            AssetDatabase.SaveAssets();
        }

        // ---------- Font ----------

        private static void EnsureFont()
        {
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (fontAsset == null)
            {
                var font = AssetDatabase.LoadAssetAtPath<Font>(FontTtfPath);
                if (font == null)
                {
                    Debug.LogWarning($"[ContentSetup] {FontTtfPath} not found, keeping the current default font.");
                    return;
                }

                fontAsset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
                fontAsset.name = "Rubik SDF";
                AssetDatabase.CreateAsset(fontAsset, FontAssetPath);

                fontAsset.material.name = "Rubik SDF Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
                fontAsset.atlasTextures[0].name = "Rubik SDF Atlas";
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);

                var fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackFontPath);
                if (fallback != null)
                    fontAsset.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };

                EditorUtility.SetDirty(fontAsset);
                Debug.Log("[ContentSetup] Created " + FontAssetPath);
            }

            var settings = new SerializedObject(TMP_Settings.instance);
            SerializedProperty defaultFont = settings.FindProperty("m_defaultFontAsset");
            if (defaultFont != null && defaultFont.objectReferenceValue != fontAsset)
            {
                defaultFont.objectReferenceValue = fontAsset;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // ---------- Sprites ----------

        // Страховка на случай, если PNG импортировались раньше, чем скомпилировался SpriteImportSettings.
        private static void EnsureSpriteImport()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { CustomersFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is TextureImporter importer && importer.textureType != TextureImporterType.Sprite)
                    importer.SaveAndReimport();
            }
        }

        private static Sprite LoadPose(string character, string pose)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{CustomersFolder}/{character}/character_{character}_{pose}.png");
        }

        private static void WriteSkin(SerializedProperty skin, string character)
        {
            skin.FindPropertyRelative("Idle").objectReferenceValue = LoadPose(character, "idle");
            skin.FindPropertyRelative("Cheer").objectReferenceValue = LoadPose(character, "cheer0");
            skin.FindPropertyRelative("Interact").objectReferenceValue = LoadPose(character, "interact");

            SerializedProperty walk = skin.FindPropertyRelative("Walk");
            walk.arraySize = WalkFrames;
            for (int i = 0; i < WalkFrames; i++)
                walk.GetArrayElementAtIndex(i).objectReferenceValue = LoadPose(character, "walk" + i);
        }

        // ---------- Configs ----------

        private static CustomerConfig EnsureCustomerConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<CustomerConfig>(CustomerConfigPath);
            if (config != null)
                return config;

            config = ScriptableObject.CreateInstance<CustomerConfig>();
            AssetDatabase.CreateAsset(config, CustomerConfigPath);

            var so = new SerializedObject(config);
            SerializedProperty skins = so.FindProperty("_customerSkins");
            skins.arraySize = CustomerCharacters.Length;
            for (int i = 0; i < CustomerCharacters.Length; i++)
                WriteSkin(skins.GetArrayElementAtIndex(i), CustomerCharacters[i]);
            WriteSkin(so.FindProperty("_sellerSkin"), SellerCharacter);

            SerializedProperty phrases = so.FindProperty("_phraseKeys");
            phrases.arraySize = PhraseCount;
            for (int i = 0; i < PhraseCount; i++)
                phrases.GetArrayElementAtIndex(i).stringValue = $"customer.phrase_{i + 1:00}";

            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[ContentSetup] Created " + CustomerConfigPath);
            return config;
        }

        private static AudioConfig EnsureAudioConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<AudioConfig>(AudioConfigPath);
            if (config != null)
                return config;

            config = ScriptableObject.CreateInstance<AudioConfig>();
            AssetDatabase.CreateAsset(config, AudioConfigPath);

            var entries = new (SoundId id, string[] clips, float volume, float minInterval)[]
            {
                (SoundId.Tap, new[] { "click_002" }, 0.7f, 0.04f),
                (SoundId.Coin, new[] { "chips-collide-1", "chips-collide-2", "chips-collide-3" }, 0.55f, 0.07f),
                (SoundId.Buy, new[] { "confirmation_001" }, 0.8f, 0.05f),
                (SoundId.Unlock, new[] { "maximize_006" }, 0.9f, 0.1f),
                (SoundId.Hire, new[] { "confirmation_002" }, 0.9f, 0.1f),
                (SoundId.Upgrade, new[] { "confirmation_003" }, 0.9f, 0.1f),
                (SoundId.Tab, new[] { "switch_002" }, 0.6f, 0.05f),
                (SoundId.Bubble, new[] { "pluck_001" }, 0.5f, 0.2f),
            };

            var so = new SerializedObject(config);
            SerializedProperty sounds = so.FindProperty("_sounds");
            sounds.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++)
            {
                SerializedProperty entry = sounds.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("Id").enumValueIndex = (int)entries[i].id;
                entry.FindPropertyRelative("Volume").floatValue = entries[i].volume;
                entry.FindPropertyRelative("PitchJitter").floatValue = 0.06f;
                entry.FindPropertyRelative("MinInterval").floatValue = entries[i].minInterval;

                SerializedProperty clips = entry.FindPropertyRelative("Clips");
                clips.arraySize = entries[i].clips.Length;
                for (int c = 0; c < entries[i].clips.Length; c++)
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + entries[i].clips[c] + ".ogg");
                    if (clip == null)
                        Debug.LogWarning($"[ContentSetup] Audio clip '{entries[i].clips[c]}' not found.");
                    clips.GetArrayElementAtIndex(c).objectReferenceValue = clip;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[ContentSetup] Created " + AudioConfigPath);
            return config;
        }

        // Набор магазина репутации, согласованный на этапе 08 (дальше правится в ассете).
        private static ReputationShopConfig EnsureReputationShop()
        {
            var config = AssetDatabase.LoadAssetAtPath<ReputationShopConfig>(ReputationShopPath);
            if (config != null)
                return config;

            config = ScriptableObject.CreateInstance<ReputationShopConfig>();
            AssetDatabase.CreateAsset(config, ReputationShopPath);

            var items = new (string id, ShopEffect effect, double[] values, long[] prices)[]
            {
                ("offline_limit", ShopEffect.OfflineLimitHours, new double[] { 1, 2, 3, 4, 5, 6 }, new long[] { 5, 10, 20, 40, 80, 160 }),
                ("start_coins", ShopEffect.StartCoins, new double[] { 1e3, 1e5, 1e7 }, new long[] { 3, 15, 75 }),
                ("free_managers", ShopEffect.FreeManagers, new double[] { 1, 2, 3 }, new long[] { 10, 50, 250 }),
                ("fast_customers", ShopEffect.CycleTimeFactor, new[] { 0.9, 0.8, 0.7 }, new long[] { 20, 80, 320 }),
            };

            var so = new SerializedObject(config);
            SerializedProperty list = so.FindProperty("_items");
            list.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
            {
                SerializedProperty item = list.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("Id").stringValue = items[i].id;
                item.FindPropertyRelative("NameKey").stringValue = $"shop.{items[i].id}.name";
                item.FindPropertyRelative("DescriptionKey").stringValue = $"shop.{items[i].id}.desc";
                item.FindPropertyRelative("Effect").enumValueIndex = (int)items[i].effect;

                SerializedProperty values = item.FindPropertyRelative("Values");
                values.arraySize = items[i].values.Length;
                for (int v = 0; v < items[i].values.Length; v++)
                    values.GetArrayElementAtIndex(v).doubleValue = items[i].values[v];

                SerializedProperty prices = item.FindPropertyRelative("Prices");
                prices.arraySize = items[i].prices.Length;
                for (int p = 0; p < items[i].prices.Length; p++)
                    prices.GetArrayElementAtIndex(p).longValue = items[i].prices[p];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[ContentSetup] Created " + ReputationShopPath);
            return config;
        }
    }
}
