using System.Collections.Generic;
using NUnit.Framework;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Save;
using ShaurmaTycoon.Stations;
using ShaurmaTycoon.Upgrades;

namespace ShaurmaTycoon.Tests
{
    public class CitySaveParticipantTests
    {
        private TestStationDefinition _first;
        private TestStationDefinition _second;
        private TestUpgradeDefinition[] _upgradeDefinitions;

        [SetUp]
        public void SetUp()
        {
            _first = new TestStationDefinition { Id = "first", StartLevel = 1 };
            _second = new TestStationDefinition { Id = "second", StartLevel = 0, BaseCost = 60, BaseIncome = 60, CycleTime = 3 };
            _upgradeDefinitions = new[]
            {
                new TestUpgradeDefinition { Id = "first_x3", TargetStation = _first, IncomeMultiplier = 3 },
                new TestUpgradeDefinition { Id = "all_x2", TargetStation = null, IncomeMultiplier = 2 },
            };
        }

        private (Wallet wallet, StationController stations, UpgradeService upgrades, CitySaveParticipant participant) CreateGame()
        {
            var wallet = new Wallet();
            var stations = new StationController(new IStationDefinition[] { _first, _second },
                new MilestoneTable(new[] { 25 }, 2), wallet);
            var upgrades = new UpgradeService(_upgradeDefinitions, stations, wallet);
            return (wallet, stations, upgrades, new CitySaveParticipant(wallet, stations, upgrades));
        }

        [Test]
        public void CaptureThenApply_RestoresEverything()
        {
            var game = CreateGame();
            game.wallet.Add(1e6);
            game.stations.TryBuy(0, 30);
            game.stations.TryBuy(1, 1);
            game.stations.Stations[1].IsAutomated = true;
            game.stations.Tap(0);
            game.stations.Tick(0.15f);
            game.upgrades.TryBuy(0);
            var data = new SaveData();
            game.participant.Capture(data);

            var restored = CreateGame();
            restored.participant.Apply(data);

            Assert.AreEqual(game.wallet.Coins, restored.wallet.Coins);
            StationModel first = restored.stations.Stations[0];
            StationModel second = restored.stations.Stations[1];
            Assert.AreEqual(31, first.Level);
            Assert.AreEqual(0.3, first.CycleTime, 1e-9);
            Assert.IsTrue(first.IsRunning);
            Assert.AreEqual(game.stations.Stations[0].Progress, first.Progress, 1e-9);
            Assert.AreEqual(1, second.Level);
            Assert.IsTrue(second.IsAutomated);
            Assert.IsTrue(restored.upgrades.IsPurchased(0));
            Assert.IsFalse(restored.upgrades.IsPurchased(1));
            Assert.AreEqual(3, first.IncomeMultiplier);
        }

        [Test]
        public void Capture_WritesStationsAndUpgradesById()
        {
            var game = CreateGame();
            game.wallet.Add(100);
            game.upgrades.TryBuy(1);
            var data = new SaveData();

            game.participant.Capture(data);

            Assert.AreEqual(2, data.Stations.Count);
            Assert.AreEqual("first", data.Stations[0].Id);
            Assert.AreEqual(new List<string> { "all_x2" }, data.PurchasedUpgrades);
        }

        [Test]
        public void Apply_EmptySave_KeepsConfigDefaults()
        {
            var game = CreateGame();

            game.participant.Apply(new SaveData());

            Assert.AreEqual(0, game.wallet.Coins);
            Assert.AreEqual(1, game.stations.Stations[0].Level);
            Assert.AreEqual(0, game.stations.Stations[1].Level);
        }

        [Test]
        public void Apply_UnknownStationAndUpgrade_AreIgnored()
        {
            var game = CreateGame();
            var data = new SaveData
            {
                Coins = 42,
                Stations = new List<StationSave> { new StationSave { Id = "removed_station", Level = 50 } },
                PurchasedUpgrades = new List<string> { "removed_upgrade" },
            };

            game.participant.Apply(data);

            Assert.AreEqual(42, game.wallet.Coins);
            Assert.AreEqual(1, game.stations.Stations[0].Level);
            Assert.AreEqual(1, game.stations.Stations[0].IncomeMultiplier);
        }

        [Test]
        public void Restore_LockedStation_CannotBeAutomatedOrRunning()
        {
            var game = CreateGame();
            var data = new SaveData
            {
                Stations = new List<StationSave> { new StationSave { Id = "second", Level = 0, Automated = true, Running = true, Progress = 0.5 } },
            };

            game.participant.Apply(data);

            StationModel second = game.stations.Stations[1];
            Assert.IsFalse(second.IsAutomated);
            Assert.IsFalse(second.IsRunning);
            Assert.AreEqual(0, second.Progress);
        }
    }
}
