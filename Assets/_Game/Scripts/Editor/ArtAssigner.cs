using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using ShaurmaTycoon.Stations;
using ShaurmaTycoon.UI;
using UnityEditor;
using UnityEngine;

namespace ShaurmaTycoon.EditorTools
{
    /// <summary>
    /// Меню ShaurmaTycoon/Art/Assign Icons: раскладывает нарисованные иконки по ассетам по именам файлов
    /// (Docs/Visual/ASSET_PROMPTS.md, пакеты A и B). UI-иконки — в Configs/UiIcons.asset, еда станций —
    /// в StationConfig._icon (Sprites/Stations/&lt;папка города&gt;/&lt;имя ассета станции&gt;.png).
    /// Отсутствующие файлы не ошибка: в отчёте их список, в игре — заглушка. Вызывается и из Build Game UI.
    /// </summary>
    public static class ArtAssigner
    {
        public const string IconsFolder = "Assets/_Game/Sprites/UI/Icons";
        public const string StationIconsFolder = "Assets/_Game/Sprites/Stations";
        private const string StationConfigsFolder = "Assets/_Game/Configs/Stations";
        private const string IconsAssetPath = "Assets/_Game/Configs/UiIcons.asset";

        /// <summary>Поле UiIcons → имя файла в IconsFolder (без .png).</summary>
        public static readonly IReadOnlyDictionary<string, string> IconFiles = new Dictionary<string, string>
        {
            { nameof(UiIcons.Coin), "icon_coin" },
            { nameof(UiIcons.Bill), "icon_bill" },
            { nameof(UiIcons.Reputation), "icon_reputation" },
            { nameof(UiIcons.Income), "icon_income" },
            { nameof(UiIcons.TabStations), "tab_stations" },
            { nameof(UiIcons.TabManagers), "tab_managers" },
            { nameof(UiIcons.TabUpgrades), "tab_upgrades" },
            { nameof(UiIcons.TabCity), "tab_city" },
            { nameof(UiIcons.TabShop), "tab_shop" },
            { nameof(UiIcons.Settings), "icon_settings" },
            { nameof(UiIcons.Gift), "icon_gift" },
            { nameof(UiIcons.Calendar), "icon_calendar" },
            { nameof(UiIcons.Quests), "icon_quests" },
            { nameof(UiIcons.Ad), "icon_ad" },
            { nameof(UiIcons.Boost), "icon_boost" },
            { nameof(UiIcons.Lock), "icon_lock" },
            { nameof(UiIcons.Offline), "icon_offline" },
            { nameof(UiIcons.Check), "icon_check" },
            { nameof(UiIcons.Close), "icon_close" },
            { nameof(UiIcons.TutorialHand), "tutorial_hand" },
            { nameof(UiIcons.Zen), "icon_zen" },
        };

        [MenuItem("ShaurmaTycoon/Art/Assign Icons")]
        public static void AssignMenu()
        {
            AssignAll();
        }

        /// <returns>Ассет иконок интерфейса (создаётся, если его нет).</returns>
        public static UiIcons AssignAll()
        {
            var missing = new List<string>();
            UiIcons icons = AssignUiIcons(missing, out int uiAssigned);
            int stationsAssigned = AssignStationIcons(missing, out int stationsTotal);
            int layersAssigned = AssignCityScenes(missing, out int layersTotal);
            int kioskFound = CountKioskSprites(missing);
            int customersFound = AssignCharacters(missing, out bool sellerFound, out int catPoses);
            int ambientFound = AssignAmbient(missing);
            int coinPacksFound = AssignCoinPackIcons(missing, out int coinPacksTotal);
            int mapFound = AssignMap(missing);
            int audioFound = AudioAssigner.AssignAll(missing);
            AssetDatabase.SaveAssets();

            string report = $"[Assign Icons] UI: {uiAssigned}/{IconFiles.Count}, stations: {stationsAssigned}/{stationsTotal}, " +
                            $"city layers: {layersAssigned}/{layersTotal}, kiosk: {kioskFound}/{KioskFiles.Length}, " +
                            $"customers: {customersFound}/{CustomerNames.Length}, seller: {(sellerFound ? "yes" : "no")}, cat: {catPoses}/5, " +
                            $"ambient: {ambientFound}/{AmbientFiles.Length}, coin packs: {coinPacksFound}/{coinPacksTotal}, " +
                            $"map: {mapFound}/{MapFiles.Length}, sounds: {audioFound}/{AudioAssigner.Files.Length}.";
            if (missing.Count == 0)
                Debug.Log(report);
            else
                Debug.LogWarning(report + " Not found (placeholders stay):\n" + string.Join("\n", missing));
            return icons;
        }

