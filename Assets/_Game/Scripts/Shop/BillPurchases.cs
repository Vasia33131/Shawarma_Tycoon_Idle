using System;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Managers;
using ShaurmaTycoon.Stations;
using ShaurmaTycoon.Upgrades;

namespace ShaurmaTycoon.Shop
{
    /// <summary>
    /// Покупки за купюры вместо монет (этап 08e): уровни станций, улучшения, менеджеры — по курсу BillPricing.
    /// Цена показывается игроку заранее (quote); при покупке берётся текущая цена, но не больше показанной.
    /// Сама покупка — те же методы сервисов, что и за монеты (Grant*), поэтому результат одинаковый.
    /// </summary>
    public sealed class BillPurchases
    {
        private readonly StationController _stations;
        private readonly UpgradeService _upgrades;
        private readonly ManagerController _managers;
        private readonly BillWallet _bills;
        private readonly Action _saveNow;

        public BillPurchases(StationController stations, UpgradeService upgrades, ManagerController managers, BillWallet bills,
            BillPricing pricing, Action saveNow)
        {
            _stations = stations ?? throw new ArgumentNullException(nameof(stations));
            _upgrades = upgrades ?? throw new ArgumentNullException(nameof(upgrades));
            _managers = managers ?? throw new ArgumentNullException(nameof(managers));
            _bills = bills ?? throw new ArgumentNullException(nameof(bills));
            Pricing = pricing ?? throw new ArgumentNullException(nameof(pricing));
            _saveNow = saveNow;
        }

        public BillPricing Pricing { get; }
        public BillWallet Bills => _bills;

        public long StationPrice(int index, int count)
        {
            if (index < 0 || index >= _stations.Stations.Count || count <= 0)
                return 0;
            return Pricing.BillsFor(_stations.Stations[index].GetCost(count));
        }

        public long UpgradePrice(int index)
        {
            return _upgrades.CanGrant(index) ? Pricing.BillsFor(_upgrades.GetDefinition(index).Cost) : 0;
        }

        public long ManagerPrice(int index)
        {
            return _managers.CanHire(index) ? Pricing.BillsFor(_managers.GetCost(index)) : 0;
        }

        public bool TryBuyStation(int index, int count, long quotedPrice)
        {
            return Pay(StationPrice(index, count), quotedPrice, BillSink.Station) && Finish(_stations.GrantLevels(index, count));
        }

        public bool TryBuyUpgrade(int index, long quotedPrice)
        {
            return Pay(UpgradePrice(index), quotedPrice, BillSink.Upgrade) && Finish(_upgrades.Grant(index));
        }

        public bool TryHireManager(int index, long quotedPrice)
        {
            return Pay(ManagerPrice(index), quotedPrice, BillSink.Manager) && Finish(_managers.Grant(index));
        }

        // price == 0 — покупка невозможна (неверный индекс, уже куплено); цена выросла после показа — не списываем.
        private bool Pay(long price, long quotedPrice, BillSink sink)
        {
            return price > 0 && price <= quotedPrice && _bills.TrySpend(price, sink);
        }

        private bool Finish(bool granted)
        {
            _saveNow?.Invoke();
            return granted;
        }
    }
}
