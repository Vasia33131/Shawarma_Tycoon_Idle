using System;

namespace ShaurmaTycoon.Economy
{
    /// <summary>
    /// Сборщик короткого текста в переиспользуемый char[] — без аллокаций после прогрева.
    /// AppendNumber форматирует как BigNumberFormatter: 999, 1.23K, 45.6M, 789B, 1.00T, 1.00aa…
    /// (три значащие цифры, лишнее отбрасывается — игрок не видит больше, чем у него есть).
    /// </summary>
    public sealed class NumberText
    {
        // Компенсирует погрешность double при отбрасывании (1.23 * 100 = 122.99999…).
        private const double TruncateEpsilon = 1e-9;

        private static readonly string[] Suffixes = BuildSuffixes();

        private char[] _buffer;

        public NumberText(int capacity = 32)
        {
            _buffer = new char[Math.Max(capacity, 8)];
        }

        public char[] Buffer => _buffer;
        public int Length { get; private set; }

        public NumberText Clear()
        {
            Length = 0;
            return this;
        }

        public NumberText Append(char value)
        {
            EnsureCapacity(1);
            _buffer[Length++] = value;
            return this;
        }

        public NumberText Append(string value)
        {
            if (string.IsNullOrEmpty(value))
                return this;

            EnsureCapacity(value.Length);
            value.CopyTo(0, _buffer, Length, value.Length);
            Length += value.Length;
            return this;
        }

        public NumberText AppendInteger(long value)
        {
            if (value < 0)
            {
                Append('-');
                value = -value;
            }

            int digits = 1;
            for (long rest = value / 10; rest > 0; rest /= 10)
                digits++;

            EnsureCapacity(digits);
            for (int i = Length + digits - 1; i >= Length; i--)
            {
                _buffer[i] = (char)('0' + value % 10);
                value /= 10;
            }
            Length += digits;
            return this;
        }

        public NumberText AppendNumber(double value)
        {
            if (double.IsNaN(value))
                return Append('0');
            if (double.IsPositiveInfinity(value))
                return Append('∞');
            if (double.IsNegativeInfinity(value))
                return Append('-').Append('∞');
            if (value < 0)
            {
                Append('-');
                value = -value;
            }
            if (value < 1000)
                return AppendInteger((long)Math.Floor(value));

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

            int decimals = scaled < 10 ? 2 : scaled < 100 ? 1 : 0;
            AppendFixed(scaled, decimals);
            return Append(Suffixes[tier]);
        }

        public override string ToString()
        {
            return new string(_buffer, 0, Length);
        }

        // Пишет value с decimals знаками после точки, отбрасывая (а не округляя) остальные.
        private void AppendFixed(double value, int decimals)
        {
            long factor = decimals == 2 ? 100 : decimals == 1 ? 10 : 1;
            long digits = (long)Math.Floor(value * factor + TruncateEpsilon);

            AppendInteger(digits / factor);
            if (decimals == 0)
                return;

            Append('.');
            long fraction = digits % factor;
            if (decimals == 2 && fraction < 10)
                Append('0');
            AppendInteger(fraction);
        }

        private void EnsureCapacity(int extra)
        {
            if (Length + extra > _buffer.Length)
                Array.Resize(ref _buffer, Math.Max(_buffer.Length * 2, Length + extra));
        }

        // "", K, M, B, T, затем aa…az, ba…zz — 681 ступень, с запасом покрывает весь диапазон double (~1e308 → «dt»).
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
