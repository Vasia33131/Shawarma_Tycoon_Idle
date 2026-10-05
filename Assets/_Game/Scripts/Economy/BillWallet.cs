using System;

namespace ShaurmaTycoon.Economy
{
    /// <summary>Откуда пришли купюры (для будущей аналитики).</summary>
    public enum BillSource
    {
        Purchase,
        Ad,
        Debug,
        Reward,
    }

    /// <summary>На что потрачены купюры.</summary>
    public enum BillSink
    {
        Coins,
        Station,
        Upgrade,
        Manager,
    }

    /// <summary>
    /// Купюры — донатная валюта игрока. Живёт в Boot и не сбрасывается при переезде в новый город.
    /// Значение всегда неотрицательное.
    /// </summary>
    public sealed class BillWallet
    {
        public long Bills { get; private set; }

        /// <summary>Аргументы — новое количество и изменение (+N при начислении, −N при трате, 0 при загрузке).</summary>
        public event Action<long, long> BillsChanged;

        public void Add(long amount, BillSource source)
        {
            if (amount <= 0)
                return;

            long before = Bills;
            Bills = amount > long.MaxValue - Bills ? long.MaxValue : Bills + amount;
            BillsChanged?.Invoke(Bills, Bills - before);
        }

        public bool CanAfford(long amount)
        {
            return amount >= 0 && amount <= Bills;
        }

        public bool TrySpend(long amount, BillSink sink)
        {
            if (!CanAfford(amount))
                return false;

            if (amount > 0)
            {
                Bills -= amount;
                BillsChanged?.Invoke(Bills, -amount);
            }
            return true;
        }

        /// <summary>Загрузка сейва. Отрицательное значение превращается в 0.</summary>
        public void SetBills(long value)
        {
            Bills = Math.Max(0, value);
            BillsChanged?.Invoke(Bills, 0);
        }
    }
}
