using System;
using System.Collections.Generic;

namespace ShaurmaTycoon.Save
{
    /// <summary>
    /// Всё сохраняемое состояние игры (JSON через JsonUtility: только поля, словари — списками).
    /// Новые поля добавлять со значением по умолчанию и повышать CurrentVersion, миграция — в SaveMigrator.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public double Coins;

        /// <summary>Купюры (донатная валюта) — не сбрасываются при переезде.</summary>
        public long Bills;

        /// <summary>Время последнего сохранения, DateTime.Ticks в UTC (для офлайн-дохода).</summary>
        public long SavedAtUtcTicks;

        public List<StationSave> Stations = new List<StationSave>();
        public List<string> PurchasedUpgrades = new List<string>();

        public PrestigeSave Prestige = new PrestigeSave();
        public SettingsSave Settings = new SettingsSave();
        public AdRewardSave AdBills = new AdRewardSave();

        /// <summary>Куплен «Стартовый набор» (постоянная покупка, не повторяется).</summary>
        public bool StarterPackBought;

        /// <summary>Куплено «Без рекламы» (постоянная покупка).</summary>
        public bool NoAdsBought;

        /// <summary>Куплен «Постоянный ×2» (постоянная покупка).</summary>
        public bool IncomeX2Bought;
    }
}
