using System;
using NUnit.Framework;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Stations;

namespace ShaurmaTycoon.Tests
{
    public class StationModelTests
    {
        private const double Tolerance = 1e-9;

        private static MilestoneTable Milestones => new MilestoneTable(new[] { 25, 50, 100, 200, 300, 400 }, 2);

        private static StationModel CreateStation(int startLevel = 1)
        {
            return new StationModel(new TestStationDefinition { StartLevel = startLevel }, Milestones);
        }

        [Test]
        public void NewStation_StartsAtStartLevel_NotRunning()
        {
            StationModel station = CreateStation();

            Assert.AreEqual(1, station.Level);
            Assert.IsTrue(station.IsUnlocked);
            Assert.IsFalse(station.IsRunning);
        }

        [Test]
        public void WithoutTap_NoIncome()
        {
            Assert.AreEqual(0, CreateStation().Advance(10));
        }

        [Test]
        public void Tap_IncomeOnlyAfterCycleCompletes()
        {
            StationModel station = CreateStation();

            Assert.IsTrue(station.TryStartCycle());
            Assert.AreEqual(0, station.Advance(0.5));
            Assert.AreEqual(1, station.Advance(0.2));
            Assert.IsFalse(station.IsRunning);
        }

        [Test]
        public void Tap_WhileRunning_IsIgnored()
        {
            StationModel station = CreateStation();
            station.TryStartCycle();
            station.Advance(0.3);

            Assert.IsFalse(station.TryStartCycle());
            Assert.AreEqual(0.5, station.Progress, Tolerance);
        }

        [Test]
        public void ManualCycle_LargeDelta_GivesOneCycleOnly()
        {
            StationModel station = CreateStation();
            station.TryStartCycle();

            Assert.AreEqual(1, station.Advance(100));
        }

        [Test]
        public void LockedStation_CannotRun()
        {
            StationModel station = CreateStation(startLevel: 0);

            Assert.IsFalse(station.IsUnlocked);
            Assert.IsFalse(station.TryStartCycle());
            station.IsAutomated = true;
            Assert.AreEqual(0, station.Advance(10));
        }

        [Test]
        public void Automated_CountsAllCompletedCycles()
        {
            StationModel station = CreateStation();
            station.IsAutomated = true;

            Assert.AreEqual(5, station.Advance(3.1));
            Assert.AreEqual(1d / 6, station.Progress, Tolerance);
            Assert.IsTrue(station.IsRunning);
        }

        [Test]
        public void IncomePerCycle_ScalesWithLevel()
        {
            var wallet = new Wallet();
            wallet.Add(1000);
            StationModel station = CreateStation();

            station.TryBuy(wallet, 9);

            Assert.AreEqual(10, station.IncomePerCycle);
        }

        [Test]
        public void TryBuy_SpendsBulkCost()
        {
            var wallet = new Wallet();
            wallet.Add(100);
            StationModel station = CreateStation();
            double cost = station.GetCost(3);

            Assert.IsTrue(station.TryBuy(wallet, 3));
            Assert.AreEqual(4, station.Level);
            Assert.AreEqual(100 - cost, wallet.Coins, Tolerance);
        }

        [Test]
        public void FirstUpgrade_CostsBaseTimesGrowth()
        {
            Assert.AreEqual(4 * 1.07, CreateStation().GetCost(1), Tolerance);
        }

        [Test]
        public void TryBuy_NotEnoughMoney_KeepsLevel()
        {
            var wallet = new Wallet();
            wallet.Add(4);
            StationModel station = CreateStation();
            int levelChangedCount = 0;
            station.LevelChanged += _ => levelChangedCount++;

            Assert.IsFalse(station.TryBuy(wallet, 1));
            Assert.AreEqual(1, station.Level);
            Assert.AreEqual(4, wallet.Coins);
            Assert.AreEqual(0, levelChangedCount);
        }

        [Test]
        public void UnlockCost_EqualsBaseCost()
        {
            var wallet = new Wallet();
            wallet.Add(4);
            StationModel station = CreateStation(startLevel: 0);

            Assert.AreEqual(4, station.GetCost(1));
            Assert.IsTrue(station.TryBuy(wallet, 1));
            Assert.IsTrue(station.IsUnlocked);
            Assert.AreEqual(0, wallet.Coins);
        }

        [Test]
        public void Milestone25_HalvesCycleTime_KeepsProgressFraction()
        {
            var wallet = new Wallet();
            wallet.Add(1e6);
            StationModel station = CreateStation();
            station.TryBuy(wallet, 23);
            station.TryStartCycle();
            station.Advance(0.3);

            Assert.AreEqual(24, station.Level);
            Assert.AreEqual(0.6, station.CycleTime, Tolerance);

            station.TryBuy(wallet, 1);

            Assert.AreEqual(25, station.Level);
            Assert.AreEqual(0.3, station.CycleTime, Tolerance);
            Assert.AreEqual(0.5, station.Progress, Tolerance);
            Assert.AreEqual(0, station.Advance(0.12));
            Assert.AreEqual(25, station.Advance(0.06));
        }

        [Test]
        public void TryBuy_RaisesLevelChanged()
        {
            var wallet = new Wallet();
            wallet.Add(100);
            StationModel station = CreateStation();
            StationModel changed = null;
            station.LevelChanged += s => changed = s;

            station.TryBuy(wallet, 1);

            Assert.AreSame(station, changed);
        }

        [Test]
        public void NullWallet_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => CreateStation().TryBuy(null, 1));
        }
    }
}
