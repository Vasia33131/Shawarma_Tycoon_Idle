namespace ShaurmaTycoon.Balance
{
    /// <summary>Одна покупка прогона баланса: время, тип и индекс станции или улучшения.</summary>
    public readonly struct BalancePurchase
    {
        public BalancePurchase(double time, BalancePurchaseKind kind, int index)
        {
            Time = time;
            Kind = kind;
            Index = index;
        }

        public double Time { get; }
        public BalancePurchaseKind Kind { get; }
        public int Index { get; }
    }
}
