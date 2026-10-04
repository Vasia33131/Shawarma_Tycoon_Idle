using System;

namespace ShaurmaTycoon.Prestige
{
    /// <summary>
    /// Формулы престижа (GDD §7): всего репутации = floor(sqrt(заработано за всё время / делитель)),
    /// множитель дохода = 1 + бонус × полученная репутация.
    /// </summary>
    public readonly struct PrestigeRules
    {
        public PrestigeRules(double reputationDivisor, double incomeBonusPerReputation)
        {
            if (!(reputationDivisor > 0))
                throw new ArgumentOutOfRangeException(nameof(reputationDivisor), reputationDivisor, "Must be positive.");
            ReputationDivisor = reputationDivisor;
            IncomeBonusPerReputation = Math.Max(0, incomeBonusPerReputation);
        }

        public double ReputationDivisor { get; }
        public double IncomeBonusPerReputation { get; }

        public long TotalReputationFor(double lifetimeEarned)
        {
            if (!(lifetimeEarned > 0))
                return 0;

            double total = Math.Floor(Math.Sqrt(lifetimeEarned / ReputationDivisor));
            return total >= long.MaxValue ? long.MaxValue : (long)total;
        }

        /// <summary>Сколько репутации даст переход сейчас («минус уже полученная»).</summary>
        public long ReputationGain(double lifetimeEarned, long reputationEarned)
        {
            return Math.Max(0, TotalReputationFor(lifetimeEarned) - reputationEarned);
        }

        public double IncomeMultiplier(long reputationEarned)
        {
            return 1 + IncomeBonusPerReputation * Math.Max(0, reputationEarned);
        }
    }
}
