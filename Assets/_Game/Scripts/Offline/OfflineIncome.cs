namespace ShaurmaTycoon.Offline
{
    /// <summary>Результат расчёта офлайн-дохода.</summary>
    public readonly struct OfflineIncome
    {
        public static readonly OfflineIncome None = new OfflineIncome(0, 0, false);

        public OfflineIncome(double seconds, double amount, bool isCapped)
        {
            Seconds = seconds;
            Amount = amount;
            IsCapped = isCapped;
        }

        /// <summary>Засчитанное время отсутствия (уже с учётом лимита).</summary>
        public double Seconds { get; }

        /// <summary>Сколько монет начислить (без бонуса рекламы).</summary>
        public double Amount { get; }

        /// <summary>Отсутствие было дольше лимита и обрезано.</summary>
        public bool IsCapped { get; }

        public bool HasIncome => Amount > 0;
    }
}
