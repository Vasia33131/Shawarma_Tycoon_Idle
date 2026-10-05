namespace ShaurmaTycoon.Balance
{
    /// <summary>Снимок прогона баланса: время, заработано всего, доход в секунду.</summary>
    public readonly struct BalanceSample
    {
        public BalanceSample(double time, double earned, double incomePerSecond)
        {
            Time = time;
            Earned = earned;
            IncomePerSecond = incomePerSecond;
        }

        public double Time { get; }
        public double Earned { get; }
        public double IncomePerSecond { get; }
    }
}
