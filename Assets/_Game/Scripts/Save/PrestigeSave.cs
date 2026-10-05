using System;
using System.Collections.Generic;

namespace ShaurmaTycoon.Save
{
    /// <summary>Сохранённый прогресс престижа: переживает переход между городами.</summary>
    [Serializable]
    public sealed class PrestigeSave
    {
        public int CityIndex;
        public double CityEarned;
        public double LifetimeEarned;
        public long ReputationEarned;
        public long ReputationSpent;

        /// <summary>Часть ReputationEarned из наград (задания дня), не из переездов.</summary>
        public long ReputationBonus;
        public List<ShopLevelSave> ShopLevels = new List<ShopLevelSave>();
    }
}
