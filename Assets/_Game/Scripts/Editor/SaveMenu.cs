using System;
using ShaurmaTycoon.Platform;
using ShaurmaTycoon.Save;
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

        [MenuItem("ShaurmaTycoon/Save/Shift Save Time −1 h")]
        private static void ShiftOneHour()
        {
            ShiftSaveTime(TimeSpan.FromHours(-1));
        }

        [MenuItem("ShaurmaTycoon/Save/Shift Save Time −3 h")]
        private static void ShiftThreeHours()
        {
            ShiftSaveTime(TimeSpan.FromHours(-3));
        }

        [MenuItem("ShaurmaTycoon/Save/Shift Save Time +1 h (clock moved back)")]
        private static void ShiftToFuture()
        {
            ShiftSaveTime(TimeSpan.FromHours(1));
        }

        /// <summary>
        /// «Прошли сутки» для удержания: календарь, задания дня и подарок сдвигаются на день назад —
        /// при следующем Play доступна награда следующего дня и выдаются новые задания.
        /// </summary>
        [MenuItem("ShaurmaTycoon/Save/Shift Retention Day −1 d")]
        private static void ShiftRetentionDay()
        {
            if (!PlayerPrefs.HasKey(EditorPlatformService.SaveKey))
            {
                Debug.LogWarning("[Save] No save to shift. Play at least 10 seconds first.");
                return;
            }

            var serializer = new JsonSaveSerializer();
            SaveData data = SaveMigrator.Migrate(serializer.Deserialize(PlayerPrefs.GetString(EditorPlatformService.SaveKey)));
            RetentionSave retention = data.Retention;
            long day = TimeSpan.TicksPerDay;
            if (retention.DailyLastClaimDayTicks > day)
                retention.DailyLastClaimDayTicks -= day;
            if (retention.QuestsDayTicks > day)
                retention.QuestsDayTicks -= day;
            if (retention.GiftLastClaimUtcTicks > day)
                retention.GiftLastClaimUtcTicks -= day;
            Write(serializer.Serialize(data), "save with retention day shifted by −1 d");
        }

        /// <summary>Сдвигает время последнего сохранения — имитация отсутствия игрока.</summary>
        private static void ShiftSaveTime(TimeSpan shift)
        {
            if (!PlayerPrefs.HasKey(EditorPlatformService.SaveKey))
            {
                Debug.LogWarning("[Save] No save to shift. Play at least 10 seconds first.");
                return;
            }

            var serializer = new JsonSaveSerializer();
            SaveData data = serializer.Deserialize(PlayerPrefs.GetString(EditorPlatformService.SaveKey));
            data.SavedAtUtcTicks += shift.Ticks;
            Write(serializer.Serialize(data), $"save with time shifted by {shift.TotalHours:+0;-0} h");
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
