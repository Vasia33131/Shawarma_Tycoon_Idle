using System.IO;
using ShaurmaTycoon.Localization;
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

        private const float HudHeight = 200f;
        private const float TabBarHeight = 180f;
        private const float RowHeight = 220f;
        private const float ManagerRowHeight = 180f;
        private const float UpgradeRowHeight = 160f;
        private const float HeaderHeight = 110f;

        private static readonly Color Cream = new Color(1f, 0.95f, 0.88f);
        private static readonly Color RowColor = new Color(1f, 0.99f, 0.96f);
        private static readonly Color DarkBrown = new Color(0.36f, 0.18f, 0.07f);
        private static readonly Color Brown = new Color(0.45f, 0.25f, 0.12f);
        private static readonly Color Orange = new Color(0.95f, 0.55f, 0.15f);
        private static readonly Color Kiosk = new Color(1f, 0.72f, 0.38f);
        private static readonly Color Green = new Color(0.30f, 0.69f, 0.31f);
        private static readonly Color BarBack = new Color(0.36f, 0.18f, 0.07f, 0.25f);
        private static readonly Color Gold = new Color(1f, 0.80f, 0.20f);
        private static readonly Color LightGold = new Color(1f, 0.90f, 0.60f);
        private static readonly Color Muted = new Color(0.45f, 0.35f, 0.28f);

        private static Sprite _roundSprite;
        private static Sprite _circleSprite;
        private static TMP_FontAsset _font;

        [MenuItem("ShaurmaTycoon/Build Game UI")]
        public static void Build()
        {
            _font = TMP_Settings.defaultFontAsset;
            if (_font == null)
            {
                EditorUtility.DisplayDialog("Build Game UI",
                    "TMP Essentials не найдены. Импортируй их: Window → TextMeshPro → Import TMP Essential Resources.", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

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

            StationRowView rowPrefab = BuildRowPrefab();
            BuildScreen(canvas, rowPrefab, BuildManagerRowPrefab(), BuildUpgradeRowPrefab());

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Build Game UI] Done");
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
            Button iconButton = AddButton(icon);
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
            Button barButton = AddButton(bar);

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
            Button buyButton = AddButton(buy);
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
            Button unlockButton = AddButton(unlock);
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
            Set(so, "_buyButton", buyButton);
            Set(so, "_buyCountText", buyCount);
            Set(so, "_buyCostText", buyCost);
            Set(so, "_lockedRoot", locked);
            Set(so, "_lockedNameText", lockedName);
            Set(so, "_unlockButton", unlockButton);
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

            Button hireButton = BuildRowButton(root.transform, "HireButton", Green, new Vector2(270f, 140f), out TMP_Text hireText);

            var so = new SerializedObject(view);
            Set(so, "_icon", icon);
            Set(so, "_iconLabel", iconLabel);
            Set(so, "_nameText", nameText);
            Set(so, "_descriptionText", description);
            Set(so, "_hireButton", hireButton);
            Set(so, "_hireText", hireText);
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

            Button buyButton = BuildRowButton(root.transform, "BuyButton", Orange, new Vector2(270f, 120f), out TMP_Text buyText);

            var so = new SerializedObject(view);
            Set(so, "_icon", icon);
            Set(so, "_iconLabel", iconLabel);
            Set(so, "_descriptionText", description);
            Set(so, "_buyButton", buyButton);
            Set(so, "_buyText", buyText);
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

        private static Button BuildRowButton(Transform parent, string name, Color color, Vector2 size, out TMP_Text label)
        {
            GameObject buttonObject = CreateUi(name, parent);
            SetRect(buttonObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20f, 0f), size);
            AddImage(buttonObject, color, _roundSprite);
            Button button = AddButton(buttonObject);
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
            UpgradeRowView upgradeRowPrefab)
        {
            GameObject screen = CreateUi(ScreenName, canvas.transform);
            Stretch(screen, Vector2.zero, Vector2.zero);
            var gameScreen = screen.AddComponent<GameScreen>();
            var layout = screen.AddComponent<ResponsiveLayout>();

            GameObject background = CreateUi("Background", screen.transform);
            Stretch(background, Vector2.zero, Vector2.zero);
            AddImage(background, Cream, null).raycastTarget = false;

            // Ларёк (заглушка до этапа 05).
            GameObject kiosk = CreateUi("KioskArea", screen.transform);
            Stretch(kiosk, Vector2.zero, Vector2.zero);
            AddImage(kiosk, Kiosk, null).raycastTarget = false;
            TMP_Text title = AddLocalizedText(kiosk.transform, "Title", "game.title", 80, DarkBrown, bold: true);
            SetRect(title.gameObject, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(-80f, 110f));
            TMP_Text placeholder = AddLocalizedText(kiosk.transform, "Placeholder", "kiosk.placeholder", 34, Brown, bold: false);
            SetRect(placeholder.gameObject, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -50f), new Vector2(-80f, 60f));

            // Контент: панели вкладок.
            GameObject content = CreateUi("ContentArea", screen.transform);
            Stretch(content, Vector2.zero, Vector2.zero);

            StationsPanel stationsPanel = BuildStationsPanel(content.transform, rowPrefab);
            ManagersPanel managersPanel = BuildManagersPanel(content.transform, managerRowPrefab);
            UpgradesPanel upgradesPanel = BuildUpgradesPanel(content.transform, upgradeRowPrefab);
            GameObject city = BuildPlaceholderPanel(content.transform, "CityPanel");

            HudView hud = BuildHud(screen.transform);
            TabBar tabBar = BuildTabBar(screen.transform, out Button[] tabButtons);

            var tabSo = new SerializedObject(tabBar);
            SetArray(tabSo, "_buttons", tabButtons);
            SetArray(tabSo, "_panels", stationsPanel.gameObject, managersPanel.gameObject, upgradesPanel.gameObject, city);
            tabSo.ApplyModifiedPropertiesWithoutUndo();

            var screenSo = new SerializedObject(gameScreen);
            Set(screenSo, "_hud", hud);
            Set(screenSo, "_stationsPanel", stationsPanel);
            Set(screenSo, "_managersPanel", managersPanel);
            Set(screenSo, "_upgradesPanel", upgradesPanel);
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
            Button buyModeButton = AddButton(buyMode);
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

        private static GameObject BuildPlaceholderPanel(Transform parent, string name)
        {
            GameObject panel = CreateUi(name, parent);
            Stretch(panel, Vector2.zero, Vector2.zero);
            TMP_Text text = AddLocalizedText(panel.transform, "ComingSoon", "tab.coming_soon", 48, Muted, bold: true);
            Stretch(text.gameObject, new Vector2(40f, 0f), new Vector2(-40f, 0f));
            panel.SetActive(false);
            return panel;
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

            string[] keys = { "tab.stations", "tab.managers", "tab.upgrades", "tab.city" };
            buttons = new Button[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                GameObject tab = CreateUi("Tab_" + keys[i].Substring(4), bar.transform);
                AddImage(tab, Brown, _roundSprite);
                buttons[i] = AddButton(tab);
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

        private static Button AddButton(GameObject go)
        {
            var button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            return button;
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
