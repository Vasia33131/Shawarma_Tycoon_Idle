using System;
using System.Collections.Generic;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Platform;

namespace ShaurmaTycoon.Shop
{
    /// <summary>
    /// Магазин купюр: покупка паков через платформу (купюры — только после подтверждения оплаты)
    /// и обмен купюр на монеты. Купленные монеты не считаются заработком для престижа.
    /// После каждой покупки игра сразу сохраняется.
    /// </summary>
    public sealed class BillShop
    {
        private const double SecondsPerMinute = 60;

        private readonly IReadOnlyList<BillPackConfig> _billPacks;
        private readonly IReadOnlyList<CoinPackConfig> _coinPacks;
        private readonly BillWallet _bills;
        private readonly Wallet _coins;
        private readonly Func<double> _incomePerSecond;
        private readonly double _cityScale;
        private readonly IPlatformService _platform;
        private readonly Action _saveNow;

        /// <param name="incomePerSecond">Текущий доход города в секунду.</param>
        /// <param name="cityScale">Масштаб значений города — для минимальной награды мешка.</param>
        /// <param name="saveNow">Немедленное сохранение после покупки; может быть null.</param>
        public BillShop(IReadOnlyList<BillPackConfig> billPacks, IReadOnlyList<CoinPackConfig> coinPacks, BillWallet bills, Wallet coins,
            Func<double> incomePerSecond, double cityScale, IPlatformService platform, Action saveNow)
        {
            _billPacks = billPacks ?? Array.Empty<BillPackConfig>();
            _coinPacks = coinPacks ?? Array.Empty<CoinPackConfig>();
            _bills = bills ?? throw new ArgumentNullException(nameof(bills));
            _coins = coins ?? throw new ArgumentNullException(nameof(coins));
            _incomePerSecond = incomePerSecond ?? throw new ArgumentNullException(nameof(incomePerSecond));
            _cityScale = cityScale > 0 && !double.IsInfinity(cityScale) ? cityScale : 1;
            _platform = platform;
            _saveNow = saveNow;
        }

        /// <summary>Аргумент — индекс пака купюр.</summary>
        public event Action<int> BillsPurchased;

        /// <summary>Аргументы — индекс мешка и сколько монет начислено.</summary>
        public event Action<int, double> CoinsPurchased;

        /// <summary>Идёт оплата пака: окно платформы открыто, повторные покупки запрещены.</summary>
        public bool IsPurchasing { get; private set; }

        public int BillPackCount => _billPacks.Count;
        public int CoinPackCount => _coinPacks.Count;

        public BillPackConfig GetBillPack(int index) => _billPacks[index];
        public CoinPackConfig GetCoinPack(int index) => _coinPacks[index];

        public double GetCoinReward(int index)
        {
            CoinPackConfig pack = _coinPacks[index];
            double income = _incomePerSecond();
            if (!(income >= 0) || double.IsInfinity(income))
                income = 0;

            double reward = Math.Max(income * pack.IncomeMinutes * SecondsPerMinute, pack.MinCoins * _cityScale);
            return double.IsInfinity(reward) ? double.MaxValue : reward;
        }

        public bool CanBuyCoins(int index)
        {
            return IsValid(index, _coinPacks.Count) && _bills.CanAfford(_coinPacks[index].PriceBills);
        }

        public bool TryBuyCoins(int index)
        {
            if (!CanBuyCoins(index))
                return false;

            double reward = GetCoinReward(index);
            if (!_bills.TrySpend(_coinPacks[index].PriceBills, BillSink.Coins))
                return false;

            _coins.Add(reward);
            _saveNow?.Invoke();
            CoinsPurchased?.Invoke(index, reward);
            return true;
        }

        /// <summary>Открывает оплату пака. onDone(true) — купюры начислены.</summary>
        /// <returns>false — оплата не началась (нет платформы, идёт другая покупка, неверный индекс).</returns>
        public bool BuyBillPack(int index, Action<bool> onDone)
        {
            if (_platform == null || IsPurchasing || !IsValid(index, _billPacks.Count))
                return false;

            IsPurchasing = true;
            BillPackConfig pack = _billPacks[index];
            _platform.Purchase(pack.ProductId, success =>
            {
                IsPurchasing = false;
                if (success)
                    GrantPack(index);
                onDone?.Invoke(success);
            });
            return true;
        }

        /// <summary>
        /// Выдаёт оплаченный пак по id товара платформы (покупка не выдана до перезапуска) и подтверждает её.
        /// </summary>
        /// <returns>false — такого пака нет.</returns>
        public bool TryGrantPurchasedPack(string productId)
        {
            for (int i = 0; i < _billPacks.Count; i++)
            {
                if (_billPacks[i].ProductId != productId)
                    continue;

                GrantPack(i);
                return true;
            }

            return false;
        }

        // Купюры → сохранение → только потом подтверждение платформе: если игра упадёт между шагами,
        // покупка останется неподтверждённой и выдастся при следующем запуске.
        private void GrantPack(int index)
        {
            BillPackConfig pack = _billPacks[index];
            _bills.Add(pack.Bills, BillSource.Purchase);
            _saveNow?.Invoke();
            _platform?.ConsumePurchase(pack.ProductId);
            BillsPurchased?.Invoke(index);
        }

        private static bool IsValid(int index, int count)
        {
            return index >= 0 && index < count;
        }
    }
}