        private const string BillShopPath = "Assets/_Game/Configs/BillShop.asset";
        public const string ShopIconsFolder = "Assets/_Game/Sprites/Shop";

        /// <summary>Иконки мешков монет: Sprites/Shop/&lt;Id пака&gt;.png (пакет I); нет картинки — icon_coin, а не цветная плашка.</summary>
        private static int AssignCoinPackIcons(List<string> missing, out int total)
        {
            total = 0;
            var shop = AssetDatabase.LoadAssetAtPath<ShaurmaTycoon.Shop.BillShopConfig>(BillShopPath);
            if (shop == null)
                return 0;

            Sprite fallback = LoadSprite($"{IconsFolder}/icon_coin.png");
            int found = 0;
            var so = new SerializedObject(shop);
            SerializedProperty packs = so.FindProperty("_coinPacks");
            total = packs.arraySize;
            for (int i = 0; i < packs.arraySize; i++)
            {
                SerializedProperty pack = packs.GetArrayElementAtIndex(i);
                string path = $"{ShopIconsFolder}/{pack.FindPropertyRelative("Id").stringValue}.png";
                Sprite sprite = LoadSprite(path);
                if (sprite != null)
                    found++;
                else
                    missing.Add(path);
                pack.FindPropertyRelative("Icon").objectReferenceValue = sprite != null ? sprite : fallback;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return found;
        }

        // ---------- City map (V5b) ----------

        public const string MapFolder = "Assets/_Game/Sprites/Map";

        /// <summary>Пакет I: фон карты, миниатюры городов, фургон, булавка «Вы здесь» (Sprites/Map/&lt;имя&gt;.png).</summary>
        public static readonly string[] MapFiles =
        {
            "map_bg", "map_city_01", "map_city_02", "map_city_03", "map_city_04", "map_city_05", "map_van", "map_pin",
        };

        public static Sprite LoadMapSprite(string name)
        {
            return LoadSprite($"{MapFolder}/{name}.png");
        }

        /// <summary>Считает картинки карты и ставит городам миниатюры по номеру: City_03_Sochi.asset → map_city_03.</summary>
        private static int AssignMap(List<string> missing)
        {
            int found = 0;
            foreach (string name in MapFiles)
            {
                if (LoadMapSprite(name) != null)
                    found++;
                else
                    missing.Add($"{MapFolder}/{name}.png");
            }

            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(Prestige.CityConfig), new[] { CityConfigsFolder }))
            {
                string configPath = AssetDatabase.GUIDToAssetPath(guid);
                Match match = Regex.Match(Path.GetFileName(configPath), @"^City_(\d+)_");
                if (!match.Success)
                    continue;

                var so = new SerializedObject(AssetDatabase.LoadAssetAtPath<Prestige.CityConfig>(configPath));
                so.FindProperty("_mapIcon").objectReferenceValue = LoadMapSprite($"map_city_{int.Parse(match.Groups[1].Value):00}");
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            return found;
        }

        private static UiIcons AssignUiIcons(List<string> missing, out int assigned)
        {
            var icons = AssetDatabase.LoadAssetAtPath<UiIcons>(IconsAssetPath);
            if (icons == null)
            {
                icons = ScriptableObject.CreateInstance<UiIcons>();
                AssetDatabase.CreateAsset(icons, IconsAssetPath);
            }

            assigned = 0;
            var so = new SerializedObject(icons);
            foreach (KeyValuePair<string, string> pair in IconFiles)
            {
                string path = $"{IconsFolder}/{pair.Value}.png";
                Sprite sprite = LoadSprite(path);
                so.FindProperty(pair.Key).objectReferenceValue = sprite;
                if (sprite != null)
                    assigned++;
                else
                    missing.Add(path);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(icons);
            return icons;
        }

        private static int AssignStationIcons(List<string> missing, out int total)
        {
            int assigned = 0;
            total = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(StationConfig), new[] { StationConfigsFolder }))
            {
                string configPath = AssetDatabase.GUIDToAssetPath(guid);
                var config = AssetDatabase.LoadAssetAtPath<StationConfig>(configPath);
                if (config == null)
                    continue;

                total++;
                // Configs/Stations/City01/Station_01_X.asset → Sprites/Stations/City01/Station_01_X.png
                string city = Path.GetFileName(Path.GetDirectoryName(configPath));
                string spritePath = $"{StationIconsFolder}/{city}/{Path.GetFileNameWithoutExtension(configPath)}.png";
                Sprite sprite = LoadSprite(spritePath);

                var so = new SerializedObject(config);
                SerializedProperty icon = so.FindProperty("_icon");
                if (icon.objectReferenceValue != sprite)
                {
                    icon.objectReferenceValue = sprite;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                if (sprite != null)
                    assigned++;
                else
                    missing.Add(spritePath);
            }
            return assigned;
        }

