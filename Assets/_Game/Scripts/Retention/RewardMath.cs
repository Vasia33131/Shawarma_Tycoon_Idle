using System;

namespace ShaurmaTycoon.Retention
{
    /// <summary>Награды «N минут дохода»: доход/с × 60 × N, но не меньше минимума за минуту (в начале игры дохода нет).</summary>
    public static class RewardMath
    {
        public static double CoinsPerMinute(double incomePerSecond, double minCoinsPerMinute)
        {
            double income = incomePerSecond > 0 && !double.IsInfinity(incomePerSecond) ? incomePerSecond * 60 : 0;
            return Math.Max(income, Math.Max(0, minCoinsPerMinute));
        }

        public static double CoinsForMinutes(double coinsPerMinute, double minutes)
        {
            return Math.Floor(Math.Max(0, coinsPerMinute) * Math.Max(0, minutes));
        }

        /// <summary>Начало суток UTC.</summary>
        public static long DayTicks(DateTime nowUtc)
        {
            return nowUtc.Date.Ticks;
        }

        /// <summary>
        /// Сохранённые сутки «из будущего» больше чем на день (часы были переведены вперёд) — прижать к сегодняшним,
        /// иначе игрок ждал бы награду до той даты. На день вперёд оставляем: так часы назад не дают награду повторно.
        /// </summary>
        public static long ClampFutureDay(long savedDayTicks, DateTime nowUtc)
        {
            long today = DayTicks(nowUtc);
            return savedDayTicks > today + TimeSpan.TicksPerDay ? today : savedDayTicks;
        }
    }
}
