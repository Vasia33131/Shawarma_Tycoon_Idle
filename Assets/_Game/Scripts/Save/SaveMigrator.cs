using System;
using System.Collections.Generic;

namespace ShaurmaTycoon.Save
{
    /// <summary>
    /// Приводит загруженный сейв к текущей схеме: миграции по версии и исправление некорректных значений
    /// (отсутствующие списки, NaN, отрицательные числа, пустые id, дубликаты).
    /// </summary>
    public static class SaveMigrator
    {
        public static SaveData Migrate(SaveData data)
        {
            if (data == null)
                return new SaveData();

            // Сейвы до появления версии считаем версией 1.
            if (data.Version <= 0)
                data.Version = 1;

            // Здесь будут шаги миграции: if (data.Version == 1) { …; data.Version = 2; }

            Normalize(data);
            return data;
        }

        private static void Normalize(SaveData data)
        {
            if (!(data.Coins >= 0) || double.IsInfinity(data.Coins))
                data.Coins = 0;
            if (data.Bills < 0)
                data.Bills = 0;
            if (data.SavedAtUtcTicks < 0)
                data.SavedAtUtcTicks = 0;

            data.Stations = NormalizeStations(data.Stations);
            data.PurchasedUpgrades = NormalizeIds(data.PurchasedUpgrades);
            data.Prestige = NormalizePrestige(data.Prestige);
            if (data.Settings == null)
                data.Settings = new SettingsSave();
            data.AdBills = NormalizeAdBills(data.AdBills);
        }

        private static AdRewardSave NormalizeAdBills(AdRewardSave save)
        {
            if (save == null)
                return new AdRewardSave();
            if (save.LastClaimUtcTicks < 0 || save.LastClaimUtcTicks > DateTime.MaxValue.Ticks)
                save.LastClaimUtcTicks = 0;
            if (save.DayUtcTicks < 0 || save.DayUtcTicks > DateTime.MaxValue.Ticks)
                save.DayUtcTicks = 0;
            if (save.ClaimsToday < 0)
                save.ClaimsToday = 0;
            return save;
        }

        private static PrestigeSave NormalizePrestige(PrestigeSave prestige)
        {
            if (prestige == null)
                return new PrestigeSave();

            if (prestige.CityIndex < 0)
                prestige.CityIndex = 0;
            if (!(prestige.CityEarned >= 0) || double.IsInfinity(prestige.CityEarned))
                prestige.CityEarned = 0;
            if (!(prestige.LifetimeEarned >= prestige.CityEarned) || double.IsInfinity(prestige.LifetimeEarned))
                prestige.LifetimeEarned = prestige.CityEarned;
            if (prestige.ReputationEarned < 0)
                prestige.ReputationEarned = 0;
            if (prestige.ReputationSpent < 0)
                prestige.ReputationSpent = 0;
            if (prestige.ReputationSpent > prestige.ReputationEarned)
                prestige.ReputationSpent = prestige.ReputationEarned;

            var levels = new List<ShopLevelSave>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            if (prestige.ShopLevels != null)
            {
                foreach (ShopLevelSave level in prestige.ShopLevels)
                {
                    if (level == null || string.IsNullOrEmpty(level.Id) || level.Level <= 0 || !seen.Add(level.Id))
                        continue;
                    levels.Add(level);
                }
            }
            prestige.ShopLevels = levels;
            return prestige;
        }

        private static List<StationSave> NormalizeStations(List<StationSave> stations)
        {
            var result = new List<StationSave>(stations?.Count ?? 0);
            if (stations == null)
                return result;

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (StationSave station in stations)
            {
                if (station == null || string.IsNullOrEmpty(station.Id) || !seen.Add(station.Id))
                    continue;

                if (station.Level < 0)
                    station.Level = 0;
                if (!(station.Progress >= 0) || station.Progress >= 1)
                    station.Progress = 0;
                result.Add(station);
            }
            return result;
        }

        private static List<string> NormalizeIds(List<string> ids)
        {
            var result = new List<string>(ids?.Count ?? 0);
            if (ids == null)
                return result;

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in ids)
            {
                if (!string.IsNullOrEmpty(id) && seen.Add(id))
                    result.Add(id);
            }
            return result;
        }
    }
}
