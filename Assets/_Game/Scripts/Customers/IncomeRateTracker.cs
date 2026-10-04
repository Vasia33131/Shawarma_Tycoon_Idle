using System;

namespace ShaurmaTycoon.Customers
{
    /// <summary>
    /// Сглаженный фактический доход в секунду (включая ручные тапы) — для визуала: длины очереди и темпа клиентов.
    /// Экспоненциальное сглаживание с постоянной времени TimeConstant.
    /// </summary>
    public sealed class IncomeRateTracker
    {
        private double _pending;

        public IncomeRateTracker(double timeConstant = 5)
        {
            if (!(timeConstant > 0))
                throw new ArgumentOutOfRangeException(nameof(timeConstant), timeConstant, "Must be positive.");
            TimeConstant = timeConstant;
        }

        public double TimeConstant { get; }

        public double Rate { get; private set; }

        public void Add(double amount)
        {
            if (amount > 0 && !double.IsInfinity(amount))
                _pending += amount;
        }

        public void Tick(double deltaTime)
        {
            if (!(deltaTime > 0))
                return;

            double instantRate = _pending / deltaTime;
            _pending = 0;
            Rate += (instantRate - Rate) * (1 - Math.Exp(-deltaTime / TimeConstant));
        }
    }
}