        // ---------- City scenes (V3) ----------

        public const string SceneFolder = "Assets/_Game/Sprites/Scene";
        public const string KioskFolder = "Assets/_Game/Sprites/Kiosk";
        private const string CityConfigsFolder = "Assets/_Game/Configs/Cities";

        /// <summary>Пакет D: части нарисованного ларька (Sprites/Kiosk/&lt;имя&gt;.png).</summary>
        public static readonly string[] KioskFiles =
        {
            "kiosk_body", "kiosk_counter", "kiosk_awning", "kiosk_sign", "kiosk_spit",
            "decor_garland", "decor_menu_board", "decor_table_umbrella", "decor_sign_lights", "decor_flowers", "decor_flags",
        };

        /// <summary>Вечер по городам (Docs/Visual/ART_DIRECTION.md): верх, середина, горизонт, ларёк, луна, звёзды.</summary>
        private static readonly Dictionary<int, (string top, string middle, string horizon, string booth, bool moon, bool stars)> Evenings =
            new Dictionary<int, (string, string, string, string, bool, bool)>
            {
                { 1, ("#5B4B8A", "#F2A07B", "#FFD7A0", "#D9604A", false, false) },
                { 2, ("#4E5A9C", "#E58FA6", "#FFD3B5", "#2F8F6B", false, false) },
                { 3, ("#3D6FA8", "#F29A5C", "#FFE0A3", "#2E9FB5", false, false) },
                { 4, ("#6B7FB8", "#C9B3E0", "#F4E3F0", "#3F5C9E", true, false) },
                { 5, ("#232A5C", "#6B4E8C", "#E08F6F", "#B23A48", true, true) },
            };

        public static Sprite LoadKioskSprite(string name)
        {
            return LoadSprite($"{KioskFolder}/{name}.png");
        }

        public static Sprite LoadSceneSprite(string relativePath)
        {
            return LoadSprite($"{SceneFolder}/{relativePath}.png");
        }

        /// <summary>
        /// Фоны города по номеру в имени ассета: City_03_Sochi.asset → Sprites/Scene/City03/bg_far.png, bg_near.png.
        /// Вечерние цвета (небо, ларёк, луна, звёзды) задаются один раз — пока у города не задан верх неба;
        /// дальше их можно править в ассете руками.
        /// </summary>
        private static int AssignCityScenes(List<string> missing, out int total)
        {
            int assigned = 0;
            total = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(Prestige.CityConfig), new[] { CityConfigsFolder }))
            {
                string configPath = AssetDatabase.GUIDToAssetPath(guid);
                Match match = Regex.Match(Path.GetFileName(configPath), @"^City_(\d+)_");
                if (!match.Success)
                    continue;

                int number = int.Parse(match.Groups[1].Value);
                var so = new SerializedObject(AssetDatabase.LoadAssetAtPath<Prestige.CityConfig>(configPath));
                foreach (string layer in new[] { "far", "near" })
                {
                    total++;
                    string path = $"{SceneFolder}/City{number:00}/bg_{layer}.png";
                    Sprite sprite = LoadSprite(path);
                    so.FindProperty("_bg" + (layer == "far" ? "Far" : "Near")).objectReferenceValue = sprite;
                    if (sprite != null)
                        assigned++;
                    else
                        missing.Add(path);
                }

                SerializedProperty top = so.FindProperty("_skyTop");
                if (top.colorValue.a <= 0f && Evenings.TryGetValue(number, out var evening))
                {
                    top.colorValue = Hex(evening.top);
                    so.FindProperty("_skyMiddle").colorValue = Hex(evening.middle);
                    so.FindProperty("_skyHorizon").colorValue = Hex(evening.horizon);
                    so.FindProperty("_boothColor").colorValue = Hex(evening.booth);
                    so.FindProperty("_showMoon").boolValue = evening.moon;
                    so.FindProperty("_showStars").boolValue = evening.stars;
                }
                AssignVehicles(so, number);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            return assigned;
        }

