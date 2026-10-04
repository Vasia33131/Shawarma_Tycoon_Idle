using System;
using System.Collections.Generic;
using NUnit.Framework;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Managers;
using ShaurmaTycoon.Offline;
using ShaurmaTycoon.Prestige;
using ShaurmaTycoon.Save;
using ShaurmaTycoon.Stations;

namespace ShaurmaTycoon.Tests
{
    public class PrestigeTests
    {
        private static readonly PrestigeRules Gdd = new PrestigeRules(reputationDivisor: 1e6, incomeBonusPerReputation: 0.02);

        private sealed class MemoryStorage : ISaveStorage
        {
            public string Json;
            public int Writes;

            public void Save(string json, Action<bool> onComplete)
            {
                Json = json;
                Writes++;
                onComplete?.Invoke(true);
            }

            public void Load(Action<string> onLoaded)
            {
                onLoaded?.Invoke(Json);
            }
        }

        private sealed class NullSerializer : ISaveSerializer
        {
            public string Serialize(SaveData data) => "saved";
            public SaveData Deserialize(string json) => new SaveData();
        }

        private sealed class CountingParticipant : ISaveParticipant
        {
            public int Captures;
            public void Capture(SaveData data) => Captures++;
            public void Apply(SaveData data) { }
        }

        // ---------- Rules ----------

        [TestCase(0, 0)]
        [TestCase(999999, 0)]
        [TestCase(1e6, 1)]
        [TestCase(1e9, 31)]
        [TestCase(1e10, 100)]
        public void TotalReputation_FollowsSqrtFormula(double lifetime, long expected)
        {
            Assert.AreEqual(expected, Gdd.TotalReputationFor(lifetime));
        }

        [Test]
        public void Gain_SubtractsAlreadyEarned()
        {
            Assert.AreEqual(69, Gdd.ReputationGain(1e10, 31));
            Assert.AreEqual(0, Gdd.ReputationGain(1e9, 50));
        }

        [Test]
        public void IncomeMultiplier_IsTwoPercentPerReputation()
        {
            Assert.AreEqual(1.62, Gdd.IncomeMultiplier(31), 1e-12);
            Assert.AreEqual(1, Gdd.IncomeMultiplier(0));
        }

        // ---------- State & save ----------

        [Test]
        public void AddEarned_CountsCityAndLifetime_IgnoresInvalid()
        {
            var state = new PrestigeState();
            state.AddEarned(100);
            state.AddEarned(-5);
            state.AddEarned(double.NaN);

            Assert.AreEqual(100, state.CityEarned);
            Assert.AreEqual(100, state.LifetimeEarned);
        }

        [Test]
        public void SpendingReputation_DoesNotReduceEarned()
        {
            var state = new PrestigeState();
            state.CompletePrestige(31, 1);

            Assert.IsTrue(state.TrySpend(20));
            Assert.IsFalse(state.TrySpend(20));
            Assert.AreEqual(31, state.ReputationEarned);
            Assert.AreEqual(11, state.FreeReputation);
        }

        [Test]
        public void SaveParticipant_RoundTrip()
        {
            var state = new PrestigeState();
            state.AddEarned(5e9);
            state.CompletePrestige(70, 1);
            state.AddEarned(123);
            state.TrySpend(15);
            state.SetShopLevel("offline", 2);
            var data = new SaveData();
            new PrestigeSaveParticipant(state).Capture(data);

            var restored = new PrestigeState();
            new PrestigeSaveParticipant(restored).Apply(data);

            Assert.AreEqual(1, restored.CityIndex);
            Assert.AreEqual(123, restored.CityEarned);
            Assert.AreEqual(5e9 + 123, restored.LifetimeEarned);
            Assert.AreEqual(70, restored.ReputationEarned);
            Assert.AreEqual(15, restored.ReputationSpent);
            Assert.AreEqual(2, restored.GetShopLevel("offline"));
        }

        [Test]
        public void Migrator_FixesInvalidPrestige()
        {
            var data = new SaveData
            {
                Prestige = new PrestigeSave
                {
                    CityIndex = -2, CityEarned = double.NaN, LifetimeEarned = -1, ReputationEarned = 10, ReputationSpent = 50,
                    ShopLevels = new List<ShopLevelSave> { null, new ShopLevelSave { Id = "a", Level = 0 }, new ShopLevelSave { Id = "b", Level = 2 } },
                },
            };

            SaveMigrator.Migrate(data);

            Assert.AreEqual(0, data.Prestige.CityIndex);
            Assert.AreEqual(0, data.Prestige.CityEarned);
            Assert.AreEqual(0, data.Prestige.LifetimeEarned);
            Assert.AreEqual(10, data.Prestige.ReputationSpent);
            Assert.AreEqual(1, data.Prestige.ShopLevels.Count);
        }

