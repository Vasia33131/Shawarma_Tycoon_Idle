using System;

namespace ShaurmaTycoon.Localization
{
    /// <summary>Длительность для игрока: «1 ч 23 мин», «2 ч», «5 мин» (не меньше 1 минуты).</summary>
    public static class DurationFormatter
    {
        public static string Format(double seconds, Loc loc)
        {
            long totalMinutes = Math.Max(1, (long)Math.Floor(Math.Max(0, seconds) / 60));
            long hours = totalMinutes / 60;
            long minutes = totalMinutes % 60;

            if (hours == 0)
                return loc.Format("time.minutes", minutes);
            if (minutes == 0)
                return loc.Format("time.hours", hours);
            return loc.Format("time.hours_minutes", hours, minutes);
        }
    }
}
