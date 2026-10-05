using System.Collections.Generic;
using System.IO;
using ShaurmaTycoon.Core;
using ShaurmaTycoon.Core.Audio;
using ShaurmaTycoon.Customers;
using ShaurmaTycoon.Localization;
using ShaurmaTycoon.Prestige;
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
        private const float CityCardHeight = 910f;
        private const float MapHeight = 560f;
        private const int KioskDecorCount = 6;
        private const float GroundRatio = 0.2f;
        private const int CustomerPoolSize = 9;
        private const int CoinPoolSize = 24;
        private const int FloatingTextPoolSize = 12;

        private static readonly Color Cream = new Color(1f, 0.95f, 0.88f);
        private static readonly Color RowColor = new Color(1f, 0.99f, 0.96f);
        private static readonly Color DarkBrown = new Color(0.36f, 0.18f, 0.07f);
        private static readonly Color Brown = new Color(0.45f, 0.25f, 0.12f);
        private static readonly Color Orange = new Color(0.95f, 0.55f, 0.15f);
        private static readonly Color Sky = new Color(1f, 0.87f, 0.68f);
        private static readonly Color Pavement = new Color(0.78f, 0.60f, 0.45f);
        private static readonly Color Curb = new Color(0.62f, 0.45f, 0.32f);
        private static readonly Color BoothRed = new Color(0.85f, 0.30f, 0.20f);
        private static readonly Color WindowDark = new Color(0.25f, 0.14f, 0.09f);
        private static readonly Color Wood = new Color(0.60f, 0.38f, 0.20f);
        private static readonly Color Meat = new Color(0.62f, 0.36f, 0.16f);
        private static readonly Color MeatDark = new Color(0.45f, 0.24f, 0.10f);
        private static readonly Color AwningWhite = new Color(0.98f, 0.96f, 0.92f);
        private static readonly Color Steel = new Color(0.62f, 0.62f, 0.66f);
        private static readonly Color CoinInner = new Color(1f, 0.65f, 0.10f);
        private static readonly Color FloatText = new Color(0.85f, 0.42f, 0.05f);
        private static readonly Color Reputation = new Color(0.55f, 0.35f, 0.85f);
        private static readonly Color Green = new Color(0.30f, 0.69f, 0.31f);
        private static readonly Color BarBack = new Color(0.36f, 0.18f, 0.07f, 0.25f);
        private static readonly Color Gold = new Color(1f, 0.80f, 0.20f);
        private static readonly Color LightGold = new Color(1f, 0.90f, 0.60f);
        private static readonly Color Muted = new Color(0.45f, 0.35f, 0.28f);
        private static readonly Color LeafDark = new Color(0.20f, 0.52f, 0.24f);
        private static readonly Color BillGreen = new Color(0.25f, 0.62f, 0.33f);
        private static readonly Color BillLight = new Color(0.72f, 0.90f, 0.70f);
        private static readonly Color Land = new Color(0.85f, 0.91f, 0.70f);
        private static readonly Color Hill = new Color(0.76f, 0.85f, 0.58f);
        private static readonly Color Water = new Color(0.62f, 0.82f, 0.95f);

        private static Sprite _roundSprite;
        private static Sprite _circleSprite;
        private static TMP_FontAsset _font;

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
            _roundSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            _circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

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

            AssignInstallerConfigs(scene, audioConfig, musicConfig, customerConfig, shopConfig, billShopConfig);

            StationRowView rowPrefab = BuildRowPrefab();
            BuildScreen(canvas, rowPrefab, BuildManagerRowPrefab(), BuildUpgradeRowPrefab(), BuildShopRowPrefab());

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Build Game UI] Done");
        }

        private static void AssignInstallerConfigs(Scene scene, AudioConfig audio, MusicConfig music, CustomerConfig customers,
            ReputationShopConfig shop, BillShopConfig billShop)
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
            AddImage(root, RowColor, _roundSprite);
            root.AddComponent<LayoutElement>().preferredHeight = RowHeight;
            var view = root.AddComponent<StationRowView>();

            // Иконка-кнопка слева.
            GameObject icon = CreateUi("IconButton", root.transform);
            SetRect(icon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(180f, 180f));
            Image iconImage = AddImage(icon, Orange, _circleSprite, sliced: false);
            Button iconButton = AddButton(icon, SoundId.Tap);
            TMP_Text iconLabel = AddText(icon.transform, "IconLabel", "1", 80, Color.white, TextAlignmentOptions.Center, bold: true);
            Stretch(iconLabel.gameObject, new Vector2(0f, 30f), Vector2.zero);

            GameObject badge = CreateUi("LevelBadge", icon.transform);
            SetRect(badge, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(150f, 50f));
            AddImage(badge, DarkBrown, _roundSprite);
            TMP_Text levelText = AddText(badge.transform, "LevelText", "Ур. 1", 30, Color.white, TextAlignmentOptions.Center, bold: true, autoSize: true);
            Stretch(levelText.gameObject, new Vector2(8f, 0f), new Vector2(-8f, 0f));

            // Название, прогресс, веха — по центру.
            TMP_Text nameText = AddText(root.transform, "NameText", "Station", 42, DarkBrown, TextAlignmentOptions.Left, bold: true, autoSize: true);
            SetRect(nameText.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(220f, -16f), new Vector2(-490f, 56f));

            GameObject bar = CreateUi("ProgressBar", root.transform);
            SetRect(bar, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(220f, -4f), new Vector2(-490f, 72f));
            AddImage(bar, BarBack, _roundSprite);
            Button barButton = AddButton(bar, SoundId.Tap);
            PulseEffect tapPulse = bar.AddComponent<PulseEffect>();

            GameObject fill = CreateUi("Fill", bar.transform);
            var fillRect = (RectTransform)fill.transform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            AddImage(fill, Gold, _roundSprite).raycastTarget = false;

            TMP_Text incomeText = AddText(bar.transform, "IncomeText", "+1", 34, DarkBrown, TextAlignmentOptions.Left, bold: true, autoSize: true);
            Stretch(incomeText.gameObject, new Vector2(18f, 0f), new Vector2(-130f, 0f));
            TMP_Text cycleText = AddText(bar.transform, "CycleTimeText", "0.6 с", 28, DarkBrown, TextAlignmentOptions.Right, autoSize: true);
            SetRect(cycleText.gameObject, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(120f, 0f));

            TMP_Text milestoneText = AddText(root.transform, "MilestoneText", "", 28, Muted, TextAlignmentOptions.Left, autoSize: true);
            SetRect(milestoneText.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(220f, 14f), new Vector2(-490f, 40f));

            // Кнопка покупки справа.
            GameObject buy = CreateUi("BuyButton", root.transform);
            SetRect(buy, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(230f, 180f));
            AddImage(buy, Green, _roundSprite);
            Button buyButton = AddButton(buy, SoundId.Buy);
            TMP_Text buyCount = AddText(buy.transform, "BuyCountText", "×1", 34, Color.white, TextAlignmentOptions.Center, bold: true, autoSize: true);
            SetRect(buyCount.gameObject, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-20f, 0f));
            TMP_Text buyCost = AddText(buy.transform, "BuyCostText", "4", 42, Color.white, TextAlignmentOptions.Center, bold: true, autoSize: true);
            SetRect(buyCost.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-20f, 0f));

            // Оверлей закрытой станции.
            GameObject locked = CreateUi("Locked", root.transform);
            Stretch(locked, Vector2.zero, Vector2.zero);
            AddImage(locked, new Color(0.25f, 0.13f, 0.05f, 0.88f), _roundSprite);
            TMP_Text lockedName = AddText(locked.transform, "LockedNameText", "Station", 42, Color.white, TextAlignmentOptions.Left, bold: true, autoSize: true);
            SetRect(lockedName.gameObject, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(-400f, -40f));
            GameObject unlock = CreateUi("UnlockButton", locked.transform);
            SetRect(unlock, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(300f, 170f));
            AddImage(unlock, Orange, _roundSprite);
            Button unlockButton = AddButton(unlock, SoundId.Unlock);
            AddPulseWhileInteractable(unlock, unlockButton);
            TMP_Text unlockCost = AddText(unlock.transform, "UnlockCostText", "Открыть\n60", 38, Color.white, TextAlignmentOptions.Center, bold: true, autoSize: true);
            Stretch(unlockCost.gameObject, new Vector2(12f, 8f), new Vector2(-12f, -8f));

            var so = new SerializedObject(view);
            SetArray(so, "_tapButtons", iconButton, barButton);
            Set(so, "_icon", iconImage);
            Set(so, "_iconLabel", iconLabel);
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
            AddImage(root, RowColor, _roundSprite);
            root.AddComponent<LayoutElement>().preferredHeight = ManagerRowHeight;
            var view = root.AddComponent<ManagerRowView>();

            Image icon = BuildRowIcon(root.transform, 140f, out TMP_Text iconLabel, 64);

            TMP_Text nameText = AddText(root.transform, "NameText", "Station", 40, DarkBrown, TextAlignmentOptions.Left, bold: true, autoSize: true);
            SetRect(nameText.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(180f, -20f), new Vector2(-470f, 56f));
            TMP_Text description = AddText(root.transform, "DescriptionText", "", 30, Muted, TextAlignmentOptions.TopLeft, autoSize: true);
            SetRect(description.gameObject, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(180f, 18f), new Vector2(-470f, -94f));

            Button hireButton = BuildRowButton(root.transform, "HireButton", Green, new Vector2(270f, 140f), SoundId.Hire, out TMP_Text hireText);

            var so = new SerializedObject(view);
            Set(so, "_icon", icon);
            Set(so, "_iconLabel", iconLabel);
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
            AddImage(root, RowColor, _roundSprite);
            root.AddComponent<LayoutElement>().preferredHeight = UpgradeRowHeight;
            var view = root.AddComponent<UpgradeRowView>();

            Image icon = BuildRowIcon(root.transform, 120f, out TMP_Text iconLabel, 48);

            TMP_Text description = AddText(root.transform, "DescriptionText", "Station: income ×3", 38, DarkBrown, TextAlignmentOptions.Left, bold: true, autoSize: true);
            SetRect(description.gameObject, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), new Vector2(160f, 0f), new Vector2(-450f, -30f));

            Button buyButton = BuildRowButton(root.transform, "BuyButton", Orange, new Vector2(270f, 120f), SoundId.Upgrade, out TMP_Text buyText);

            var so = new SerializedObject(view);
            Set(so, "_icon", icon);
            Set(so, "_iconLabel", iconLabel);
            Set(so, "_descriptionText", description);
            Set(so, "_buyButton", buyButton);
            Set(so, "_buyText", buyText);
            Set(so, "_buyOffer", AddBillOffer(buyButton));
            so.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, UpgradeRowPrefabPath).GetComponent<UpgradeRowView>();
        }

        private static Image BuildRowIcon(Transform parent, float size, out TMP_Text label, float labelSize)
        {
            GameObject icon = CreateUi("Icon", parent);
            SetRect(icon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(size, size));
            Image image = AddImage(icon, Orange, _circleSprite, sliced: false);
            image.raycastTarget = false;
            label = AddText(icon.transform, "IconLabel", "1", labelSize, Color.white, TextAlignmentOptions.Center, bold: true, autoSize: true);
            Stretch(label.gameObject, new Vector2(10f, 0f), new Vector2(-10f, 0f));
            return image;
        }

        private static Button BuildRowButton(Transform parent, string name, Color color, Vector2 size, SoundId sound, out TMP_Text label)
        {
            GameObject buttonObject = CreateUi(name, parent);
            SetRect(buttonObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20f, 0f), size);
            AddImage(buttonObject, color, _roundSprite);
            Button button = AddButton(buttonObject, sound);
            AddPulseWhileInteractable(buttonObject, button);
            label = AddText(buttonObject.transform, "Label", "", 36, Color.white, TextAlignmentOptions.Center, bold: true, autoSize: true);
            Stretch(label.gameObject, new Vector2(12f, 8f), new Vector2(-12f, -8f));
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
            AddImage(background, Cream, null).raycastTarget = false;

            // Ларёк и улица с клиентами.
            GameObject kiosk = CreateUi("KioskArea", screen.transform);
            Stretch(kiosk, Vector2.zero, Vector2.zero);
            Image sky = AddImage(kiosk, Sky, null);
            sky.raycastTarget = false;
            kiosk.AddComponent<RectMask2D>();
            KioskView kioskView = BuildKiosk(kiosk.transform, sky);
            CustomerQueue customers = BuildStreet(kiosk.transform, kioskView);

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
            var cityPanelSo = new SerializedObject(cityPanel);
            Set(cityPanelSo, "_confirmPopup", prestigePopup);
            cityPanelSo.ApplyModifiedPropertiesWithoutUndo();
            BuildEffects(screen.transform, hud.CoinIcon, out CoinFlyEffect coinFly, out FloatingTextEffect floatingText);
            // Окна без своих эффектов — поверх летящих монет и цифр дохода.
            prestigePopup.transform.SetAsLastSibling();
            settingsPopup.transform.SetAsLastSibling();
            billOfferPopup.transform.SetAsLastSibling();

            var tabSo = new SerializedObject(tabBar);
            SetArray(tabSo, "_buttons", tabButtons);
            var shopPanelSo = new SerializedObject(shopPanel);
            Set(shopPanelSo, "_tabBadge", BuildTabBadge(tabButtons[4].transform));
            shopPanelSo.ApplyModifiedPropertiesWithoutUndo();
            SetArray(tabSo, "_panels", stationsPanel.gameObject, managersPanel.gameObject, upgradesPanel.gameObject, cityPanel.gameObject,
                shopPanel.gameObject);
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
            Set(screenSo, "_customers", customers);
            Set(screenSo, "_coinFly", coinFly);
            Set(screenSo, "_floatingText", floatingText);
            Set(screenSo, "_offlinePopup", offlinePopup);
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
            AddImage(viewport, new Color(1f, 1f, 1f, 0f), null);
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
            AddImage(buyMode, Brown, _roundSprite);
            Button buyModeButton = AddButton(buyMode, SoundId.Tap);
            TMP_Text buyModeLabel = AddText(buyMode.transform, "Label", "Купить: ×1", 36, Color.white, TextAlignmentOptions.Center, bold: true, autoSize: true);
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
                lamps[i] = Part(lampRow.transform, "Lamp_" + i, new Vector2(-95f + 38f * i, 0f), new Vector2(28f, 28f), WindowDark, _circleSprite, sliced: false);

            RectTransform[] decor = BuildKioskDecor(area, tinted);

            var so = new SerializedObject(kioskView);
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

            GameObject flags = DecorRoot(area, "Decor_6_Flags", new Vector2(0f, 292f), new Vector2(640f, 120f));
            for (int side = -1; side <= 1; side += 2)
            {
                string suffix = side < 0 ? "Left" : "Right";
                Part(flags.transform, "Pole" + suffix, new Vector2(262f * side, 0f), new Vector2(6f, 112f), Steel, null);
                tinted.Add(Part(flags.transform, "Flag" + suffix, new Vector2(292f * side, 72f), new Vector2(56f, 36f), BoothRed, null));
                Part(flags.transform, "Tip" + suffix, new Vector2(262f * side, 106f), new Vector2(14f, 14f), Gold, _circleSprite, sliced: false);
            }
            decor[5] = (RectTransform)flags.transform;

            foreach (RectTransform item in decor)
                item.gameObject.SetActive(false);
            return decor;
        }

        private static void BuildPlant(Transform parent, string suffix, float x, float leaves, float pot)
        {
            float side = Mathf.Sign(x);
            Part(parent, "Leaves" + suffix, new Vector2(x, pot * 0.7f), new Vector2(leaves, leaves), Green, _circleSprite, sliced: false);
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

        private static CustomerQueue BuildStreet(Transform area, KioskView kiosk)
        {
            GameObject street = CreateUi("Street", area);
            Stretch(street, Vector2.zero, Vector2.zero);
            var queue = street.AddComponent<CustomerQueue>();

            Sprite previewSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CustomerIdlePath);
            var pool = new CustomerView[CustomerPoolSize];
            for (int i = 0; i < pool.Length; i++)
            {
                GameObject customer = CreateUi("Customer_" + i, street.transform);
                SetRect(customer, new Vector2(0.5f, GroundRatio), new Vector2(0.5f, GroundRatio), new Vector2(0.5f, 0f), new Vector2(0f, -12f), new Vector2(165f, 220f));

                GameObject body = CreateUi("Body", customer.transform);
                Stretch(body, Vector2.zero, Vector2.zero);
                body.transform.localScale = new Vector3(-1f, 1f, 1f);
                Image bodyImage = AddImage(body, Color.white, previewSprite, sliced: false);
                bodyImage.preserveAspect = true;

                var button = customer.AddComponent<Button>();
                button.targetGraphic = bodyImage;
                button.transition = Selectable.Transition.None;

                var view = customer.AddComponent<CustomerView>();
                var viewSo = new SerializedObject(view);
                Set(viewSo, "_body", bodyImage);
                Set(viewSo, "_button", button);
                viewSo.ApplyModifiedPropertiesWithoutUndo();

                customer.SetActive(false);
                pool[i] = view;
            }

            GameObject bubble = CreateUi("Bubble", street.transform);
            SetRect(bubble, new Vector2(0.5f, GroundRatio), new Vector2(0.5f, GroundRatio), new Vector2(0.15f, 0f), new Vector2(0f, 250f), new Vector2(400f, 110f));
            AddImage(bubble, Color.white, _roundSprite).raycastTarget = false;
            TMP_Text bubbleText = AddText(bubble.transform, "Text", "", 32, DarkBrown, TextAlignmentOptions.Center, bold: true, autoSize: true);
            Stretch(bubbleText.gameObject, new Vector2(18f, 10f), new Vector2(-18f, -10f));
            bubble.SetActive(false);

            var so = new SerializedObject(queue);
            Set(so, "_street", street.transform);
            SetArray(so, "_pool", pool);
            Set(so, "_bubble", bubble.transform);
            Set(so, "_bubbleText", bubbleText);
            Set(so, "_kiosk", kiosk);
            so.ApplyModifiedPropertiesWithoutUndo();
            return queue;
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
                AddImage(coin, Gold, _circleSprite, sliced: false).raycastTarget = false;
                GameObject inner = CreateUi("Inner", coin.transform);
                SetRect(inner, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(38f, 38f));
                AddImage(inner, CoinInner, _circleSprite, sliced: false).raycastTarget = false;
                coin.SetActive(false);
                coins[i] = (RectTransform)coin.transform;
            }

            var texts = new TMP_Text[FloatingTextPoolSize];
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = AddText(layer.transform, "FloatingText_" + i, "+1", 44, FloatText, TextAlignmentOptions.Center, bold: true);
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
            AddImage(root, new Color(0f, 0f, 0f, 0.6f), null);
            var popup = root.AddComponent<OfflineIncomePopup>();

            GameObject panel = CreateUi("Panel", root.transform);
            SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 760f));
            AddImage(panel, Cream, _roundSprite);

            TMP_Text title = AddLocalizedText(panel.transform, "Title", "offline.title", 56, DarkBrown, bold: true);
            SetRect(title.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(-80f, 80f));
            TMP_Text subtitle = AddText(panel.transform, "Subtitle", "", 36, Muted, TextAlignmentOptions.Center, autoSize: true);
            SetRect(subtitle.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(-80f, 60f));

            GameObject coin = CreateUi("CoinIcon", panel.transform);
            SetRect(coin, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 110f), new Vector2(110f, 110f));
            AddImage(coin, Gold, _circleSprite, sliced: false).raycastTarget = false;
            TMP_Text amount = AddText(panel.transform, "Amount", "+0", 96, Orange, TextAlignmentOptions.Center, bold: true, autoSize: true);
            SetRect(amount.gameObject, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(-80f, 130f));
            TMP_Text limit = AddText(panel.transform, "Limit", "", 30, BoothRed, TextAlignmentOptions.Center, autoSize: true);
            SetRect(limit.gameObject, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -105f), new Vector2(-80f, 50f));

            Button claim = BuildPopupButton(panel.transform, "ClaimButton", Brown, new Vector2(-195f, 70f), SoundId.Buy, out TMP_Text claimText);
            var claimLocalized = claimText.gameObject.AddComponent<LocalizedText>();
            var claimSo = new SerializedObject(claimLocalized);
            claimSo.FindProperty("_key").stringValue = "offline.claim";
            claimSo.ApplyModifiedPropertiesWithoutUndo();
            claimText.text = "offline.claim";

            Button claimDouble = BuildPopupButton(panel.transform, "ClaimDoubleButton", Green, new Vector2(195f, 70f), SoundId.Tap, out TMP_Text claimDoubleText);
            AddPulseWhileInteractable(claimDouble.gameObject, claimDouble);

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

            root.SetActive(false);
            return popup;
        }

        private static Button BuildPopupButton(Transform parent, string name, Color color, Vector2 bottomCenterOffset, SoundId sound,
            out TMP_Text label)
        {
            GameObject buttonObject = CreateUi(name, parent);
            SetRect(buttonObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), bottomCenterOffset, new Vector2(360f, 130f));
            AddImage(buttonObject, color, _roundSprite);
            Button button = AddButton(buttonObject, sound);
            label = AddText(buttonObject.transform, "Label", "", 38, Color.white, TextAlignmentOptions.Center, bold: true, autoSize: true);
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
            AddImage(card, RowColor, _roundSprite);
            card.AddComponent<LayoutElement>().preferredHeight = CityCardHeight;

            CityMapView map = BuildCityMap(card.transform);

            float infoTop = -(20f + MapHeight + 22f);
            GameObject bar = CreateUi("ProgressBar", card.transform);
            SetCardLine(bar, infoTop, 60f);
            AddImage(bar, BarBack, _roundSprite).raycastTarget = false;
            GameObject fill = CreateUi("Fill", bar.transform);
            var fillRect = (RectTransform)fill.transform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            AddImage(fill, Reputation, _roundSprite).raycastTarget = false;
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
            AddImage(prestige, Orange, _roundSprite);
            Button prestigeButton = AddButton(prestige, SoundId.Unlock);
            AddPulseWhileInteractable(prestige, prestigeButton);
            TMP_Text prestigeText = AddText(prestige.transform, "Label", "", 36, Color.white, TextAlignmentOptions.Center, bold: true, autoSize: true);
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
        /// Карта: «земля» с озером, холмами и деревьями, плашка выбранного города сверху и область,
        /// в которую CityMapView ставит города и дорогу (якорями по CityConfig.MapPosition).
        /// </summary>
        private static CityMapView BuildCityMap(Transform card)
        {
            const float sideInset = 60f;
            const float bottomInset = 50f;
            const float bannerHeight = 92f;
            const float topInset = bannerHeight + 32f;

            GameObject frame = CreateUi("Map", card);
            SetRect(frame, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(-40f, MapHeight));
            AddImage(frame, Land, _roundSprite).raycastTarget = false;
            var map = frame.AddComponent<CityMapView>();

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
                MapBlob(frame.transform, "Tree_" + i, trees[i], new Vector2(46f, 46f), i % 2 == 0 ? Green : LeafDark);

            GameObject area = CreateUi("Area", frame.transform);
            Stretch(area, new Vector2(sideInset, bottomInset), new Vector2(-sideInset, -topInset));

            GameObject traveler = CreateUi("Traveler", area.transform);
            SetRect(traveler, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80f, 70f));
            Part(traveler.transform, "Body", new Vector2(0f, 26f), new Vector2(70f, 38f), Orange, _roundSprite);
            Part(traveler.transform, "Window", new Vector2(20f, 44f), new Vector2(20f, 14f), AwningWhite, _roundSprite);
            Part(traveler.transform, "WheelBack", new Vector2(-20f, 16f), new Vector2(20f, 20f), DarkBrown, _circleSprite, sliced: false);
            Part(traveler.transform, "WheelFront", new Vector2(20f, 16f), new Vector2(20f, 20f), DarkBrown, _circleSprite, sliced: false);

            GameObject banner = CreateUi("SelectedCity", frame.transform);
            SetRect(banner, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(-24f, bannerHeight));
            AddImage(banner, new Color(Cream.r, Cream.g, Cream.b, 0.92f), _roundSprite).raycastTarget = false;
            TMP_Text selectedName = AddText(banner.transform, "Name", "", 38, DarkBrown, TextAlignmentOptions.Left, bold: true, autoSize: true);
            SetRect(selectedName.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(-48f, 46f));
            TMP_Text selectedInfo = AddText(banner.transform, "Info", "", 28, Muted, TextAlignmentOptions.Left, autoSize: true);
            SetRect(selectedInfo.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(-48f, 36f));

            var so = new SerializedObject(map);
            Set(so, "_area", area.transform);
            Set(so, "_nodePrefab", BuildMapNodePrefab());
            Set(so, "_roadDotPrefab", BuildRoadDotPrefab());
            Set(so, "_traveler", traveler.transform);
            Set(so, "_selectedNameText", selectedName);
            Set(so, "_selectedInfoText", selectedInfo);
            // Размер области карты в портрете: карточка 1040 − поля карты 40 − отступы области.
            so.FindProperty("_referenceSize").vector2Value = new Vector2(1000f - 2f * sideInset, MapHeight - bottomInset - topInset);
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
            AddImage(root, new Color(1f, 1f, 1f, 0f), null);
            Button button = AddButton(root, SoundId.Tap);
            var view = root.AddComponent<CityMapNodeView>();

            GameObject body = CreateUi("Body", root.transform);
            SetRect(body, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 100f));

            GameObject selection = CreateUi("Selection", body.transform);
            SetRect(selection, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(128f, 128f));
            AddImage(selection, Gold, _circleSprite, sliced: false).raycastTarget = false;

            GameObject circle = CreateUi("Circle", body.transform);
            SetRect(circle, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 100f));
            Image circleImage = AddImage(circle, Green, _circleSprite, sliced: false);
            circleImage.raycastTarget = false;
            var pulse = circle.AddComponent<PulseEffect>();
            TMP_Text number = AddText(circle.transform, "Number", "1", 50, Color.white, TextAlignmentOptions.Center, bold: true);
            Stretch(number.gameObject, Vector2.zero, Vector2.zero);

            GameObject pin = CreateUi("HerePin", body.transform);
            SetRect(pin, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), new Vector2(0f, 58f), new Vector2(170f, 42f));
            AddImage(pin, DarkBrown, _roundSprite).raycastTarget = false;
            TMP_Text pinText = AddLocalizedText(pin.transform, "Label", "map.here", 26, Color.white, bold: true);
            Stretch(pinText.gameObject, new Vector2(8f, 2f), new Vector2(-8f, -2f));

            TMP_Text nameText = AddText(root.transform, "Name", "Город", 30, DarkBrown, TextAlignmentOptions.Center, bold: true, autoSize: true);
            SetRect(nameText.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(210f, 40f));
            nameText.enableWordWrapping = false;

            var so = new SerializedObject(view);
            Set(so, "_button", button);
            Set(so, "_circle", circleImage);
            Set(so, "_numberText", number);
            Set(so, "_nameText", nameText);
            Set(so, "_selection", selection);
            Set(so, "_herePin", pin);
            Set(so, "_body", body.transform);
            Set(so, "_pulse", pulse);
            so.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, MapNodePrefabPath).GetComponent<CityMapNodeView>();
        }

        private static Image BuildRoadDotPrefab()
        {
            GameObject root = CreateUi("RoadDot", null);
            ((RectTransform)root.transform).sizeDelta = new Vector2(16f, 16f);
            AddImage(root, BarBack, _circleSprite, sliced: false).raycastTarget = false;
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
            AddImage(root, RowColor, _roundSprite);
            root.AddComponent<LayoutElement>().preferredHeight = ShopRowHeight;
            var view = root.AddComponent<ShopItemRowView>();

            TMP_Text nameText = AddText(root.transform, "NameText", "Item", 40, DarkBrown, TextAlignmentOptions.Left, bold: true, autoSize: true);
            SetRect(nameText.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(30f, -16f), new Vector2(-340f, 52f));
            TMP_Text description = AddText(root.transform, "DescriptionText", "", 30, Muted, TextAlignmentOptions.Left, autoSize: true);
            SetRect(description.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(30f, -70f), new Vector2(-340f, 46f));
            TMP_Text level = AddText(root.transform, "LevelText", "", 28, Reputation, TextAlignmentOptions.Left, bold: true, autoSize: true);
            SetRect(level.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(30f, 14f), new Vector2(-340f, 38f));

            Button buyButton = BuildRowButton(root.transform, "BuyButton", Reputation, new Vector2(270f, 120f), SoundId.Upgrade, out TMP_Text buyText);

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

        private const float StarterCardHeight = 480f;

        /// <summary>
        /// Карточка «Стартовый набор»: золотая рамка, красная пульсирующая ленточка «Супер-выгода!», большая иконка набора слева,
        /// справа название и две плитки наград (купюры + монеты), внизу большая зелёная кнопка цены.
        /// </summary>
        private static StarterPackCard BuildStarterPackCard(Transform list)
        {
            GameObject root = CreateUi("StarterPackCard", list);
            root.AddComponent<LayoutElement>().preferredHeight = StarterCardHeight;
            AddImage(root, Gold, _roundSprite).raycastTarget = false;
            var card = root.AddComponent<StarterPackCard>();

            GameObject inner = CreateUi("Inner", root.transform);
            Stretch(inner, new Vector2(10f, 10f), new Vector2(-10f, -10f));
            AddImage(inner, LightGold, _roundSprite).raycastTarget = false;

            // Иконка набора: чуть наклонена, спрайт назначает StarterPackCard из конфига.
            GameObject icon = CreateUi("Icon", inner.transform);
            SetRect(icon, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(135f, -200f), new Vector2(210f, 210f));
            icon.transform.localRotation = Quaternion.Euler(0f, 0f, 6f);
            Image iconImage = AddImage(icon, Color.white, null, sliced: false);
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            GameObject content = CreateUi("Content", inner.transform);
            Stretch(content, new Vector2(250f, 0f), Vector2.zero);

            GameObject ribbon = CreateUi("Ribbon", root.transform);
            SetRect(ribbon, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -36f), new Vector2(460f, 62f));
            AddImage(ribbon, BoothRed, _roundSprite).raycastTarget = false;
            TMP_Text ribbonText = AddText(ribbon.transform, "Label", "", 36, Color.white, TextAlignmentOptions.Center, bold: true, autoSize: true);
            Stretch(ribbonText.gameObject, new Vector2(14f, 4f), new Vector2(-14f, -4f));
            ribbonText.raycastTarget = false;
            var ribbonPulse = ribbon.AddComponent<PulseEffect>();

            TMP_Text title = AddText(content.transform, "TitleText", "", 52, DarkBrown, TextAlignmentOptions.Center, bold: true, autoSize: true);
            SetRect(title.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -76f), new Vector2(-20f, 68f));
            title.raycastTarget = false;

            TMP_Text billsText = BuildStarterTile(content.transform, "BillsTile", BillGreen, BillLight, -1f);
            TMP_Text coinsText = BuildStarterTile(content.transform, "CoinsTile", Orange, Gold, 1f);

            TMP_Text plus = AddText(content.transform, "Plus", "+", 64, DarkBrown, TextAlignmentOptions.Center, bold: true);
            SetRect(plus.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -230f), new Vector2(60f, 70f));
            plus.raycastTarget = false;

            TMP_Text once = AddText(inner.transform, "OnceText", "", 34, BoothRed, TextAlignmentOptions.Center, bold: true, autoSize: true);
            SetRect(once.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -302f), new Vector2(-40f, 44f));
            once.raycastTarget = false;

            GameObject buy = CreateUi("BuyButton", inner.transform);
            SetRect(buy, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(620f, 112f));
            AddImage(buy, Green, _roundSprite);
            Button buyButton = AddButton(buy, SoundId.Buy);
            AddPulseWhileInteractable(buy, buyButton);
            TMP_Text buyText = AddText(buy.transform, "Label", "", 50, Color.white, TextAlignmentOptions.Center, bold: true, autoSize: true);
            Stretch(buyText.gameObject, new Vector2(16f, 8f), new Vector2(-16f, -8f));
            buyText.raycastTarget = false;

            var so = new SerializedObject(card);
            Set(so, "_icon", iconImage);
            Set(so, "_titleText", title);
            Set(so, "_ribbonText", ribbonText);
            Set(so, "_onceText", once);
            Set(so, "_billsText", billsText);
            Set(so, "_coinsText", coinsText);
            Set(so, "_buyButton", buyButton);
            Set(so, "_buyText", buyText);
            Set(so, "_ribbonPulse", ribbonPulse);
            so.ApplyModifiedPropertiesWithoutUndo();
            return card;
        }

        // Плитка награды: цветной прямоугольник, круглый значок и крупный текст. side: -1 — слева от центра, 1 — справа.
        private static TMP_Text BuildStarterTile(Transform parent, string name, Color color, Color markColor, float side)
        {
            GameObject tile = CreateUi(name, parent);
            SetRect(tile, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(side < 0f ? 1f : 0f, 1f),
                new Vector2(side * 18f, -150f), new Vector2(340f, 160f));
            AddImage(tile, color, _roundSprite).raycastTarget = false;

            GameObject mark = CreateUi("Mark", tile.transform);
            SetRect(mark, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -36f), new Vector2(56f, 56f));
            AddImage(mark, markColor, _circleSprite, sliced: false).raycastTarget = false;

            TMP_Text text = AddText(tile.transform, "Text", "", 38, Color.white, TextAlignmentOptions.Center, bold: true, autoSize: true);
            SetRect(text.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(-20f, 74f));
            text.raycastTarget = false;
            return text;
        }

        // Красный кружок «!» в углу кнопки вкладки: есть бесплатная награда.
        private static GameObject BuildTabBadge(Transform tab)
        {
            GameObject badge = CreateUi("Badge", tab);
            SetRect(badge, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-10f, -10f), new Vector2(44f, 44f));
            AddImage(badge, BoothRed, _circleSprite, sliced: false).raycastTarget = false;
            TMP_Text mark = AddText(badge.transform, "Mark", "!", 34, Color.white, TextAlignmentOptions.Center, bold: true);
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
            AddImage(root, RowColor, _roundSprite);
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
            AddImage(badge, BoothRed, _roundSprite).raycastTarget = false;
            TMP_Text badgeText = AddLocalizedText(badge.transform, "Label", "shop.best", 28, Color.white, bold: true);
            Stretch(badgeText.gameObject, new Vector2(8f, 2f), new Vector2(-8f, -2f));
            badge.SetActive(false);

            Button button = BuildRowButton(root.transform, "BuyButton", BillGreen, new Vector2(270f, 120f), SoundId.Buy, out TMP_Text buttonText);

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
            AddImage(hint, BillGreen, _roundSprite).raycastTarget = false;
            GameObject mark = CreateUi("Mark", hint.transform);
            SetRect(mark, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18f, 18f));
            AddImage(mark, BillLight, _circleSprite, sliced: false).raycastTarget = false;
            hint.SetActive(false);

            var trigger = go.AddComponent<BillOfferTrigger>();
            var so = new SerializedObject(trigger);
            Set(so, "_button", button);
            Set(so, "_hint", hint);
            so.ApplyModifiedPropertiesWithoutUndo();
            return trigger;
        }

        /// <summary>Окно «Не хватает монет»: «Купить» слева, «Отмена» справа — под частыми тапами по кнопкам строк справа.</summary>
        private static BillOfferPopup BuildBillOfferPopup(Transform parent)
        {
            GameObject root = CreateUi("BillOfferPopup", parent);
            Stretch(root, Vector2.zero, Vector2.zero);
            AddImage(root, new Color(0f, 0f, 0f, 0.6f), null);
            var popup = root.AddComponent<BillOfferPopup>();

            GameObject panel = CreateUi("Panel", root.transform);
            SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 600f));
            AddImage(panel, Cream, _roundSprite);

            TMP_Text title = AddLocalizedText(panel.transform, "Title", "bills.offer_title", 52, DarkBrown, bold: true);
            SetRect(title.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(-80f, 80f));

            GameObject note = CreateUi("Icon", panel.transform);
            SetRect(note, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -136f), new Vector2(120f, 80f));
            AddImage(note, BillGreen, _roundSprite).raycastTarget = false;
            GameObject noteMark = CreateUi("Mark", note.transform);
            SetRect(noteMark, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 40f));
            AddImage(noteMark, BillLight, _circleSprite, sliced: false).raycastTarget = false;

            TMP_Text body = AddText(panel.transform, "Body", "", 36, DarkBrown, TextAlignmentOptions.Center, autoSize: true);
            SetRect(body.gameObject, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(-80f, 140f));

            Button confirm = BuildPopupButton(panel.transform, "ConfirmButton", BillGreen, new Vector2(-195f, 50f), SoundId.Buy, out TMP_Text confirmText);
            Button cancel = BuildPopupButton(panel.transform, "CancelButton", Brown, new Vector2(195f, 50f), SoundId.Tap, out TMP_Text cancelText);
            AddLocalizedKey(cancelText, "bills.offer_cancel");

            var so = new SerializedObject(popup);
            Set(so, "_root", root);
            Set(so, "_panel", panel.transform);
            Set(so, "_bodyText", body);
            Set(so, "_confirmButton", confirm);
            Set(so, "_confirmText", confirmText);
            Set(so, "_cancelButton", cancel);
            so.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);
            return popup;
        }

        // ---------- Settings ----------

        /// <summary>Шестерёнка в HUD (слева сверху, напротив репутации) и окно настроек со звуком и музыкой.</summary>
        private static SettingsPopup BuildSettingsPopup(Transform parent, Transform hud)
        {
            GameObject gear = CreateUi("SettingsButton", hud);
            SetRect(gear, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -14f), new Vector2(96f, 96f));
            AddImage(gear, Brown, _circleSprite, sliced: false);
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

            GameObject root = CreateUi("SettingsPopup", parent);
            Stretch(root, Vector2.zero, Vector2.zero);
            AddImage(root, new Color(0f, 0f, 0f, 0.6f), null);
            var popup = root.AddComponent<SettingsPopup>();

            GameObject panel = CreateUi("Panel", root.transform);
            SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 640f));
            AddImage(panel, Cream, _roundSprite);

            TMP_Text title = AddLocalizedText(panel.transform, "Title", "settings.title", 56, DarkBrown, bold: true);
            SetRect(title.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(-80f, 80f));

            Button sound = BuildSettingsRow(panel.transform, "Sound", "settings.sound", 90f, out TMP_Text soundText);
            Button music = BuildSettingsRow(panel.transform, "Music", "settings.music", -50f, out TMP_Text musicText);

            Button close = BuildPopupButton(panel.transform, "CloseButton", Brown, new Vector2(0f, 50f), SoundId.Tap, out TMP_Text closeText);
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
            so.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);
            return popup;
        }

        // Строка настроек: подпись слева, переключатель «Вкл/Выкл» справа; y — смещение от центра панели.
        private static Button BuildSettingsRow(Transform panel, string name, string labelKey, float y, out TMP_Text stateText)
        {
            TMP_Text label = AddLocalizedText(panel, name + "Label", labelKey, 44, DarkBrown, bold: true);
            label.alignment = TextAlignmentOptions.Left;
            SetRect(label.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(60f, y), new Vector2(420f, 80f));

            GameObject toggle = CreateUi(name + "Toggle", panel);
            SetRect(toggle, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-60f, y), new Vector2(240f, 100f));
            AddImage(toggle, Green, _roundSprite);
            Button button = AddButton(toggle, SoundId.Tap);
            stateText = AddText(toggle.transform, "Label", "", 40, Color.white, TextAlignmentOptions.Center, bold: true, autoSize: true);
            Stretch(stateText.gameObject, new Vector2(12f, 6f), new Vector2(-12f, -6f));
            return button;
        }

        private static PrestigeConfirmPopup BuildPrestigePopup(Transform parent)
        {
            GameObject root = CreateUi("PrestigePopup", parent);
            Stretch(root, Vector2.zero, Vector2.zero);
            AddImage(root, new Color(0f, 0f, 0f, 0.6f), null);
            var popup = root.AddComponent<PrestigeConfirmPopup>();

            GameObject panel = CreateUi("Panel", root.transform);
            SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 720f));
            AddImage(panel, Cream, _roundSprite);

            TMP_Text title = AddText(panel.transform, "Title", "", 50, DarkBrown, TextAlignmentOptions.Center, bold: true, autoSize: true);
            SetRect(title.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(-80f, 130f));
            TMP_Text body = AddLocalizedText(panel.transform, "Body", "prestige.confirm_body", 32, Muted, bold: false);
            SetRect(body.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(-80f, 180f));
            TMP_Text gain = AddText(panel.transform, "Gain", "", 40, Reputation, TextAlignmentOptions.Center, bold: true, autoSize: true);
            SetRect(gain.gameObject, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -100f), new Vector2(-80f, 80f));

            Button cancel = BuildPopupButton(panel.transform, "CancelButton", Brown, new Vector2(-195f, 60f), SoundId.Tap, out TMP_Text cancelText);
            AddLocalizedKey(cancelText, "prestige.confirm_no");
            Button confirm = BuildPopupButton(panel.transform, "ConfirmButton", Orange, new Vector2(195f, 60f), SoundId.Unlock, out TMP_Text confirmText);
            AddLocalizedKey(confirmText, "prestige.confirm_yes");

            var so = new SerializedObject(popup);
            Set(so, "_root", root);
            Set(so, "_panel", panel.transform);
            Set(so, "_titleText", title);
            Set(so, "_gainText", gain);
            Set(so, "_confirmButton", confirm);
            Set(so, "_cancelButton", cancel);
            so.ApplyModifiedPropertiesWithoutUndo();

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
            AddImage(hud, DarkBrown, null);
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
            AddImage(coinIcon, Gold, _circleSprite, sliced: false).raycastTarget = false;
            var iconLayout = coinIcon.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 90f;
            iconLayout.preferredHeight = 90f;

            TMP_Text coins = AddText(coinsRow.transform, "CoinsText", "0", 84, Color.white, TextAlignmentOptions.Left, bold: true);
            coins.enableWordWrapping = false;

            TMP_Text income = AddText(hud.transform, "IncomeText", "", 42, LightGold, TextAlignmentOptions.Center, bold: true);
            SetRect(income.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(-40f, 56f));

            var so = new SerializedObject(hudView);
            Set(so, "_coinsText", coins);
            Set(so, "_incomeText", income);
            Set(so, "_coinIcon", coinIcon.transform);

            // Купюры — справа сверху, видны всегда; по тапу — магазин.
            GameObject bills = CreateUi("Bills", hud.transform);
            SetRect(bills, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(200f, 64f));
            AddImage(bills, Brown, _roundSprite);
            Button billsButton = AddButton(bills, SoundId.Tap);
            GameObject note = CreateUi("Icon", bills.transform);
            SetRect(note, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(54f, 36f));
            AddImage(note, BillGreen, _roundSprite).raycastTarget = false;
            GameObject noteMark = CreateUi("Mark", note.transform);
            SetRect(noteMark, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18f, 18f));
            AddImage(noteMark, BillLight, _circleSprite, sliced: false).raycastTarget = false;
            TMP_Text billsText = AddText(bills.transform, "Text", "0", 38, Color.white, TextAlignmentOptions.Center, bold: true, autoSize: true);
            Stretch(billsText.gameObject, new Vector2(66f, 4f), new Vector2(-12f, -4f));
            Set(so, "_billsRoot", bills.transform);
            Set(so, "_billsText", billsText);
            Set(so, "_billsButton", billsButton);

            // Репутация — под купюрами (показывается, когда появится).
            GameObject reputation = CreateUi("Reputation", hud.transform);
            SetRect(reputation, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -94f), new Vector2(200f, 64f));
            AddImage(reputation, Brown, _roundSprite).raycastTarget = false;
            GameObject reputationIcon = CreateUi("Icon", reputation.transform);
            SetRect(reputationIcon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(48f, 48f));
            AddImage(reputationIcon, Reputation, _circleSprite, sliced: false).raycastTarget = false;
            TMP_Text reputationText = AddText(reputation.transform, "Text", "0", 38, Color.white, TextAlignmentOptions.Center, bold: true, autoSize: true);
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
            AddImage(bar, DarkBrown, null);
            var horizontal = bar.AddComponent<HorizontalLayoutGroup>();
            horizontal.padding = new RectOffset(16, 16, 16, 16);
            horizontal.spacing = 12f;
            horizontal.childControlWidth = true;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = true;
            horizontal.childForceExpandHeight = true;
            var tabBar = bar.AddComponent<TabBar>();

            string[] keys = { "tab.stations", "tab.managers", "tab.upgrades", "tab.city", "tab.shop" };
            buttons = new Button[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                GameObject tab = CreateUi("Tab_" + keys[i].Substring(4), bar.transform);
                AddImage(tab, Brown, _roundSprite);
                buttons[i] = AddButton(tab, SoundId.Tab);
                TMP_Text label = AddLocalizedText(tab.transform, "Label", keys[i], 36, Color.white, bold: true);
                Stretch(label.gameObject, new Vector2(8f, 0f), new Vector2(-8f, 0f));
            }

            return tabBar;
        }

        // ---------- Helpers ----------

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
                image.pixelsPerUnitMultiplier = 0.35f;
            }
            return image;
        }

        private static Button AddButton(GameObject go, SoundId sound)
        {
            var button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();

            var feedback = go.AddComponent<ButtonFeedback>();
            var so = new SerializedObject(feedback);
            so.FindProperty("_sound").enumValueIndex = (int)sound;
            so.ApplyModifiedPropertiesWithoutUndo();
            return button;
        }

        private static void AddPulseWhileInteractable(GameObject go, Button button)
        {
            var pulse = go.AddComponent<PulseEffect>();
            var so = new SerializedObject(pulse);
            Set(so, "_whileInteractable", button);
            so.ApplyModifiedPropertiesWithoutUndo();
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
