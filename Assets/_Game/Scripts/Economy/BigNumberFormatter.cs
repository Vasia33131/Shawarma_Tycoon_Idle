using System;
using System.Globalization;

namespace ShaurmaTycoon.Economy
{
    /// <summary>
    /// Форматирует большие числа: 999, 1.23K, 45.6M, 789B, 1.00T, 1.00aa, 1.00ab…
    /// Три значащие цифры, лишнее отбрасывается (игрок не видит больше, чем у него есть).
    /// Создаёт строку — вызывать при изменении значения, а не каждый кадр.
    /// </summary>
    public static class BigNumberFormatter
    {
        // Компенсирует погрешность double при отбрасывании (1.23 * 100 = 122.99999…).
        private const double TruncateEpsilon = 1e-9;

        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
        private static readonly string[] Suffixes = BuildSuffixes();

        public static string Format(double value)
        {
            if (double.IsNaN(value))
                return "0";
            if (double.IsInfinity(value))
                return value > 0 ? "∞" : "-∞";
            if (value < 0)
                return "-" + Format(-value);
            if (value < 1000)
                return Math.Floor(value).ToString("0", Invariant);

            int tier = (int)Math.Floor(Math.Log10(value) / 3);
            double scaled = value / Math.Pow(1000, tier);

            // Log10 может ошибиться на границе тысяч — поправляем.
            if (scaled >= 1000)
            {
                tier++;
                scaled /= 1000;
            }
            else if (scaled < 1)
            {
                tier--;
                scaled *= 1000;
            }

            if (tier >= Suffixes.Length)
                return value.ToString("0.00e0", Invariant);

            if (scaled < 10)
                return Truncate(scaled, 100).ToString("0.00", Invariant) + Suffixes[tier];
            if (scaled < 100)
                return Truncate(scaled, 10).ToString("0.0", Invariant) + Suffixes[tier];
            return Truncate(scaled, 1).ToString("0", Invariant) + Suffixes[tier];
        }

        private static double Truncate(double value, double factor)
        {
            return Math.Floor(value * factor + TruncateEpsilon) / factor;
        }

        // "", K, M, B, T, затем aa…az, ba…zz.
        private static string[] BuildSuffixes()
        {
            const int letters = 26;
            var suffixes = new string[5 + letters * letters];
            suffixes[0] = string.Empty;
            suffixes[1] = "K";
            suffixes[2] = "M";
            suffixes[3] = "B";
            suffixes[4] = "T";

            int index = 5;
            for (int first = 0; first < letters; first++)
            {
                for (int second = 0; second < letters; second++)
                    suffixes[index++] = new string(new[] { (char)('a' + first), (char)('a' + second) });
            }

            return suffixes;
        }
    }
}
