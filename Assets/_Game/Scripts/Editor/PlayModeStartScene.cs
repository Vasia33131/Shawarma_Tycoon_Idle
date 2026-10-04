using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ShaurmaTycoon.EditorTools
{
    /// <summary>
    /// Play в редакторе всегда стартует со сцены Boot (там создаются сервисы), из какой бы сцены его ни нажали.
    /// После выхода из Play редактор возвращается к открытой сцене.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeStartScene
    {
        private const string BootScenePath = "Assets/_Game/Scenes/Boot.unity";

        static PlayModeStartScene()
        {
            // Ассеты могут быть ещё не импортированы при первой загрузке — назначаем после неё.
            EditorApplication.delayCall += Assign;
        }

        private static void Assign()
        {
            var bootScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScenePath);
            if (bootScene == null)
            {
                Debug.LogWarning($"[PlayModeStartScene] {BootScenePath} not found. Run ShaurmaTycoon/Setup Project.");
                return;
            }

            EditorSceneManager.playModeStartScene = bootScene;
        }
    }
}