        [Test]
        public void Migrator_MissingPrestige_IsCreated()
        {
            Assert.IsNotNull(SaveMigrator.Migrate(new SaveData { Prestige = null }).Prestige);
        }

        // ---------- Shop ----------

        private static ShopItemConfig[] CreateShopItems()
        {
            return new[]
            {
                new ShopItemConfig { Id = "offline", Effect = ShopEffect.OfflineLimitHours, Values = new double[] { 1, 2, 3 }, Prices = new long[] { 5, 10, 20 } },
                new ShopItemConfig { Id = "speed", Effect = ShopEffect.CycleTimeFactor, Values = new[] { 0.9, 0.8 }, Prices = new long[] { 20, 80 } },
            };
        }

        [Test]
        public void Shop_BuyLevels_SpendsFreeReputation()
        {
            var state = new PrestigeState();
            state.CompletePrestige(16, 1);
            var shop = new ReputationShop(CreateShopItems(), state);

            Assert.IsTrue(shop.TryBuy(0));
            Assert.IsTrue(shop.TryBuy(0));
            Assert.IsFalse(shop.TryBuy(0));

            Assert.AreEqual(2, shop.GetLevel(0));
            Assert.AreEqual(1, state.FreeReputation);
            Assert.AreEqual(16, state.ReputationEarned);
            Assert.AreEqual(2, shop.GetEffect(ShopEffect.OfflineLimitHours));
        }

        [Test]
        public void Shop_Effects_HaveBaseValuesWithoutPurchases()
        {
            var shop = new ReputationShop(CreateShopItems(), new PrestigeState());

            Assert.AreEqual(0, shop.GetEffect(ShopEffect.OfflineLimitHours));
            Assert.AreEqual(1, shop.GetEffect(ShopEffect.CycleTimeFactor));
            Assert.AreEqual(0, shop.GetEffect(ShopEffect.StartCoins));
        }

        [Test]
        public void Shop_MaxLevel_CannotBuyMore()
        {
            var state = new PrestigeState();
            state.CompletePrestige(1000, 0);
            var shop = new ReputationShop(CreateShopItems(), state);

            shop.TryBuy(1);
            shop.TryBuy(1);

            Assert.IsTrue(shop.IsMaxed(1));
            Assert.AreEqual(-1, shop.GetNextPrice(1));
            Assert.IsFalse(shop.TryBuy(1));
            Assert.AreEqual(0.8, shop.GetEffect(ShopEffect.CycleTimeFactor), 1e-12);
        }

        // ---------- Station modifiers, free managers, offline bonus ----------

        [Test]
        public void GlobalModifiers_ScaleIncomeAndSpeed_KeepProgress()
        {
            var wallet = new Wallet();
            var stations = new StationController(new IStationDefinition[] { new TestStationDefinition() },
                new MilestoneTable(new int[0], 2), wallet);
            StationModel station = stations.Stations[0];
            station.TryStartCycle();
            station.Advance(0.3);

            stations.SetGlobalModifiers(1.62, 1 / 0.9);

            Assert.AreEqual(1.62, station.IncomePerCycle, 1e-12);
            Assert.AreEqual(0.6 * 0.9, station.CycleTime, 1e-12);
            Assert.AreEqual(0.5, station.Progress, 1e-12);
        }

        [Test]
        public void FreeManagers_HireWhenStationUnlocks()
        {
            var wallet = new Wallet();
            var stations = new StationController(new IStationDefinition[]
            {
                new TestStationDefinition { Id = "a" },
                new TestStationDefinition { Id = "b", StartLevel = 0, BaseCost = 60 },
                new TestStationDefinition { Id = "c", StartLevel = 0, BaseCost = 60 },
            }, new MilestoneTable(new int[0], 2), wallet);
            var managers = new ManagerController(stations, wallet, 1000);

            managers.SetFreeManagers(2);

            Assert.IsTrue(stations.Stations[0].IsAutomated);
            Assert.IsFalse(stations.Stations[1].IsAutomated);

            wallet.Add(120);
            stations.TryBuy(1, 1);
            stations.TryBuy(2, 1);

            Assert.IsTrue(stations.Stations[1].IsAutomated);
            Assert.IsFalse(stations.Stations[2].IsAutomated);
            Assert.AreEqual(0, wallet.Coins);
        }

