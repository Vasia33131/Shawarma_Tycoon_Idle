using System;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Platform;
using ShaurmaTycoon.Save;

namespace ShaurmaTycoon.Shop
{
    /// <summary>
    /// Стартовый набор: покупка за реальные деньги один раз навсегда — купюры и монеты.
    /// Монеты = стоимость CoinBills купюр по курсу игры (но не меньше MinIncomeMinutes минут дохода),
    /// считаются в момент покупки. Товар постоянный: платформе его не «подтверждают», а факт покупки живёт в сейве.
    /// Монеты за покупку не считаются заработком для престижа.
    /// </summary>
    public sealed class StarterPack : ISaveParticipant
    {
        private const double SecondsPerMinute = 60;

        private readonly StarterPackConfig _config;
        private readonly BillWallet _bills;
        private readonly Wallet _coins;
        private readonly BillPricing _pricing;
        private readonly Func<double> _incomePerSecond;
        private readonly IPlatformService _platform;
        private readonly Action _saveNow;

        /// <param name="incomePerSecond">Текущий доход города в секунду.</param>
        /// <param name="saveNow">Немедленное сохранение после покупки; может быть null.</param>
        public StarterPack(StarterPackConfig config, BillWallet bills, Wallet coins, BillPricing pricing, Func<double> incomePerSecond,
            IPlatformService platform, Action saveNow)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _bills = bills ?? throw new ArgumentNullException(nameof(bills));
            _coins = coins ?? throw new ArgumentNullException(nameof(coins));
            _pricing = pricing ?? throw new ArgumentNullException(nameof(pricing));
            _incomePerSecond = incomePerSecond ?? throw new ArgumentNullException(nameof(incomePerSecond));
            _platform = platform;
            _saveNow = saveNow;
        }

        /// <summary>Набор куплен (выдан) — карточку больше не показываем.</summary>
        public event Action Purchased;

        public bool IsPurchased { get; private set; }

        /// <summary>Идёт оплата: окно платформы открыто, повторное нажатие запрещено.</summary>
        public bool IsPurchasing { get; private set; }

        public StarterPackConfig Config => _config;
        public long Bills => _config.Bills;
        public int DisplayPrice => _config.DisplayPrice;
        public bool CanBuy => !IsPurchased && !IsPurchasing && _platform != null;

        /// <summary>Сколько монет получит игрок, если купит сейчас.</summary>
        public double GetCoinReward()
        {
            double income = _incomePerSecond();
            if (!(income >= 0) || double.IsInfinity(income))
                income = 0;

            double reward = Math.Max(_config.CoinBills * _pricing.CoinsPerBill, income * _config.MinIncomeMinutes * SecondsPerMinute);
            return double.IsInfinity(reward) ? double.MaxValue : reward;
        }

        /// <summary>Открывает оплату. onDone(true) — набор выдан.</summary>
        /// <returns>false — оплата не началась.</returns>
        public bool Buy(Action<bool> onDone)
        {
            if (!CanBuy)
                return false;

            IsPurchasing = true;
            _platform.Purchase(_config.ProductId, success =>
            {
                IsPurchasing = false;
                if (success)
                    Grant();
                onDone?.Invoke(success);
            });
            return true;
        }

        /// <summary>Выдаёт набор, оплаченный раньше (игра закрылась до выдачи или сейв потерян). Уже выданный — не повторяет.</summary>
        /// <returns>true — это id стартового набора (выдан сейчас или уже был выдан).</returns>
        public bool TryGrantPurchased(string productId)
        {
            if (productId != _config.ProductId)
                return false;

            if (!IsPurchased)
                Grant();
            return true;
        }

        private void Grant()
        {
            double coins = GetCoinReward();
            IsPurchased = true;
            _bills.Add(_config.Bills, BillSource.Purchase);
            _coins.Add(coins);
            _saveNow?.Invoke();
            Purchased?.Invoke();
        }

        public void Capture(SaveData data)
        {
            data.StarterPackBought = IsPurchased;
        }

        public void Apply(SaveData data)
        {
            IsPurchased = data.StarterPackBought;
        }
    }
}
