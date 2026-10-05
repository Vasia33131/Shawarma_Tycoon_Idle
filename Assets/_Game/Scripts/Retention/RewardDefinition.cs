using System;

namespace ShaurmaTycoon.Retention
{
    /// <summary>Награда в конфиге: для монет Amount — минуты дохода, для купюр и репутации — количество.</summary>
    [Serializable]
    public struct RewardDefinition
    {
        public RewardKind Kind;
        public double Amount;

        public RewardDefinition(RewardKind kind, double amount)
        {
            Kind = kind;
            Amount = amount;
        }
    }
}