        [Test]
        public void OfflineSettings_ExtraLimit_AddsSeconds()
        {
            var settings = new OfflineSettings(0.5, 7200, 60).WithExtraLimit(3 * 3600);

            Assert.AreEqual(5 * 3600, settings.LimitSeconds);
            Assert.AreEqual(0.5, settings.IncomeRate);
        }

        // ---------- Controller ----------

        private static (PrestigeState state, SaveService save, MemoryStorage storage, CountingParticipant city) CreateWorld()
        {
            var storage = new MemoryStorage();
            var save = new SaveService(storage, new NullSerializer(), () => new DateTime(2026, 10, 4, 0, 0, 0, DateTimeKind.Utc));
            save.Load(null);
            var city = new CountingParticipant();
            save.Register(city);
            return (new PrestigeState(), save, storage, city);
        }

        [Test]
        public void Controller_BelowThreshold_CannotPrestige()
        {
            var world = CreateWorld();
            world.state.AddEarned(5e8);
            var controller = new PrestigeController(world.state, Gdd, 1e9, 0, 2, world.save, world.city, _ => 0, () => { });

            Assert.IsFalse(controller.CanPrestige);
            Assert.AreEqual(0.5, controller.Progress, 1e-12);
            Assert.IsFalse(controller.TryPrestige());
        }

        [Test]
        public void Controller_Prestige_ResetsCity_GrantsReputation_SavesAndReloads()
        {
            var world = CreateWorld();
            world.save.Data.Coins = 12345;
            world.save.Data.Stations.Add(new StationSave { Id = "a", Level = 50 });
            world.save.Data.PurchasedUpgrades.Add("x");
            world.state.AddEarned(1e9);
            int reloads = 0;
            var controller = new PrestigeController(world.state, Gdd, 1e9, 0, 2, world.save, world.city,
                city => city == 1 ? 1e4 : 0, () => reloads++);

            Assert.AreEqual(31, controller.ForecastGain);
            Assert.AreEqual(1.62, controller.IncomeMultiplierAfterPrestige, 1e-12);
            Assert.IsTrue(controller.TryPrestige());

            Assert.AreEqual(1, reloads);
            Assert.AreEqual(31, world.state.ReputationEarned);
            Assert.AreEqual(1, world.state.CityIndex);
            Assert.AreEqual(0, world.state.CityEarned);
            Assert.AreEqual(1e9, world.state.LifetimeEarned);
            Assert.AreEqual(10000, world.save.Data.Coins);
            Assert.AreEqual(0, world.save.Data.Stations.Count);
            Assert.AreEqual(0, world.save.Data.PurchasedUpgrades.Count);
            Assert.AreEqual(1, world.storage.Writes);

            // Участник города отключён до сброса: последующие сохранения его не трогают.
            int captures = world.city.Captures;
            world.save.SaveNow();
            Assert.AreEqual(captures, world.city.Captures);
        }

        [Test]
        public void Controller_LastCity_RestartsSameCity()
        {
            var world = CreateWorld();
            world.state.CompletePrestige(31, 1);
            world.state.AddEarned(2e10);
            var controller = new PrestigeController(world.state, Gdd, 1e10, 1, 2, world.save, world.city, _ => 0, () => { });

            Assert.IsTrue(controller.IsLastCity);
            Assert.AreEqual(1, controller.NextCityIndex);
            Assert.IsTrue(controller.TryPrestige());
            Assert.AreEqual(1, world.state.CityIndex);
        }

        [Test]
        public void Controller_ThresholdReachedButNoGain_CannotPrestige()
        {
            var world = CreateWorld();
            world.state.CompletePrestige(1000, 0);
            world.state.AddEarned(1e9);
            var controller = new PrestigeController(world.state, Gdd, 1e9, 0, 2, world.save, world.city, _ => 0, () => { });

            Assert.IsTrue(controller.IsThresholdReached);
            Assert.AreEqual(0, controller.ForecastGain);
            Assert.IsFalse(controller.CanPrestige);
        }
    }
}
