using System;

namespace ShaurmaTycoon.Shop
{
    /// <summary>
    /// Курс купюр к монетам — один на всю игру и тот же, что у мешков монет:
    /// 1 купюра = доход за secondsPerBill, но не меньше minCoinsPerBill (× масштаб города), пока доход маленький.
    /// </summary>
    public sealed class BillPricing
    {
        // Защита от «ровно 5 купюр» → 6 из-за погрешности деления.
        private const double RoundingSlack = 1e-9;

        private readonly Func<double> _incomePerSecond;
        private readonly double _secondsPerBill;
        private readonly double _minCoinsPerBill;

        /// <param name="incomePerSecond">Текущий доход города в секунду.</param>
        /// <param name="minCoinsPerBill">Минимум монет за купюру, уже умноженный на масштаб города.</param>
        public BillPricing(Func<double> incomePerSecond, double secondsPerBill, double minCoinsPerBill)
        {
            _incomePerSecond = incomePerSecond ?? throw new ArgumentNullException(nameof(incomePerSecond));
            if (!(secondsPerBill > 0) || double.IsInfinity(secondsPerBill))
                throw new ArgumentOutOfRangeException(nameof(secondsPerBill), secondsPerBill, "Must be positive and finite.");
            if (!(minCoinsPerBill > 0) || double.IsInfinity(minCoinsPerBill))
                throw new ArgumentOutOfRangeException(nameof(minCoinsPerBill), minCoinsPerBill, "Must be positive and finite.");

            _secondsPerBill = secondsPerBill;
            _minCoinsPerBill = minCoinsPerBill;
        }

        public double CoinsPerBill
        {
            get
            {
                double income = _incomePerSecond();
                if (!(income > 0) || double.IsInfinity(income))
                    return _minCoinsPerBill;
                return Math.Max(income * _secondsPerBill, _minCoinsPerBill);
            }
        }

        /// <summary>Сколько купюр стоит покупка ценой coins монет: округление вверх, минимум 1; 0 — бесплатно.</summary>
        public long BillsFor(double coins)
        {
            if (!(coins > 0))
                return 0;
            if (double.IsInfinity(coins))
                return long.MaxValue;

            double bills = Math.Ceiling(coins / CoinsPerBill - RoundingSlack);
            if (bills >= long.MaxValue)
                return long.MaxValue;
            return Math.Max(1, (long)bills);
        }
    }
}
