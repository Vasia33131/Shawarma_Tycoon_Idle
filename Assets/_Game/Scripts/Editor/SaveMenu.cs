using ShaurmaTycoon.Platform;
using UnityEditor;
using UnityEngine;

namespace ShaurmaTycoon.EditorTools
{
    /// <summary>
    /// Меню ShaurmaTycoon/Save: посмотреть, удалить и подменить сохранение редактора (PlayerPrefs).
    /// Во время Play автосейв перезапишет изменения — менять сейв лучше вне Play.
    /// </summary>
    public static class SaveMenu
    {
        private const string LegacySaveJson = "{\"Coins\":500}";
        private const string CorruptSaveJson = "{\"Coins\":500, this is not json";

        [MenuItem("ShaurmaTycoon/Save/Print Save")]
        private static void PrintSave()
        {
            Debug.Log(PlayerPrefs.HasKey(EditorPlatformService.SaveKey)
                ? "[Save] " + PlayerPrefs.GetString(EditorPlatformService.SaveKey)
                : "[Save] No save.");
        }

        [MenuItem("ShaurmaTycoon/Save/Delete Save")]
        private static void DeleteSave()
        {
            if (!EditorUtility.DisplayDialog("Delete Save", "Удалить сохранение редактора? Следующий запуск начнётся с нуля.", "Удалить", "Отмена"))
                return;

            PlayerPrefs.DeleteKey(EditorPlatformService.SaveKey);
            PlayerPrefs.Save();
            WarnIfPlaying();
            Debug.Log("[Save] Deleted.");
        }

        [MenuItem("ShaurmaTycoon/Save/Write Legacy Test Save")]
        private static void WriteLegacySave()
        {
            Write(LegacySaveJson, "legacy (only Coins = 500)");
        }

        [MenuItem("ShaurmaTycoon/Save/Write Corrupt Test Save")]
        private static void WriteCorruptSave()
        {
            Write(CorruptSaveJson, "corrupt JSON");
        }

        private static void Write(string json, string description)
        {
            PlayerPrefs.SetString(EditorPlatformService.SaveKey, json);
            PlayerPrefs.Save();
            WarnIfPlaying();
            Debug.Log($"[Save] Wrote {description} save: {json}");
        }

        private static void WarnIfPlaying()
        {
            if (Application.isPlaying)
                Debug.LogWarning("[Save] Changed during Play: autosave will overwrite it. Stop Play and press Play again.");
        }
    }
}