        private static int CountKioskSprites(List<string> missing)
        {
            int found = 0;
            foreach (string name in KioskFiles)
            {
                string path = $"{KioskFolder}/{name}.png";
                if (LoadSprite(path) != null)
                    found++;
                else
                    missing.Add(path);
            }
            return found;
        }

        // ---------- Living world (V5) ----------

        public const string AmbientFolder = "Assets/_Game/Sprites/Ambient";

        /// <summary>Пакет F: транспорт, зонтик, воробей (Sprites/Ambient/&lt;имя&gt;.png).</summary>
        public static readonly string[] AmbientFiles = { "car_01", "car_02", "car_03", "trolleybus", "scooter_courier", "umbrella", "bird" };

        private static readonly string[] CommonVehicles = { "car_01", "car_02", "car_03", "scooter_courier" };
        // Троллейбусы — в Питере и Москве.
        private static readonly int[] TrolleybusCities = { 4, 5 };

        // Высота на экране, px: картинки нарисованы в разном масштабе, а клиенты впереди — 230.
        private static readonly (string file, float height)[] VehicleHeights =
        {
            ("car_01", 118f), ("car_02", 125f), ("car_03", 130f), ("trolleybus", 215f), ("scooter_courier", 150f),
        };

        public static Sprite LoadAmbientSprite(string name)
        {
            return LoadSprite($"{AmbientFolder}/{name}.png");
        }

