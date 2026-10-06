using System;
using NUnit.Framework;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Platform;
using ShaurmaTycoon.Save;
using ShaurmaTycoon.Shop;

namespace ShaurmaTycoon.Tests
{
    public class AdBillRewardTests
    {
        private static readonly DateTime Noon = new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);

        private static AdBillReward Create()
        {
            return new AdBillReward(2, TimeSpan.FromMinutes(10), 6);
        }

        // ---------- Rules ----------

        [Test]
        public void Fresh_CanClaimImmediately()
        {
            AdBillReward rules = Create();

            Assert.IsTrue(rules.CanClaim(Noon));
            Assert.AreEqual(TimeSpan.Zero, rules.TimeLeft(Noon));
            Assert.AreEqual(6, rules.RemainingToday(Noon));
        }

        [Test]
        public void AfterClaim_WaitsTenMinutes()
        {
            AdBillReward rules = Create();
            Assert.IsTrue(rules.Claim(Noon));

            Assert.IsFalse(rules.CanClaim(Noon.AddMinutes(9)));
            Assert.AreEqual(TimeSpan.FromMinutes(1), rules.TimeLeft(Noon.AddMinutes(9)));
            Assert.IsFalse(rules.Claim(Noon.AddMinutes(9)));
            Assert.IsTrue(rules.CanClaim(Noon.AddMinutes(10)));
        }

        [Test]
        public void DailyLimit_StopsAfterSix_AndResetsAtUtcMidnight()
        {
            AdBillReward rules = Create();
            DateTime t = Noon;
            for (int i = 0; i < 6; i++, t = t.AddMinutes(10))
                Assert.IsTrue(rules.Claim(t), $"claim {i + 1}");

            Assert.IsTrue(rules.IsDailyLimitReached(t));
            Assert.IsFalse(rules.CanClaim(t.AddHours(1)));
            Assert.AreEqual(Noon.Date.AddDays(1) - t, rules.TimeLeft(t));

            DateTime tomorrow = Noon.Date.AddDays(1).AddMinutes(1);
            Assert.IsTrue(rules.CanClaim(tomorrow));
            Assert.AreEqual(6, rules.RemainingToday(tomorrow));
        }

        [Test]
        public void ClockMovedBack_NoRewardNow_ButNotBlockedForever()
        {
            AdBillReward rules = Create();
            rules.Claim(Noon);
            DateTime back = Noon.AddHours(-5);

            Assert.IsFalse(rules.CanClaim(back));
            Assert.IsTrue(rules.CanClaim(back.AddMinutes(10)));
        }

        [Test]
        public void Load_RestoresCooldown()
        {
            AdBillReward source = Create();
            source.Claim(Noon);

            AdBillReward restored = Create();
            restored.Load(source.LastClaimUtcTicks, source.DayUtcTicks, source.ClaimsToday);

            Assert.IsFalse(restored.CanClaim(Noon.AddMinutes(5)));
            Assert.AreEqual(5, restored.RemainingToday(Noon.AddMinutes(5)));
        }

        // ---------- Service ----------

        private sealed class FakePlatform : IPlatformService
        {
            public int Shown;
            private Action<bool> _pending;

            public bool IsInitialized => true;
            public string Language => "ru";
            public DateTime ServerTimeUtc => Noon;
            public void Init(Action onReady) => onReady?.Invoke();
            public void GameReady() { }
            public void SetGameplayPaused(bool paused) { }
            public void GetCatalog(Action<ProductPrice[]> onLoaded) => onLoaded?.Invoke(null);
            public event Action<bool> AdShowing { add { } remove { } }
            public void ShowInterstitial(Action onClosed) => onClosed?.Invoke();
            public void Save(string json, Action<bool> onComplete) => onComplete?.Invoke(true);
            public void Load(Action<string> onLoaded) => onLoaded?.Invoke(null);
            public void SetLeaderboardScore(string leaderboardId, double score) { }
            public void Purchase(string productId, Action<bool> onComplete) => onComplete?.Invoke(true);
            public void GetPurchases(Action<string[]> onLoaded) => onLoaded?.Invoke(new string[0]);
            public void ConsumePurchase(string productId) { }

            public void ShowRewarded(Action<bool> onComplete)
            {
                Shown++;
                _pending = onComplete;
            }

            public void Finish(bool watched)
            {
                Action<bool> callback = _pending;
                _pending = null;
                callback?.Invoke(watched);
            }
        }

        private sealed class World
        {
            public DateTime Now = Noon;
            public BillWallet Bills = new BillWallet();
            public FakePlatform Platform = new FakePlatform();
            public int Saves;
            public AdBillService Service;
        }

        private static World CreateWorld()
        {
            var world = new World();
            world.Service = new AdBillService(Create(), world.Bills, world.Platform, () => world.Now, () => world.Saves++);
            return world;
        }

        [Test]
        public void Watched_GivesTwoBills_StartsCooldown_AndSaves()
        {
            World world = CreateWorld();
            bool? result = null;

            Assert.IsTrue(world.Service.TryWatch(ok => result = ok));
            Assert.IsTrue(world.Service.IsShowing);
            Assert.IsFalse(world.Service.TryWatch(null), "second tap while the ad is open");

            world.Platform.Finish(true);

            Assert.AreEqual(true, result);
            Assert.AreEqual(2, world.Bills.Bills);
            Assert.AreEqual(1, world.Saves);
            Assert.IsFalse(world.Service.CanWatch);
            Assert.AreEqual(1, world.Platform.Shown);
        }

        [Test]
        public void ClosedEarly_GivesNothing_AndKeepsCooldownFree()
        {
            World world = CreateWorld();
            bool? result = null;
            world.Service.TryWatch(ok => result = ok);

            world.Platform.Finish(false);

            Assert.AreEqual(false, result);
            Assert.AreEqual(0, world.Bills.Bills);
            Assert.AreEqual(0, world.Saves);
            Assert.IsTrue(world.Service.CanWatch);
        }

        [Test]
        public void Cooldown_SurvivesSaveAndLoad()
        {
            World world = CreateWorld();
            world.Service.TryWatch(null);
            world.Platform.Finish(true);
            var data = new SaveData();
            world.Service.Capture(data);

            World restarted = CreateWorld();
            restarted.Now = Noon.AddMinutes(3);
            restarted.Service.Apply(data);

            Assert.IsFalse(restarted.Service.CanWatch);
            Assert.IsFalse(restarted.Service.TryWatch(null));
            Assert.AreEqual(0, restarted.Platform.Shown);
        }

        [Test]
        public void Migrator_FixesBrokenAdSave()
        {
            var data = new SaveData { AdBills = new AdRewardSave { LastClaimUtcTicks = -1, DayUtcTicks = -5, ClaimsToday = -2 } };

            SaveData migrated = SaveMigrator.Migrate(data);

            Assert.AreEqual(0, migrated.AdBills.LastClaimUtcTicks);
            Assert.AreEqual(0, migrated.AdBills.DayUtcTicks);
            Assert.AreEqual(0, migrated.AdBills.ClaimsToday);
            Assert.IsNotNull(SaveMigrator.Migrate(new SaveData { AdBills = null }).AdBills);
        }
    }
}
