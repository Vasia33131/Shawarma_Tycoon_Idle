using System;
using NUnit.Framework;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Stations;
using ShaurmaTycoon.Upgrades;

namespace ShaurmaTycoon.Tests
{
    public class UpgradeServiceTests
    {
        private TestStationDefinition _first;
        private TestStationDefinition _second;
        private Wallet _wallet;
        private StationController _stations;

        [SetUp]
        public void SetUp()
        {
            _first = new TestStationDefinition { Id = "first", BaseIncome = 1 };
            _second = new TestStationDefinition { Id = "second", BaseIncome = 10 };
            _wallet = new Wallet();
            _stations = new StationController(new IStationDefinition[] { _first, _second }, new MilestoneTable(new int[0], 2), _wallet);
        }

        private UpgradeService CreateService(params IUpgradeDefinition[] upgrades)
        {
            return new UpgradeService(upgrades, _stations, _wallet);
        }

        [Test]
        public void StationUpgrade_MultipliesOnlyTarget()
        {
            UpgradeService service = CreateService(new TestUpgradeDefinition { TargetStation = _first, IncomeMultiplier = 3, Cost = 100 });
            _wallet.Add(100);

            Assert.IsTrue(service.TryBuy(0));
            Assert.AreEqual(3, _stations.Stations[0].IncomePerCycle);
            Assert.AreEqual(10, _stations.Stations[1].IncomePerCycle);
            Assert.AreEqual(0, _wallet.Coins);
        }

        [Test]
        public void AllStationsUpgrade_MultipliesEveryStation()
        {
            UpgradeService service = CreateService(new TestUpgradeDefinition { TargetStation = null, IncomeMultiplier = 2 });
            _wallet.Add(100);

            service.TryBuy(0);

            Assert.AreEqual(2, _stations.Stations[0].IncomePerCycle);
            Assert.AreEqual(20, _stations.Stations[1].IncomePerCycle);
            Assert.AreEqual(UpgradeService.AllStations, service.GetTargetStationIndex(0));
        }

        [Test]
        public void Upgrades_Stack_Multiplicatively()
        {
            UpgradeService service = CreateService(
                new TestUpgradeDefinition { Id = "a", TargetStation = _first, IncomeMultiplier = 3 },
                new TestUpgradeDefinition { Id = "b", TargetStation = _first, IncomeMultiplier = 3 },
                new TestUpgradeDefinition { Id = "c", TargetStation = null, IncomeMultiplier = 2 });
            _wallet.Add(1000);

            service.TryBuy(0);
            service.TryBuy(1);
            service.TryBuy(2);

            Assert.AreEqual(18, _stations.Stations[0].IncomeMultiplier);
            Assert.AreEqual(2, _stations.Stations[1].IncomeMultiplier);
        }

        [Test]
        public void TryBuy_Twice_SecondFails()
        {
            UpgradeService service = CreateService(new TestUpgradeDefinition { TargetStation = _first, Cost = 100 });
            _wallet.Add(1000);

            Assert.IsTrue(service.TryBuy(0));
            Assert.IsFalse(service.TryBuy(0));
            Assert.AreEqual(900, _wallet.Coins);
            Assert.AreEqual(3, _stations.Stations[0].IncomeMultiplier);
        }

        [Test]
        public void TryBuy_NotEnoughMoney_Fails()
        {
            UpgradeService service = CreateService(new TestUpgradeDefinition { TargetStation = _first, Cost = 100 });
            _wallet.Add(99);

            Assert.IsFalse(service.TryBuy(0));
            Assert.IsFalse(service.IsPurchased(0));
            Assert.AreEqual(1, _stations.Stations[0].IncomeMultiplier);
        }

        [Test]
        public void TryBuy_RaisesEvent()
        {
            UpgradeService service = CreateService(new TestUpgradeDefinition { TargetStation = _second });
            _wallet.Add(1000);
            int purchased = -1;
            service.UpgradePurchased += i => purchased = i;

            service.TryBuy(0);

            Assert.AreEqual(0, purchased);
            Assert.AreEqual(1, service.GetTargetStationIndex(0));
        }

        [Test]
        public void TargetOutsideCity_Throws()
        {
            var stranger = new TestStationDefinition { Id = "stranger" };

            Assert.Throws<ArgumentException>(() => CreateService(new TestUpgradeDefinition { TargetStation = stranger }));
        }

        [Test]
        public void InvalidMultiplier_Throws()
        {
            Assert.Throws<ArgumentException>(() => CreateService(new TestUpgradeDefinition { IncomeMultiplier = 0.5 }));
        }

        [Test]
        public void InvalidIndex_ReturnsFalse()
        {
            UpgradeService service = CreateService();

            Assert.IsFalse(service.TryBuy(0));
            Assert.IsFalse(service.TryBuy(-1));
        }
    }
}