        private static void AssignVehicles(SerializedObject city, int number)
        {
            var vehicles = new List<Sprite>();
            foreach (string file in CommonVehicles)
                AddIfFound(vehicles, file);
            if (System.Array.IndexOf(TrolleybusCities, number) >= 0)
                AddIfFound(vehicles, "trolleybus");

            SerializedProperty list = city.FindProperty("_vehicles");
            list.arraySize = vehicles.Count;
            for (int i = 0; i < vehicles.Count; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = vehicles[i];
        }

        private static void AddIfFound(List<Sprite> list, string file)
        {
            Sprite sprite = LoadAmbientSprite(file);
            if (sprite != null)
                list.Add(sprite);
        }

        /// <summary>Отчёт по пакету F и высоты транспорта в Ambient.asset (только если таблица ещё пустая — ручные правки остаются).</summary>
        private static int AssignAmbient(List<string> missing)
        {
            int found = 0;
            foreach (string name in AmbientFiles)
            {
                string path = $"{AmbientFolder}/{name}.png";
                if (LoadSprite(path) != null)
                    found++;
                else
                    missing.Add(path);
            }

            var so = new SerializedObject(ContentSetup.EnsureAmbient());
            SerializedProperty sizes = so.FindProperty("_vehicleSizes");
            if (sizes.arraySize == 0)
            {
                var rows = new List<(Sprite sprite, float height)>();
                foreach ((string file, float height) in VehicleHeights)
                {
                    Sprite sprite = LoadAmbientSprite(file);
                    if (sprite != null)
                        rows.Add((sprite, height));
                }
                sizes.arraySize = rows.Count;
                for (int i = 0; i < rows.Count; i++)
                {
                    SerializedProperty row = sizes.GetArrayElementAtIndex(i);
                    row.FindPropertyRelative(nameof(Ambient.VehicleSize.Sprite)).objectReferenceValue = rows[i].sprite;
                    row.FindPropertyRelative(nameof(Ambient.VehicleSize.Height)).floatValue = rows[i].height;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            return found;
        }

        // ---------- Characters (V4) ----------

        public const string CharactersFolder = "Assets/_Game/Sprites/Characters";
        private const string CustomerConfigPath = "Assets/_Game/Configs/Customers.asset";
        public const string CatConfigPath = "Assets/_Game/Configs/Cat.asset";

        /// <summary>Пакет E: клиенты (Characters/Customers/&lt;имя&gt;.png и &lt;имя&gt;_happy.png) и их реплики.</summary>
        public static readonly string[] CustomerNames =
        {
            "student", "babushka", "office", "courier", "tourist", "builder", "girl_headphones", "fisherman", "hipster", "schoolkid",
        };

        /// <summary>
        /// Новые персонажи заменяют клиентов Kenney в Customers.asset, только если найден хотя бы один:
        /// иначе остаются прежние скины с покадровой ходьбой. Продавец — так же. Кот — Configs/Cat.asset.
        /// </summary>
        private static int AssignCharacters(List<string> missing, out bool sellerFound, out int catPoses)
        {
            var config = AssetDatabase.LoadAssetAtPath<ShaurmaTycoon.Customers.CustomerConfig>(CustomerConfigPath);
            sellerFound = false;
            int found = 0;
            if (config != null)
            {
                var so = new SerializedObject(config);
                var skins = new List<(Sprite idle, Sprite happy, string name)>();
                foreach (string name in CustomerNames)
                {
                    string path = $"{CharactersFolder}/Customers/{name}.png";
                    Sprite idle = LoadSprite(path);
                    if (idle == null)
                    {
                        missing.Add(path);
                        continue;
                    }
                    skins.Add((idle, LoadSprite($"{CharactersFolder}/Customers/{name}_happy.png"), name));
                }
                found = skins.Count;

                if (skins.Count > 0)
                {
                    SerializedProperty list = so.FindProperty("_customerSkins");
                    list.arraySize = skins.Count;
                    for (int i = 0; i < skins.Count; i++)
                        WriteSkin(list.GetArrayElementAtIndex(i), skins[i].idle, skins[i].happy, skins[i].idle, "customer.phrase." + skins[i].name);
                }

                Sprite seller = LoadSprite($"{CharactersFolder}/Seller/seller_idle.png");
                sellerFound = seller != null;
                if (sellerFound)
                {
                    WriteSkin(so.FindProperty("_sellerSkin"), seller, LoadSprite($"{CharactersFolder}/Seller/seller_happy.png"),
                        LoadSprite($"{CharactersFolder}/Seller/seller_work.png"), string.Empty);
                }
                else
                {
                    missing.Add($"{CharactersFolder}/Seller/seller_idle.png");
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            catPoses = AssignCat(missing);
            return found;
        }

        // Скин без кадров ходьбы: идёт процедурно; персонажи V4 нарисованы лицом влево.
        private static void WriteSkin(SerializedProperty skin, Sprite idle, Sprite cheer, Sprite interact, string phraseKey)
        {
            skin.FindPropertyRelative("Idle").objectReferenceValue = idle;
            skin.FindPropertyRelative("Cheer").objectReferenceValue = cheer;
            skin.FindPropertyRelative("Interact").objectReferenceValue = interact;
            skin.FindPropertyRelative("Walk").arraySize = 0;
            skin.FindPropertyRelative("FacesLeft").boolValue = true;
            skin.FindPropertyRelative("PhraseKey").stringValue = phraseKey;
        }

        private static int AssignCat(List<string> missing)
        {
            var cat = AssetDatabase.LoadAssetAtPath<ShaurmaTycoon.Customers.CatConfig>(CatConfigPath);
            if (cat == null)
            {
                cat = ScriptableObject.CreateInstance<ShaurmaTycoon.Customers.CatConfig>();
                AssetDatabase.CreateAsset(cat, CatConfigPath);
            }

            int found = 0;
            var so = new SerializedObject(cat);
            foreach ((string field, string file) in new[]
                     { ("_sleep", "cat_sleep"), ("_sit", "cat_sit"), ("_purr", "cat_purr"), ("_stretch", "cat_stretch"), ("_bed", "cat_bed") })
            {
                string path = $"{CharactersFolder}/Cat/{file}.png";
                Sprite sprite = LoadSprite(path);
                so.FindProperty(field).objectReferenceValue = sprite;
                if (sprite != null)
                    found++;
                else
                    missing.Add(path);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return found;
        }

        public static ShaurmaTycoon.Customers.CatConfig LoadCatConfig()
        {
            return AssetDatabase.LoadAssetAtPath<ShaurmaTycoon.Customers.CatConfig>(CatConfigPath);
        }

        private static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out Color color) ? color : Color.magenta;
        }

        // Страховка: если PNG импортировался до SpriteImportSettings, переимпортировать как спрайт.
        private static Sprite LoadSprite(string path)
        {
            if (!File.Exists(path))
                return null;

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
                return sprite;

            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.SaveAndReimport();
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return sprite;
        }
    }
}
