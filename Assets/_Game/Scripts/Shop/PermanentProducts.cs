using System;
using ShaurmaTycoon.Platform;
using ShaurmaTycoon.Save;

namespace ShaurmaTycoon.Shop
{
    public enum PermanentProduct
    {
        /// <summary>Без межстраничной рекламы (реклама за награду остаётся по желанию игрока).</summary>
        NoAds,

        /// <summary>Весь доход ×2 навсегда.</summary>
        IncomeX2,
    }

    /// <summary>
    /// Постоянные покупки за реальные деньги: «Без рекламы» и «Постоянный ×2». Куплены один раз навсегда,
    /// платформе не «подтверждаются» (не расходуемые), факт покупки хранится в сейве.
    /// Эффекты применяют подписчики Purchased и вызывающий код через Has.
    /// </summary>
    public sealed class PermanentProducts : ISaveParticipant
    {
        public const double IncomeX2Multiplier = 2;

        private readonly PermanentOfferConfig _noAds;
        private readonly PermanentOfferConfig _incomeX2;
        private readonly IPlatformService _platform;
        private readonly Action _saveNow;
        private bool _hasNoAds;
        private bool _hasIncomeX2;

        public PermanentProducts(PermanentOfferConfig noAds, PermanentOfferConfig incomeX2, IPlatformService platform, Action saveNow)
        {
            _noAds = noAds ?? throw new ArgumentNullException(nameof(noAds));
            _incomeX2 = incomeX2 ?? throw new ArgumentNullException(nameof(incomeX2));
            _platform = platform;
            _saveNow = saveNow;
        }

        /// <summary>Товар только что выдан (не поднимается при загрузке сейва).</summary>
        public event Action<PermanentProduct> Purchased;

        /// <summary>Идёт оплата: окно платформы открыто, другие покупки запрещены.</summary>
        public bool IsPurchasing { get; private set; }

        /// <summary>Множитель дохода от покупок: 2 при «Постоянный ×2», иначе 1.</summary>
        public double IncomeMultiplier => _hasIncomeX2 ? IncomeX2Multiplier : 1;

        public PermanentOfferConfig GetOffer(PermanentProduct product) => product == PermanentProduct.NoAds ? _noAds : _incomeX2;

        public bool Has(PermanentProduct product) => product == PermanentProduct.NoAds ? _hasNoAds : _hasIncomeX2;

        public bool CanBuy(PermanentProduct product) => !Has(product) && !IsPurchasing && _platform != null;

        /// <summary>Открывает оплату. onDone(true) — товар выдан.</summary>
        /// <returns>false — оплата не началась.</returns>
        public bool Buy(PermanentProduct product, Action<bool> onDone)
        {
            if (!CanBuy(product))
                return false;

            IsPurchasing = true;
            _platform.Purchase(GetOffer(product).ProductId, success =>
            {
                IsPurchasing = false;
                if (success)
                    Grant(product);
                onDone?.Invoke(success);
            });
            return true;
        }

        /// <summary>Выдаёт товар, оплаченный раньше (сейв потерян или игра закрылась до выдачи). Уже выданный не повторяет.</summary>
        /// <returns>true — это id одного из постоянных товаров.</returns>
        public bool TryGrantPurchased(string productId)
        {
            foreach (PermanentProduct product in new[] { PermanentProduct.NoAds, PermanentProduct.IncomeX2 })
            {
                if (GetOffer(product).ProductId != productId)
                    continue;

                if (!Has(product))
                    Grant(product);
                return true;
            }

            return false;
        }

        private void Grant(PermanentProduct product)
        {
            if (product == PermanentProduct.NoAds)
                _hasNoAds = true;
            else
                _hasIncomeX2 = true;

            _saveNow?.Invoke();
            Purchased?.Invoke(product);
        }

        public void Capture(SaveData data)
        {
            data.NoAdsBought = _hasNoAds;
            data.IncomeX2Bought = _hasIncomeX2;
        }

        public void Apply(SaveData data)
        {
            _hasNoAds = data.NoAdsBought;
            _hasIncomeX2 = data.IncomeX2Bought;
        }
    }
}
