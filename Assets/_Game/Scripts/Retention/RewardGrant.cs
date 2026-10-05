namespace ShaurmaTycoon.Retention
{
    /// <summary>Выданная награда: Amount — монеты, купюры или репутация (для скина 0).</summary>
    public readonly struct RewardGrant
    {
        public RewardGrant(RewardKind kind, double amount)
        {
            Kind = kind;
            Amount = amount;
        }

        public RewardKind Kind { get; }
        public double Amount { get; }
    }
}
