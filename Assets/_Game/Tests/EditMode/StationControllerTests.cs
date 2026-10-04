using NUnit.Framework;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Stations;

namespace ShaurmaTycoon.Tests
{
    public class StationControllerTests
    {
        private Wallet _wallet;
        private StationController _controller;

        [SetUp]
        public void SetUp()
        {
            _wallet = new Wallet();
            var definitions = new IStationDefinition[]
            {
                new TestStationDefinition { Id = "a", BaseIncome = 1, CycleTime = 1 },
                new TestStationDefinition { Id = "b", BaseIncome = 10, CycleTime = 1 },
                new TestStationDefinition { Id = "locked", StartLevel = 0, BaseCost = 60 },
            };
            _controller = new StationController(definitions, new MilestoneTable(new[] { 25 }, 2), _wallet);
        }

        [Test]
        public void CreatesModelPerDefinition()
        {
            Assert.AreEqual(3, _controller.Stations.Count);
            Assert.AreEqual("b", _controller.Stations[1].Definition.Id);
        }

        [Test]
        public void Tick_AddsTotalIncome_InOneEvent()
        {
            int events = 0;
            _wallet.CoinsChanged += _ => events++;
            _controller.Tap(0);
            _controller.Tap(1);

            _controller.Tick(1f);

            Assert.AreEqual(11, _wallet.Coins);
            Assert.AreEqual(1, events);
        }

        [Test]
        public void Tick_NoIncome_NoEvent()
        {
            int events = 0;
            _wallet.CoinsChanged += _ => events++;

            _controller.Tick(1f);

            Assert.AreEqual(0, events);
        }

        [Test]
        public void IncomePerSecond_CountsOnlyAutomatedStations()
        {
            Assert.AreEqual(0, _controller.IncomePerSecond);

            _controller.Stations[1].IsAutomated = true;
            _controller.Stations[2].IsAutomated = true;

            Assert.AreEqual(10, _controller.IncomePerSecond);
        }

        [Test]
        public void Earned_ReportsTotalPerTick()
        {
            double earned = 0;
            int events = 0;
            _controller.Earned += amount =>
            {
                earned += amount;
                events++;
            };
            _controller.Tap(0);
            _controller.Tap(1);

            _controller.Tick(0.5f);
            _controller.Tick(0.6f);

            Assert.AreEqual(11, earned);
            Assert.AreEqual(1, events);
        }

        [Test]
        public void TapAllIdle_StartsOnlyIdleManualUnlockedStations()
        {
            _controller.Stations[1].IsAutomated = true;
            _controller.Tap(0);

            Assert.AreEqual(0, _controller.TapAllIdle());

            _controller.Tick(1f);
            Assert.AreEqual(1, _controller.TapAllIdle());
            Assert.IsTrue(_controller.Stations[0].IsRunning);
            Assert.IsFalse(_controller.Stations[2].IsRunning);
        }

        [Test]
        public void InvalidIndex_ReturnsFalse()
        {
            Assert.IsFalse(_controller.Tap(-1));
            Assert.IsFalse(_controller.Tap(3));
            Assert.IsFalse(_controller.TryBuy(5, 1));
        }

        [Test]
        public void TryBuy_UnlocksLockedStation()
        {
            _wallet.Add(60);

            Assert.IsTrue(_controller.TryBuy(2, 1));
            Assert.IsTrue(_controller.Stations[2].IsUnlocked);
            Assert.AreEqual(0, _wallet.Coins);
        }
    }
}
