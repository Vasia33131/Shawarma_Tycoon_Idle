using System;

namespace ShaurmaTycoon.Economy
{
    /// <summary>
    /// Формулы цены уровней (GDD §5): cost(n) = baseCost × growth^n.
    /// level — сколько уровней уже куплено; цена следующего уровня = LevelCost(level).
    /// </summary>
    public static class EconomyMath
    {
        /// <summary>Предел для ×Max, чтобы не переполнить int при вырожденных конфигах.</summary>
        public const int MaxBulkCount = 1_000_000;

        private const double GrowthEpsilon = 1e-12;

        public static double LevelCost(double baseCost, double growth, int level)
        {
            ValidateGrowth(growth);
            return baseCost * Math.Pow(growth, level);
        }

        /// <summary>Цена покупки count уровней подряд, начиная с уровня level (сумма геометрической прогрессии).</summary>
        public static double BulkCost(double baseCost, double growth, int level, int count)
        {
            ValidateGrowth(growth);
            if (count <= 0)
                return 0;

            if (growth - 1 < GrowthEpsilon)
                return baseCost * count;

            return LevelCost(baseCost, growth, level) * (Math.Pow(growth, count) - 1) / (growth - 1);
        }

        /// <summary>Сколько уровней можно купить на money (×Max). Считается формулой, а не циклом.</summary>
        public static int MaxAffordable(double baseCost, double growth, int level, double money)
        {
            ValidateGrowth(growth);
            if (!(money > 0) || !(baseCost > 0))
                return 0;

            double firstCost = LevelCost(baseCost, growth, level);
            if (firstCost > money)
                return 0;

            double estimate = growth - 1 < GrowthEpsilon
                ? Math.Floor(money / baseCost)
                : Math.Floor(Math.Log(money * (growth - 1) / firstCost + 1) / Math.Log(growth));

            int count = (int)Math.Min(Math.Max(estimate, 0), MaxBulkCount);

            // Коррекция погрешности double: обычно 0–1 шаг.
            while (count > 0 && BulkCost(baseCost, growth, level, count) > money)
                count--;
            while (count < MaxBulkCount && BulkCost(baseCost, growth, level, count + 1) <= money)
                count++;

            return count;
        }

        private static void ValidateGrowth(double growth)
        {
            if (!(growth >= 1))
                throw new ArgumentOutOfRangeException(nameof(growth), growth, "Growth must be >= 1.");
        }
    }
}
