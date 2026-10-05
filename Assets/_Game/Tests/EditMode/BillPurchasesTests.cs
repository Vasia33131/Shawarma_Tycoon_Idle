using NUnit.Framework;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Managers;
using ShaurmaTycoon.Shop;
using ShaurmaTycoon.Stations;
using ShaurmaTycoon.Upgrades;

namespace ShaurmaTycoon.Tests
{
    public class BillPurchasesTests
    {
        // ---------- BillPricing ----------

        [Test]
        public void Pricing_NoIncome_UsesMinimumRate()
        {
            var pricing = new BillPricing(() => 0, 360, 200);

            Assert.AreEqual(200, pricing.CoinsPerBill);
            Assert.AreEqual(1, pricing.BillsFor(1));
            Assert.AreEqual(1, pricing.BillsFor(200));
            Assert.AreEqual(2, pricing.BillsFor(201));
        }

        [Test]
        public void Pricing_WithIncome_OneBillIsSixMinutesOfIncome()
        {
            var pricing = new BillPricing(() => 10, 360, 200);

            Assert.AreEqual(3600, pricing.CoinsPerBill);
            Assert.AreEqual(5, pricing.BillsFor(18000));
            Assert.AreEqual(6, pricing.BillsFor(18001));
        }

        [TestCase(0)]
        [TestCase(-5)]
        [TestCase(double.NaN)]
        public void Pricing_NothingToPay_IsZero(double coins)
        {
            Assert.AreEqual(0, new BillPricing(() => 10, 360, 200).BillsFor(coins));
        }

        [Test]
        public void Pricing_HugeCost_ClampsToMaxValue()
        {
            var pricing = new BillPricing(() => 0, 360, 1);

            Assert.AreEqual(long.MaxValue, pricing.BillsFor(1e300));
            Assert.AreEqual(long.MaxValue, pricing.BillsFor(double.PositiveInfinity));
        }

        [Test]
        public void Pricing_InvalidIncome_FallsBackToMinimum()
        {
            Assert.AreEqual(200, new BillPricing(() => double.NaN, 360, 200).CoinsPerBill);
            Assert.AreEqual(200, new BillPricing(() => double.PositiveInfinity, 360, 200).CoinsPerBill);
        }

        // ---------- BillPurchases ----------

        private sealed class World
        {
            public Wallet Coins = new Wallet();
            public BillWallet Bills = new BillWallet();
            public StationController Stations;
            public UpgradeService Upgrades;
            public ManagerController Managers;
            public BillPurchases Purchases;
            public int Saves;
        }

        // Станция 0 открыта (уровень 1, база 4), станция 1 закрыта (база 60). Доход 0 → 1 купюра = 200 монет.
        private static World Create()
        {
            var first = new TestStationDefinition { Id = "first", BaseCost = 4, StartLevel = 1 };
            var second = new TestStationDefinition { Id = "second", BaseCost = 60, StartLevel = 0 };
            var world = new World();
            world.Stations = new StationController(new IStationDefinition[] { first, second }, new MilestoneTable(new int[0], 2), world.Coins);
            world.Upgrades = new UpgradeService(new IUpgradeDefinition[] { new TestUpgradeDefinition { Id = "up", Cost = 1000, TargetStation = first } },
                world.Stations, world.Coins);
            world.Managers = new ManagerController(world.Stations, world.Coins, 10);
            world.Purchases = new BillPurchases(world.Stations, world.Upgrades, world.Managers, world.Bills,
                new BillPricing(() => 0, 360, 200), () => world.Saves++);
            return world;
        }

        [Test]
        public void BuyStation_PaysBills_NotCoins_AndAddsLevels()
        {
            World world = Create();
            world.Bills.Add(10, BillSource.Debug);
            long price = world.Purchases.StationPrice(0, 10);

            Assert.IsTrue(world.Purchases.TryBuyStation(0, 10, price));

            Assert.AreEqual(11, world.Stations.Stations[0].Level);
            Assert.AreEqual(10 - price, world.Bills.Bills);
            Assert.AreEqual(0, world.Coins.Coins);
            Assert.AreEqual(1, world.Saves);
        }

        [Test]
        public void UnlockStation_ForOneBill()
        {
            World world = Create();
            world.Bills.Add(1, BillSource.Debug);

            Assert.AreEqual(1, world.Purchases.StationPrice(1, 1));
            Assert.IsTrue(world.Purchases.TryBuyStation(1, 1, 1));
            Assert.IsTrue(world.Stations.Stations[1].IsUnlocked);
        }

        [Test]
        public void BuyUpgrade_MarksPurchased_AndAppliesMultiplier()
        {
            World world = Create();
            world.Bills.Add(5, BillSource.Debug);

            Assert.AreEqual(5, world.Purchases.UpgradePrice(0));
            Assert.IsTrue(world.Purchases.TryBuyUpgrade(0, 5));

            Assert.IsTrue(world.Upgrades.IsPurchased(0));
            Assert.AreEqual(3, world.Stations.Stations[0].IncomeMultiplier);
            Assert.AreEqual(0, world.Purchases.UpgradePrice(0));
        }

        [Test]
        public void HireManager_Automates_AndRaisesHired()
        {
            World world = Create();
            world.Bills.Add(1, BillSource.Debug);
            int hired = -1;
            world.Managers.ManagerHired += index => hired = index;

            Assert.IsTrue(world.Purchases.TryHireManager(0, world.Purchases.ManagerPrice(0)));

            Assert.IsTrue(world.Managers.IsHired(0));
            Assert.AreEqual(0, hired);
            Assert.AreEqual(0, world.Purchases.ManagerPrice(0));
        }

        [Test]
        public void NotEnoughBills_ChangesNothing()
        {
            World world = Create();
            long price = world.Purchases.StationPrice(0, 50);
            world.Bills.Add(price - 1, BillSource.Debug);

            Assert.IsFalse(world.Purchases.TryBuyStation(0, 50, price));

            Assert.AreEqual(1, world.Stations.Stations[0].Level);
            Assert.AreEqual(price - 1, world.Bills.Bills);
            Assert.AreEqual(0, world.Saves);
        }

        [Test]
        public void PriceRoseAfterQuote_IsRejected()
        {
            World world = Create();
            world.Bills.Add(100, BillSource.Debug);

            Assert.IsFalse(world.Purchases.TryBuyUpgrade(0, world.Purchases.UpgradePrice(0) - 1));
            Assert.IsFalse(world.Upgrades.IsPurchased(0));
            Assert.AreEqual(100, world.Bills.Bills);
        }

        [Test]
        public void ImpossiblePurchases_CostNothing()
        {
            World world = Create();
            world.Bills.Add(100, BillSource.Debug);
            world.Purchases.TryBuyUpgrade(0, 100);

            Assert.IsFalse(world.Purchases.TryBuyUpgrade(0, 100), "already purchased");
            Assert.IsFalse(world.Purchases.TryHireManager(1, 100), "station locked");
            Assert.IsFalse(world.Purchases.TryBuyStation(5, 1, 100), "bad index");
            Assert.IsFalse(world.Purchases.TryBuyStation(0, 0, 100), "zero count");
            Assert.AreEqual(95, world.Bills.Bills);
        }
    }

}
