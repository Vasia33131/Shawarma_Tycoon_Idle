using NUnit.Framework;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Managers;
using ShaurmaTycoon.Stations;

namespace ShaurmaTycoon.Tests
{
    public class ManagerControllerTests
    {
        private Wallet _wallet;
        private StationController _stations;
        private ManagerController _managers;

        [SetUp]
        public void SetUp()
        {
            _wallet = new Wallet();
            var definitions = new IStationDefinition[]
            {
                new TestStationDefinition { BaseCost = 4 },
                new TestStationDefinition { BaseCost = 60, StartLevel = 0 },
            };
            _stations = new StationController(definitions, new MilestoneTable(new int[0], 2), _wallet);
            _managers = new ManagerController(_stations, _wallet, 1000);
        }

        [Test]
        public void Cost_IsBaseCostTimesMultiplier()
        {
            Assert.AreEqual(4000, _managers.GetCost(0));
            Assert.AreEqual(60000, _managers.GetCost(1));
        }

        [Test]
        public void TryHire_SpendsAndAutomates()
        {
            _wallet.Add(5000);
            int hiredIndex = -1;
            _managers.ManagerHired += i => hiredIndex = i;

            Assert.IsTrue(_managers.TryHire(0));
            Assert.IsTrue(_managers.IsHired(0));
            Assert.IsTrue(_stations.Stations[0].IsAutomated);
            Assert.AreEqual(1000, _wallet.Coins);
            Assert.AreEqual(0, hiredIndex);
        }

        [Test]
        public void TryHire_NotEnoughMoney_Fails()
        {
            _wallet.Add(3999);

            Assert.IsFalse(_managers.TryHire(0));
            Assert.IsFalse(_managers.IsHired(0));
            Assert.AreEqual(3999, _wallet.Coins);
        }

        [Test]
        public void TryHire_LockedStation_Fails()
        {
            _wallet.Add(1e9);

            Assert.IsFalse(_managers.CanHire(1));
            Assert.IsFalse(_managers.TryHire(1));
            Assert.AreEqual(1e9, _wallet.Coins);
        }

        [Test]
        public void TryHire_Twice_SecondFails()
        {
            _wallet.Add(1e6);
            _managers.TryHire(0);
            double coins = _wallet.Coins;

            Assert.IsFalse(_managers.TryHire(0));
            Assert.AreEqual(coins, _wallet.Coins);
        }

        [Test]
        public void HiredStation_EarnsWithoutTaps()
        {
            _wallet.Add(4000);
            _managers.TryHire(0);

            _stations.Tick(0.6f * 3 + 0.01f);

            Assert.AreEqual(3, _wallet.Coins);
        }

        [Test]
        public void InvalidIndex_IsSafe()
        {
            Assert.IsFalse(_managers.IsHired(-1));
            Assert.IsFalse(_managers.CanHire(5));
            Assert.IsFalse(_managers.TryHire(5));
        }
    }
}
