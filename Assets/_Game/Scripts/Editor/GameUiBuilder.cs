using System.Collections.Generic;
using System.IO;
using ShaurmaTycoon.Ambient;
using ShaurmaTycoon.Core;
using ShaurmaTycoon.Core.Audio;
using ShaurmaTycoon.Customers;
using ShaurmaTycoon.Localization;
using ShaurmaTycoon.Prestige;
using ShaurmaTycoon.Retention;
using ShaurmaTycoon.Shop;
using ShaurmaTycoon.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ShaurmaTycoon.EditorTools
{
    /// <summary>
    /// Меню ShaurmaTycoon/Build Game UI: собирает префаб строки станции и игровой экран в сцене Game.
    /// Размеры — в координатах портретного канваса 1080×1920.
    /// </summary>
    public static class GameUiBuilder
    {
        private const string GameScenePath = "Assets/_Game/Scenes/Game.unity";
        private const string PrefabFolder = "Assets/_Game/Prefabs/UI";
        private const string RowPrefabPath = PrefabFolder + "/StationRow.prefab";
        private const string ManagerRowPrefabPath = PrefabFolder + "/ManagerRow.prefab";
        private const string UpgradeRowPrefabPath = PrefabFolder + "/UpgradeRow.prefab";
        private const string ScreenName = "GameScreen";
        private const string CityCatalogPath = "Assets/_Game/Configs/Cities/CityCatalog.asset";
        private const string ShopRowPrefabPath = PrefabFolder + "/ShopItemRow.prefab";
        private const string MapNodePrefabPath = PrefabFolder + "/CityMapNode.prefab";
        private const string RoadDotPrefabPath = PrefabFolder + "/RoadDot.prefab";
        private const string BillShopRowPrefabPath = PrefabFolder + "/BillShopRow.prefab";

        private const float HudHeight = 200f;
        private const float TabBarHeight = 180f;
        private const float RowHeight = 220f;
        private const float ManagerRowHeight = 180f;
        private const float UpgradeRowHeight = 160f;
        private const float HeaderHeight = 110f;
        private const float ShopRowHeight = 170f;
        private const float CityCardHeight = 1110f;
        // Высота карты под пропорции map_bg (1024×780) при ширине 1000: поляны городов совпадают с точками.
        private const float MapHeight = 760f;
        private const float MapWidth = 1000f;
        private const float MapIconSize = 120f;
        private const int KioskDecorCount = 6;
        private const float GroundRatio = 0.2f;
        private const int CustomerPoolSize = 9;
        private const int StreetHeartPoolSize = 6;
        private const int CatHeartPoolSize = 4;
        // Кот слева от ларька, левее меню-доски: в ландшафте зона ларька — половина экрана (≈960), коробка ещё целиком видна.
        private const float CatX = -400f;
        private const float CatBedWidth = 160f;
        private const float CatWidth = 124f;
        // Зонтик в руке клиента (персонаж смотрит влево): точка хвата на umbrella.png (низ прямой ручки над крючком),
        // кисть — доли ширины вперёд и высоты от ног, наклон купола назад, высота зонтика — доля роста.
        private static readonly Vector2 UmbrellaGrip = new Vector2(0.5f, 0.2f);
        private static readonly Vector2 UmbrellaHand = new Vector2(0.12f, 0.42f);
        private const float UmbrellaTilt = 38f;
        private const float UmbrellaHeight = 0.82f;
        private const int CoinPoolSize = 24;
        private const int FloatingTextPoolSize = 12;

        // Все цвета — из Configs/UiTheme.asset (этап V1). Имена — роли в этом сборщике.
        private static Color Cream => _theme.Panel;
        private static Color RowColor => _theme.Row;
        private static Color DarkBrown => _theme.TextDark;
        private static Color Brown => _theme.Neutral;
        private static Color Orange => _theme.Secondary;
        private static Color Sky => _theme.Sky;
        private static Color Pavement => _theme.Pavement;
        private static Color Curb => _theme.Curb;
        private static Color BoothRed => _theme.Booth;
        private static Color Badge => _theme.Badge;
        private static Color WindowDark => _theme.WindowDark;
        private static Color Wood => _theme.Wood;
        private static Color Meat => _theme.Meat;
        private static Color MeatDark => _theme.MeatDark;
        private static Color AwningWhite => _theme.Awning;
        private static Color Steel => _theme.Steel;
        private static Color CoinInner => _theme.CoinInner;
        private static Color Reputation => _theme.Reputation;
        private static Color Green => _theme.Primary;
        private static Color Leaf => _theme.Leaf;
        private static Color BarBack => _theme.RowShade;
        private static Color Gold => _theme.Gold;
        private static Color LightGold => _theme.LightGold;
        private static Color Muted => _theme.TextMuted;
        private static Color LeafDark => _theme.LeafDark;
        private static Color BillGreen => _theme.Bills;
        private static Color BillLight => _theme.BillsLight;
        private static Color Land => _theme.Land;
        private static Color Hill => _theme.Hill;
        private static Color Water => _theme.Water;
        private static Color Paper => _theme.Paper;
        private static Color GiftRed => _theme.GiftRed;
        private static Color Light => _theme.TextLight;

        private static UiTheme _theme;
        private static CustomerConfig _customerConfig;
        // Иконки пакета A (этап V2); пустое поле — рисуем заглушку из примитивов, как раньше.
        private static UiIcons _icons;
        private static Sprite _roundSprite;
        private static Sprite _circleSprite;
        private static Sprite _circleOutlineSprite;
        private static Sprite _outlineSprite;
        private static Sprite _cardSprite;
        private static Sprite _buttonSprite;
        private static Sprite _buttonPressedSprite;
        private static Sprite _edgeShadowSprite;
        private static Sprite _glowSprite;
        private static Sprite _pillSprite;
        private static TMP_FontAsset _font;
        private static Material _outlinedMaterial;

        /// <summary>Стиль объёмной кнопки (Docs/Visual/ART_DIRECTION.md, «Палитра»).</summary>
        private enum ButtonStyle
        {
            Primary,
            Secondary,
            Neutral,
            Reward,
            Bills,
            Reputation,
        }

        private static Color StyleColor(ButtonStyle style)
        {
            switch (style)
            {
                case ButtonStyle.Primary: return _theme.Primary;
                case ButtonStyle.Secondary: return _theme.Secondary;
                case ButtonStyle.Reward: return _theme.Reward;
                case ButtonStyle.Bills: return _theme.Bills;
                case ButtonStyle.Reputation: return _theme.Reputation;
                default: return _theme.Neutral;
            }
        }

        [MenuItem("ShaurmaTycoon/Build Game UI")]
        public static void Build()
        {
            if (TMP_Settings.instance == null)
            {
                EditorUtility.DisplayDialog("Build Game UI",
                    "TMP Essentials не найдены. Импортируй их: Window → TextMeshPro → Import TMP Essential Resources.", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            ContentSetup.EnsureAll(out AudioConfig audioConfig, out MusicConfig musicConfig, out CustomerConfig customerConfig,
                out ReputationShopConfig shopConfig, out BillShopConfig billShopConfig);
            _font = TMP_Settings.defaultFontAsset;
            _customerConfig = customerConfig;
            _theme = ContentSetup.EnsureTheme();
            _outlinedMaterial = ContentSetup.EnsureOutlineMaterial(_theme);
            ProceduralSprites.EnsureGenerated(_theme);
            _roundSprite = ProceduralSprites.Load(ProceduralSprites.RoundedFill);
            _outlineSprite = ProceduralSprites.Load(ProceduralSprites.RoundedOutline);
            _cardSprite = ProceduralSprites.Load(ProceduralSprites.CardFill);
            _buttonSprite = ProceduralSprites.Load(ProceduralSprites.ButtonFill);
            _buttonPressedSprite = ProceduralSprites.Load(ProceduralSprites.ButtonFillPressed);
            _edgeShadowSprite = ProceduralSprites.Load(ProceduralSprites.EdgeShadow);
            _glowSprite = ProceduralSprites.Load(ProceduralSprites.RoundedGlow);
            _pillSprite = ProceduralSprites.Load(ProceduralSprites.PillFill);
            _circleSprite = ProceduralSprites.Load(ProceduralSprites.CircleFill);
            _circleOutlineSprite = ProceduralSprites.Load(ProceduralSprites.CircleOutline);
            _icons = ArtAssigner.AssignAll();

            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            GameObject canvas = FindRoot(scene, "Canvas");
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Build Game UI", "В сцене Game нет объекта Canvas. Запусти ShaurmaTycoon/Setup Project.", "OK");
                return;
            }

            Transform oldScreen = canvas.transform.Find(ScreenName);
            if (oldScreen != null)
            {
                if (!EditorUtility.DisplayDialog("Build Game UI",
                        "Экран уже собран. Пересобрать? Ручные правки в GameScreen и префабах строк (Prefabs/UI) пропадут.", "Пересобрать", "Отмена"))
                    return;
                Object.DestroyImmediate(oldScreen.gameObject);
            }

            Transform debugLabel = canvas.transform.Find("DebugLabel_Stage00");
            if (debugLabel != null)
                Object.DestroyImmediate(debugLabel.gameObject);

            AssignInstallerConfigs(scene, audioConfig, musicConfig, customerConfig, shopConfig, billShopConfig, ContentSetup.EnsureRetention());

            StationRowView rowPrefab = BuildRowPrefab();
            BuildScreen(canvas, rowPrefab, BuildManagerRowPrefab(), BuildUpgradeRowPrefab(), BuildShopRowPrefab());

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Build Game UI] Done");
        }

        private static void AssignInstallerConfigs(Scene scene, AudioConfig audio, MusicConfig music, CustomerConfig customers,
            ReputationShopConfig shop, BillShopConfig billShop, RetentionConfig retention)
        {
            GameObject installerObject = FindRoot(scene, nameof(GameSceneInstaller));
            GameSceneInstaller installer = installerObject != null ? installerObject.GetComponent<GameSceneInstaller>() : null;
            if (installer == null)
            {
                Debug.LogError("[Build Game UI] GameSceneInstaller not found in the Game scene.");
                return;
            }

            var so = new SerializedObject(installer);
            Set(so, "_audio", audio);
            Set(so, "_music", music);
            Set(so, "_billShop", billShop);
            Set(so, "_customers", customers);
            Set(so, "_shop", shop);
            Set(so, "_retention", retention);
            Set(so, "_ambient", ContentSetup.EnsureAmbient());
            Set(so, "_ambientAudio", ContentSetup.EnsureAmbientAudio());
            Set(so, "_cities", AssetDatabase.LoadAssetAtPath<CityCatalog>(CityCatalogPath));
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- Station row prefab ----------

        private static StationRowView BuildRowPrefab()
        {
            Directory.CreateDirectory(PrefabFolder);

            GameObject root = CreateUi("StationRow", null);
            var rootRect = (RectTransform)root.transform;
            rootRect.sizeDelta = new Vector2(1000f, RowHeight);
            AddCard(root, RowColor);
            root.AddComponent<LayoutElement>().preferredHeight = RowHeight;
            var view = root.AddComponent<StationRowView>();

            // Иконка-кнопка слева.
            GameObject icon = CreateUi("IconButton", root.transform);
            SetRect(icon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(180f, 180f));
            Image iconImage = AddCircle(icon, Orange);
            Button iconButton = AddButton(icon, SoundId.Tap);
            TMP_Text iconLabel = AddText(icon.transform, "IconLabel", "1", 80, Light, TextAlignmentOptions.Center, bold: true);
            Outlined(iconLabel);
            Stretch(iconLabel.gameObject, new Vector2(0f, 30f), Vector2.zero);
            // Еда станции (StationConfig.Icon) — чуть выше центра: снизу бейдж уровня. Спрайт назначает строка в игре.
            Image iconArt = AddArt(icon.transform, "Art", null);
            iconArt.rectTransform.anchorMin = new Vector2(0.08f, 0.2f);
            iconArt.rectTransform.anchorMax = new Vector2(0.92f, 0.98f);

            GameObject badge = CreateUi("LevelBadge", icon.transform);
            SetRect(badge, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(150f, 50f));
            AddImage(badge, _theme.Outline, _pillSprite);
            TMP_Text levelText = AddText(badge.transform, "LevelText", "Ур. 1", 30, Light, TextAlignmentOptions.Center, bold: true, autoSize: true);
            Stretch(levelText.gameObject, new Vector2(8f, 0f), new Vector2(-8f, 0f));

            // Название, прогресс, веха — по центру.
            TMP_Text nameText = AddText(root.transform, "NameText", "Station", 42, DarkBrown, TextAlignmentOptions.Left, bold: true, autoSize: true);
            SetRect(nameText.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(220f, -16f), new Vector2(-490f, 56f));

            GameObject bar = CreateUi("ProgressBar", root.transform);
            SetRect(bar, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(220f, -4f), new Vector2(-490f, 72f));
            RectTransform fillRect = AddProgressFill(bar, Gold);
            Button barButton = AddButton(bar, SoundId.Tap);
            PulseEffect tapPulse = AddPulse(bar);

            TMP_Text incomeText = AddText(bar.transform, "IncomeText", "+1", 34, DarkBrown, TextAlignmentOptions.Left, bold: true, autoSize: true);
            Stretch(incomeText.gameObject, new Vector2(18f, 0f), new Vector2(-130f, 0f));
            TMP_Text cycleText = AddText(bar.transform, "CycleTimeText", "0.6 с", 28, DarkBrown, TextAlignmentOptions.Right, autoSize: true);
            SetRect(cycleText.gameObject, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(120f, 0f));

            TMP_Text milestoneText = AddText(root.transform, "MilestoneText", "", 28, Muted, TextAlignmentOptions.Left, autoSize: true);
            SetRect(milestoneText.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(220f, 14f), new Vector2(-490f, 40f));

            // Кнопка покупки справа.
            GameObject buy = CreateUi("BuyButton", root.transform);
            SetRect(buy, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(230f, 180f));
            Button buyButton = AddSoftButton(buy, ButtonStyle.Primary, SoundId.Buy);
            TMP_Text buyCount = AddButtonLabel(buy.transform, "BuyCountText", "×1", 34);
            SetRect(buyCount.gameObject, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-20f, 0f));
            TMP_Text buyCost = AddButtonLabel(buy.transform, "BuyCostText", "4", 42);
            SetRect(buyCost.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-20f, 0f));
            AddAvailableGlow(buyButton);

            // Оверлей закрытой станции.
            GameObject locked = CreateUi("Locked", root.transform);
            Stretch(locked, Vector2.zero, Vector2.zero);
            AddImage(locked, UiTheme.WithAlpha(_theme.Outline, 0.88f), _cardSprite);
            // Светлый силуэт будущей еды и замок — «что-то вкусное скоро откроется».
            Image lockedArt = AddArtAt(locked.transform, "LockedArt", null, new Vector2(0f, 0.5f), new Vector2(110f, 0f), 150f);
            lockedArt.color = UiTheme.WithAlpha(Light, 0.16f);
            lockedArt.enabled = false;
            float lockedNameLeft = 40f;
            if (_icons.Lock != null)
            {
                AddArtAt(locked.transform, "Lock", _icons.Lock, new Vector2(0f, 0.5f), new Vector2(160f, -42f), 70f);
                lockedNameLeft = 220f;
            }
            TMP_Text lockedName = AddText(locked.transform, "LockedNameText", "Station", 42, Light, TextAlignmentOptions.Left, bold: true, autoSize: true);
            SetRect(lockedName.gameObject, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), new Vector2(lockedNameLeft, 0f),
                new Vector2(-360f - lockedNameLeft, -40f));
            GameObject unlock = CreateUi("UnlockButton", locked.transform);
            SetRect(unlock, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(300f, 170f));
            Button unlockButton = AddSoftButton(unlock, ButtonStyle.Secondary, SoundId.Unlock);
            TMP_Text unlockCost = AddButtonLabel(unlock.transform, "UnlockCostText", "Открыть\n60", 38);
            Stretch(unlockCost.gameObject, new Vector2(12f, 8f), new Vector2(-12f, -8f));
            AddAvailableGlow(unlockButton);

            var so = new SerializedObject(view);
            SetArray(so, "_tapButtons", iconButton, barButton);
            Set(so, "_icon", iconImage);
            Set(so, "_iconLabel", iconLabel);
            Set(so, "_iconArt", iconArt);
            Set(so, "_lockedIconArt", lockedArt);
            Set(so, "_nameText", nameText);
            Set(so, "_levelText", levelText);
            Set(so, "_progressFill", fillRect);
            Set(so, "_incomeText", incomeText);
            Set(so, "_cycleTimeText", cycleText);
            Set(so, "_milestoneText", milestoneText);
            Set(so, "_tapPulse", tapPulse);
            Set(so, "_buyButton", buyButton);
            Set(so, "_buyCountText", buyCount);
            Set(so, "_buyCostText", buyCost);
            Set(so, "_lockedRoot", locked);
            Set(so, "_lockedNameText", lockedName);
            Set(so, "_unlockButton", unlockButton);
            Set(so, "_buyOffer", AddBillOffer(buyButton));
            Set(so, "_unlockOffer", AddBillOffer(unlockButton));
            Set(so, "_unlockCostText", unlockCost);
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, RowPrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<StationRowView>();
        }

        // ---------- Manager / upgrade row prefabs ----------

        private static ManagerRowView BuildManagerRowPrefab()
        {
            GameObject root = CreateUi("ManagerRow", null);
            ((RectTransform)root.transform).sizeDelta = new Vector2(1000f, ManagerRowHeight);
            AddCard(root, RowColor);
            root.AddComponent<LayoutElement>().preferredHeight = ManagerRowHeight;
            var view = root.AddComponent<ManagerRowView>();

            Image icon = BuildRowIcon(root.transform, 140f, out TMP_Text iconLabel, 64, out Image iconArt);
            // Колпак повара в углу значка: это строка найма.
            if (_icons.TabManagers != null)
                AddArtAt(icon.transform, "ManagerBadge", _icons.TabManagers, new Vector2(1f, 0f), new Vector2(-8f, 14f), 58f);

            TMP_Text nameText = AddText(root.transform, "NameText", "Station", 40, DarkBrown, TextAlignmentOptions.Left, bold: true, autoSize: true);
            SetRect(nameText.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(180f, -20f), new Vector2(-470f, 56f));
            TMP_Text description = AddText(root.transform, "DescriptionText", "", 30, Muted, TextAlignmentOptions.TopLeft, autoSize: true);
            SetRect(description.gameObject, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(180f, 18f), new Vector2(-470f, -94f));

            Button hireButton = BuildRowButton(root.transform, "HireButton", ButtonStyle.Primary, new Vector2(270f, 140f), SoundId.Hire, out TMP_Text hireText);

            var so = new SerializedObject(view);
            Set(so, "_icon", icon);
            Set(so, "_iconLabel", iconLabel);
            Set(so, "_iconArt", iconArt);
            Set(so, "_nameText", nameText);
            Set(so, "_descriptionText", description);
            Set(so, "_hireButton", hireButton);
            Set(so, "_hireText", hireText);
            Set(so, "_hireOffer", AddBillOffer(hireButton));
            so.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, ManagerRowPrefabPath).GetComponent<ManagerRowView>();
        }

        private static UpgradeRowView BuildUpgradeRowPrefab()
        {
            GameObject root = CreateUi("UpgradeRow", null);
            ((RectTransform)root.transform).sizeDelta = new Vector2(1000f, UpgradeRowHeight);
            AddCard(root, RowColor);
            root.AddComponent<LayoutElement>().preferredHeight = UpgradeRowHeight;
            var view = root.AddComponent<UpgradeRowView>();

            Image icon = BuildRowIcon(root.transform, 120f, out TMP_Text iconLabel, 34, out Image iconArt, badgeLabel: true);

            TMP_Text description = AddText(root.transform, "DescriptionText", "Station: income ×3", 38, DarkBrown, TextAlignmentOptions.Left, bold: true, autoSize: true);
            SetRect(description.gameObject, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), new Vector2(160f, 0f), new Vector2(-450f, -30f));

            Button buyButton = BuildRowButton(root.transform, "BuyButton", ButtonStyle.Secondary, new Vector2(270f, 120f), SoundId.Upgrade, out TMP_Text buyText);

            var so = new SerializedObject(view);
            Set(so, "_icon", icon);
            Set(so, "_iconLabel", iconLabel);
            Set(so, "_iconArt", iconArt);
            Set(so, "_allStationsIcon", _icons.TabUpgrades);
            Set(so, "_descriptionText", description);
            Set(so, "_buyButton", buyButton);
            Set(so, "_buyText", buyText);
            Set(so, "_buyOffer", AddBillOffer(buyButton));
            so.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, UpgradeRowPrefabPath).GetComponent<UpgradeRowView>();
        }

        /// <param name="badgeLabel">Подпись — бейджем в правом нижнем углу (множитель улучшения), а не номером по центру.</param>
        private static Image BuildRowIcon(Transform parent, float size, out TMP_Text label, float labelSize, out Image art,
            bool badgeLabel = false)
        {
            GameObject icon = CreateUi("Icon", parent);
            SetRect(icon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(size, size));
            Image image = AddCircle(icon, Orange);
            image.raycastTarget = false;
            art = AddArt(icon.transform, "Art", null, 0.08f);

            if (!badgeLabel)
            {
                label = AddText(icon.transform, "IconLabel", "1", labelSize, Light, TextAlignmentOptions.Center, bold: true, autoSize: true);
                Outlined(label);
                Stretch(label.gameObject, new Vector2(10f, 0f), new Vector2(-10f, 0f));
                return image;
            }

            GameObject badge = CreateUi("Badge", icon.transform);
            SetRect(badge, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(14f, -4f), new Vector2(size * 0.72f, size * 0.36f));
            AddImage(badge, _theme.Secondary, _pillSprite).raycastTarget = false;
            label = AddButtonLabel(badge.transform, "IconLabel", "×3", labelSize);
            Stretch(label.gameObject, new Vector2(6f, 2f), new Vector2(-6f, -2f));
            return image;
        }

        // Кнопка справа в строке списка: объёмная, светится, пока доступна (без пульса — спокойный ритм).
        private static Button BuildRowButton(Transform parent, string name, ButtonStyle style, Vector2 size, SoundId sound, out TMP_Text label)
        {
            GameObject buttonObject = CreateUi(name, parent);
            SetRect(buttonObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20f, 0f), size);
            Button button = AddSoftButton(buttonObject, style, sound);
            label = AddButtonLabel(buttonObject.transform, "Label", "", 36);
            Stretch(label.gameObject, new Vector2(12f, 8f), new Vector2(-12f, -8f));
            AddAvailableGlow(button);
            return button;
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ---------- Screen ----------

        private static void BuildScreen(GameObject canvas, StationRowView rowPrefab, ManagerRowView managerRowPrefab,
            UpgradeRowView upgradeRowPrefab, ShopItemRowView shopRowPrefab)
        {
            GameObject screen = CreateUi(ScreenName, canvas.transform);
            Stretch(screen, Vector2.zero, Vector2.zero);
            var gameScreen = screen.AddComponent<GameScreen>();
            var layout = screen.AddComponent<ResponsiveLayout>();

            GameObject background = CreateUi("Background", screen.transform);
            Stretch(background, Vector2.zero, Vector2.zero);
            AddImage(background, _theme.ScreenBackground, null).raycastTarget = false;

            // Ларёк и улица с клиентами.
            GameObject kiosk = CreateUi("KioskArea", screen.transform);
            Stretch(kiosk, Vector2.zero, Vector2.zero);
            Image sky = AddImage(kiosk, Sky, null);
            sky.raycastTarget = false;
            kiosk.AddComponent<RectMask2D>();
            // Сцена города (небо, луна, слои) — под тротуаром и ларьком.
            CitySceneView cityScene = BuildCityScene(kiosk.transform);
            KioskView kioskView = ArtAssigner.LoadKioskSprite("kiosk_body") != null
                ? BuildKioskArt(kiosk.transform, sky)
                : BuildKiosk(kiosk.transform, sky);
            // Кот — до улицы: уходящие клиенты проходят перед ним.
            CatView cat = BuildCat(kiosk.transform);
            CustomerQueue customers = BuildStreet(kiosk.transform, kioskView, cat);
            AmbientConfig ambientConfig = ContentSetup.EnsureAmbient();
            AmbientWorldView ambient = BuildAmbient(kiosk.transform, cityScene.transform, ambientConfig);
            WeatherView weather = BuildWeather(kiosk.transform, cityScene.transform, ambientConfig);

            // Контент: панели вкладок.
            GameObject content = CreateUi("ContentArea", screen.transform);
            Stretch(content, Vector2.zero, Vector2.zero);

            StationsPanel stationsPanel = BuildStationsPanel(content.transform, rowPrefab);
            ManagersPanel managersPanel = BuildManagersPanel(content.transform, managerRowPrefab);
            UpgradesPanel upgradesPanel = BuildUpgradesPanel(content.transform, upgradeRowPrefab);
            CityPanel cityPanel = BuildCityPanel(content.transform, shopRowPrefab);
            ShopPanel shopPanel = BuildShopPanel(content.transform);

            HudView hud = BuildHud(screen.transform);
            TabBar tabBar = BuildTabBar(screen.transform, out Button[] tabButtons);
            OfflineIncomePopup offlinePopup = BuildOfflinePopup(screen.transform);
            PrestigeConfirmPopup prestigePopup = BuildPrestigePopup(screen.transform);
            SettingsPopup settingsPopup = BuildSettingsPopup(screen.transform, hud.transform);
            BillOfferPopup billOfferPopup = BuildBillOfferPopup(screen.transform);
            RetentionHud retentionHud = BuildRetention(screen.transform, kiosk.transform, hud.transform, out DailyRewardPopup dailyPopup,
                out QuestsPopup questsPopup, out AdOfferPopup adOfferPopup);
            var cityPanelSo = new SerializedObject(cityPanel);
            Set(cityPanelSo, "_confirmPopup", prestigePopup);
            cityPanelSo.ApplyModifiedPropertiesWithoutUndo();
            BuildEffects(screen.transform, hud.CoinIcon, out CoinFlyEffect coinFly, out FloatingTextEffect floatingText);
            ZenMode zenMode = BuildZenMode(screen.transform, layout, kiosk.transform, cityScene.transform, hud.transform, tabBar.transform,
                content.transform);
            // Окна без своих эффектов — поверх летящих монет и цифр дохода.
            prestigePopup.transform.SetAsLastSibling();
            settingsPopup.transform.SetAsLastSibling();
            billOfferPopup.transform.SetAsLastSibling();
            adOfferPopup.transform.SetAsLastSibling();
            TutorialHand tutorialHand = BuildTutorialHand(screen.transform, offlinePopup, dailyPopup, questsPopup, adOfferPopup, billOfferPopup,
                prestigePopup, settingsPopup);

            var tabSo = new SerializedObject(tabBar);
            SetArray(tabSo, "_buttons", tabButtons);
            var shopPanelSo = new SerializedObject(shopPanel);
            Set(shopPanelSo, "_tabBadge", BuildTabBadge(tabButtons[4].transform));
            Set(shopPanelSo, "_adIcon", _icons.Ad);
            SetColor(shopPanelSo, "_billColor", BillGreen);
            SetColor(shopPanelSo, "_billMarkColor", BillLight);
            SetColor(shopPanelSo, "_coinColor", Gold);
            SetColor(shopPanelSo, "_coinMarkColor", CoinInner);
            SetColor(shopPanelSo, "_billButtonColor", _theme.Bills);
            SetColor(shopPanelSo, "_coinButtonColor", _theme.Secondary);
            SetColor(shopPanelSo, "_adColor", Reputation);
            SetColor(shopPanelSo, "_adMarkColor", Light);
            SetColor(shopPanelSo, "_premiumColor", Badge);
            SetColor(shopPanelSo, "_premiumMarkColor", LightGold);
            SetColor(shopPanelSo, "_premiumButtonColor", _theme.Primary);
            shopPanelSo.ApplyModifiedPropertiesWithoutUndo();
            SetArray(tabSo, "_panels", stationsPanel.gameObject, managersPanel.gameObject, upgradesPanel.gameObject, cityPanel.gameObject,
                shopPanel.gameObject);
            SetColor(tabSo, "_activeColor", _theme.Secondary);
            SetColor(tabSo, "_inactiveColor", _theme.Neutral);
            tabSo.FindProperty("_raise").floatValue = _theme.TabRaise;
            tabSo.FindProperty("_transitionDuration").floatValue = _theme.TabTransition;
            tabSo.ApplyModifiedPropertiesWithoutUndo();

            var screenSo = new SerializedObject(gameScreen);
            Set(screenSo, "_hud", hud);
            Set(screenSo, "_stationsPanel", stationsPanel);
            Set(screenSo, "_managersPanel", managersPanel);
            Set(screenSo, "_upgradesPanel", upgradesPanel);
            Set(screenSo, "_cityPanel", cityPanel);
            Set(screenSo, "_prestigePopup", prestigePopup);
            Set(screenSo, "_settingsPopup", settingsPopup);
            Set(screenSo, "_shopPanel", shopPanel);
            Set(screenSo, "_tabBar", tabBar);
            Set(screenSo, "_billOfferPopup", billOfferPopup);
            screenSo.FindProperty("_shopTabIndex").intValue = 4;
            Set(screenSo, "_kiosk", kioskView);
            Set(screenSo, "_scene", cityScene);
            Set(screenSo, "_customers", customers);
            Set(screenSo, "_cat", cat);
            Set(screenSo, "_ambient", ambient);
            Set(screenSo, "_weather", weather);
            Set(screenSo, "_coinFly", coinFly);
            Set(screenSo, "_floatingText", floatingText);
            Set(screenSo, "_offlinePopup", offlinePopup);
            Set(screenSo, "_retentionHud", retentionHud);
            Set(screenSo, "_dailyPopup", dailyPopup);
            Set(screenSo, "_questsPopup", questsPopup);
            Set(screenSo, "_adOfferPopup", adOfferPopup);
            Set(screenSo, "_tutorialHand", tutorialHand);
            Set(screenSo, "_zenMode", zenMode);
            screenSo.ApplyModifiedPropertiesWithoutUndo();

            var layoutSo = new SerializedObject(layout);
            Set(layoutSo, "_scaler", canvas.GetComponent<CanvasScalerAdapter>());
            Set(layoutSo, "_kioskArea", kiosk.transform);
            Set(layoutSo, "_contentArea", content.transform);
            layoutSo.FindProperty("_hudHeight").floatValue = HudHeight;
            layoutSo.FindProperty("_tabBarHeight").floatValue = TabBarHeight;
            layoutSo.ApplyModifiedPropertiesWithoutUndo();

            // Портретная раскладка по умолчанию, чтобы сцена выглядела правильно и вне Play.
            ((RectTransform)kiosk.transform).anchorMin = new Vector2(0f, 1f);
            ((RectTransform)kiosk.transform).offsetMin = new Vector2(0f, -(HudHeight + 520f));
            ((RectTransform)kiosk.transform).offsetMax = new Vector2(0f, -HudHeight);
            ((RectTransform)content.transform).offsetMin = new Vector2(0f, TabBarHeight);
            ((RectTransform)content.transform).offsetMax = new Vector2(0f, -(HudHeight + 520f));
        }

        /// <summary>
        /// Режим любования (V7): кнопка с icon_zen в HUD справа от настроек, прозрачная ловушка тапов в зоне ларька
        /// (сразу над сценой города — кот, клиенты и ларёк выше неё и остаются кликабельными), подсказка внизу экрана.
        /// HUD, вкладки, список и круглые кнопки в зоне ларька получают CanvasGroup — в режиме они тают.
        /// </summary>
        private static ZenMode BuildZenMode(Transform screen, ResponsiveLayout layout, Transform kioskArea, Transform cityScene, Transform hud,
            Transform tabBar, Transform content)
        {
            GameObject buttonObject = CreateUi("ZenButton", hud);
            SetRect(buttonObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(128f, -14f), new Vector2(96f, 96f));
            if (_icons.Zen != null)
                AddImage(buttonObject, Color.white, _icons.Zen, sliced: false).preserveAspect = true;
            else
                AddCircle(buttonObject, Cream);
            Button enter = AddButton(buttonObject, SoundId.Tap);

            GameObject catcherObject = CreateUi("ZenExitCatcher", kioskArea);
            catcherObject.transform.SetSiblingIndex(cityScene.GetSiblingIndex() + 1);
            Stretch(catcherObject, Vector2.zero, Vector2.zero);
            Image catcherImage = AddImage(catcherObject, Color.clear, null);
            var catcher = catcherObject.AddComponent<Button>();
            catcher.targetGraphic = catcherImage;
            catcher.transition = Selectable.Transition.None;
            catcherObject.SetActive(false);

            GameObject hintObject = CreateUi("ZenHint", screen);
            SetRect(hintObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(560f, 84f));
            AddImage(hintObject, UiTheme.WithAlpha(_theme.Outline, 0.55f), _pillSprite).raycastTarget = false;
            TMP_Text hintText = AddLocalizedText(hintObject.transform, "Text", "zen.hint", 34, Light, bold: true);
            Stretch(hintText.gameObject, new Vector2(24f, 8f), new Vector2(-24f, -8f));
            hintText.raycastTarget = false;
            var hint = hintObject.AddComponent<CanvasGroup>();
            hint.alpha = 0f;
            hint.blocksRaycasts = false;
            hintObject.SetActive(false);

            var groups = new List<Object> { EnsureGroup(hud), EnsureGroup(tabBar), EnsureGroup(content) };
            foreach (string name in new[] { "CalendarButton", "QuestsButton", "GiftButton" })
            {
                Transform item = kioskArea.Find(name);
                if (item != null)
                    groups.Add(EnsureGroup(item));
            }

            var zen = screen.gameObject.AddComponent<ZenMode>();
            var so = new SerializedObject(zen);
            Set(so, "_layout", layout);
            Set(so, "_enterButton", enter);
            Set(so, "_exitCatcher", catcher);
            Set(so, "_hud", hud);
            Set(so, "_tabBar", tabBar);
            SetArray(so, "_hiddenGroups", groups.ToArray());
            Set(so, "_hint", hint);
            so.ApplyModifiedPropertiesWithoutUndo();
            return zen;
        }

        private static CanvasGroup EnsureGroup(Transform target)
        {
            return target.TryGetComponent(out CanvasGroup group) ? group : target.gameObject.AddComponent<CanvasGroup>();
        }

        /// <summary>Панель вкладки: заголовок высотой HeaderHeight и прокручиваемый вертикальный список.</summary>
        private static GameObject BuildListPanel(Transform parent, string name, string titleKey,
            out RectTransform header, out RectTransform list)
        {
            GameObject panel = CreateUi(name, parent);
            Stretch(panel, Vector2.zero, Vector2.zero);

            GameObject headerObject = CreateUi("Header", panel.transform);
            SetRect(headerObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, HeaderHeight));
            TMP_Text headerTitle = AddLocalizedText(headerObject.transform, "Title", titleKey, 46, DarkBrown, bold: true);
            headerTitle.alignment = TextAlignmentOptions.Left;
            SetRect(headerTitle.gameObject, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(-400f, 0f));
            header = (RectTransform)headerObject.transform;

            GameObject scroll = CreateUi("Scroll", panel.transform);
            Stretch(scroll, Vector2.zero, new Vector2(0f, -HeaderHeight));
            var scrollRect = scroll.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.scrollSensitivity = 40f;

            GameObject viewport = CreateUi("Viewport", scroll.transform);
            Stretch(viewport, Vector2.zero, Vector2.zero);
            AddImage(viewport, Color.clear, null);
            viewport.AddComponent<RectMask2D>();

            GameObject listObject = CreateUi("Content", viewport.transform);
            list = (RectTransform)listObject.transform;
            list.anchorMin = new Vector2(0f, 1f);
            list.anchorMax = new Vector2(1f, 1f);
            list.pivot = new Vector2(0.5f, 1f);
            list.offsetMin = Vector2.zero;
            list.offsetMax = Vector2.zero;
            var vertical = listObject.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(20, 20, 10, 30);
            vertical.spacing = 16f;
            vertical.childControlWidth = true;
            vertical.childControlHeight = true;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;
            listObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = (RectTransform)viewport.transform;
            scrollRect.content = list;
            return panel;
        }

        private static StationsPanel BuildStationsPanel(Transform parent, StationRowView rowPrefab)
        {
            GameObject panel = BuildListPanel(parent, "StationsPanel", "tab.stations", out RectTransform header, out RectTransform list);
            var stationsPanel = panel.AddComponent<StationsPanel>();

            GameObject buyMode = CreateUi("BuyModeButton", header);
            SetRect(buyMode, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(330f, 86f));
            Button buyModeButton = AddSoftButton(buyMode, ButtonStyle.Neutral, SoundId.Tap);
            TMP_Text buyModeLabel = AddButtonLabel(buyMode.transform, "Label", "Купить: ×1", 36);
            Stretch(buyModeLabel.gameObject, new Vector2(12f, 0f), new Vector2(-12f, 0f));
            var buyModeView = buyMode.AddComponent<BuyModeButton>();
            var buyModeSo = new SerializedObject(buyModeView);
            Set(buyModeSo, "_button", buyModeButton);
            Set(buyModeSo, "_label", buyModeLabel);
            buyModeSo.ApplyModifiedPropertiesWithoutUndo();

            var so = new SerializedObject(stationsPanel);
            Set(so, "_content", list);
            Set(so, "_rowPrefab", rowPrefab);
            Set(so, "_buyModeButton", buyModeView);
            so.ApplyModifiedPropertiesWithoutUndo();
            return stationsPanel;
        }

        private static ManagersPanel BuildManagersPanel(Transform parent, ManagerRowView rowPrefab)
        {
            GameObject panel = BuildListPanel(parent, "ManagersPanel", "tab.managers", out _, out RectTransform list);
            var managersPanel = panel.AddComponent<ManagersPanel>();

            var so = new SerializedObject(managersPanel);
            Set(so, "_content", list);
            Set(so, "_rowPrefab", rowPrefab);
            so.ApplyModifiedPropertiesWithoutUndo();

            panel.SetActive(false);
            return managersPanel;
        }

        private static UpgradesPanel BuildUpgradesPanel(Transform parent, UpgradeRowView rowPrefab)
        {
            GameObject panel = BuildListPanel(parent, "UpgradesPanel", "tab.upgrades", out _, out RectTransform list);
            var upgradesPanel = panel.AddComponent<UpgradesPanel>();

            TMP_Text allPurchased = AddLocalizedText(panel.transform, "AllPurchased", "upgrade.none", 44, Muted, bold: true);
            Stretch(allPurchased.gameObject, new Vector2(40f, 0f), new Vector2(-40f, -HeaderHeight));
            allPurchased.gameObject.SetActive(false);

            var so = new SerializedObject(upgradesPanel);
            Set(so, "_content", list);
            Set(so, "_rowPrefab", rowPrefab);
            Set(so, "_allPurchased", allPurchased.gameObject);
            so.ApplyModifiedPropertiesWithoutUndo();

            panel.SetActive(false);
            return upgradesPanel;
        }

        // ---------- Kiosk, street, effects ----------

        private const string SellerIdlePath = "Assets/_Game/Sprites/Customers/maleAdventurer/character_maleAdventurer_idle.png";
        private const string CustomerIdlePath = "Assets/_Game/Sprites/Customers/malePerson/character_malePerson_idle.png";

        private static KioskView BuildKiosk(Transform area, Image sky)
        {
            GameObject pavement = CreateUi("Pavement", area);
            StretchAnchors(pavement, Vector2.zero, new Vector2(1f, GroundRatio));
            AddImage(pavement, Pavement, null).raycastTarget = false;
            GameObject curb = CreateUi("Curb", pavement.transform);
            SetRect(curb, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 10f));
            AddImage(curb, Curb, null).raycastTarget = false;

            GameObject booth = CreateUi("Booth", area);
            SetRect(booth, new Vector2(0.5f, GroundRatio), new Vector2(0.5f, GroundRatio), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(560f, 370f));
            var kioskView = booth.AddComponent<KioskView>();

            GameObject body = CreateUi("Body", booth.transform);
            SetRect(body, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 250f));
            Image bodyImage = AddImage(body, BoothRed, _roundSprite);
            bodyImage.raycastTarget = false;
            var tinted = new List<Object> { bodyImage };

            GameObject window = CreateUi("Window", body.transform);
            SetRect(window, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(400f, 160f));
            AddImage(window, WindowDark, _roundSprite).raycastTarget = false;
            window.AddComponent<RectMask2D>();

            GameObject pole = CreateUi("SpitPole", window.transform);
            SetRect(pole, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(125f, 0f), new Vector2(8f, -16f));
            AddImage(pole, Steel, null).raycastTarget = false;

            GameObject spit = CreateUi("Spit", window.transform);
            SetRect(spit, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(125f, 0f), new Vector2(76f, 120f));
            AddImage(spit, Meat, _roundSprite).raycastTarget = false;
            spit.AddComponent<RectMask2D>();
            var stripes = new RectTransform[5];
            for (int i = 0; i < stripes.Length; i++)
            {
                GameObject stripe = CreateUi("Stripe_" + i, spit.transform);
                SetRect(stripe, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0f, 10f));
                AddImage(stripe, MeatDark, null).raycastTarget = false;
                stripes[i] = (RectTransform)stripe.transform;
            }

            GameObject seller = CreateUi("Seller", window.transform);
            SetRect(seller, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-70f, -70f), new Vector2(165f, 220f));
            Image sellerImage = AddImage(seller, Color.white, AssetDatabase.LoadAssetAtPath<Sprite>(SellerIdlePath), sliced: false);
            sellerImage.preserveAspect = true;
            sellerImage.raycastTarget = false;

            GameObject counter = CreateUi("Counter", body.transform);
            SetRect(counter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(460f, 26f));
            AddImage(counter, Wood, _roundSprite).raycastTarget = false;

            GameObject awning = CreateUi("Awning", booth.transform);
            SetRect(awning, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 245f), new Vector2(620f, 56f));
            var awningLayout = awning.AddComponent<HorizontalLayoutGroup>();
            awningLayout.childControlWidth = true;
            awningLayout.childControlHeight = true;
            awningLayout.childForceExpandWidth = true;
            awningLayout.childForceExpandHeight = true;
            for (int i = 0; i < 8; i++)
            {
                GameObject stripe = CreateUi("AwningStripe_" + i, awning.transform);
                Image stripeImage = AddImage(stripe, i % 2 == 0 ? BoothRed : AwningWhite, null);
                stripeImage.raycastTarget = false;
                if (i % 2 == 0)
                    tinted.Add(stripeImage);
            }

            GameObject sign = CreateUi("Sign", booth.transform);
            SetRect(sign, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 304f), new Vector2(460f, 66f));
            AddImage(sign, Gold, _roundSprite).raycastTarget = false;
            TMP_Text signText = AddLocalizedText(sign.transform, "Text", "game.title", 40, DarkBrown, bold: true);
            Stretch(signText.gameObject, new Vector2(16f, 4f), new Vector2(-16f, -4f));

            // Лампы над вывеской — «звёзды» ларька, по одной за менеджера.
            GameObject lampRow = CreateUi("Lamps", booth.transform);
            SetRect(lampRow, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 378f), new Vector2(230f, 28f));
            var lamps = new Object[KioskDecorCount];
            for (int i = 0; i < lamps.Length; i++)
                lamps[i] = Part(lampRow.transform, "Lamp_" + i, new Vector2(-95f + 38f * i, 0f), new Vector2(28f, 28f), _theme.LampOff, _circleSprite, sliced: false);

            RectTransform[] decor = BuildKioskDecor(area, tinted);

            var so = new SerializedObject(kioskView);
            SetColor(so, "_lampOnColor", Gold);
            SetColor(so, "_lampOffColor", _theme.LampOff);
            SetArray(so, "_managerDecor", decor);
            SetArray(so, "_levelLamps", lamps);
            Set(so, "_seller", sellerImage);
            Set(so, "_spit", spit.transform);
            SetArray(so, "_spitStripes", stripes);
            Set(so, "_window", window.transform);
            Set(so, "_sky", sky);
            SetArray(so, "_boothTinted", tinted.ToArray());
            so.ApplyModifiedPropertiesWithoutUndo();
            return kioskView;
        }

        /// <summary>
        /// Украшения ларька по числу менеджеров (KioskView._managerDecor): гирлянда, меню-доска, столик под зонтом,
        /// рамка вывески, цветы, флаги. Все стоят на земле — растут при появлении снизу вверх.
        /// </summary>
        private static RectTransform[] BuildKioskDecor(Transform area, List<Object> tinted)
        {
            var decor = new RectTransform[KioskDecorCount];

            GameObject garland = DecorRoot(area, "Decor_1_Garland", new Vector2(0f, 214f), new Vector2(640f, 34f));
            Color[] bulbs = { Gold, Green, Reputation, Orange, AwningWhite };
            for (int i = 0; i < 11; i++)
            {
                float x = -300f + 60f * i;
                float sag = 12f * (1f - (x / 300f) * (x / 300f));
                Part(garland.transform, "Bulb_" + i, new Vector2(x, 12f - sag), new Vector2(20f, 20f), bulbs[i % bulbs.Length], _circleSprite, sliced: false);
            }
            decor[0] = (RectTransform)garland.transform;

            GameObject menu = DecorRoot(area, "Decor_2_MenuBoard", new Vector2(-440f, 0f), new Vector2(150f, 220f));
            Part(menu.transform, "LegLeft", new Vector2(-45f, 0f), new Vector2(10f, 80f), Wood, null);
            Part(menu.transform, "LegRight", new Vector2(45f, 0f), new Vector2(10f, 80f), Wood, null);
            Image board = Part(menu.transform, "Board", new Vector2(0f, 70f), new Vector2(150f, 140f), Wood, _roundSprite);
            Image slate = Part(board.transform, "Slate", new Vector2(0f, 8f), new Vector2(134f, 124f), WindowDark, _roundSprite);
            Part(slate.transform, "Title", new Vector2(0f, 94f), new Vector2(100f, 18f), Gold, _roundSprite);
            for (int i = 0; i < 3; i++)
            {
                Part(slate.transform, "Line_" + i, new Vector2(-12f, 64f - 24f * i), new Vector2(76f, 8f), AwningWhite, null);
                Part(slate.transform, "Price_" + i, new Vector2(46f, 61f - 24f * i), new Vector2(14f, 14f), Gold, _circleSprite, sliced: false);
            }
            decor[1] = (RectTransform)menu.transform;

            GameObject table = DecorRoot(area, "Decor_3_Table", new Vector2(435f, 0f), new Vector2(200f, 250f));
            Part(table.transform, "Pole", Vector2.zero, new Vector2(8f, 206f), Steel, null);
            Image canopy = Part(table.transform, "Canopy", new Vector2(0f, 196f), new Vector2(200f, 48f), Orange, _roundSprite);
            Part(canopy.transform, "Stripe", Vector2.zero, new Vector2(50f, 48f), AwningWhite, null);
            Part(table.transform, "Top", new Vector2(0f, 74f), new Vector2(150f, 14f), Wood, _roundSprite);
            Part(table.transform, "Cup", new Vector2(34f, 88f), new Vector2(18f, 24f), AwningWhite, _roundSprite);
            for (int side = -1; side <= 1; side += 2)
            {
                Part(table.transform, side < 0 ? "StoolLegLeft" : "StoolLegRight", new Vector2(70f * side, 0f), new Vector2(8f, 44f), Wood, null);
                Part(table.transform, side < 0 ? "StoolLeft" : "StoolRight", new Vector2(70f * side, 40f), new Vector2(46f, 12f), Brown, _roundSprite);
            }
            decor[2] = (RectTransform)table.transform;

            GameObject frame = DecorRoot(area, "Decor_4_SignFrame", new Vector2(0f, 297f), new Vector2(484f, 80f));
            Part(frame.transform, "Bottom", Vector2.zero, new Vector2(484f, 7f), Orange, null);
            Part(frame.transform, "Top", new Vector2(0f, 73f), new Vector2(484f, 7f), Orange, null);
            Part(frame.transform, "Left", new Vector2(-238.5f, 0f), new Vector2(7f, 80f), Orange, null);
            Part(frame.transform, "Right", new Vector2(238.5f, 0f), new Vector2(7f, 80f), Orange, null);
            for (int i = 0; i < 4; i++)
                Part(frame.transform, "Bulb_" + i, new Vector2(i % 2 == 0 ? -242f : 242f, i < 2 ? -6f : 66f), new Vector2(18f, 18f), Gold, _circleSprite, sliced: false);
            decor[3] = (RectTransform)frame.transform;

            // Слева — куст между ларьком и меню, справа — горшок поменьше: дальше стоит столик.
            GameObject plants = DecorRoot(area, "Decor_5_Plants", Vector2.zero, new Vector2(740f, 150f));
            BuildPlant(plants.transform, "Left", -318f, 100f, 64f);
            BuildPlant(plants.transform, "Right", 300f, 72f, 50f);
            decor[4] = (RectTransform)plants.transform;

            decor[5] = BuildFlagsDecor(area, tinted, new Vector2(0f, 292f), 262f);

            foreach (RectTransform item in decor)
                item.gameObject.SetActive(false);
            return decor;
        }

        // Два флажка на шестах по бокам ларька (цвет ларька). halfSpan — расстояние шеста от центра.
        private static RectTransform BuildFlagsDecor(Transform area, List<Object> tinted, Vector2 position, float halfSpan)
        {
            GameObject flags = DecorRoot(area, "Decor_6_Flags", position, new Vector2(halfSpan * 2f + 116f, 120f));
            for (int side = -1; side <= 1; side += 2)
            {
                string suffix = side < 0 ? "Left" : "Right";
                Part(flags.transform, "Pole" + suffix, new Vector2(halfSpan * side, 0f), new Vector2(6f, 112f), Steel, null);
                tinted.Add(Part(flags.transform, "Flag" + suffix, new Vector2((halfSpan + 30f) * side, 72f), new Vector2(56f, 36f), BoothRed, null));
                Part(flags.transform, "Tip" + suffix, new Vector2(halfSpan * side, 106f), new Vector2(14f, 14f), Gold, _circleSprite, sliced: false);
            }
            return (RectTransform)flags.transform;
        }

        // ---------- City scene and drawn kiosk (V3) ----------

        // Нарисованный ларёк: ширина корпуса; всё остальное — от неё (ларёк + навес + вывеска ≈ 1.7 ширины в высоту,
        // помещается в портретную зону 520 над тротуаром). Ларёк стоит чуть ниже линии тротуара — «на» нём.
        private const float KioskBodyWidth = 250f;
        private const float KioskBaseY = -36f;
        // Общая линия земли от бордюра: на ней стоят ларёк, клиенты, кот, столик, меню-доска, цветы и столбики флажков.
        private const float GroundY = KioskBaseY;
        // Шарики-навершия на концах decor_flags.png: доля ширины от центра и доля высоты от низа (замерено по альфе картинки).
        private const float FlagsKnobX = 0.46f;
        private const float FlagsKnobY = 0.90f;
        // Скрутка навеса в kiosk_awning.png — точка, к которой цепляется гирлянда: доля ширины от центра и доля высоты от низа.
        private const float AwningRollX = 0.38f;
        private const float AwningRollY = 0.86f;
        // Проём рамки decor_sign_lights.png (доли картинки) и видимая доска kiosk_sign.png (без прозрачных полей).
        private static readonly Rect SignFrameHole = new Rect(59f / 512f, 64f / 396f, 393f / 512f, 268f / 396f);
        private static readonly Vector2 SignBoardVisible = new Vector2(481f / 512f, 256f / 300f);
        // Рамка лежит поверх вывески: по высоте её проём заходит на доску на 4 px, по бокам — на 14 px
        // (иначе пришлось бы растягивать лампочки в овалы: пропорции рамки 1.29, вывески 1.71).
        private const float SignFrameOverlapY = 4f;
        private const float SignFrameOverlapX = 14f;
        // Окно в kiosk_body.png (доли картинки): по нему ставятся продавец, вертел и прилавок.
        private static readonly Rect KioskWindow = new Rect(0.195f, 0.443f, 0.618f, 0.318f);

        /// <summary>
        /// Сцена за ларьком: градиент неба, луна со свечением, дальний и ближний слои города на линии тротуара
        /// (по ширине зоны + запас на покачивание; высоту держит AspectRatioFitter). Спрайты и цвета ставит
        /// CitySceneView.Apply по CityConfig; в редакторе сцена показывает город 1.
        /// </summary>
        private static CitySceneView BuildCityScene(Transform area)
        {
            GameObject root = CreateUi("CityScene", area);
            Stretch(root, Vector2.zero, Vector2.zero);
            var view = root.AddComponent<CitySceneView>();

            GameObject skyObject = CreateUi("SkyGradient", root.transform);
            Stretch(skyObject, Vector2.zero, Vector2.zero);
            // CanvasRenderer — явно: RequireComponent базового Graphic на наследнике срабатывает не всегда.
            skyObject.AddComponent<CanvasRenderer>();
            var gradient = skyObject.AddComponent<GradientImage>();
            gradient.raycastTarget = false;

            // Луна — правее центра, под подарком в углу не прячется.
            GameObject moon = CreateUi("Moon", root.transform);
            SetRect(moon, new Vector2(0.72f, 1f), new Vector2(0.72f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -78f), new Vector2(96f, 96f));
            Image moonGlow = AddArtAt(moon.transform, "Glow", ProceduralSprites.Load(ProceduralSprites.GlowSoft), new Vector2(0.5f, 0.5f), Vector2.zero, 240f);
            moonGlow.color = UiTheme.WithAlpha(_theme.Glow, 0.55f);
            AddArt(moon.transform, "Moon", ArtAssigner.LoadSceneSprite("Common/moon"));
            moon.SetActive(false);

            GameObject layers = CreateUi("Layers", root.transform);
            Stretch(layers, Vector2.zero, Vector2.zero);
            var fade = layers.AddComponent<CanvasGroup>();
            fade.blocksRaycasts = false;
            fade.interactable = false;
            Image far = BuildSceneLayer(layers.transform, "BgFar", 8f);
            Image near = BuildSceneLayer(layers.transform, "BgNear", 14f);

            var so = new SerializedObject(view);
            Set(so, "_sky", gradient);
            Set(so, "_far", far);
            Set(so, "_near", near);
            Set(so, "_moon", moon);
            Set(so, "_moonGlow", moonGlow);
            Set(so, "_fadeGroup", fade);
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        // Слой города: по ширине зоны (+ запас на покачивание), низ — на линии тротуара, высоту держит пропорция картинки.
        private static Image BuildSceneLayer(Transform parent, string name, float margin)
        {
            GameObject go = CreateUi(name, parent);
            SetRect(go, new Vector2(0f, GroundRatio), new Vector2(1f, GroundRatio), new Vector2(0.5f, 0f), new Vector2(0f, -4f),
                new Vector2(margin * 2f, 300f));
            Image image = AddImage(go, Color.white, null, sliced: false);
            image.raycastTarget = false;
            image.enabled = false;
            var fitter = go.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            fitter.aspectRatio = 3f;
            return image;
        }

        /// <summary>
        /// Ларёк из картинок пакета D: корпус (цвет города) → вертел и продавец в окне → прилавок → навес (цвет города)
        /// → вывеска «ШАУРМА» → лампы-лампочки под навесом. Те же поля KioskView, что у процедурного ларька.
        /// </summary>
        private static KioskView BuildKioskArt(Transform area, Image sky)
        {
            GameObject pavement = CreateUi("Pavement", area);
            StretchAnchors(pavement, Vector2.zero, new Vector2(1f, GroundRatio));
            AddImage(pavement, Pavement, null).raycastTarget = false;
            GameObject curb = CreateUi("Curb", pavement.transform);
            SetRect(curb, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 10f));
            AddImage(curb, Curb, null).raycastTarget = false;

            // Тёплый свет от ларька на тротуаре.
            GameObject light = CreateUi("KioskLight", area);
            SetRect(light, new Vector2(0.5f, GroundRatio), new Vector2(0.5f, GroundRatio), new Vector2(0.5f, 0.5f), new Vector2(0f, -36f), new Vector2(560f, 110f));
            AddImage(light, UiTheme.WithAlpha(_theme.Glow, 0.35f), ProceduralSprites.Load(ProceduralSprites.GlowSoft), sliced: false).raycastTarget = false;

            float b = KioskBodyWidth;
            GameObject booth = CreateUi("Booth", area);
            SetRect(booth, new Vector2(0.5f, GroundRatio), new Vector2(0.5f, GroundRatio), new Vector2(0.5f, 0f), new Vector2(0f, KioskBaseY),
                new Vector2(b * 1.3f, b * 1.75f));
            var kioskView = booth.AddComponent<KioskView>();
            var tinted = new List<Object>();

            Sprite bodySprite = ArtAssigner.LoadKioskSprite("kiosk_body");
            float bodyHeight = b * bodySprite.rect.height / bodySprite.rect.width;
            tinted.Add(Part(booth.transform, "Body", Vector2.zero, new Vector2(b, bodyHeight), BoothRed, bodySprite, sliced: false));

            // Окно: вертел и продавец обрезаются по нему; позиция окна — цель «монеты из ларька».
            GameObject window = CreateUi("Window", booth.transform);
            SetRect(window, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2((KioskWindow.center.x - 0.5f) * b, KioskWindow.y * bodyHeight), new Vector2(KioskWindow.width * b, KioskWindow.height * bodyHeight));
            window.AddComponent<RectMask2D>();
            float windowWidth = KioskWindow.width * b;

            Sprite spitSprite = ArtAssigner.LoadKioskSprite("kiosk_spit");
            Image spit = Part(window.transform, "Spit", new Vector2(windowWidth * 0.3f, 4f), new Vector2(34f, 78f), Color.white, spitSprite, sliced: false);
            spit.preserveAspect = true;
            if (spitSprite == null)
                spit.color = Meat;

            GameObject seller = CreateUi("Seller", window.transform);
            SetRect(seller, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-windowWidth * 0.1f, KioskWindow.height * 252f - 136f), new Vector2(92f, 132f));
            Image sellerImage = AddImage(seller, Color.white, AssetDatabase.LoadAssetAtPath<Sprite>(SellerIdlePath), sliced: false);
            sellerImage.preserveAspect = true;
            sellerImage.raycastTarget = false;

            KioskArtPart(booth.transform, "Counter", "kiosk_counter", KioskWindow.y * bodyHeight - b * 0.12f, b * 0.82f, null);
            Image awning = KioskArtPart(booth.transform, "Awning", "kiosk_awning", bodyHeight * 0.8f, b * 1.26f, tinted);
            float signBottom = bodyHeight * 0.8f + (awning != null ? awning.rectTransform.sizeDelta.y : b * 0.5f) - b * 0.12f;
            Image sign = KioskArtPart(booth.transform, "Sign", "kiosk_sign", signBottom, b * 0.84f, null);
            if (sign != null)
            {
                TMP_Text signText = AddLocalizedText(sign.transform, "Text", "kiosk.sign", 40, DarkBrown, bold: true);
                Stretch(signText.gameObject, new Vector2(30f, 22f), new Vector2(-30f, -22f));
            }

            // Лампочки под краем навеса — по одной за менеджера; у горящих мягкий ореол.
            GameObject lampRow = CreateUi("Lamps", booth.transform);
            SetRect(lampRow, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, bodyHeight * 0.8f - 14f), new Vector2(b, 24f));
            var lamps = new Object[KioskDecorCount];
            var glows = new Object[KioskDecorCount];
            Sprite glowSprite = ProceduralSprites.Load(ProceduralSprites.GlowSoft);
            float step = b * 0.8f / (KioskDecorCount - 1);
            for (int i = 0; i < KioskDecorCount; i++)
            {
                float x = -b * 0.4f + step * i;
                Image glow = Part(lampRow.transform, "Glow_" + i, new Vector2(x, -18f), new Vector2(60f, 60f), _theme.Glow, glowSprite, sliced: false);
                glow.enabled = false;
                glows[i] = glow;
                lamps[i] = Part(lampRow.transform, "Lamp_" + i, new Vector2(x, 0f), new Vector2(20f, 20f), _theme.LampOff, _circleSprite, sliced: false);
            }

            float signCenter = sign != null ? signBottom + sign.rectTransform.sizeDelta.y * 0.5f : signBottom;
            Vector2 signSize = sign != null ? sign.rectTransform.sizeDelta : new Vector2(b * 0.84f, b * 0.5f);
            Vector2 awningRoll = awning != null
                ? new Vector2(awning.rectTransform.sizeDelta.x * AwningRollX, bodyHeight * 0.8f + awning.rectTransform.sizeDelta.y * AwningRollY)
                : new Vector2(b * 0.5f, bodyHeight * 0.8f + b * 0.4f);
            RectTransform[] decor = BuildKioskDecorArt(area, tinted, signCenter, signSize, awningRoll);

            var so = new SerializedObject(kioskView);
            SetColor(so, "_lampOnColor", Gold);
            SetColor(so, "_lampOffColor", _theme.LampOff);
            SetArray(so, "_managerDecor", decor);
            SetArray(so, "_levelLamps", lamps);
            SetArray(so, "_lampGlows", glows);
            Set(so, "_seller", sellerImage);
            Set(so, "_spit", spit.transform);
            SetArray(so, "_spitStripes");
            Set(so, "_window", window.transform);
            Set(so, "_sky", sky);
            SetArray(so, "_boothTinted", tinted.ToArray());
            so.ApplyModifiedPropertiesWithoutUndo();
            return kioskView;
        }

        // Часть ларька по ширине width (высота — по пропорции картинки), низ на высоте bottom от основания ларька.
        private static Image KioskArtPart(Transform booth, string name, string file, float bottom, float width, List<Object> tinted)
        {
            Sprite sprite = ArtAssigner.LoadKioskSprite(file);
            if (sprite == null)
                return null;
            float height = width * sprite.rect.height / sprite.rect.width;
            Image image = Part(booth, name, new Vector2(0f, bottom), new Vector2(width, height), tinted != null ? BoothRed : Color.white, sprite, sliced: false);
            tinted?.Add(image);
            return image;
        }

        /// <summary>
        /// Украшения нарисованного ларька в порядке KioskView._managerDecor: гирлянда, меню-доска, столик под зонтом,
        /// рамка вывески, цветы, флаги. Нет картинки — пустое место (флаги — прежние, из примитивов).
        /// </summary>
        /// <param name="signCenter">Центр вывески по высоте от основания ларька — рамка из лампочек ставится вокруг неё.</param>
        /// <param name="signSize">Размер картинки вывески: проём рамки подгоняется под видимую доску.</param>
        /// <param name="awningRoll">Скрутка навеса (x — от центра, y — от основания ларька): сюда цепляются флажки.</param>
        private static RectTransform[] BuildKioskDecorArt(Transform area, List<Object> tinted, float signCenter, Vector2 signSize,
            Vector2 awningRoll)
        {
            float b = KioskBodyWidth;
            var decor = new RectTransform[KioskDecorCount];
            decor[0] = DecorArt(area, "Decor_1_Garland", "decor_garland", new Vector2(0f, KioskBaseY + b * 0.8f), b * 1.08f);
            decor[1] = DecorArt(area, "Decor_2_MenuBoard", "decor_menu_board", new Vector2(-b * 1.08f, GroundY), b * 0.46f);
            decor[2] = DecorArt(area, "Decor_3_Table", "decor_table_umbrella", new Vector2(b * 1.02f, GroundY), b * 0.62f);
            decor[3] = BuildSignFrame(area, KioskBaseY + signCenter, signSize);
            decor[4] = DecorArt(area, "Decor_5_Flowers", "decor_flowers", new Vector2(-b * 0.6f, GroundY), b * 0.4f);
            Sprite flagsSprite = ArtAssigner.LoadKioskSprite("decor_flags");
            decor[5] = flagsSprite != null
                ? BuildFlagsArt(area, tinted, flagsSprite, awningRoll)
                : BuildFlagsDecor(area, tinted, new Vector2(0f, KioskBaseY + b * 0.55f), b * 0.75f);

            foreach (RectTransform item in decor)
                item.gameObject.SetActive(false);
            return decor;
        }

        /// <summary>
        /// Две гирлянды флажков по бокам ларька — от навеса к меню-доске и к столику (не перекрывают вывеску).
        /// Флажки белые/серые — тонируются цветом города.
        /// </summary>
        private static RectTransform BuildFlagsArt(Transform area, List<Object> tinted, Sprite sprite, Vector2 awningRoll)
        {
            float b = KioskBodyWidth;
            float width = b * 0.88f;
            float height = width * sprite.rect.height / sprite.rect.width;
            // Внутренний шарик гирлянды — ровно на скрутке навеса.
            float bottom = KioskBaseY + awningRoll.y - height * FlagsKnobY;
            GameObject root = DecorRoot(area, "Decor_6_Flags", new Vector2(0f, bottom), new Vector2(b * 3.2f, height));
            for (int side = -1; side <= 1; side += 2)
            {
                // Внутренний конец гирлянды — на углу навеса, внешний (шарик-навершие на картинке) — на деревянном столбике.
                float center = side * (awningRoll.x + width * FlagsKnobX);
                float poleX = center + side * width * FlagsKnobX;
                float poleTop = height * FlagsKnobY;
                float poleBottom = GroundY - bottom;
                Part(root.transform, side < 0 ? "PoleLeft" : "PoleRight", new Vector2(poleX, poleBottom),
                    new Vector2(10f, poleTop - poleBottom), Wood, null);
                Part(root.transform, side < 0 ? "PoleFootLeft" : "PoleFootRight", new Vector2(poleX, poleBottom - 5f),
                    new Vector2(34f, 12f), _theme.Outline, _circleSprite, sliced: false);
                Image flags = Part(root.transform, side < 0 ? "FlagsLeft" : "FlagsRight", new Vector2(center, 0f),
                    new Vector2(width, height), BoothRed, sprite, sliced: false);
                tinted.Add(flags);
            }
            return (RectTransform)root.transform;
        }

        /// <summary>
        /// Рамка из лампочек вокруг вывески: проём рамки по размеру видимой доски (с небольшим заходом на неё),
        /// поэтому между рамкой и доской нет щелей. Центр проёма совпадает с центром рамки.
        /// </summary>
        private static RectTransform BuildSignFrame(Transform area, float centerY, Vector2 signSize)
        {
            Sprite sprite = ArtAssigner.LoadKioskSprite("decor_sign_lights");
            Vector2 board = Vector2.Scale(signSize, SignBoardVisible);
            float holeWidth = board.x - SignFrameOverlapX * 2f;
            float holeHeight = board.y - SignFrameOverlapY * 2f;
            var size = new Vector2(holeWidth / SignFrameHole.width, holeHeight / SignFrameHole.height);
            GameObject root = DecorRoot(area, "Decor_4_SignFrame", new Vector2(0f, centerY - size.y * 0.5f), size);
            // Пропорции рамки подогнаны под вывеску — картинку растягиваем, а не вписываем.
            if (sprite != null)
                AddArt(root.transform, "Art", sprite).preserveAspect = false;
            return (RectTransform)root.transform;
        }

        private static RectTransform DecorArt(Transform area, string name, string file, Vector2 position, float width)
        {
            Sprite sprite = ArtAssigner.LoadKioskSprite(file);
            float height = sprite != null ? width * sprite.rect.height / sprite.rect.width : 10f;
            GameObject root = DecorRoot(area, name, position, new Vector2(width, height));
            if (sprite != null)
                AddArt(root.transform, "Art", sprite);
            return (RectTransform)root.transform;
        }

        private static void BuildPlant(Transform parent, string suffix, float x, float leaves, float pot)
        {
            float side = Mathf.Sign(x);
            Part(parent, "Leaves" + suffix, new Vector2(x, pot * 0.7f), new Vector2(leaves, leaves), Leaf, _circleSprite, sliced: false);
            Part(parent, "LeavesTop" + suffix, new Vector2(x - 16f * side, pot * 0.7f + leaves * 0.6f), new Vector2(leaves * 0.56f, leaves * 0.56f),
                LeafDark, _circleSprite, sliced: false);
            Part(parent, "Pot" + suffix, new Vector2(x, 0f), new Vector2(pot, pot * 0.85f), Brown, _roundSprite);
        }

        // Корень украшения: стоит на земле (якорь — линия земли по центру), pivot снизу.
        private static GameObject DecorRoot(Transform area, string name, Vector2 position, Vector2 size)
        {
            GameObject root = CreateUi(name, area);
            SetRect(root, new Vector2(0.5f, GroundRatio), new Vector2(0.5f, GroundRatio), new Vector2(0.5f, 0f), position, size);
            return root;
        }

        // Деталь рисунка: позиция — от середины нижнего края родителя до середины нижнего края детали.
        private static Image Part(Transform parent, string name, Vector2 position, Vector2 size, Color color, Sprite sprite, bool sliced = true)
        {
            GameObject go = CreateUi(name, parent);
            SetRect(go, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), position, size);
            Image image = AddImage(go, color, sprite, sliced);
            image.raycastTarget = false;
            return image;
        }

        private static CustomerQueue BuildStreet(Transform area, KioskView kiosk, CatView cat)
        {
            // Улица опущена на линию земли: клиенты стоят на тротуаре вровень с ларьком, а не на бордюре.
            GameObject street = CreateUi("Street", area);
            Stretch(street, new Vector2(0f, GroundY), new Vector2(0f, GroundY));
            var queue = street.AddComponent<CustomerQueue>();

            // Персонажи V4 (без кадров ходьбы): высота — CharacterHeight, ширина — по пропорции холста спрайта.
            // Иначе прежние Kenney 165×220. Тело стоит на ногах (pivot снизу): подскок и «дыхание» — от земли.
            CustomerSkin firstSkin = _customerConfig != null && _customerConfig.CustomerSkins.Count > 0 ? _customerConfig.CustomerSkins[0] : null;
            bool procedural = firstSkin != null && firstSkin.Idle != null && firstSkin.Walk.Length == 0;
            Sprite previewSprite = procedural ? firstSkin.Idle : AssetDatabase.LoadAssetAtPath<Sprite>(CustomerIdlePath);
            Vector2 size = new Vector2(165f, 220f);
            if (procedural)
            {
                float height = _customerConfig.CharacterHeight;
                size = new Vector2(height * previewSprite.rect.width / previewSprite.rect.height, height);
            }
            Sprite shadowSprite = ProceduralSprites.Load(ProceduralSprites.GlowSoft);
            Sprite umbrellaSprite = ArtAssigner.LoadAmbientSprite("umbrella");

            var pool = new CustomerView[CustomerPoolSize];
            for (int i = 0; i < pool.Length; i++)
            {
                GameObject customer = CreateUi("Customer_" + i, street.transform);
                SetRect(customer, new Vector2(0.5f, GroundRatio), new Vector2(0.5f, GroundRatio), new Vector2(0.5f, 0f), Vector2.zero, size);

                GameObject shadow = CreateUi("Shadow", customer.transform);
                SetRect(shadow, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f),
                    new Vector2(Mathf.Max(90f, size.x * 1.05f), 34f));
                AddImage(shadow, UiTheme.WithAlpha(_theme.Outline, 0.25f), shadowSprite, sliced: false).raycastTarget = false;

                // Зонтик (V5) — в руке, как на прогулке: ручка у кисти чуть впереди, купол завален назад за плечо.
                // Он позади тела (голова и рука перед ним), а «держатель» повторяет подскок и наклон тела (CustomerView).
                Image umbrella = null;
                GameObject umbrellaRig = null;
                if (umbrellaSprite != null)
                {
                    umbrellaRig = CreateUi("UmbrellaRig", customer.transform);
                    SetRect(umbrellaRig, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, size);
                    GameObject umbrellaObject = CreateUi("Umbrella", umbrellaRig.transform);
                    float umbrellaHeight = size.y * UmbrellaHeight;
                    SetRect(umbrellaObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), UmbrellaGrip,
                        new Vector2(-size.x * UmbrellaHand.x, size.y * UmbrellaHand.y),
                        new Vector2(umbrellaHeight * umbrellaSprite.rect.width / umbrellaSprite.rect.height, umbrellaHeight));
                    umbrellaObject.transform.localRotation = Quaternion.Euler(0f, 0f, -UmbrellaTilt);
                    umbrella = AddImage(umbrellaObject, Color.white, umbrellaSprite, sliced: false);
                    umbrella.raycastTarget = false;
                    umbrellaObject.SetActive(false);
                }

                GameObject body = CreateUi("Body", customer.transform);
                SetRect(body, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, size);
                body.transform.localScale = new Vector3(procedural ? 1f : -1f, 1f, 1f);
                Image bodyImage = AddImage(body, Color.white, previewSprite, sliced: false);
                bodyImage.preserveAspect = true;

                var button = customer.AddComponent<Button>();
                button.targetGraphic = bodyImage;
                button.transition = Selectable.Transition.None;

                var view = customer.AddComponent<CustomerView>();
                var viewSo = new SerializedObject(view);
                Set(viewSo, "_body", bodyImage);
                Set(viewSo, "_button", button);
                Set(viewSo, "_shadow", shadow.transform);
                Set(viewSo, "_umbrella", umbrella);
                Set(viewSo, "_umbrellaRig", umbrellaRig != null ? umbrellaRig.transform : null);
                viewSo.ApplyModifiedPropertiesWithoutUndo();

                customer.SetActive(false);
                pool[i] = view;
            }

            // Сердечки над довольными клиентами: координаты как у клиентов (x от окна, y от тротуара).
            var hearts = new Object[StreetHeartPoolSize];
            for (int i = 0; i < hearts.Length; i++)
            {
                GameObject heart = CreateUi("Heart_" + i, street.transform);
                SetRect(heart, new Vector2(0.5f, GroundRatio), new Vector2(0.5f, GroundRatio), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52f, 52f));
                hearts[i] = AddHeart(heart);
                heart.SetActive(false);
            }

            GameObject bubble = CreateUi("Bubble", street.transform);
            SetRect(bubble, new Vector2(0.5f, GroundRatio), new Vector2(0.5f, GroundRatio), new Vector2(0.15f, 0f), new Vector2(0f, 250f), new Vector2(400f, 110f));
            AddCard(bubble, Paper).raycastTarget = false;
            TMP_Text bubbleText = AddText(bubble.transform, "Text", "", 32, DarkBrown, TextAlignmentOptions.Center, bold: true, autoSize: true);
            Stretch(bubbleText.gameObject, new Vector2(18f, 10f), new Vector2(-18f, -10f));
            bubble.SetActive(false);

            var so = new SerializedObject(queue);
            Set(so, "_street", street.transform);
            SetArray(so, "_pool", pool);
            Set(so, "_bubble", bubble.transform);
            Set(so, "_bubbleText", bubbleText);
            Set(so, "_kiosk", kiosk);
            SetArray(so, "_hearts", hearts);
            Set(so, "_cat", cat);
            so.ApplyModifiedPropertiesWithoutUndo();
            return queue;
        }

        private static Image AddHeart(GameObject go)
        {
            Image image = AddImage(go, Badge, ProceduralSprites.Load(ProceduralSprites.Heart), sliced: false);
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>
        /// Кот Лаваш на коробке с пледом слева от ларька (V4): не мешает очереди (она справа от окна) и руке туториала.
        /// Нет Configs/Cat.asset или поз сна/сидения (пакет E не положен) — кота нет.
        /// </summary>
        private static CatView BuildCat(Transform area)
        {
            CatConfig config = ArtAssigner.LoadCatConfig();
            if (config == null || !config.HasPoses)
                return null;

            float bedWidth = CatBedWidth;
            float bedHeight = config.Bed != null ? bedWidth * config.Bed.rect.height / config.Bed.rect.width : 0f;
            // Кот лежит на пледе: низ кота чуть ниже переднего края коробки (≈0.6 её высоты).
            float catBottom = bedHeight * 0.42f;
            Vector2 catSize = new Vector2(CatWidth, CatWidth * config.Sit.rect.height / config.Sit.rect.width);

            GameObject root = CreateUi("Cat", area);
            SetRect(root, new Vector2(0.5f, GroundRatio), new Vector2(0.5f, GroundRatio), new Vector2(0.5f, 0f), new Vector2(CatX, GroundY),
                new Vector2(bedWidth, catBottom + catSize.y));

            GameObject shadow = CreateUi("Shadow", root.transform);
            SetRect(shadow, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(bedWidth * 1.15f, 40f));
            AddImage(shadow, UiTheme.WithAlpha(_theme.Outline, 0.25f), ProceduralSprites.Load(ProceduralSprites.GlowSoft), sliced: false)
                .raycastTarget = false;

            // Тап по коробке — тоже погладить.
            if (config.Bed != null)
            {
                GameObject bed = CreateUi("Bed", root.transform);
                SetRect(bed, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(bedWidth, bedHeight));
                AddImage(bed, Color.white, config.Bed, sliced: false).preserveAspect = true;
            }

            GameObject body = CreateUi("Body", root.transform);
            SetRect(body, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, catBottom), catSize);
            Image bodyImage = AddImage(body, Color.white, config.Sleep, sliced: false);
            bodyImage.preserveAspect = true;

            // Эффекты от низа кота: «z» и сердечки не качаются вместе с телом.
            GameObject fx = CreateUi("Fx", root.transform);
            SetRect(fx, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, catBottom), Vector2.zero);

            TMP_Text snore = AddText(fx.transform, "Snore", "z", 44, Light, TextAlignmentOptions.Center, bold: true);
            Outlined(snore);
            SetRect(snore.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60f, 60f));
            snore.raycastTarget = false;
            snore.gameObject.SetActive(false);

            var hearts = new Object[CatHeartPoolSize];
            for (int i = 0; i < hearts.Length; i++)
            {
                GameObject heart = CreateUi("Heart_" + i, fx.transform);
                SetRect(heart, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46f, 46f));
                hearts[i] = AddHeart(heart);
                heart.SetActive(false);
            }

            var button = root.AddComponent<Button>();
            button.targetGraphic = bodyImage;
            button.transition = Selectable.Transition.None;

            var view = root.AddComponent<CatView>();
            var so = new SerializedObject(view);
            Set(so, "_config", config);
            Set(so, "_body", bodyImage);
            Set(so, "_button", button);
            Set(so, "_snore", snore);
            SetArray(so, "_hearts", hearts);
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        // ---------- Living world (V5) ----------

        /// <summary>
        /// Геометрия ларька от середины линии тротуара: точка над вертелом (пар), верх навеса и его ширина.
        /// art — ларёк нарисованный (пакет D); у процедурного навеса картинки нет.
        /// </summary>
        private static (Vector2 steam, float awningTop, float awningWidth, bool art) KioskGeometry()
        {
            float b = KioskBodyWidth;
            Sprite body = ArtAssigner.LoadKioskSprite("kiosk_body");
            if (body == null)
                return (new Vector2(125f, 200f), 370f, 560f, false);

            float bodyHeight = b * body.rect.height / body.rect.width;
            Sprite awning = ArtAssigner.LoadKioskSprite("kiosk_awning");
            float awningWidth = b * 1.26f;
            float awningHeight = awning != null ? awningWidth * awning.rect.height / awning.rect.width : b * 0.5f;
            var steam = new Vector2((KioskWindow.center.x - 0.5f) * b + KioskWindow.width * b * 0.3f, KioskBaseY + KioskWindow.y * bodyHeight + 70f);
            return (steam, KioskBaseY + bodyHeight * 0.8f + awningHeight, awningWidth, true);
        }

        /// <summary>
        /// Живой мир: звёзды за луной; облака и далёкие птицы между луной и слоями города; транспорт между ближним слоем
        /// и тротуаром (в конце CityScene); светлячки, пар и воробей — поверх ларька и клиентов. Пулы — по AmbientConfig.
        /// </summary>
        private static AmbientWorldView BuildAmbient(Transform area, Transform scene, AmbientConfig config)
        {
            var geometry = KioskGeometry();
            Sprite glow = ProceduralSprites.Load(ProceduralSprites.GlowSoft);

            GameObject starLayer = CreateUi("Stars", scene);
            Stretch(starLayer, Vector2.zero, Vector2.zero);
            starLayer.transform.SetSiblingIndex(1);
            var stars = new Object[config.StarCount];
            for (int i = 0; i < stars.Length; i++)
                stars[i] = AmbientImage(starLayer.transform, "Star_" + i, new Vector2(0.5f, 0.8f), new Vector2(0.5f, 0.5f), new Vector2(4f, 4f), Light, _circleSprite);

            Transform layers = scene.Find("Layers");
            GameObject cloudLayer = CreateUi("Clouds", scene);
            Stretch(cloudLayer, Vector2.zero, Vector2.zero);
            cloudLayer.transform.SetSiblingIndex(layers.GetSiblingIndex());
            var cloudSprites = new List<Object>();
            foreach (string name in new[] { "cloud_01", "cloud_02", "cloud_03" })
            {
                Sprite sprite = ArtAssigner.LoadSceneSprite("Common/" + name);
                if (sprite != null)
                    cloudSprites.Add(sprite);
            }
            var clouds = new Object[config.CloudCount];
            for (int i = 0; i < clouds.Length; i++)
            {
                clouds[i] = AmbientImage(cloudLayer.transform, "Cloud_" + i, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                    new Vector2(config.CloudWidth, config.CloudWidth * 0.5f), Color.white, cloudSprites.Count > 0 ? (Sprite)cloudSprites[0] : null);
            }

            GameObject birdLayer = CreateUi("FarBirds", scene);
            Stretch(birdLayer, Vector2.zero, Vector2.zero);
            birdLayer.transform.SetSiblingIndex(layers.GetSiblingIndex());
            var birds = new Object[] { BuildFarBird(birdLayer.transform, 0), BuildFarBird(birdLayer.transform, 1) };

            GameObject road = CreateUi("Road", scene);
            Stretch(road, Vector2.zero, Vector2.zero);
            Image vehicle = AmbientImage(road.transform, "Vehicle", new Vector2(0.5f, GroundRatio), new Vector2(0.5f, 0f), new Vector2(240f, 120f),
                Color.white, ArtAssigner.LoadAmbientSprite("car_01"));
            // Свет фар — впереди по ходу (транспорт едет влево).
            GameObject headlight = CreateUi("Headlight", vehicle.transform);
            SetRect(headlight, new Vector2(0f, 0.28f), new Vector2(0f, 0.28f), new Vector2(0.5f, 0.5f), new Vector2(-30f, 0f), new Vector2(200f, 90f));
            AddImage(headlight, UiTheme.WithAlpha(_theme.Glow, config.HeadlightAlpha), glow, sliced: false).raycastTarget = false;

            GameObject front = CreateUi("AmbientFront", area);
            Stretch(front, Vector2.zero, Vector2.zero);
            var fireflies = new Object[Mathf.Max(config.FireflyCount, config.FireflyCountBoosted)];
            for (int i = 0; i < fireflies.Length; i++)
                fireflies[i] = AmbientImage(front.transform, "Firefly_" + i, new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.5f), new Vector2(12f, 12f), _theme.Glow, glow);
            var steam = new Object[config.SteamCount];
            for (int i = 0; i < steam.Length; i++)
            {
                steam[i] = AmbientImage(front.transform, "Steam_" + i, new Vector2(0.5f, GroundRatio), new Vector2(0.5f, 0.5f),
                    new Vector2(config.SteamSize, config.SteamSize), Color.white, glow);
            }
            Image sparrow = AmbientImage(front.transform, "Sparrow", new Vector2(0.5f, GroundRatio), new Vector2(0.5f, 0f),
                new Vector2(config.SparrowSize, config.SparrowSize), Color.white, ArtAssigner.LoadAmbientSprite("bird"));

            float b = KioskBodyWidth;
            var perches = new List<Vector2> { new Vector2(-230f, -58f) };
            if (geometry.art)
            {
                // На навесе по бокам от вывески.
                perches.Add(new Vector2(b * 0.5f, geometry.awningTop - 6f));
                perches.Add(new Vector2(-b * 0.5f, geometry.awningTop - 6f));
            }

            var view = front.AddComponent<AmbientWorldView>();
            var so = new SerializedObject(view);
            Set(so, "_area", area);
            SetArray(so, "_clouds", clouds);
            SetArray(so, "_cloudSprites", cloudSprites.ToArray());
            SetArray(so, "_stars", stars);
            SetArray(so, "_farBirds", birds);
            SerializedProperty offsets = so.FindProperty("_farBirdOffsets");
            offsets.arraySize = 2;
            offsets.GetArrayElementAtIndex(0).vector2Value = Vector2.zero;
            offsets.GetArrayElementAtIndex(1).vector2Value = new Vector2(46f, -14f);
            Set(so, "_vehicle", vehicle);
            so.FindProperty("_roadY").floatValue = 12f;
            SetArray(so, "_fireflies", fireflies);
            SetArray(so, "_steam", steam);
            so.FindProperty("_steamOrigin").vector2Value = geometry.steam;
            Set(so, "_sparrow", sparrow);
            SerializedProperty perchList = so.FindProperty("_perches");
            perchList.arraySize = perches.Count;
            for (int i = 0; i < perches.Count; i++)
                perchList.GetArrayElementAtIndex(i).vector2Value = perches[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        // Далёкая птица-«галочка»: два тонких крыла, поднятых от центра; взмах — scaleY корня (AmbientWorldView).
        private static RectTransform BuildFarBird(Transform parent, int index)
        {
            GameObject bird = CreateUi("FarBird_" + index, parent);
            SetRect(bird, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44f, 16f));
            Color color = UiTheme.WithAlpha(_theme.Outline, 0.55f);
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject wing = CreateUi(side < 0 ? "WingLeft" : "WingRight", bird.transform);
                SetRect(wing, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(side < 0 ? 1f : 0f, 0.5f), Vector2.zero, new Vector2(22f, 5f));
                wing.transform.localRotation = Quaternion.Euler(0f, 0f, side * 28f);
                AddImage(wing, color, _roundSprite, sliced: false).raycastTarget = false;
            }
            bird.SetActive(false);
            return (RectTransform)bird.transform;
        }

        private static Image AmbientImage(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 size, Color color, Sprite sprite)
        {
            GameObject go = CreateUi(name, parent);
            SetRect(go, anchor, anchor, pivot, Vector2.zero, size);
            Image image = AddImage(go, color, sprite, sliced: false);
            image.raycastTarget = false;
            go.SetActive(false);
            return image;
        }

        /// <summary>
        /// Погода: затемнение неба — за ларьком (перед дорогой); лужи и снег у бордюра — на тротуаре за ларьком и клиентами;
        /// снег на навесе — под вывеской; капли, снежинки и отскоки капель — поверх всего в зоне ларька.
        /// </summary>
        private static WeatherView BuildWeather(Transform area, Transform scene, AmbientConfig config)
        {
            var geometry = KioskGeometry();

            GameObject darken = CreateUi("RainDarken", scene);
            Stretch(darken, Vector2.zero, Vector2.zero);
            Transform road = scene.Find("Road");
            if (road != null)
                darken.transform.SetSiblingIndex(road.GetSiblingIndex());
            Image darkenImage = AddImage(darken, UiTheme.WithAlpha(_theme.Overlay, 1f), null);
            darkenImage.raycastTarget = false;
            darken.SetActive(false);

            GameObject ground = CreateUi("WeatherGround", area);
            Stretch(ground, Vector2.zero, Vector2.zero);
            Transform pavement = area.Find("Pavement");
            if (pavement != null)
                ground.transform.SetSiblingIndex(pavement.GetSiblingIndex() + 1);
            var puddles = new Object[config.PuddleCount.y];
            var ripples = new Object[puddles.Length];
            Sprite outline = ProceduralSprites.Load(ProceduralSprites.CircleOutline);
            for (int i = 0; i < puddles.Length; i++)
            {
                // По X лужи раскладывает WeatherView; по Y — доля высоты тротуара.
                float depth = GroundRatio * (0.3f + 0.4f * ((i * 0.618f) % 1f));
                Image puddle = AmbientImage(ground.transform, "Puddle_" + i, new Vector2(0.5f, depth), new Vector2(0.5f, 0.5f), new Vector2(130f, 24f),
                    UiTheme.WithAlpha(_theme.Water, 1f), _circleSprite);
                puddles[i] = puddle;
                GameObject ripple = CreateUi("Ripple", puddle.transform);
                Stretch(ripple, Vector2.zero, Vector2.zero);
                Image rippleImage = AddImage(ripple, Light, outline, sliced: false);
                rippleImage.raycastTarget = false;
                ripple.SetActive(false);
                ripples[i] = rippleImage;
            }
            GameObject groundSnow = CreateUi("GroundSnow", ground.transform);
            SetRect(groundSnow, new Vector2(0f, GroundRatio), new Vector2(1f, GroundRatio), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 22f));
            AddImage(groundSnow, Color.white, _roundSprite, sliced: false).raycastTarget = false;
            groundSnow.SetActive(false);
            var drifts = new List<Object> { groundSnow.GetComponent<Image>() };

            Transform booth = area.Find("Booth");
            Transform awning = booth != null ? booth.Find("Awning") : null;
            if (geometry.art && awning != null)
            {
                Image awningSnow = Part(booth, "AwningSnow", new Vector2(0f, geometry.awningTop - KioskBaseY - 10f),
                    new Vector2(geometry.awningWidth * 0.9f, 18f), Color.white, _roundSprite, sliced: false);
                awningSnow.transform.SetSiblingIndex(awning.GetSiblingIndex() + 1);
                awningSnow.gameObject.SetActive(false);
                drifts.Add(awningSnow);
            }

            GameObject front = CreateUi("WeatherFront", area);
            Stretch(front, Vector2.zero, Vector2.zero);
            var drops = new Object[config.RainDropCount];
            for (int i = 0; i < drops.Length; i++)
            {
                drops[i] = AmbientImage(front.transform, "Drop_" + i, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), config.RainDropSize,
                    UiTheme.WithAlpha(Light, config.RainAlpha), _roundSprite);
            }
            var flakes = new Object[config.SnowflakeCount];
            for (int i = 0; i < flakes.Length; i++)
                flakes[i] = AmbientImage(front.transform, "Flake_" + i, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(7f, 7f), Color.white, _circleSprite);
            var splashes = new Object[config.SplashCount];
            for (int i = 0; i < splashes.Length; i++)
                splashes[i] = AmbientImage(front.transform, "Splash_" + i, new Vector2(0.5f, GroundRatio), new Vector2(0.5f, 0.5f), new Vector2(7f, 7f), Light, _circleSprite);

            var view = front.AddComponent<WeatherView>();
            var so = new SerializedObject(view);
            Set(so, "_area", area);
            Set(so, "_skyDarken", darkenImage);
            SetArray(so, "_drops", drops);
            SetArray(so, "_puddles", puddles);
            SetArray(so, "_ripples", ripples);
            SetArray(so, "_splashes", splashes);
            SetArray(so, "_flakes", flakes);
            SetArray(so, "_drifts", drifts.ToArray());
            so.FindProperty("_awningTop").vector2Value = new Vector2(0f, geometry.awningTop - 4f);
            so.FindProperty("_awningWidth").floatValue = geometry.awningWidth;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static void BuildEffects(Transform parent, RectTransform coinTarget, out CoinFlyEffect coinFly,
            out FloatingTextEffect floatingText)
        {
            GameObject layer = CreateUi("EffectsLayer", parent);
            Stretch(layer, Vector2.zero, Vector2.zero);

            var coins = new RectTransform[CoinPoolSize];
            for (int i = 0; i < coins.Length; i++)
            {
                GameObject coin = CreateUi("Coin_" + i, layer.transform);
                SetRect(coin, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64f, 64f));
                if (_icons.Coin != null)
                {
                    Image coinImage = AddImage(coin, Color.white, _icons.Coin, sliced: false);
                    coinImage.preserveAspect = true;
                    coinImage.raycastTarget = false;
                }
                else
                {
                    AddCircle(coin, Gold).raycastTarget = false;
                    GameObject inner = CreateUi("Inner", coin.transform);
                    SetRect(inner, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(38f, 38f));
                    AddImage(inner, CoinInner, _circleSprite, sliced: false).raycastTarget = false;
                }
                coin.SetActive(false);
                coins[i] = (RectTransform)coin.transform;
            }

            var texts = new TMP_Text[FloatingTextPoolSize];
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = AddText(layer.transform, "FloatingText_" + i, "+1", 44, Gold, TextAlignmentOptions.Center, bold: true);
                Outlined(text);
                SetRect(text.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 64f));
                text.enableWordWrapping = false;
                text.gameObject.SetActive(false);
                texts[i] = text;
            }

            coinFly = layer.AddComponent<CoinFlyEffect>();
            var coinSo = new SerializedObject(coinFly);
            Set(coinSo, "_layer", layer.transform);
            SetArray(coinSo, "_coins", coins);
            Set(coinSo, "_target", coinTarget);
            coinSo.ApplyModifiedPropertiesWithoutUndo();

            floatingText = layer.AddComponent<FloatingTextEffect>();
            var textSo = new SerializedObject(floatingText);
            Set(textSo, "_layer", layer.transform);
            SetArray(textSo, "_texts", texts);
            textSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void StretchAnchors(GameObject go, Vector2 anchorMin, Vector2 anchorMax)
        {
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static OfflineIncomePopup BuildOfflinePopup(Transform parent)
        {
            GameObject root = CreateUi("OfflinePopup", parent);
            Stretch(root, Vector2.zero, Vector2.zero);
            AddOverlay(root);
            var popup = root.AddComponent<OfflineIncomePopup>();

            GameObject panel = CreateUi("Panel", root.transform);
            SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 760f));
            AddCard(panel, Cream);

            TMP_Text title = AddLocalizedText(panel.transform, "Title", "offline.title", 56, DarkBrown, bold: true);
            SetRect(title.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(-80f, 80f));
            TMP_Text subtitle = AddText(panel.transform, "Subtitle", "", 36, Muted, TextAlignmentOptions.Center, autoSize: true);
            SetRect(subtitle.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(-80f, 60f));

            if (_icons.Offline != null)
            {
                // Спящий месяц на монетах — «пока тебя не было».
                AddArtAt(panel.transform, "OfflineIcon", _icons.Offline, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), 150f);
            }
            else
            {
                GameObject coin = CreateUi("CoinIcon", panel.transform);
                SetRect(coin, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 110f), new Vector2(110f, 110f));
                AddCircle(coin, Gold).raycastTarget = false;
            }
            TMP_Text amount = AddText(panel.transform, "Amount", "+0", 96, Gold, TextAlignmentOptions.Center, bold: true, autoSize: true);
            Outlined(amount);
            SetRect(amount.gameObject, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(-80f, 130f));
            TMP_Text limit = AddText(panel.transform, "Limit", "", 30, Badge, TextAlignmentOptions.Center, autoSize: true);
            SetRect(limit.gameObject, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -105f), new Vector2(-80f, 50f));

            Button claim = BuildPopupButton(panel.transform, "ClaimButton", ButtonStyle.Neutral, new Vector2(-195f, 70f), SoundId.Buy, out TMP_Text claimText);
            var claimLocalized = claimText.gameObject.AddComponent<LocalizedText>();
            var claimSo = new SerializedObject(claimLocalized);
            claimSo.FindProperty("_key").stringValue = "offline.claim";
            claimSo.ApplyModifiedPropertiesWithoutUndo();
            claimText.text = "offline.claim";

            Button claimDouble = BuildPopupButton(panel.transform, "ClaimDoubleButton", ButtonStyle.Reward, new Vector2(195f, 70f), SoundId.Tap, out TMP_Text claimDoubleText);
            AddButtonIcon(claimDouble, claimDoubleText, _icons.Ad, 64f);
            AddAvailableGlow(claimDouble);

            var so = new SerializedObject(popup);
            Set(so, "_root", root);
            Set(so, "_panel", panel.transform);
            Set(so, "_subtitleText", subtitle);
            Set(so, "_amountText", amount);
            Set(so, "_limitText", limit);
            Set(so, "_claimButton", claim);
            Set(so, "_claimDoubleButton", claimDouble);
            Set(so, "_claimDoubleText", claimDoubleText);
            so.ApplyModifiedPropertiesWithoutUndo();

            AddPopupAnimator(root, panel);
            root.SetActive(false);
            return popup;
        }

        private static Button BuildPopupButton(Transform parent, string name, ButtonStyle style, Vector2 bottomCenterOffset, SoundId sound,
            out TMP_Text label)
        {
            GameObject buttonObject = CreateUi(name, parent);
            SetRect(buttonObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), bottomCenterOffset, new Vector2(360f, 130f));
            Button button = AddSoftButton(buttonObject, style, sound);
            label = AddButtonLabel(buttonObject.transform, "Label", "", 38);
            Stretch(label.gameObject, new Vector2(16f, 8f), new Vector2(-16f, -8f));
            return button;
        }

        // ---------- City tab, reputation shop, prestige popup ----------

        private static CityPanel BuildCityPanel(Transform parent, ShopItemRowView shopRowPrefab)
        {
            GameObject panel = BuildListPanel(parent, "CityPanel", "map.title", out _, out RectTransform list);
            var cityPanel = panel.AddComponent<CityPanel>();

            // Одна карточка: карта филиалов, под ней прогресс к следующему городу и кнопка перехода.
            GameObject card = CreateUi("CityCard", list);
            ((RectTransform)card.transform).sizeDelta = new Vector2(1000f, CityCardHeight);
            AddCard(card, RowColor);
            card.AddComponent<LayoutElement>().preferredHeight = CityCardHeight;

            CityMapView map = BuildCityMap(card.transform);

            float infoTop = -(20f + MapHeight + 22f);
            GameObject bar = CreateUi("ProgressBar", card.transform);
            SetCardLine(bar, infoTop, 60f);
            RectTransform fillRect = AddProgressFill(bar, Reputation);
            bar.GetComponent<Image>().raycastTarget = false;
            TMP_Text progress = AddText(bar.transform, "ProgressText", "0 / 1.00B", 30, DarkBrown, TextAlignmentOptions.Center, bold: true, autoSize: true);
            Stretch(progress.gameObject, new Vector2(16f, 0f), new Vector2(-16f, 0f));

            TMP_Text gain = AddText(card.transform, "GainText", "", 34, Reputation, TextAlignmentOptions.Left, bold: true, autoSize: true);
            SetCardLine(gain.gameObject, infoTop - 70f, 48f);
            TMP_Text bonus = AddText(card.transform, "BonusText", "", 30, DarkBrown, TextAlignmentOptions.Left, autoSize: true);
            SetRect(bonus.gameObject, new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 1f), new Vector2(30f, infoTop - 122f), new Vector2(-40f, 44f));
            TMP_Text reputation = AddText(card.transform, "ReputationText", "", 30, DarkBrown, TextAlignmentOptions.Right, autoSize: true);
            SetRect(reputation.gameObject, new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, infoTop - 122f), new Vector2(-40f, 44f));

            GameObject prestige = CreateUi("PrestigeButton", card.transform);
            SetRect(prestige, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(-60f, 100f));
            Button prestigeButton = AddSoftButton(prestige, ButtonStyle.Secondary, SoundId.Unlock);
            TMP_Text prestigeText = AddButtonLabel(prestige.transform, "Label", "", 36);
            // Переезд — редкое большое событие: пульс здесь уместен (одна из немногих «дышащих» кнопок).
            AddPulseWhileInteractable(prestige, prestigeButton);
            Stretch(prestigeText.gameObject, new Vector2(16f, 6f), new Vector2(-16f, -6f));

            GameObject shopTitle = CreateUi("ShopTitle", list);
            shopTitle.AddComponent<LayoutElement>().preferredHeight = 80f;
            TMP_Text shopTitleText = AddLocalizedText(shopTitle.transform, "Title", "shop.title", 42, DarkBrown, bold: true);
            shopTitleText.alignment = TextAlignmentOptions.BottomLeft;
            Stretch(shopTitleText.gameObject, new Vector2(10f, 0f), new Vector2(-10f, 0f));

            var so = new SerializedObject(cityPanel);
            Set(so, "_map", map);
            Set(so, "_progressFill", fillRect);
            Set(so, "_progressText", progress);
            Set(so, "_gainText", gain);
            Set(so, "_bonusText", bonus);
            Set(so, "_reputationText", reputation);
            Set(so, "_prestigeButton", prestigeButton);
            Set(so, "_prestigeButtonText", prestigeText);
            Set(so, "_shopContent", list);
            Set(so, "_shopRowPrefab", shopRowPrefab);
            so.ApplyModifiedPropertiesWithoutUndo();

            panel.SetActive(false);
            return cityPanel;
        }

        /// <summary>
        /// Карта: нарисованный фон map_bg (нет — «земля» с озером, холмами и деревьями), плашка выбранного города
        /// сверху и область, в которую CityMapView ставит города и дорогу (якорями по CityConfig.MapPosition).
        /// С фоном область совпадает с картинкой целиком: точки городов — доли картинки, т. е. центры её полян.
        /// </summary>
        private static CityMapView BuildCityMap(Transform card)
        {
            const float sideInset = 60f;
            const float bottomInset = 50f;
            const float bannerHeight = 84f;
            const float topInset = bannerHeight + 32f;

            Sprite background = ArtAssigner.LoadMapSprite("map_bg");
            Sprite van = ArtAssigner.LoadMapSprite("map_van");
            bool hasCityIcons = ArtAssigner.LoadMapSprite("map_city_01") != null;

            GameObject frame = CreateUi("Map", card);
            SetRect(frame, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(-40f, MapHeight));
            var map = frame.AddComponent<CityMapView>();

            GameObject area;
            Vector2 referenceSize;
            if (background != null)
            {
                // Скруглённая маска как у карточки. Картинка с городами вписана целиком (города не обрезаются),
                // а поля на широком экране закрывает тот же фон «на обрез», чуть темнее.
                AddImage(frame, Color.white, _roundSprite).raycastTarget = false;
                frame.AddComponent<Mask>().showMaskGraphic = false;
                float aspect = background.rect.width / background.rect.height;

                GameObject cover = CreateUi("BackgroundCover", frame.transform);
                Stretch(cover, Vector2.zero, Vector2.zero);
                AddImage(cover, new Color(0.8f, 0.8f, 0.8f, 1f), background, sliced: false).raycastTarget = false;
                var coverFit = cover.AddComponent<AspectRatioFitter>();
                coverFit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                coverFit.aspectRatio = aspect;

                GameObject art = CreateUi("Background", frame.transform);
                Stretch(art, Vector2.zero, Vector2.zero);
                AddImage(art, Color.white, background, sliced: false).raycastTarget = false;
                var fit = art.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fit.aspectRatio = aspect;

                area = CreateUi("Area", art.transform);
                Stretch(area, Vector2.zero, Vector2.zero);
                float height = Mathf.Min(MapHeight, MapWidth / aspect);
                referenceSize = new Vector2(height * aspect, height);
            }
            else
            {
                AddImage(frame, Land, _roundSprite).raycastTarget = false;
                // Пейзаж — в свободных местах между городами (позиции в долях карты).
                MapBlob(frame.transform, "Hill_0", new Vector2(0.22f, 0.55f), new Vector2(260f, 120f), Hill);
                MapBlob(frame.transform, "Hill_1", new Vector2(0.62f, 0.45f), new Vector2(240f, 110f), Hill);
                MapBlob(frame.transform, "Lake", new Vector2(0.50f, 0.72f), new Vector2(190f, 80f), Water);
                var trees = new[]
                {
                    new Vector2(0.05f, 0.70f), new Vector2(0.11f, 0.76f), new Vector2(0.88f, 0.70f), new Vector2(0.94f, 0.62f),
                    new Vector2(0.29f, 0.10f), new Vector2(0.33f, 0.16f), new Vector2(0.71f, 0.10f), new Vector2(0.76f, 0.15f),
                };
                for (int i = 0; i < trees.Length; i++)
                    MapBlob(frame.transform, "Tree_" + i, trees[i], new Vector2(46f, 46f), i % 2 == 0 ? Leaf : LeafDark);

                area = CreateUi("Area", frame.transform);
                Stretch(area, new Vector2(sideInset, bottomInset), new Vector2(-sideInset, -topInset));
                // Размер области карты в портрете: ширина карты − отступы области.
                referenceSize = new Vector2(MapWidth - 2f * sideInset, MapHeight - bottomInset - topInset);
            }

            GameObject traveler = CreateUi("Traveler", area.transform);
            if (van != null)
            {
                // Опорная точка — у колёс: фургон едет по штрихам, а не над ними.
                SetRect(traveler, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.2f), Vector2.zero, new Vector2(110f, 110f));
                Image vanImage = AddImage(traveler, Color.white, van, sliced: false);
                vanImage.preserveAspect = true;
                vanImage.raycastTarget = false;
            }
            else
            {
                SetRect(traveler, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80f, 70f));
                Part(traveler.transform, "Body", new Vector2(0f, 26f), new Vector2(70f, 38f), Orange, _roundSprite);
                Part(traveler.transform, "Window", new Vector2(20f, 44f), new Vector2(20f, 14f), AwningWhite, _roundSprite);
                Part(traveler.transform, "WheelBack", new Vector2(-20f, 16f), new Vector2(20f, 20f), DarkBrown, _circleSprite, sliced: false);
                Part(traveler.transform, "WheelFront", new Vector2(20f, 16f), new Vector2(20f, 20f), DarkBrown, _circleSprite, sliced: false);
            }

            // Плашка: [миниатюра] Имя ........ статус / [монета] порог · масштаб.
            GameObject banner = CreateUi("SelectedCity", frame.transform);
            SetRect(banner, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(-24f, bannerHeight));
            AddImage(banner, UiTheme.WithAlpha(Cream, 0.92f), _roundSprite).raycastTarget = false;
            Image selectedIcon = null;
            float textLeft = 24f;
            if (hasCityIcons)
            {
                selectedIcon = AddArtAt(banner.transform, "Icon", null, new Vector2(0f, 0.5f), new Vector2(14f + 36f, 0f), 72f);
                textLeft = 100f;
            }
            const float statusWidth = 260f;
            TMP_Text selectedName = AddText(banner.transform, "Name", "", 36, DarkBrown, TextAlignmentOptions.Left, bold: true, autoSize: true);
            SetRect(selectedName.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(textLeft, -4f), new Vector2(-(textLeft + statusWidth + 20f), 44f));
            TMP_Text selectedStatus = AddText(banner.transform, "Status", "", 28, Muted, TextAlignmentOptions.Right, bold: true, autoSize: true);
            SetRect(selectedStatus.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -8f), new Vector2(statusWidth, 36f));
            float infoLeft = textLeft;
            if (_icons.Coin != null)
            {
                AddArtAt(banner.transform, "Coin", _icons.Coin, new Vector2(0f, 0f), new Vector2(textLeft + 15f, 23f), 30f);
                infoLeft += 38f;
            }
            TMP_Text selectedInfo = AddText(banner.transform, "Info", "", 26, Muted, TextAlignmentOptions.Left, autoSize: true);
            SetRect(selectedInfo.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(infoLeft, 6f), new Vector2(-(infoLeft + 20f), 34f));

            var so = new SerializedObject(map);
            Set(so, "_area", area.transform);
            Set(so, "_nodePrefab", BuildMapNodePrefab());
            Set(so, "_roadDotPrefab", BuildRoadDotPrefab());
            Set(so, "_traveler", traveler.transform);
            Set(so, "_selectedNameText", selectedName);
            Set(so, "_selectedInfoText", selectedInfo);
            Set(so, "_selectedStatusText", selectedStatus);
            Set(so, "_selectedIcon", selectedIcon);
            SetColor(so, "_statusColor", Muted);
            SetColor(so, "_nextStatusColor", _theme.Secondary);
            so.FindProperty("_referenceSize").vector2Value = referenceSize;
            so.FindProperty("_dotSpacing").floatValue = 30f;
            so.FindProperty("_nodeClearance").floatValue = hasCityIcons ? 72f : 58f;
            SetColor(so, "_roadDoneColor", _theme.Primary);
            SetColor(so, "_roadProgressColor", Reputation);
            SetColor(so, "_roadAheadColor", UiTheme.WithAlpha(_theme.Outline, 0.45f));
            SetColor(so, "_roadLockedColor", UiTheme.WithAlpha(_theme.Outline, 0.3f));
            so.ApplyModifiedPropertiesWithoutUndo();
            return map;
        }

        // Пятно пейзажа (холм, озеро, дерево) в точке anchor карты.
        private static void MapBlob(Transform frame, string name, Vector2 anchor, Vector2 size, Color color)
        {
            GameObject go = CreateUi(name, frame);
            SetRect(go, anchor, anchor, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            AddImage(go, color, _circleSprite, sliced: false).raycastTarget = false;
        }

        private static CityMapNodeView BuildMapNodePrefab()
        {
            GameObject root = CreateUi("CityMapNode", null);
            ((RectTransform)root.transform).sizeDelta = new Vector2(200f, 150f);
            AddImage(root, Color.clear, null);
            Button button = AddButton(root, SoundId.Tap);
            var view = root.AddComponent<CityMapNodeView>();

            Vector2 center = new Vector2(0.5f, 0.5f);
            GameObject body = CreateUi("Body", root.transform);
            SetRect(body, center, center, center, Vector2.zero, new Vector2(MapIconSize, MapIconSize));

            // Свечение следующего города, ореол выбора (размер ставит CityMapNodeView), затем пульсирующее содержимое.
            Image glow = AddArtAt(body.transform, "Glow", ProceduralSprites.Load(ProceduralSprites.GlowSoft), center, Vector2.zero, 220f);
            glow.color = UiTheme.WithAlpha(_theme.Secondary, 0.85f);
            glow.gameObject.SetActive(false);

            GameObject selection = CreateUi("Selection", body.transform);
            SetRect(selection, center, center, center, Vector2.zero, new Vector2(128f, 128f));
            AddImage(selection, Gold, _circleSprite, sliced: false).raycastTarget = false;

            GameObject content = CreateUi("Content", body.transform);
            SetRect(content, center, center, center, Vector2.zero, new Vector2(MapIconSize, MapIconSize));
            PulseEffect pulse = AddPulse(content);

            GameObject circle = CreateUi("Circle", content.transform);
            SetRect(circle, center, center, center, Vector2.zero, new Vector2(100f, 100f));
            Image circleImage = AddCircle(circle, Green);
            circleImage.raycastTarget = false;
            TMP_Text number = AddText(circle.transform, "Number", "1", 50, Light, TextAlignmentOptions.Center, bold: true);
            Outlined(number);
            Stretch(number.gameObject, Vector2.zero, Vector2.zero);

            Image icon = AddArt(content.transform, "Icon", null);
            icon.enabled = true;
            icon.gameObject.SetActive(false);

            GameObject lockObject = null;
            if (_icons.Lock != null)
            {
                lockObject = AddArtAt(content.transform, "Lock", _icons.Lock, center, new Vector2(0f, -4f), 52f).gameObject;
                lockObject.SetActive(false);
            }

            // Бейдж номера в правом нижнем углу миниатюры; у открытых городов вместо номера — галочка.
            const float badgeSize = 42f;
            GameObject badge = CreateUi("Badge", content.transform);
            SetRect(badge, center, center, center, new Vector2(MapIconSize * 0.36f, -MapIconSize * 0.36f), new Vector2(badgeSize, badgeSize));
            Image badgeCircle = AddCircle(badge, Green);
            badgeCircle.raycastTarget = false;
            TMP_Text badgeNumber = AddText(badge.transform, "Number", "1", 26, Light, TextAlignmentOptions.Center, bold: true);
            Outlined(badgeNumber);
            Stretch(badgeNumber.gameObject, Vector2.zero, Vector2.zero);
            Image badgeCheck = AddArt(badge.transform, "Check", _icons.Check, 0.14f);
            badgeCheck.gameObject.SetActive(false);
            badge.SetActive(false);

            Sprite pinSprite = ArtAssigner.LoadMapSprite("map_pin");
            GameObject pin = CreateUi("HerePin", body.transform);
            if (pinSprite != null)
            {
                // Булавка над миниатюрой, кончиком чуть заходит на неё; качается вверх-вниз.
                SetRect(pin, center, center, new Vector2(0.5f, 0f), new Vector2(0f, MapIconSize * 0.5f - 12f), new Vector2(56f, 56f));
                Image pinImage = AddImage(pin, Color.white, pinSprite, sliced: false);
                pinImage.preserveAspect = true;
                pinImage.raycastTarget = false;
                pin.AddComponent<BobEffect>();
            }
            else
            {
                SetRect(pin, center, center, new Vector2(0.5f, 0f), new Vector2(0f, 58f), new Vector2(170f, 42f));
                AddImage(pin, _theme.Outline, _pillSprite).raycastTarget = false;
                TMP_Text pinText = AddLocalizedText(pin.transform, "Label", "map.here", 26, Light, bold: true);
                Stretch(pinText.gameObject, new Vector2(8f, 2f), new Vector2(-8f, -2f));
            }

            // Подпись на светлой ленточке — читается на любом фоне; ширину ленточки по имени ставит CityMapNodeView.
            GameObject namePill = CreateUi("NamePill", root.transform);
            SetRect(namePill, center, center, new Vector2(0.5f, 1f), new Vector2(0f, -MapIconSize * 0.5f - 4f), new Vector2(160f, 40f));
            AddImage(namePill, UiTheme.WithAlpha(Paper, 0.9f), _pillSprite).raycastTarget = false;
            TMP_Text nameText = AddText(namePill.transform, "Name", "Город", 28, DarkBrown, TextAlignmentOptions.Center, bold: true);
            Stretch(nameText.gameObject, new Vector2(10f, 0f), new Vector2(-10f, 0f));
            nameText.enableWordWrapping = false;
            nameText.overflowMode = TextOverflowModes.Overflow;

            var so = new SerializedObject(view);
            Set(so, "_button", button);
            Set(so, "_circle", circleImage);
            Set(so, "_numberText", number);
            Set(so, "_nameText", nameText);
            Set(so, "_selection", selection);
            Set(so, "_herePin", pin);
            Set(so, "_body", body.transform);
            Set(so, "_pulse", pulse);
            Set(so, "_icon", icon);
            Set(so, "_badge", badge);
            Set(so, "_badgeCircle", badgeCircle);
            Set(so, "_badgeNumber", badgeNumber);
            Set(so, "_badgeCheck", badgeCheck.gameObject);
            Set(so, "_lock", lockObject);
            Set(so, "_glow", glow.gameObject);
            Set(so, "_namePill", namePill.transform);
            SetColor(so, "_doneColor", _theme.Primary);
            SetColor(so, "_nextColor", _theme.Secondary);
            SetColor(so, "_lockedColor", _theme.Disabled);
            SetColor(so, "_lockedIconTint", UiTheme.WithAlpha(_theme.Disabled, 0.75f));
            SetColor(so, "_nameColor", DarkBrown);
            SetColor(so, "_lockedNameColor", UiTheme.WithAlpha(Muted, 0.7f));
            so.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, MapNodePrefabPath).GetComponent<CityMapNodeView>();
        }

        // Штрих дороги: вытянутая «пилюля», CityMapView поворачивает её по касательной к кривой.
        private static Image BuildRoadDotPrefab()
        {
            GameObject root = CreateUi("RoadDot", null);
            ((RectTransform)root.transform).sizeDelta = new Vector2(22f, 10f);
            AddImage(root, BarBack, _pillSprite, sliced: false).raycastTarget = false;
            return SavePrefab(root, RoadDotPrefabPath).GetComponent<Image>();
        }

        // Строка карточки города: растянута по ширине с отступами 30, отсчёт сверху.
        private static void SetCardLine(GameObject go, float top, float height)
        {
            SetRect(go, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, top), new Vector2(-60f, height));
        }

        private static ShopItemRowView BuildShopRowPrefab()
        {
            GameObject root = CreateUi("ShopItemRow", null);
            ((RectTransform)root.transform).sizeDelta = new Vector2(1000f, ShopRowHeight);
            AddCard(root, RowColor);
            root.AddComponent<LayoutElement>().preferredHeight = ShopRowHeight;
            var view = root.AddComponent<ShopItemRowView>();

            TMP_Text nameText = AddText(root.transform, "NameText", "Item", 40, DarkBrown, TextAlignmentOptions.Left, bold: true, autoSize: true);
            SetRect(nameText.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(30f, -16f), new Vector2(-340f, 52f));
            TMP_Text description = AddText(root.transform, "DescriptionText", "", 30, Muted, TextAlignmentOptions.Left, autoSize: true);
            SetRect(description.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(30f, -70f), new Vector2(-340f, 46f));
            TMP_Text level = AddText(root.transform, "LevelText", "", 28, Reputation, TextAlignmentOptions.Left, bold: true, autoSize: true);
            SetRect(level.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(30f, 14f), new Vector2(-340f, 38f));

            Button buyButton = BuildRowButton(root.transform, "BuyButton", ButtonStyle.Reputation, new Vector2(270f, 120f), SoundId.Upgrade, out TMP_Text buyText);

            var so = new SerializedObject(view);
            Set(so, "_nameText", nameText);
            Set(so, "_descriptionText", description);
            Set(so, "_levelText", level);
            Set(so, "_buyButton", buyButton);
            Set(so, "_buyText", buyText);
            so.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, ShopRowPrefabPath).GetComponent<ShopItemRowView>();
        }

        // ---------- Shop tab (bills) ----------

        /// <summary>Вкладка «Магазин»: заголовок и группа паков купюр, заголовок и группа мешков монет (строки — в ShopPanel).</summary>
        private static ShopPanel BuildShopPanel(Transform parent)
        {
            GameObject panel = BuildListPanel(parent, "ShopPanel", "tab.shop", out _, out RectTransform list);
            var shopPanel = panel.AddComponent<ShopPanel>();

            StarterPackCard starterCard = BuildStarterPackCard(list);

            BuildSectionTitle(list, "PremiumTitle", "shop.premium_title");
            RectTransform premiumGroup = BuildRowGroup(list, "PremiumGroup");
            RectTransform adGroup = BuildRowGroup(list, "AdGroup");
            BuildSectionTitle(list, "BillsTitle", "shop.bills_title");
            RectTransform billsGroup = BuildRowGroup(list, "BillsGroup");
            BuildSectionTitle(list, "CoinsTitle", "shop.coins_title");
            RectTransform coinsGroup = BuildRowGroup(list, "CoinsGroup");

            var so = new SerializedObject(shopPanel);
            Set(so, "_starterCard", starterCard);
            Set(so, "_premiumGroup", premiumGroup);
            Set(so, "_adGroup", adGroup);
            Set(so, "_billsGroup", billsGroup);
            Set(so, "_coinsGroup", coinsGroup);
            Set(so, "_rowPrefab", BuildBillShopRowPrefab());
            so.ApplyModifiedPropertiesWithoutUndo();

            panel.SetActive(false);
            return shopPanel;
        }

        // ---------- Starter pack (09) ----------

        private const float StarterCardHeight = 540f;

        /// <summary>
        /// Карточка «Стартовый набор»: золотая рамка, красная ленточка «Супер-выгода!», большая иконка набора на светлом ореоле слева,
        /// справа название и две плитки наград (пачка купюр + монета) с кружком «+» между ними, под ними «Только один раз», внизу кнопка цены.
        /// </summary>
        private static StarterPackCard BuildStarterPackCard(Transform list)
        {
            GameObject root = CreateUi("StarterPackCard", list);
            root.AddComponent<LayoutElement>().preferredHeight = StarterCardHeight;
            AddCard(root, Gold).raycastTarget = false;
            var card = root.AddComponent<StarterPackCard>();

            GameObject inner = CreateUi("Inner", root.transform);
            Stretch(inner, new Vector2(12f, 16f), new Vector2(-12f, -12f));
            AddImage(inner, LightGold, _roundSprite).raycastTarget = false;

            // Светлый ореол под иконкой, чтобы подарок «светился» на золотом фоне.
            GameObject halo = CreateUi("Halo", inner.transform);
            SetRect(halo, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(140f, -200f), new Vector2(250f, 250f));
            AddImage(halo, UiTheme.WithAlpha(Color.white, 0.55f), _circleSprite, sliced: false).raycastTarget = false;

            // Иконка набора: чуть наклонена, спрайт назначает StarterPackCard из конфига.
            GameObject icon = CreateUi("Icon", inner.transform);
            SetRect(icon, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(140f, -200f), new Vector2(220f, 220f));
            icon.transform.localRotation = Quaternion.Euler(0f, 0f, 6f);
            Image iconImage = AddImage(icon, Color.white, null, sliced: false);
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            GameObject content = CreateUi("Content", inner.transform);
            Stretch(content, new Vector2(260f, 0f), new Vector2(-10f, 0f));

            GameObject ribbon = CreateUi("Ribbon", root.transform);
            SetRect(ribbon, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -36f), new Vector2(460f, 62f));
            AddImage(ribbon, Badge, _pillSprite).raycastTarget = false;
            TMP_Text ribbonText = AddText(ribbon.transform, "Label", "", 36, Light, TextAlignmentOptions.Center, bold: true, autoSize: true);
            Outlined(ribbonText);
            Stretch(ribbonText.gameObject, new Vector2(14f, 4f), new Vector2(-14f, -4f));
            ribbonText.raycastTarget = false;
            // Ленточка без пульса (V1): на экране «дышат» только подарок, буст, переезд и следующий город на карте.

            TMP_Text title = AddText(content.transform, "TitleText", "", 52, DarkBrown, TextAlignmentOptions.Center, bold: true, autoSize: true);
            SetRect(title.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(-20f, 68f));
            title.raycastTarget = false;

            TMP_Text billsText = BuildStarterTile(content.transform, "BillsTile", BillGreen, _icons.Bill, BillLight, -1f);
            TMP_Text coinsText = BuildStarterTile(content.transform, "CoinsTile", Orange, _icons.Coin, Gold, 1f);

            // «+» в кружке поверх стыка плиток.
            GameObject plus = CreateUi("Plus", content.transform);
            SetRect(plus, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -231f), new Vector2(64f, 64f));
            AddCircle(plus, Cream).raycastTarget = false;
            TMP_Text plusText = AddText(plus.transform, "Label", "+", 50, DarkBrown, TextAlignmentOptions.Center, bold: true);
            Stretch(plusText.gameObject, new Vector2(0f, 2f), Vector2.zero);
            plusText.raycastTarget = false;

            TMP_Text once = AddText(inner.transform, "OnceText", "", 34, Badge, TextAlignmentOptions.Center, bold: true, autoSize: true);
            SetRect(once.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -326f), new Vector2(-40f, 44f));
            once.raycastTarget = false;

            GameObject buy = CreateUi("BuyButton", inner.transform);
            SetRect(buy, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(660f, 108f));
            Button buyButton = AddSoftButton(buy, ButtonStyle.Primary, SoundId.Buy);
            TMP_Text buyText = AddButtonLabel(buy.transform, "Label", "", 50);
            Stretch(buyText.gameObject, new Vector2(16f, 8f), new Vector2(-16f, -8f));
            AddAvailableGlow(buyButton);

            var so = new SerializedObject(card);
            Set(so, "_icon", iconImage);
            Set(so, "_titleText", title);
            Set(so, "_ribbonText", ribbonText);
            Set(so, "_onceText", once);
            Set(so, "_billsText", billsText);
            Set(so, "_coinsText", coinsText);
            Set(so, "_buyButton", buyButton);
            Set(so, "_buyText", buyText);
            so.ApplyModifiedPropertiesWithoutUndo();
            return card;
        }

        // Плитка награды: цветная карточка, крупная иконка сверху и текст снизу. Без спрайта — кружок markColor.
        // side: -1 — слева от центра, 1 — справа.
        private static TMP_Text BuildStarterTile(Transform parent, string name, Color color, Sprite iconSprite, Color markColor, float side)
        {
            GameObject tile = CreateUi(name, parent);
            SetRect(tile, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(side < 0f ? 1f : 0f, 1f),
                new Vector2(side * 18f, -146f), new Vector2(340f, 170f));
            AddCard(tile, color).raycastTarget = false;

            if (iconSprite != null)
            {
                AddArtAt(tile.transform, "Icon", iconSprite, new Vector2(0.5f, 1f), new Vector2(0f, -56f), 104f);
            }
            else
            {
                GameObject mark = CreateUi("Mark", tile.transform);
                SetRect(mark, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -56f), new Vector2(64f, 64f));
                AddImage(mark, markColor, _circleSprite, sliced: false).raycastTarget = false;
            }

            TMP_Text text = AddText(tile.transform, "Text", "", 40, Light, TextAlignmentOptions.Center, bold: true, autoSize: true);
            Outlined(text);
            SetRect(text.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(-24f, 56f));
            text.raycastTarget = false;
            return text;
        }

        // Красный кружок «!» в углу кнопки вкладки: есть бесплатная награда.
        private static GameObject BuildTabBadge(Transform tab)
        {
            GameObject badge = CreateUi("Badge", tab);
            SetRect(badge, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-10f, -10f), new Vector2(44f, 44f));
            AddImage(badge, Badge, _circleSprite, sliced: false).raycastTarget = false;
            TMP_Text mark = AddText(badge.transform, "Mark", "!", 34, Light, TextAlignmentOptions.Center, bold: true);
            Stretch(mark.gameObject, Vector2.zero, Vector2.zero);
            badge.SetActive(false);
            return badge;
        }

        private static void BuildSectionTitle(Transform list, string name, string key)
        {
            GameObject title = CreateUi(name, list);
            title.AddComponent<LayoutElement>().preferredHeight = 70f;
            TMP_Text text = AddLocalizedText(title.transform, "Title", key, 42, DarkBrown, bold: true);
            text.alignment = TextAlignmentOptions.BottomLeft;
            Stretch(text.gameObject, new Vector2(10f, 0f), new Vector2(-10f, 0f));
        }

        // Вложенный вертикальный список: его высота складывается из строк, которые добавит код.
        private static RectTransform BuildRowGroup(Transform list, string name)
        {
            GameObject group = CreateUi(name, list);
            var vertical = group.AddComponent<VerticalLayoutGroup>();
            vertical.spacing = 16f;
            vertical.childControlWidth = true;
            vertical.childControlHeight = true;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;
            return (RectTransform)group.transform;
        }

        private static BillShopRowView BuildBillShopRowPrefab()
        {
            GameObject root = CreateUi("BillShopRow", null);
            ((RectTransform)root.transform).sizeDelta = new Vector2(1000f, ShopRowHeight);
            AddCard(root, RowColor);
            root.AddComponent<LayoutElement>().preferredHeight = ShopRowHeight;
            var view = root.AddComponent<BillShopRowView>();

            GameObject icon = CreateUi("Icon", root.transform);
            SetRect(icon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(138f, 138f));
            Image iconImage = AddImage(icon, BillGreen, _roundSprite);
            iconImage.raycastTarget = false;
            GameObject mark = CreateUi("Mark", icon.transform);
            SetRect(mark, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44f, 44f));
            Image markImage = AddImage(mark, BillLight, _circleSprite, sliced: false);
            markImage.raycastTarget = false;

            TMP_Text nameText = AddText(root.transform, "NameText", "Item", 40, DarkBrown, TextAlignmentOptions.Left, bold: true, autoSize: true);
            SetRect(nameText.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(166f, -24f), new Vector2(-480f, 52f));
            TMP_Text description = AddText(root.transform, "DescriptionText", "", 34, Muted, TextAlignmentOptions.Left, bold: true, autoSize: true);
            SetRect(description.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(166f, 30f), new Vector2(-480f, 50f));

            GameObject badge = CreateUi("Badge", root.transform);
            SetRect(badge, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-30f, -8f), new Vector2(180f, 44f));
            AddImage(badge, Badge, _pillSprite).raycastTarget = false;
            TMP_Text badgeText = AddLocalizedText(badge.transform, "Label", "shop.best", 28, Light, bold: true);
            Stretch(badgeText.gameObject, new Vector2(8f, 2f), new Vector2(-8f, -2f));
            badge.SetActive(false);

            Button button = BuildRowButton(root.transform, "BuyButton", ButtonStyle.Bills, new Vector2(270f, 120f), SoundId.Buy, out TMP_Text buttonText);

            var so = new SerializedObject(view);
            Set(so, "_icon", iconImage);
            Set(so, "_iconMark", markImage);
            Set(so, "_nameText", nameText);
            Set(so, "_descriptionText", description);
            Set(so, "_badge", badge);
            Set(so, "_button", button);
            Set(so, "_buttonText", buttonText);
            so.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, BillShopRowPrefabPath).GetComponent<BillShopRowView>();
        }

        // ---------- Bills instead of coins (08e) ----------

        /// <summary>
        /// Значок банкноты в углу кнопки покупки и BillOfferTrigger: нажатие на выключенную кнопку
        /// (монет не хватает) предлагает купить за купюры.
        /// </summary>
        private static BillOfferTrigger AddBillOffer(Button button)
        {
            GameObject go = button.gameObject;
            GameObject hint = CreateUi("BillHint", go.transform);
            SetRect(hint, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-6f, -6f), new Vector2(54f, 36f));
            BillGlyph(hint, 18f);
            hint.SetActive(false);

            var trigger = go.AddComponent<BillOfferTrigger>();
            var so = new SerializedObject(trigger);
            Set(so, "_button", button);
            Set(so, "_hint", hint);
            so.ApplyModifiedPropertiesWithoutUndo();
            return trigger;
        }

        // Значок купюр в прямоугольнике host: картинка icon_bill (чуть крупнее рамки) или зелёная «банкнота» из примитивов.
        private static void BillGlyph(GameObject host, float markSize)
        {
            if (_icons.Bill != null)
            {
                AddArt(host.transform, "Art", _icons.Bill, -0.3f);
                return;
            }
            AddImage(host, BillGreen, _roundSprite).raycastTarget = false;
            GameObject mark = CreateUi("Mark", host.transform);
            SetRect(mark, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(markSize, markSize));
            AddImage(mark, BillLight, _circleSprite, sliced: false).raycastTarget = false;
        }

        /// <summary>Окно «Не хватает монет»: «Купить» слева, «Отмена» справа — под частыми тапами по кнопкам строк справа.</summary>
        private static BillOfferPopup BuildBillOfferPopup(Transform parent)
        {
            GameObject root = CreateUi("BillOfferPopup", parent);
            Stretch(root, Vector2.zero, Vector2.zero);
            AddOverlay(root);
            var popup = root.AddComponent<BillOfferPopup>();

            GameObject panel = CreateUi("Panel", root.transform);
            SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 600f));
            AddCard(panel, Cream);

            TMP_Text title = AddLocalizedText(panel.transform, "Title", "bills.offer_title", 52, DarkBrown, bold: true);
            SetRect(title.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(-80f, 80f));

            GameObject note = CreateUi("Icon", panel.transform);
            SetRect(note, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -136f), new Vector2(120f, 80f));
            BillGlyph(note, 40f);

            TMP_Text body = AddText(panel.transform, "Body", "", 36, DarkBrown, TextAlignmentOptions.Center, autoSize: true);
            SetRect(body.gameObject, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(-80f, 140f));

            Button confirm = BuildPopupButton(panel.transform, "ConfirmButton", ButtonStyle.Bills, new Vector2(-195f, 50f), SoundId.Buy, out TMP_Text confirmText);
            Button cancel = BuildPopupButton(panel.transform, "CancelButton", ButtonStyle.Neutral, new Vector2(195f, 50f), SoundId.Tap, out TMP_Text cancelText);
            AddLocalizedKey(cancelText, "bills.offer_cancel");

            var so = new SerializedObject(popup);
            Set(so, "_root", root);
            Set(so, "_panel", panel.transform);
            Set(so, "_bodyText", body);
            Set(so, "_confirmButton", confirm);
            Set(so, "_confirmText", confirmText);
            Set(so, "_cancelButton", cancel);
            so.ApplyModifiedPropertiesWithoutUndo();

            AddPopupAnimator(root, panel);
            root.SetActive(false);
            return popup;
        }

        // ---------- Settings ----------

        /// <summary>Шестерёнка в HUD (слева сверху, напротив репутации) и окно настроек со звуком и музыкой.</summary>
        private static SettingsPopup BuildSettingsPopup(Transform parent, Transform hud)
        {
            GameObject gear = CreateUi("SettingsButton", hud);
            SetRect(gear, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -14f), new Vector2(96f, 96f));
            Button openButton;
            if (_icons.Settings != null)
            {
                Image gearImage = AddImage(gear, Color.white, _icons.Settings, sliced: false);
                gearImage.preserveAspect = true;
                openButton = AddButton(gear, SoundId.Tap);
            }
            else
            {
                openButton = BuildGearGlyph(gear);
            }
            return BuildSettingsPopupBody(parent, openButton);
        }

        // Шестерёнка из примитивов — пока нет icon_settings.
        private static Button BuildGearGlyph(GameObject gear)
        {
            AddCircle(gear, Brown);
            Button openButton = AddButton(gear, SoundId.Tap);
            for (int i = 0; i < 8; i++)
            {
                GameObject tooth = CreateUi("Tooth_" + i, gear.transform);
                float angle = i * 45f;
                Vector2 offset = Quaternion.Euler(0f, 0f, angle) * Vector2.up * 27f;
                SetRect(tooth, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), offset, new Vector2(16f, 18f));
                tooth.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                AddImage(tooth, LightGold, null).raycastTarget = false;
            }
            GameObject gearBody = CreateUi("Body", gear.transform);
            SetRect(gearBody, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(48f, 48f));
            AddImage(gearBody, LightGold, _circleSprite, sliced: false).raycastTarget = false;
            GameObject gearHole = CreateUi("Hole", gear.transform);
            SetRect(gearHole, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20f, 20f));
            AddImage(gearHole, Brown, _circleSprite, sliced: false).raycastTarget = false;
            return openButton;
        }

        private static SettingsPopup BuildSettingsPopupBody(Transform parent, Button openButton)
        {
            GameObject root = CreateUi("SettingsPopup", parent);
            Stretch(root, Vector2.zero, Vector2.zero);
            AddOverlay(root);
            var popup = root.AddComponent<SettingsPopup>();

            GameObject panel = CreateUi("Panel", root.transform);
            SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 780f));
            AddCard(panel, Cream);

            TMP_Text title = AddLocalizedText(panel.transform, "Title", "settings.title", 56, DarkBrown, bold: true);
            SetRect(title.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(-80f, 80f));

            Button sound = BuildSettingsRow(panel.transform, "Sound", "settings.sound", 170f, out TMP_Text soundText);
            Button music = BuildSettingsRow(panel.transform, "Music", "settings.music", 40f, out TMP_Text musicText);
            Button effects = BuildSettingsRow(panel.transform, "Effects", "settings.effects", -90f, out TMP_Text effectsText);

            Button close = BuildPopupButton(panel.transform, "CloseButton", ButtonStyle.Neutral, new Vector2(0f, 50f), SoundId.Tap, out TMP_Text closeText);
            AddButtonIcon(close, closeText, _icons.Close, 56f);
            AddLocalizedKey(closeText, "settings.close");

            var so = new SerializedObject(popup);
            Set(so, "_root", root);
            Set(so, "_panel", panel.transform);
            Set(so, "_openButton", openButton);
            Set(so, "_closeButton", close);
            Set(so, "_soundButton", sound);
            Set(so, "_soundText", soundText);
            Set(so, "_musicButton", music);
            Set(so, "_musicText", musicText);
            Set(so, "_effectsButton", effects);
            Set(so, "_effectsText", effectsText);
            SetColor(so, "_onColor", _theme.Primary);
            SetColor(so, "_offColor", _theme.Disabled);
            so.ApplyModifiedPropertiesWithoutUndo();

            AddPopupAnimator(root, panel);
            root.SetActive(false);
            return popup;
        }

        // Строка настроек: подпись слева, переключатель «Вкл/Выкл» справа; y — смещение от центра панели.
        private static Button BuildSettingsRow(Transform panel, string name, string labelKey, float y, out TMP_Text stateText)
        {
            TMP_Text label = AddLocalizedText(panel, name + "Label", labelKey, 44, DarkBrown, bold: true);
            label.alignment = TextAlignmentOptions.Left;
            label.enableAutoSizing = true;
            label.fontSizeMin = 26f;
            label.fontSizeMax = 44f;
            SetRect(label.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(60f, y), new Vector2(420f, 80f));

            GameObject toggle = CreateUi(name + "Toggle", panel);
            SetRect(toggle, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-60f, y), new Vector2(240f, 100f));
            Button button = AddSoftButton(toggle, ButtonStyle.Primary, SoundId.Tap);
            stateText = AddButtonLabel(toggle.transform, "Label", "", 40);
            Stretch(stateText.gameObject, new Vector2(12f, 6f), new Vector2(-12f, -6f));
            return button;
        }

        private static PrestigeConfirmPopup BuildPrestigePopup(Transform parent)
        {
            GameObject root = CreateUi("PrestigePopup", parent);
            Stretch(root, Vector2.zero, Vector2.zero);
            AddOverlay(root);
            var popup = root.AddComponent<PrestigeConfirmPopup>();

            GameObject panel = CreateUi("Panel", root.transform);
            SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 720f));
            AddCard(panel, Cream);

            TMP_Text title = AddText(panel.transform, "Title", "", 50, DarkBrown, TextAlignmentOptions.Center, bold: true, autoSize: true);
            SetRect(title.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(-80f, 130f));
            TMP_Text body = AddLocalizedText(panel.transform, "Body", "prestige.confirm_body", 32, Muted, bold: false);
            SetRect(body.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(-80f, 180f));
            TMP_Text gain = AddText(panel.transform, "Gain", "", 40, Reputation, TextAlignmentOptions.Center, bold: true, autoSize: true);
            SetRect(gain.gameObject, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -100f), new Vector2(-80f, 80f));

            Button cancel = BuildPopupButton(panel.transform, "CancelButton", ButtonStyle.Neutral, new Vector2(-195f, 60f), SoundId.Tap, out TMP_Text cancelText);
            AddLocalizedKey(cancelText, "prestige.confirm_no");
            Button confirm = BuildPopupButton(panel.transform, "ConfirmButton", ButtonStyle.Secondary, new Vector2(195f, 60f), SoundId.Unlock, out TMP_Text confirmText);
            AddLocalizedKey(confirmText, "prestige.confirm_yes");

            var so = new SerializedObject(popup);
            Set(so, "_root", root);
            Set(so, "_panel", panel.transform);
            Set(so, "_titleText", title);
            Set(so, "_gainText", gain);
            Set(so, "_confirmButton", confirm);
            Set(so, "_cancelButton", cancel);
            so.ApplyModifiedPropertiesWithoutUndo();

            AddPopupAnimator(root, panel);
            root.SetActive(false);
            return popup;
        }

        private static void AddLocalizedKey(TMP_Text text, string key)
        {
            var localized = text.gameObject.AddComponent<LocalizedText>();
            var so = new SerializedObject(localized);
            so.FindProperty("_key").stringValue = key;
            so.ApplyModifiedPropertiesWithoutUndo();
            text.text = key;
        }

        private static HudView BuildHud(Transform parent)
        {
            GameObject hud = CreateUi("Hud", parent);
            SetRect(hud, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, HudHeight));
            AddImage(hud, _theme.HudBackground, null);
            AddEdgeShadow(hud.transform, below: true);
            var hudView = hud.AddComponent<HudView>();

            GameObject coinsRow = CreateUi("CoinsRow", hud.transform);
            SetRect(coinsRow, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(0f, 110f));
            var row = coinsRow.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 20f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            coinsRow.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject coinIcon = CreateUi("CoinIcon", coinsRow.transform);
            if (_icons.Coin != null)
            {
                Image coinImage = AddImage(coinIcon, Color.white, _icons.Coin, sliced: false);
                coinImage.preserveAspect = true;
                coinImage.raycastTarget = false;
            }
            else
            {
                AddCircle(coinIcon, Gold).raycastTarget = false;
                GameObject coinInner = CreateUi("Inner", coinIcon.transform);
                SetRect(coinInner, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(54f, 54f));
                AddImage(coinInner, CoinInner, _circleSprite, sliced: false).raycastTarget = false;
            }
            var iconLayout = coinIcon.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 96f;
            iconLayout.preferredHeight = 96f;

            TMP_Text coins = AddText(coinsRow.transform, "CoinsText", "0", 84, Light, TextAlignmentOptions.Left, bold: true);
            Outlined(coins);
            coins.enableWordWrapping = false;

            // Доход/с: иконка + текст по центру; HudView прячет строку целиком, пока дохода нет.
            GameObject incomeRow = CreateUi("IncomeRow", hud.transform);
            SetRect(incomeRow, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(0f, 56f));
            var incomeLayout = incomeRow.AddComponent<HorizontalLayoutGroup>();
            incomeLayout.spacing = 10f;
            incomeLayout.childAlignment = TextAnchor.MiddleCenter;
            incomeLayout.childControlWidth = true;
            incomeLayout.childControlHeight = true;
            incomeLayout.childForceExpandWidth = false;
            incomeLayout.childForceExpandHeight = false;
            incomeRow.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            if (_icons.Income != null)
            {
                GameObject incomeIcon = CreateUi("Icon", incomeRow.transform);
                Image incomeImage = AddImage(incomeIcon, Color.white, _icons.Income, sliced: false);
                incomeImage.preserveAspect = true;
                incomeImage.raycastTarget = false;
                var incomeIconLayout = incomeIcon.AddComponent<LayoutElement>();
                incomeIconLayout.preferredWidth = 48f;
                incomeIconLayout.preferredHeight = 48f;
            }
            TMP_Text income = AddText(incomeRow.transform, "IncomeText", "", 42, LightGold, TextAlignmentOptions.Center, bold: true);
            Outlined(income);
            income.enableWordWrapping = false;

            var so = new SerializedObject(hudView);
            Set(so, "_coinsText", coins);
            Set(so, "_incomeText", income);
            Set(so, "_incomeRoot", incomeRow);
            Set(so, "_coinIcon", coinIcon.transform);

            // Купюры — справа сверху, видны всегда; по тапу — магазин.
            GameObject bills = CreateUi("Bills", hud.transform);
            SetRect(bills, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(200f, 64f));
            Button billsButton = AddSoftButton(bills, ButtonStyle.Neutral, SoundId.Tap);
            GameObject note = CreateUi("Icon", bills.transform);
            SetRect(note, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(54f, 36f));
            BillGlyph(note, 18f);
            TMP_Text billsText = AddButtonLabel(bills.transform, "Text", "0", 38);
            Stretch(billsText.gameObject, new Vector2(66f, 4f), new Vector2(-12f, -4f));
            Set(so, "_billsRoot", bills.transform);
            Set(so, "_billsText", billsText);
            Set(so, "_billsButton", billsButton);

            // Репутация — под купюрами (показывается, когда появится).
            GameObject reputation = CreateUi("Reputation", hud.transform);
            SetRect(reputation, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -94f), new Vector2(200f, 64f));
            AddImage(reputation, Brown, _pillSprite).raycastTarget = false;
            GameObject reputationIcon = CreateUi("Icon", reputation.transform);
            SetRect(reputationIcon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(4f, 0f), new Vector2(56f, 56f));
            if (_icons.Reputation != null)
                AddArt(reputationIcon.transform, "Art", _icons.Reputation);
            else
                AddCircle(reputationIcon, Reputation).raycastTarget = false;
            TMP_Text reputationText = AddButtonLabel(reputation.transform, "Text", "0", 38);
            Stretch(reputationText.gameObject, new Vector2(60f, 4f), new Vector2(-12f, -4f));
            Set(so, "_reputationRoot", reputation);
            Set(so, "_reputationText", reputationText);
            so.ApplyModifiedPropertiesWithoutUndo();
            return hudView;
        }

        private static TabBar BuildTabBar(Transform parent, out Button[] buttons)
        {
            GameObject bar = CreateUi("TabBar", parent);
            SetRect(bar, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, TabBarHeight));
            AddImage(bar, _theme.HudBackground, null);
            AddEdgeShadow(bar.transform, below: false);
            var horizontal = bar.AddComponent<HorizontalLayoutGroup>();
            // Сверху запас под приподнятую активную вкладку.
            horizontal.padding = new RectOffset(16, 16, 16 + Mathf.CeilToInt(_theme.TabRaise), 16);
            horizontal.spacing = 12f;
            horizontal.childControlWidth = true;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = true;
            horizontal.childForceExpandHeight = true;
            var tabBar = bar.AddComponent<TabBar>();

            string[] keys = { "tab.stations", "tab.managers", "tab.upgrades", "tab.city", "tab.shop" };
            Sprite[] tabIcons = { _icons.TabStations, _icons.TabManagers, _icons.TabUpgrades, _icons.TabCity, _icons.TabShop };
            buttons = new Button[keys.Length];
            var iconRects = new Object[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                // Сама вкладка стоит в layout; приподнимается её «тело» (TabBar двигает targetGraphic).
                GameObject tab = CreateUi("Tab_" + keys[i].Substring(4), bar.transform);
                GameObject body = CreateUi("Body", tab.transform);
                Stretch(body, Vector2.zero, Vector2.zero);
                buttons[i] = AddSoftButton(tab, body, Brown, SoundId.Tab);
                TMP_Text label = AddLocalizedText(body.transform, "Label", keys[i], 36, Light, bold: true);
                Outlined(label);
                if (tabIcons[i] != null)
                {
                    // Иконка сверху, подпись под ней мельче.
                    iconRects[i] = AddArtAt(body.transform, "Icon", tabIcons[i], new Vector2(0.5f, 1f), new Vector2(0f, -46f), 72f).rectTransform;
                    label.fontSizeMax = 28f;
                    SetRect(label.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(-12f, 40f));
                }
                else
                {
                    Stretch(label.gameObject, new Vector2(8f, 0f), new Vector2(-8f, 0f));
                }
            }

            var tabSo = new SerializedObject(tabBar);
            SetArray(tabSo, "_icons", iconRects);
            tabSo.ApplyModifiedPropertiesWithoutUndo();
            return tabBar;
        }

        // ---------- Retention (stage 10) ----------


        /// <summary>
        /// Удержание: кнопка «×2 доход» в HUD под шестерёнкой, календарь и задания слева сверху на экране ларька,
        /// подарок-ящик справа сверху, окна календаря, заданий и предложения рекламы.
        /// </summary>
        private static RetentionHud BuildRetention(Transform screen, Transform kioskArea, Transform hud, out DailyRewardPopup dailyPopup,
            out QuestsPopup questsPopup, out AdOfferPopup adOfferPopup)
        {
            var retention = screen.gameObject.AddComponent<RetentionHud>();
            var so = new SerializedObject(retention);

            // «×2 доход» — под шестерёнкой настроек.
            GameObject boost = CreateUi("BoostButton", hud);
            SetRect(boost, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -120f), new Vector2(240f, 66f));
            Button boostButton = AddSoftButton(boost, ButtonStyle.Reward, SoundId.Tap);
            TMP_Text boostText = AddButtonLabel(boost.transform, "Label", "×2", 34);
            Stretch(boostText.gameObject, new Vector2(10f, 4f), new Vector2(-10f, -4f));
            AddButtonIcon(boostButton, boostText, _icons.Boost, 50f);
            PulseEffect boostPulse = AddPulse(boost);
            Set(so, "_boostButton", boostButton);
            Set(so, "_boostText", boostText);
            Set(so, "_boostBackground", boostButton.targetGraphic);
            Set(so, "_boostPulse", boostPulse);
            SetColor(so, "_boostIdleColor", _theme.Reward);
            SetColor(so, "_boostActiveColor", Reputation);

            // Календарь и задания — круглые кнопки слева сверху на экране ларька.
            // С иконкой — светлая подложка и картинка; без неё — прежний значок из примитивов.
            Button calendar = BuildRoundButton(kioskArea, "CalendarButton", new Vector2(24f, -24f), _icons.Calendar != null ? Cream : Orange);
            if (_icons.Calendar != null)
            {
                AddArt(calendar.transform, "Icon", _icons.Calendar, 0.1f);
            }
            else
            {
                Glyph(calendar.transform, "Sheet", new Vector2(0f, -4f), new Vector2(64f, 58f), Paper, _roundSprite);
                Glyph(calendar.transform, "Header", new Vector2(0f, 20f), new Vector2(64f, 18f), Badge, _roundSprite);
                TMP_Text calendarDigit = AddText(calendar.transform, "Digit", "7", 34, DarkBrown, TextAlignmentOptions.Center, bold: true);
                SetRect(calendarDigit.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(60f, 44f));
            }
            Set(so, "_calendarButton", calendar);
            Set(so, "_calendarBadge", BuildTabBadge(calendar.transform));

            Button quests = BuildRoundButton(kioskArea, "QuestsButton", new Vector2(24f, -160f), _icons.Quests != null ? Cream : Reputation);
            if (_icons.Quests != null)
            {
                AddArt(quests.transform, "Icon", _icons.Quests, 0.1f);
            }
            else
            {
                Glyph(quests.transform, "Sheet", Vector2.zero, new Vector2(56f, 70f), Paper, _roundSprite);
                for (int i = 0; i < 3; i++)
                    Glyph(quests.transform, "Line_" + i, new Vector2(4f, 18f - i * 18f), new Vector2(32f, 7f), Brown, null);
                for (int i = 0; i < 3; i++)
                    Glyph(quests.transform, "Dot_" + i, new Vector2(-18f, 18f - i * 18f), new Vector2(9f, 9f), Green, _circleSprite, sliced: false);
            }
            Set(so, "_questsButton", quests);
            Set(so, "_questsBadge", BuildTabBadge(quests.transform));

            BuildGift(kioskArea, so);

            dailyPopup = BuildDailyRewardPopup(screen);
            questsPopup = BuildQuestsPopup(screen);
            adOfferPopup = BuildAdOfferPopup(screen);
            Set(so, "_dailyPopup", dailyPopup);
            Set(so, "_questsPopup", questsPopup);
            Set(so, "_adOffer", adOfferPopup);
            so.ApplyModifiedPropertiesWithoutUndo();
            return retention;
        }

        // Деталь значка: позиция — от центра родителя.
        private static Image Glyph(Transform parent, string name, Vector2 position, Vector2 size, Color color, Sprite sprite, bool sliced = true)
        {
            GameObject go = CreateUi(name, parent);
            SetRect(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
            Image image = AddImage(go, color, sprite, sliced);
            image.raycastTarget = false;
            return image;
        }

        private static Button BuildRoundButton(Transform parent, string name, Vector2 topLeft, Color color)
        {
            GameObject go = CreateUi(name, parent);
            SetRect(go, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), topLeft, new Vector2(120f, 120f));
            AddCircle(go, color);
            return AddButton(go, SoundId.Tap);
        }

        // Подарок-ящик справа сверху: коробка с лентой и бантом, бейдж «!» и таймер под ним.
        private static void BuildGift(Transform kioskArea, SerializedObject so)
        {
            GameObject gift = CreateUi("GiftButton", kioskArea);
            SetRect(gift, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(130f, 130f));
            AddImage(gift, Color.clear, null);
            Button button = AddButton(gift, SoundId.Tap);
            var group = gift.AddComponent<CanvasGroup>();
            PulseEffect pulse = AddPulse(gift);

            if (_icons.Gift != null)
            {
                AddArt(gift.transform, "Icon", _icons.Gift);
            }
            else
            {
                Glyph(gift.transform, "Box", new Vector2(0f, -22f), new Vector2(100f, 74f), GiftRed, _roundSprite);
                Glyph(gift.transform, "Lid", new Vector2(0f, 22f), new Vector2(118f, 30f), GiftRed, _roundSprite);
                Glyph(gift.transform, "RibbonV", new Vector2(0f, -10f), new Vector2(20f, 104f), Gold, null);
                Glyph(gift.transform, "RibbonH", new Vector2(0f, 22f), new Vector2(118f, 12f), Gold, null);
                Glyph(gift.transform, "BowL", new Vector2(-18f, 46f), new Vector2(34f, 26f), Gold, _circleSprite, sliced: false);
                Glyph(gift.transform, "BowR", new Vector2(18f, 46f), new Vector2(34f, 26f), Gold, _circleSprite, sliced: false);
            }

            TMP_Text timer = AddText(gift.transform, "Timer", "9:59", 34, Light, TextAlignmentOptions.Center, bold: true);
            Outlined(timer);
            SetRect(timer.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(170f, 44f));
            timer.enableWordWrapping = false;
            timer.gameObject.SetActive(false);

            Set(so, "_giftButton", button);
            Set(so, "_giftGroup", group);
            Set(so, "_giftPulse", pulse);
            Set(so, "_giftBadge", BuildTabBadge(gift.transform));
            Set(so, "_giftTimerText", timer);
        }

        /// <summary>Значок награды: кружок с меткой (цвет — по виду награды) и «+X». Прямоугольники — в координатах родителя.</summary>
        private static RewardIconView BuildRewardIcon(Transform parent, Vector2 iconAnchor, Vector2 iconPosition, float iconSize,
            Vector2 textAnchorMin, Vector2 textAnchorMax, Vector2 textPivot, Vector2 textPosition, Vector2 textSize, float fontSize,
            TextAlignmentOptions alignment)
        {
            GameObject root = CreateUi("Reward", parent);
            Stretch(root, Vector2.zero, Vector2.zero);
            var view = root.AddComponent<RewardIconView>();

            GameObject icon = CreateUi("Icon", root.transform);
            SetRect(icon, iconAnchor, iconAnchor, new Vector2(0.5f, 0.5f), iconPosition, new Vector2(iconSize, iconSize));
            Image iconImage = AddImage(icon, Gold, _circleSprite, sliced: false);
            iconImage.raycastTarget = false;
            GameObject mark = CreateUi("Mark", icon.transform);
            SetRect(mark, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(iconSize * 0.55f, iconSize * 0.55f));
            Image markImage = AddImage(mark, CoinInner, _circleSprite, sliced: false);
            markImage.raycastTarget = false;
            // Нарисованный значок (монета, купюры, звезда) поверх кружка; кружок прячет RewardIconView.
            Image art = AddArt(icon.transform, "Art", null, -0.1f);

            TMP_Text amount = AddText(root.transform, "Amount", "+0", fontSize, DarkBrown, alignment, bold: true, autoSize: true);
            SetRect(amount.gameObject, textAnchorMin, textAnchorMax, textPivot, textPosition, textSize);

            var so = new SerializedObject(view);
            Set(so, "_icon", iconImage);
            Set(so, "_mark", markImage);
            Set(so, "_amountText", amount);
            Set(so, "_art", art);
            Set(so, "_coinSprite", _icons.Coin);
            Set(so, "_billSprite", _icons.Bill);
            Set(so, "_reputationSprite", _icons.Reputation);
            SetColor(so, "_coinColor", Gold);
            SetColor(so, "_coinMark", CoinInner);
            SetColor(so, "_billColor", BillGreen);
            SetColor(so, "_billMark", BillLight);
            SetColor(so, "_reputationColor", Reputation);
            SetColor(so, "_reputationMark", _theme.ReputationLight);
            SetColor(so, "_skinColor", Gold);
            SetColor(so, "_skinMark", Badge);
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static GameObject BuildPopupRoot(Transform parent, string name, Vector2 panelSize, string titleKey, out GameObject panel)
        {
            GameObject root = CreateUi(name, parent);
            Stretch(root, Vector2.zero, Vector2.zero);
            AddOverlay(root);

            panel = CreateUi("Panel", root.transform);
            SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, panelSize);
            AddCard(panel, Cream);
            AddPopupAnimator(root, panel);

            if (titleKey != null)
            {
                TMP_Text title = AddLocalizedText(panel.transform, "Title", titleKey, 54, DarkBrown, bold: true);
                SetRect(title.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(-80f, 80f));
            }
            return root;
        }

        private const float DailyTileWidth = 190f;
        private const float DailyTileHeight = 230f;

        private static DailyRewardPopup BuildDailyRewardPopup(Transform parent)
        {
            GameObject root = BuildPopupRoot(parent, "DailyRewardPopup", new Vector2(900f, 940f), "retention.daily_title", out GameObject panel);
            var popup = root.AddComponent<DailyRewardPopup>();

            var tiles = new Object[7];
            float step = DailyTileWidth + 16f;
            for (int i = 0; i < tiles.Length; i++)
            {
                bool firstRow = i < 4;
                int column = firstRow ? i : i - 4;
                int columns = firstRow ? 4 : 3;
                float x = (column - (columns - 1) * 0.5f) * step;
                float y = firstRow ? -140f : -140f - DailyTileHeight - 16f;
                tiles[i] = BuildDailyTile(panel.transform, i, new Vector2(x, y));
            }

            TMP_Text status = AddText(panel.transform, "Status", "", 36, Muted, TextAlignmentOptions.Center, bold: true, autoSize: true);
            SetRect(status.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -640f), new Vector2(-80f, 60f));

            Button claim = BuildPopupButton(panel.transform, "ClaimButton", ButtonStyle.Primary, new Vector2(-195f, 50f), SoundId.Buy, out TMP_Text claimText);
            AddLocalizedKey(claimText, "retention.claim");
            AddAvailableGlow(claim);
            Button close = BuildPopupButton(panel.transform, "CloseButton", ButtonStyle.Neutral, new Vector2(195f, 50f), SoundId.Tap, out TMP_Text closeText);
            AddButtonIcon(close, closeText, _icons.Close, 56f);
            AddLocalizedKey(closeText, "settings.close");

            var so = new SerializedObject(popup);
            Set(so, "_root", root);
            Set(so, "_panel", panel.transform);
            SetArray(so, "_tiles", tiles);
            Set(so, "_statusText", status);
            Set(so, "_claimButton", claim);
            Set(so, "_closeButton", close);
            so.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);
            return popup;
        }

        private static DailyRewardTileView BuildDailyTile(Transform panel, int index, Vector2 topCenter)
        {
            GameObject tile = CreateUi("Day_" + (index + 1), panel);
            SetRect(tile, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), topCenter, new Vector2(DailyTileWidth, DailyTileHeight));
            Image background = AddCard(tile, Paper);
            background.raycastTarget = false;
            var view = tile.AddComponent<DailyRewardTileView>();

            TMP_Text day = AddText(tile.transform, "Day", "", 32, DarkBrown, TextAlignmentOptions.Center, bold: true, autoSize: true);
            SetRect(day.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(-16f, 46f));

            RewardIconView reward = BuildRewardIcon(tile.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), 84f,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(-16f, 52f), 36f,
                TextAlignmentOptions.Center);

            // Галочка «получено»: зелёный кружок с белым уголком из двух полосок.
            GameObject check = CreateUi("Check", tile.transform);
            if (_icons.Check != null)
            {
                SetRect(check, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-28f, -28f), new Vector2(72f, 72f));
                AddArt(check.transform, "Icon", _icons.Check);
            }
            else
            {
                SetRect(check, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-24f, -24f), new Vector2(52f, 52f));
                AddCircle(check, Green).raycastTarget = false;
                Image shortStroke = Glyph(check.transform, "Short", new Vector2(-8f, -3f), new Vector2(7f, 18f), Light, null);
                shortStroke.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                Image longStroke = Glyph(check.transform, "Long", new Vector2(5f, 2f), new Vector2(7f, 30f), Light, null);
                longStroke.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -40f);
            }
            check.SetActive(false);

            var so = new SerializedObject(view);
            Set(so, "_background", background);
            Set(so, "_dayText", day);
            Set(so, "_reward", reward);
            Set(so, "_check", check);
            SetColor(so, "_claimedColor", _theme.Disabled);
            SetColor(so, "_todayColor", Gold);
            SetColor(so, "_futureColor", Paper);
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private const float QuestRowHeight = 210f;

        private static QuestsPopup BuildQuestsPopup(Transform parent)
        {
            GameObject root = BuildPopupRoot(parent, "QuestsPopup", new Vector2(940f, 1060f), "retention.quests_title", out GameObject panel);
            var popup = root.AddComponent<QuestsPopup>();

            var rows = new Object[3];
            for (int i = 0; i < rows.Length; i++)
                rows[i] = BuildQuestRow(panel.transform, i, -140f - i * (QuestRowHeight + 20f));

            TMP_Text footer = AddText(panel.transform, "Footer", "", 34, Muted, TextAlignmentOptions.Center, bold: true, autoSize: true);
            SetRect(footer.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -835f), new Vector2(-80f, 54f));

            Button close = BuildPopupButton(panel.transform, "CloseButton", ButtonStyle.Neutral, new Vector2(0f, 40f), SoundId.Tap, out TMP_Text closeText);
            AddButtonIcon(close, closeText, _icons.Close, 56f);
            AddLocalizedKey(closeText, "settings.close");

            var so = new SerializedObject(popup);
            Set(so, "_root", root);
            Set(so, "_panel", panel.transform);
            SetArray(so, "_rows", rows);
            Set(so, "_footerText", footer);
            Set(so, "_closeButton", close);
            so.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);
            return popup;
        }

        private static QuestRowView BuildQuestRow(Transform panel, int index, float top)
        {
            GameObject row = CreateUi("Quest_" + index, panel);
            SetRect(row, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, top), new Vector2(-60f, QuestRowHeight));
            AddCard(row, RowColor).raycastTarget = false;
            var view = row.AddComponent<QuestRowView>();

            TMP_Text description = AddText(row.transform, "Description", "", 40, DarkBrown, TextAlignmentOptions.Left, bold: true, autoSize: true);
            SetRect(description.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(26f, -18f), new Vector2(-330f, 54f));

            GameObject bar = CreateUi("ProgressBar", row.transform);
            SetRect(bar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(26f, -84f), new Vector2(-330f, 40f));
            RectTransform fillRect = AddProgressFill(bar, Green);
            bar.GetComponent<Image>().raycastTarget = false;
            TMP_Text progress = AddText(bar.transform, "Progress", "0/0", 28, DarkBrown, TextAlignmentOptions.Center, bold: true, autoSize: true);
            Stretch(progress.gameObject, new Vector2(8f, 2f), new Vector2(-8f, -2f));

            RewardIconView reward = BuildRewardIcon(row.transform, new Vector2(0f, 0f), new Vector2(56f, 44f), 56f,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0.5f), new Vector2(96f, 44f), new Vector2(360f, 50f), 36f,
                TextAlignmentOptions.Left);

            Button button = BuildRowButton(row.transform, "ClaimButton", ButtonStyle.Primary, new Vector2(270f, 120f), SoundId.Buy, out TMP_Text buttonText);

            var so = new SerializedObject(view);
            Set(so, "_descriptionText", description);
            Set(so, "_progressText", progress);
            Set(so, "_progressFill", fillRect);
            Set(so, "_reward", reward);
            Set(so, "_button", button);
            Set(so, "_buttonText", buttonText);
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static AdOfferPopup BuildAdOfferPopup(Transform parent)
        {
            GameObject root = BuildPopupRoot(parent, "AdOfferPopup", new Vector2(860f, 600f), null, out GameObject panel);
            var popup = root.AddComponent<AdOfferPopup>();

            TMP_Text title = AddText(panel.transform, "Title", "", 52, DarkBrown, TextAlignmentOptions.Center, bold: true, autoSize: true);
            SetRect(title.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(-80f, 80f));
            TMP_Text body = AddText(panel.transform, "Body", "", 38, DarkBrown, TextAlignmentOptions.Center, autoSize: true);
            SetRect(body.gameObject, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(-80f, 220f));

            Button confirm = BuildPopupButton(panel.transform, "ConfirmButton", ButtonStyle.Reward, new Vector2(-195f, 50f), SoundId.Tap, out TMP_Text confirmText);
            AddButtonIcon(confirm, confirmText, _icons.Ad, 64f);
            AddAvailableGlow(confirm);
            Button cancel = BuildPopupButton(panel.transform, "CancelButton", ButtonStyle.Neutral, new Vector2(195f, 50f), SoundId.Tap, out TMP_Text cancelText);
            AddLocalizedKey(cancelText, "bills.offer_cancel");

            var so = new SerializedObject(popup);
            Set(so, "_root", root);
            Set(so, "_panel", panel.transform);
            Set(so, "_titleText", title);
            Set(so, "_bodyText", body);
            Set(so, "_confirmButton", confirm);
            Set(so, "_confirmText", confirmText);
            Set(so, "_cancelButton", cancel);
            so.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);
            return popup;
        }

        // ---------- Helpers ----------

        // ---------- Onboarding (stage 11) ----------

        /// <summary>
        /// Рука-подсказка поверх всего экрана: кольцо-«тап», рука (pivot — кончик пальца) и облачко с подписью.
        /// Без raycastTarget — не перехватывает нажатия. Окна из blockers скрывают её, пока открыты.
        /// </summary>
        private static TutorialHand BuildTutorialHand(Transform screen, params Component[] blockers)
        {
            GameObject overlay = CreateUi("TutorialHand", screen);
            Stretch(overlay, Vector2.zero, Vector2.zero);
            var view = overlay.AddComponent<TutorialHand>();

            GameObject root = CreateUi("Root", overlay.transform);
            Stretch(root, Vector2.zero, Vector2.zero);

            GameObject ring = CreateUi("Ring", root.transform);
            SetRect(ring, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 150f));
            AddImage(ring, UiTheme.WithAlpha(Orange, 0.55f), _circleSprite, sliced: false).raycastTarget = false;
            var ringGroup = ring.AddComponent<CanvasGroup>();
            ringGroup.blocksRaycasts = false;
            ringGroup.interactable = false;

            GameObject hand = CreateUi("Hand", root.transform);
            SetRect(hand, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(10f, 10f));
            GameObject shape = CreateUi("Shape", hand.transform);
            SetRect(shape, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(10f, 10f));
            shape.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
            if (_icons.TutorialHand != null)
            {
                // tutorial_hand указывает пальцем вниз; кончик пальца — в точке (0.58, 0.06) картинки.
                // Pivot ставим в кончик и разворачиваем на 180°: палец вверх, ровно в точке подсказки.
                GameObject art = CreateUi("Art", shape.transform);
                SetRect(art, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.58f, 0.06f), Vector2.zero, new Vector2(260f, 260f));
                art.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
                Image handImage = AddImage(art, Color.white, _icons.TutorialHand, sliced: false);
                handImage.preserveAspect = true;
                handImage.raycastTarget = false;
            }
            else
            {
                HandPart(shape.transform, "Palm", new Vector2(26f, -172f), new Vector2(130f, 120f), 0f);
                HandPart(shape.transform, "Thumb", new Vector2(-46f, -150f), new Vector2(44f, 92f), 35f);
                HandPart(shape.transform, "Knuckle1", new Vector2(36f, -112f), new Vector2(40f, 64f), 0f);
                HandPart(shape.transform, "Knuckle2", new Vector2(70f, -118f), new Vector2(38f, 58f), 0f);
                HandPart(shape.transform, "Knuckle3", new Vector2(100f, -128f), new Vector2(34f, 50f), 0f);
                HandPart(shape.transform, "Finger", new Vector2(0f, -66f), new Vector2(46f, 132f), 0f);
                Image cuff = HandPart(shape.transform, "Cuff", new Vector2(30f, -244f), new Vector2(120f, 34f), 0f);
                cuff.color = Orange;
            }

            GameObject bubble = CreateUi("Bubble", root.transform);
            SetRect(bubble, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600f, 110f));
            AddCard(bubble, Cream).raycastTarget = false;
            TMP_Text hint = AddText(bubble.transform, "Hint", "tutorial.tap", 40, DarkBrown, TextAlignmentOptions.Center, bold: true, autoSize: true);
            Stretch(hint.gameObject, new Vector2(24f, 10f), new Vector2(-24f, -10f));

            var blockerRoots = new List<Object>();
            foreach (Component blocker in blockers)
            {
                if (blocker == null)
                    continue;
                Object popupRoot = new SerializedObject(blocker).FindProperty("_root")?.objectReferenceValue;
                if (popupRoot != null)
                    blockerRoots.Add(popupRoot);
            }

            var so = new SerializedObject(view);
            Set(so, "_root", root);
            Set(so, "_hand", hand.transform);
            Set(so, "_ring", ring.transform);
            Set(so, "_ringGroup", ringGroup);
            Set(so, "_bubble", bubble.transform);
            Set(so, "_hintText", hint);
            SetArray(so, "_blockers", blockerRoots.ToArray());
            so.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);
            overlay.transform.SetAsLastSibling();
            return view;
        }

        private static Image HandPart(Transform parent, string name, Vector2 position, Vector2 size, float angle)
        {
            Image part = Glyph(parent, name, position, size, Light, _roundSprite);
            part.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            var outline = part.gameObject.AddComponent<Outline>();
            outline.effectColor = _theme.Outline;
            outline.effectDistance = new Vector2(3f, -3f);
            return part;
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                    return root;
            }
            return null;
        }

        private static GameObject CreateUi(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            if (parent != null)
                go.transform.SetParent(parent, false);
            return go;
        }

        private static void SetRect(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(GameObject go, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static Image AddImage(GameObject go, Color color, Sprite sprite, bool sliced = true)
        {
            var image = go.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            if (sprite != null && sliced)
            {
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 1f;
            }
            return image;
        }

        /// <summary>Карточка (строка, панель попапа): заливка с нижней полосой тени и тонкая мягкая обводка.</summary>
        private static Image AddCard(GameObject go, Color color)
        {
            Image image = AddImage(go, color, _cardSprite);
            GameObject outline = CreateUi("CardOutline", go.transform);
            Stretch(outline, Vector2.zero, Vector2.zero);
            AddImage(outline, UiTheme.WithAlpha(_theme.Outline, _theme.CardOutlineAlpha), _outlineSprite).raycastTarget = false;
            return image;
        }

        // Кружок с обводкой (значки, круглые кнопки). Обводка — первый ребёнок, поверх неё рисуется содержимое.
        private static Image AddCircle(GameObject go, Color color)
        {
            Image image = AddImage(go, color, _circleSprite, sliced: false);
            GameObject outline = CreateUi("CircleOutline", go.transform);
            Stretch(outline, Vector2.zero, Vector2.zero);
            AddImage(outline, _theme.Outline, _circleOutlineSprite, sliced: false).raycastTarget = false;
            return image;
        }

        private static Button AddButton(GameObject go, SoundId sound)
        {
            var button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            AddFeedback(go, sound, 0.94f);
            return button;
        }

        private static void AddFeedback(GameObject go, SoundId sound, float pressedScale)
        {
            var feedback = go.AddComponent<ButtonFeedback>();
            var so = new SerializedObject(feedback);
            so.FindProperty("_sound").enumValueIndex = (int)sound;
            so.FindProperty("_pressedScale").floatValue = pressedScale;
            so.FindProperty("_pressDuration").floatValue = _theme.PressDuration;
            so.FindProperty("_releaseDuration").floatValue = _theme.ReleaseDuration;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Объёмная кнопка на go: лицо с бортиком, обводка, слой «недоступно». Подписи добавлять после.</summary>
        private static Button AddSoftButton(GameObject go, ButtonStyle style, SoundId sound)
        {
            return AddSoftButton(go, go, StyleColor(style), sound);
        }

        /// <param name="face">Объект с лицом кнопки: сама кнопка или её дочернее «тело» (вкладки).</param>
        private static Button AddSoftButton(GameObject go, GameObject face, Color color, SoundId sound)
        {
            Image faceImage = AddImage(face, color, _buttonSprite);

            GameObject overlay = CreateUi("DisabledOverlay", face.transform);
            Stretch(overlay, Vector2.zero, Vector2.zero);
            AddImage(overlay, _theme.Disabled, _buttonSprite).raycastTarget = false;
            overlay.SetActive(false);

            GameObject outline = CreateUi("Outline", face.transform);
            Stretch(outline, Vector2.zero, Vector2.zero);
            AddImage(outline, _theme.Outline, _outlineSprite).raycastTarget = false;

            var button = go.AddComponent<SoftButton>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = faceImage;
            var so = new SerializedObject(button);
            Set(so, "_normalSprite", _buttonSprite);
            Set(so, "_pressedSprite", _buttonPressedSprite);
            SetArray(so, "_decor", outline.transform);
            Set(so, "_disabledOverlay", overlay);
            so.FindProperty("_disabledTextColor").colorValue = _theme.TextMuted;
            so.FindProperty("_pressOffset").floatValue = _theme.PressOffset;
            so.FindProperty("_pressDuration").floatValue = _theme.PressDuration;
            so.FindProperty("_releaseDuration").floatValue = _theme.ReleaseDuration;
            so.ApplyModifiedPropertiesWithoutUndo();

            AddFeedback(go, sound, _theme.PressScale);
            return button;
        }

        // ---------- Icons (V2) ----------

        /// <summary>Картинка-иконка внутри parent с полями inset (доля размера); без спрайта выключена.</summary>
        private static Image AddArt(Transform parent, string name, Sprite sprite, float inset = 0f)
        {
            GameObject go = CreateUi(name, parent);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(inset, inset);
            rect.anchorMax = new Vector2(1f - inset, 1f - inset);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = AddImage(go, Color.white, sprite, sliced: false);
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = sprite != null;
            return image;
        }

        /// <summary>Иконка размера size с центром в точке anchor родителя (+ смещение).</summary>
        private static Image AddArtAt(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 position, float size)
        {
            GameObject go = CreateUi(name, parent);
            SetRect(go, anchor, anchor, new Vector2(0.5f, 0.5f), position, new Vector2(size, size));
            Image image = AddImage(go, Color.white, sprite, sliced: false);
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Иконка слева на кнопке с подписью: подпись сдвигается вправо. Без спрайта ничего не делает.</summary>
        private static void AddButtonIcon(Button button, TMP_Text label, Sprite sprite, float size)
        {
            if (sprite == null)
                return;
            const float pad = 14f;
            AddArtAt(button.targetGraphic.transform, "Icon", sprite, new Vector2(0f, 0.5f), new Vector2(pad + size * 0.5f, 3f), size);
            RectTransform rect = label.rectTransform;
            rect.offsetMin = new Vector2(rect.offsetMin.x + size + pad * 0.5f, rect.offsetMin.y);
        }

        // Подпись на цветной кнопке: светлая, с тёмной обводкой.
        private static TMP_Text AddButtonLabel(Transform parent, string name, string text, float size)
        {
            TMP_Text label = AddText(parent, name, text, size, Light, TextAlignmentOptions.Center, bold: true, autoSize: true);
            Outlined(label);
            return label;
        }

        private static void Outlined(TMP_Text text)
        {
            if (_outlinedMaterial != null)
                text.fontSharedMaterial = _outlinedMaterial;
        }

        /// <summary>
        /// Мягкое свечение вокруг кнопки, пока она доступна (вместо пульса у кнопок в строках).
        /// Не опускается вместе с содержимым при нажатии — добавляется в декор SoftButton.
        /// </summary>
        private static void AddAvailableGlow(Button button)
        {
            const float spread = 18f;
            GameObject glow = CreateUi("AvailableGlow", button.transform);
            Stretch(glow, new Vector2(-spread, -spread), new Vector2(spread, spread));
            Image glowImage = AddImage(glow, _theme.Glow, _glowSprite);
            glowImage.raycastTarget = false;

            if (button is SoftButton)
            {
                var buttonSo = new SerializedObject(button);
                SerializedProperty decor = buttonSo.FindProperty("_decor");
                decor.arraySize++;
                decor.GetArrayElementAtIndex(decor.arraySize - 1).objectReferenceValue = glow.transform;
                buttonSo.ApplyModifiedPropertiesWithoutUndo();
            }

            var effect = button.gameObject.AddComponent<AvailableGlow>();
            var so = new SerializedObject(effect);
            Set(so, "_button", button);
            Set(so, "_glow", glowImage);
            so.FindProperty("_minAlpha").floatValue = _theme.GlowMinAlpha;
            so.FindProperty("_maxAlpha").floatValue = _theme.GlowMaxAlpha;
            so.FindProperty("_period").floatValue = _theme.GlowPeriod;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddPulseWhileInteractable(GameObject go, Button button)
        {
            PulseEffect pulse = AddPulse(go);
            var so = new SerializedObject(pulse);
            Set(so, "_whileInteractable", button);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static PulseEffect AddPulse(GameObject go)
        {
            var pulse = go.AddComponent<PulseEffect>();
            var so = new SerializedObject(pulse);
            so.FindProperty("_amplitude").floatValue = _theme.PulseAmplitude;
            so.FindProperty("_speed").floatValue = _theme.PulseSpeed;
            so.ApplyModifiedPropertiesWithoutUndo();
            return pulse;
        }

        // Корень попапа = затемнение на весь экран.
        private static void AddOverlay(GameObject root)
        {
            AddImage(root, _theme.Overlay, null);
        }

        private static void AddPopupAnimator(GameObject root, GameObject panel)
        {
            var animator = root.AddComponent<PopupAnimator>();
            var so = new SerializedObject(animator);
            Set(so, "_panel", panel.transform);
            Set(so, "_overlay", root.GetComponent<Image>());
            Set(so, "_panelGroup", panel.AddComponent<CanvasGroup>());
            so.FindProperty("_overlayAlpha").floatValue = _theme.Overlay.a;
            so.FindProperty("_showDuration").floatValue = _theme.PopupShowDuration;
            so.FindProperty("_hideDuration").floatValue = _theme.PopupHideDuration;
            so.FindProperty("_overlayFade").floatValue = _theme.PopupOverlayFade;
            so.FindProperty("_startScale").floatValue = _theme.PopupStartScale;
            so.FindProperty("_startOffset").floatValue = _theme.PopupStartOffset;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Мягкая тень у края панели (низ HUD, верх вкладок): ребёнок панели, выступает наружу и тает.
        private static void AddEdgeShadow(Transform panel, bool below)
        {
            const float height = 28f;
            GameObject shadow = CreateUi("EdgeShadow", panel);
            // У панели вкладок HorizontalLayoutGroup: без ignoreLayout тень заняла бы место первой вкладки.
            shadow.AddComponent<LayoutElement>().ignoreLayout = true;
            float y = below ? 0f : 1f;
            SetRect(shadow, new Vector2(0f, y), new Vector2(1f, y), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, height));
            // Градиент тёмный сверху; для тени над вкладками переворачиваем его.
            if (!below)
                shadow.transform.localScale = new Vector3(1f, -1f, 1f);
            Image image = AddImage(shadow, UiTheme.WithAlpha(_theme.Outline, 0.35f), _edgeShadowSprite, sliced: false);
            image.raycastTarget = false;
        }

        // Прогресс-бар: капсула-фон и заливка, ширину заливки меняет вью через anchorMax.x.
        private static RectTransform AddProgressFill(GameObject bar, Color fillColor)
        {
            AddImage(bar, BarBack, _pillSprite);
            GameObject fill = CreateUi("Fill", bar.transform);
            var fillRect = (RectTransform)fill.transform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            AddImage(fill, fillColor, _pillSprite).raycastTarget = false;
            return fillRect;
        }

        private static void SetColor(SerializedObject so, string field, Color color)
        {
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"[Build Game UI] Field '{field}' not found on {so.targetObject.GetType().Name}");
                return;
            }
            property.colorValue = color;
        }

        private static TMP_Text AddText(Transform parent, string name, string text, float size, Color color,
            TextAlignmentOptions alignment, bool bold = false, bool autoSize = false)
        {
            GameObject go = CreateUi(name, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = _font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            tmp.raycastTarget = false;
            if (autoSize)
            {
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = 18f;
                tmp.fontSizeMax = size;
            }
            return tmp;
        }

        private static TMP_Text AddLocalizedText(Transform parent, string name, string key, float size, Color color, bool bold)
        {
            TMP_Text text = AddText(parent, name, key, size, color, TextAlignmentOptions.Center, bold, autoSize: true);
            var localized = text.gameObject.AddComponent<LocalizedText>();
            var so = new SerializedObject(localized);
            so.FindProperty("_key").stringValue = key;
            so.ApplyModifiedPropertiesWithoutUndo();
            return text;
        }

        private static void Set(SerializedObject so, string field, Object value)
        {
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"[Build Game UI] Field '{field}' not found on {so.targetObject.GetType().Name}");
                return;
            }
            property.objectReferenceValue = value;
        }

        private static void SetArray(SerializedObject so, string field, params Object[] values)
        {
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"[Build Game UI] Field '{field}' not found on {so.targetObject.GetType().Name}");
                return;
            }

            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
