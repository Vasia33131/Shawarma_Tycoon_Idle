using System;

namespace ShaurmaTycoon.Ambient
{
    /// <summary>
    /// Погода по реальному времени: сутки делятся на UTC-слоты (по умолчанию 10 мин). В каждом слоте детерминированный хеш
    /// от номера слота и id города решает, будут ли осадки (вероятность — rainChance города), когда они начнутся и сколько
    /// продлятся (2–4 мин, целиком внутри слота). Зимой (декабрь–февраль) в городах со снегом вместо дождя — снег.
    /// Одно и то же время у всех игроков даёт одну и ту же погоду; состояние не сохраняется. Без аллокаций.
    /// </summary>
    public sealed class WeatherSchedule
    {
        private readonly long _slotTicks;
        private readonly double _slotMinutes;
        private readonly double _minDuration;
        private readonly double _maxDuration;

        /// <param name="slotMinutes">Длина слота, мин.</param>
        /// <param name="minDurationMinutes">Самые короткие осадки, мин.</param>
        /// <param name="maxDurationMinutes">Самые длинные осадки, мин (не больше слота).</param>
        public WeatherSchedule(double slotMinutes, double minDurationMinutes, double maxDurationMinutes)
        {
            if (!(slotMinutes > 0))
                throw new ArgumentOutOfRangeException(nameof(slotMinutes));
            _slotMinutes = slotMinutes;
            _slotTicks = (long)(slotMinutes * TimeSpan.TicksPerMinute);
            _maxDuration = Math.Max(0, Math.Min(maxDurationMinutes, slotMinutes));
            _minDuration = Math.Max(0, Math.Min(minDurationMinutes, _maxDuration));
        }

        public WeatherKind Get(DateTime utc, string cityId, float rainChance, bool snowAllowed)
        {
            if (!(rainChance > 0f))
                return WeatherKind.Clear;

            long slot = utc.Ticks / _slotTicks;
            ulong hash = Mix(HashId(cityId) ^ Mix((ulong)slot));
            double roll = (hash & 0xFFFFFF) / 16777216.0;
            if (roll >= rainChance)
                return WeatherKind.Clear;

            double duration = _minDuration + ((hash >> 24) & 0xFFFF) / 65536.0 * (_maxDuration - _minDuration);
            double start = ((hash >> 40) & 0xFFFF) / 65536.0 * (_slotMinutes - duration);
            double minute = (utc.Ticks - slot * _slotTicks) / (double)TimeSpan.TicksPerMinute;
            if (minute < start || minute >= start + duration)
                return WeatherKind.Clear;

            return snowAllowed && IsWinter(utc.Month) ? WeatherKind.Snow : WeatherKind.Rain;
        }

        public static bool IsWinter(int month)
        {
            return month == 12 || month == 1 || month == 2;
        }

        // FNV-1a: string.GetHashCode в .NET может отличаться между запусками.
        private static ulong HashId(string id)
        {
            unchecked
            {
                ulong hash = 14695981039346656037UL;
                if (id != null)
                {
                    for (int i = 0; i < id.Length; i++)
                    {
                        hash ^= id[i];
                        hash *= 1099511628211UL;
                    }
                }
                return hash;
            }
        }

        // SplitMix64: соседние слоты дают несвязанные значения.
        private static ulong Mix(ulong z)
        {
            unchecked
            {
                z += 0x9E3779B97F4A7C15UL;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }
    }
}
