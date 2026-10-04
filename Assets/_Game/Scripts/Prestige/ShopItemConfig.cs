using System;

namespace ShaurmaTycoon.Prestige
{
    /// <summary>
    /// Пункт магазина репутации: значения и цены по уровням (уровень 1 — Values[0] за Prices[0] и т. д.).
    /// </summary>
    [Serializable]
    public sealed class ShopItemConfig
    {
        public string Id;
        public string NameKey;
        public string DescriptionKey;
        public ShopEffect Effect;
        public double[] Values = new double[0];
        public long[] Prices = new long[0];

        public int MaxLevel => Math.Min(Values?.Length ?? 0, Prices?.Length ?? 0);

        /// <summary>Значение эффекта на уровне level (0 — значение «без покупок»).</summary>
        public double GetValue(int level)
        {
            if (level <= 0)
                return BaseValue(Effect);
            return Values[Math.Min(level, MaxLevel) - 1];
        }

        /// <summary>Цена следующего уровня после level или -1, если уровень максимальный.</summary>
        public long GetPrice(int level)
        {
            return level >= 0 && level < MaxLevel ? Prices[level] : -1;
        }

        public static double BaseValue(ShopEffect effect)
        {
            return effect == ShopEffect.CycleTimeFactor ? 1 : 0;
        }
    }
}
