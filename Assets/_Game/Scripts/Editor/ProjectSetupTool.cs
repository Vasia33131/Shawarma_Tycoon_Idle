using System.IO;
using ShaurmaTycoon.Core;
using ShaurmaTycoon.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ShaurmaTycoon.EditorTools
{
    /// <summary>
    /// Меню ShaurmaTycoon/Setup Project: создаёт сцены Boot и Game, настраивает Build Settings и WebGL.
    /// Повторный запуск безопасен: существующие сцены не перезаписываются.
    /// </summary>
    public static class ProjectSetupTool
    {
        private const string ScenesFolder = "Assets/_Game/Scenes";
        private const string BootScenePath = ScenesFolder + "/Boot.unity";
        private const string GameScenePath = ScenesFolder + "/Game.unity";

        [MenuItem("ShaurmaTycoon/Setup Project")]
        public static void SetupProject()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Directory.CreateDirectory(ScenesFolder);

            if (!File.Exists(BootScenePath))
                CreateBootScene();
            if (!File.Exists(GameScenePath))
                CreateGameScene();

            ApplyBuildSettings();
            ApplyPlayerSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(BootScenePath);

            Debug.Log("[Setup] Project setup complete");
        }

        private static void CreateBootScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera(Color.black);
            new GameObject(nameof(GameBootstrap)).AddComponent<GameBootstrap>();

            EditorSceneManager.SaveScene(scene, BootScenePath);
        }

        private static void CreateGameScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera(new Color(0.96f, 0.62f, 0.26f));

            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.layer = LayerMask.NameToLayer("UI");
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasObject.AddComponent<CanvasScalerAdapter>();

            CreateDebugLabel(canvasObject.transform);

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            EditorSceneManager.SaveScene(scene, GameScenePath);
        }

        private static void CreateCamera(Color background)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
        }

        // Временная надпись для проверки масштабирования; уберём на этапе 03 вместе с вводом локализации.
        private static void CreateDebugLabel(Transform parent)
        {
            var labelObject = new GameObject("DebugLabel_Stage00", typeof(RectTransform), typeof(Text));
            labelObject.layer = LayerMask.NameToLayer("UI");
            labelObject.transform.SetParent(parent, false);

            var rect = (RectTransform)labelObject.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(1000f, 200f);

            var text = labelObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 72;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = "Shaurma Tycoon";
        }

        private static void ApplyBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true),
            };
        }

        private static void ApplyPlayerSettings()
        {
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.runInBackground = false;
            PlayerSettings.defaultWebScreenWidth = 1080;
            PlayerSettings.defaultWebScreenHeight = 1920;

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
        }
    }
}
