using System;
using ShaurmaTycoon.Economy;

namespace ShaurmaTycoon.Offline
{
    /// <summary>
    /// Ожидающая награда за офлайн. Забрать можно один раз — с множителем 1 или за рекламу (×2).
    /// </summary>
    public sealed class OfflineIncomeService
    {
        private readonly Wallet _wallet;

        public OfflineIncomeService(Wallet wallet, OfflineIncome pending, double adMultiplier)
        {
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            Pending = pending;
            AdMultiplier = Math.Max(1, adMultiplier);
        }

        /// <summary>Аргумент — сколько монет зачислено.</summary>
        public event Action<double> Claimed;

        public OfflineIncome Pending { get; }
        public double AdMultiplier { get; }
        public bool IsClaimed { get; private set; }
        public bool HasPending => !IsClaimed && Pending.HasIncome;

        public bool TryClaim(bool withAdBonus)
        {
            if (!HasPending)
                return false;

            double amount = Pending.Amount * (withAdBonus ? AdMultiplier : 1);
            IsClaimed = true;
            _wallet.Add(amount);
            Claimed?.Invoke(amount);
            return true;
        }
    }
}
