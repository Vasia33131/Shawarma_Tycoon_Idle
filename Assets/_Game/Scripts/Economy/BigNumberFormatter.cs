using System;

namespace ShaurmaTycoon.Economy
{
    /// <summary>
    /// Форматирует большие числа в строку: 999, 1.23K, 45.6M, 789B, 1.00T, 1.00aa, 1.00ab…
    /// Создаёт строку — для текстов, которые меняются каждый кадр, используйте NumberText.
    /// </summary>
    public static class BigNumberFormatter
    {
        [ThreadStatic] private static NumberText _builder;

        public static string Format(double value)
        {
            if (_builder == null)
                _builder = new NumberText();

            return _builder.Clear().AppendNumber(value).ToString();
        }
    }
}
