using System;

namespace ShaurmaTycoon.Economy
{
    /// <summary>
    /// Монеты игрока. Значение всегда конечное и неотрицательное.
    /// </summary>
    public sealed class Wallet
    {
        public double Coins { get; private set; }

        /// <summary>Аргумент — новое количество монет.</summary>
        public event Action<double> CoinsChanged;

        public void Add(double amount)
        {
            if (!IsValidAmount(amount) || amount == 0)
                return;

            SetInternal(Coins + amount);
        }

        public bool CanAfford(double amount)
        {
            return IsValidAmount(amount) && amount <= Coins;
        }

        public bool TrySpend(double amount)
        {
            if (!CanAfford(amount))
                return false;

            if (amount > 0)
                SetInternal(Coins - amount);
            return true;
        }

        /// <summary>Прямая установка (загрузка сейва). Некорректное значение превращается в 0.</summary>
        public void SetCoins(double value)
        {
            SetInternal(IsValidAmount(value) ? value : 0);
        }

        private void SetInternal(double value)
        {
            Coins = double.IsPositiveInfinity(value) ? double.MaxValue : value;
            CoinsChanged?.Invoke(Coins);
        }

        private static bool IsValidAmount(double amount)
        {
            return amount >= 0 && !double.IsInfinity(amount);
        }
    }
}
