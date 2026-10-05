using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShaurmaTycoon.Shop
{
    /// <summary>Пак купюр за реальные деньги (на Яндексе — за «яны»).</summary>
    [Serializable]
    public sealed class BillPackConfig
    {
        public string Id = string.Empty;
        [Tooltip("Id товара в консоли платформы (Яндекс: каталог покупок).")]
        public string ProductId = string.Empty;
        public string NameKey = string.Empty;
        public long Bills = 20;
        [Tooltip("Цена для показа, пока нет каталога платформы (этап 09 возьмёт цену из каталога).")]
        public int DisplayPrice = 19;
        [Tooltip("Метка «Выгодно!».")]
        public bool Best;
    }

    /// <summary>Мешок монет за купюры: N минут текущего дохода, но не меньше минимума × масштаб города.</summary>
    [Serializable]
    public sealed class CoinPackConfig
    {
        public string Id = string.Empty;
        public string NameKey = string.Empty;
        public long PriceBills = 5;
        public double IncomeMinutes = 30;
        [Tooltip("Минимум монет (× масштаб города), если доход ещё маленький.")]
        public double MinCoins = 1000;
    }

    /// <summary>Магазин во вкладке «Магазин» (этап 08d): паки купюр и мешки монет.</summary>
    [CreateAssetMenu(menuName = "ShaurmaTycoon/Bill Shop Config", fileName = "BillShop")]
    public sealed class BillShopConfig : ScriptableObject
    {
        [SerializeField] private BillPackConfig[] _billPacks = new BillPackConfig[0];
        [SerializeField] private CoinPackConfig[] _coinPacks = new CoinPackConfig[0];

        [Header("Bills instead of coins (08e)")]
        [Tooltip("1 купюра = доход за столько секунд. 360 — как у малого мешка (30 мин за 5 купюр).")]
        [SerializeField] private double _secondsPerBill = 360;
        [Tooltip("Минимум монет за купюру (× масштаб города), пока доход маленький. 200 — как у малого мешка (1000 за 5).")]
        [SerializeField] private double _minCoinsPerBill = 200;

        public IReadOnlyList<BillPackConfig> BillPacks => _billPacks;
        public IReadOnlyList<CoinPackConfig> CoinPacks => _coinPacks;
        public double SecondsPerBill => _secondsPerBill;
        public double MinCoinsPerBill => _minCoinsPerBill;

        private void OnValidate()
        {
            if (!(_secondsPerBill > 0))
                _secondsPerBill = 1;
            if (!(_minCoinsPerBill > 0))
                _minCoinsPerBill = 1;
        }
    }
}
