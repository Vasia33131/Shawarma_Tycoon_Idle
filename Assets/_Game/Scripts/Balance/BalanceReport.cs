using System.Collections.Generic;

namespace ShaurmaTycoon.Balance
{
    /// <summary>Итог прогона баланса. Время в секундах от старта города; −1 — событие не случилось.</summary>
    public sealed class BalanceReport
    {
        public BalanceReport(int stationCount, int upgradeCount)
        {
            StationUnlockedAt = Filled(stationCount);
            ManagerHiredAt = Filled(stationCount);
            UpgradeBoughtAt = Filled(upgradeCount);
            FinalLevels = new int[stationCount];
        }

        public double[] StationUnlockedAt { get; }
        public double[] ManagerHiredAt { get; }
        public double[] UpgradeBoughtAt { get; }
        public int[] FinalLevels { get; }
        public List<BalancePurchase> Purchases { get; } = new List<BalancePurchase>();
        public List<BalanceSample> Samples { get; } = new List<BalanceSample>();

        /// <summary>Когда заработано всего ≥ порога переезда.</summary>
        public double ThresholdReachedAt { get; set; } = -1;

        public double TotalEarned { get; set; }
        public double SimulatedSeconds { get; set; }
        public int LevelsBought { get; set; }
        public int Taps { get; set; }

        /// <summary>Сколько покупок сделано за первые seconds секунд.</summary>
        public int PurchasesBefore(double seconds)
        {
            int count = 0;
            for (int i = 0; i < Purchases.Count && Purchases[i].Time <= seconds; i++)
                count++;
            return count;
        }

        private static double[] Filled(int count)
        {
            var array = new double[count];
            for (int i = 0; i < count; i++)
                array[i] = -1;
            return array;
        }
    }
}
