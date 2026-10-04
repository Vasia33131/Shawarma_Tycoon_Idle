using System;

namespace ShaurmaTycoon.Offline
{
    /// <summary>
    /// Офлайн-доход (GDD §6): доход/сек станций с менеджером × засчитанное время × доля.
    /// Нет сохранения, часы переведены назад или отсутствие короче минимума — ничего.
    /// </summary>
    public static class OfflineIncomeCalculator
    {
        /// <param name="automatedIncomePerSecond">Доход в секунду только станций с менеджером.</param>
        /// <param name="savedAtUtcTicks">Время последнего сохранения (0 — сохранения не было).</param>
        public static OfflineIncome Calculate(double automatedIncomePerSecond, long savedAtUtcTicks, DateTime nowUtc,
            OfflineSettings settings)
        {
            if (savedAtUtcTicks <= 0 || !(automatedIncomePerSecond > 0) || double.IsInfinity(automatedIncomePerSecond))
                return OfflineIncome.None;

            double awaySeconds = (nowUtc.Ticks - savedAtUtcTicks) / (double)TimeSpan.TicksPerSecond;
            if (!(awaySeconds >= settings.MinAwaySeconds) || awaySeconds <= 0)
                return OfflineIncome.None;

            bool capped = awaySeconds > settings.LimitSeconds;
            double seconds = capped ? settings.LimitSeconds : awaySeconds;
            double amount = automatedIncomePerSecond * seconds * settings.IncomeRate;

            return amount > 0 && !double.IsInfinity(amount)
                ? new OfflineIncome(seconds, amount, capped)
                : OfflineIncome.None;
        }
    }
}
