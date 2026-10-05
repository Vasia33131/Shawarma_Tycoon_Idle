using System;
using NUnit.Framework;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Managers;
using ShaurmaTycoon.Platform;
using ShaurmaTycoon.Prestige;
using ShaurmaTycoon.Retention;
using ShaurmaTycoon.Save;
using ShaurmaTycoon.Stations;
using ShaurmaTycoon.Upgrades;

namespace ShaurmaTycoon.Tests
{
    public class RetentionTests
    {
        private static readonly DateTime Noon = new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);

        private static readonly RewardDefinition[] Days =
        {
            new RewardDefinition(RewardKind.Coins, 10),
            new RewardDefinition(RewardKind.Bills, 2),
            new RewardDefinition(RewardKind.Coins, 30),
            new RewardDefinition(RewardKind.Bills, 3),
            new RewardDefinition(RewardKind.Coins, 60),
            new RewardDefinition(RewardKind.Bills, 5),
            new RewardDefinition(RewardKind.KioskSkin, 0),
        };

        private static readonly QuestTemplate[] Templates =
        {
            new QuestTemplate(QuestType.BuyLevels, 50, new RewardDefinition(RewardKind.Coins, 20)),
            new QuestTemplate(QuestType.HireManager, 1, new RewardDefinition(RewardKind.Reputation, 2)),
            new QuestTemplate(QuestType.BuyUpgrade, 3, new RewardDefinition(RewardKind.Coins, 30)),
            new QuestTemplate(QuestType.WatchBoost, 1, new RewardDefinition(RewardKind.Reputation, 1)),
            new QuestTemplate(QuestType.OpenGift, 2, new RewardDefinition(RewardKind.Coins, 15)),
        };

        // ---------- Calendar ----------

        [Test]
        public void Calendar_Fresh_ClaimsDayOne()
        {
            var calendar = new DailyRewardCalendar();

            Assert.IsTrue(calendar.CanClaim(Noon));
            Assert.AreEqual(0, calendar.Claim(Noon));
            Assert.AreEqual(1, calendar.NextDay);
        }

        [Test]
        public void Calendar_OncePerUtcDay()
        {
            var calendar = new DailyRewardCalendar();
            calendar.Claim(Noon);

            Assert.IsFalse(calendar.CanClaim(Noon.AddHours(11.9)));
            Assert.AreEqual(-1, calendar.Claim(Noon.AddHours(11.9)));
            Assert.AreEqual(TimeSpan.FromHours(12), calendar.TimeUntilNext(Noon));
            Assert.IsTrue(calendar.CanClaim(Noon.AddHours(12)));
            Assert.AreEqual(1, calendar.Claim(Noon.AddHours(12)));
        }

        [Test]
        public void Calendar_MissedDays_ContinueWithNextDay()
        {
            var calendar = new DailyRewardCalendar();
            calendar.Claim(Noon);
            calendar.Claim(Noon.AddDays(1));

            Assert.AreEqual(2, calendar.Claim(Noon.AddDays(5)));
        }

        [Test]
        public void Calendar_AfterSeventhDay_StartsOver()
        {
            var calendar = new DailyRewardCalendar();
            for (int i = 0; i < DailyRewardCalendar.DayCount; i++)
                Assert.AreEqual(i, calendar.Claim(Noon.AddDays(i)));

            Assert.AreEqual(0, calendar.Claim(Noon.AddDays(7)));
        }

        [Test]
        public void Calendar_ClockMovedBack_NoSecondReward()
        {
            var calendar = new DailyRewardCalendar();
            calendar.Claim(Noon);

            Assert.IsFalse(calendar.CanClaim(Noon.AddDays(-1)));
            Assert.IsFalse(calendar.CanClaim(Noon));
            Assert.IsTrue(calendar.CanClaim(Noon.AddDays(1)));
        }

        [Test]
        public void Calendar_SaveFarInFuture_IsClampedToToday()
        {
            var calendar = new DailyRewardCalendar();
            calendar.Load(3, Noon.Date.AddDays(30).Ticks);

            Assert.IsFalse(calendar.CanClaim(Noon));
            Assert.IsTrue(calendar.CanClaim(Noon.AddDays(1)));
            Assert.AreEqual(3, calendar.Claim(Noon.AddDays(1)));
        }

        [Test]
        public void Calendar_LoadInvalidDay_StartsFromFirst()
        {
            var calendar = new DailyRewardCalendar();
            calendar.Load(42, -5);

            Assert.AreEqual(0, calendar.NextDay);
            Assert.AreEqual(0, calendar.LastClaimDayTicks);
        }

        // ---------- Daily reward service ----------

        private sealed class World
        {
            public DateTime Now = Noon;
            public readonly Wallet Wallet = new Wallet();
            public readonly BillWallet Bills = new BillWallet();
            public readonly PrestigeState Prestige = new PrestigeState();
            public readonly FakePlatform Platform = new FakePlatform();
            public double CoinsPerMinute = 100;
            public int Saves;

            public RewardGranter Granter() => new RewardGranter(Wallet, Bills, Prestige, () => CoinsPerMinute);

            public DailyRewardService Daily() =>
                new DailyRewardService(new DailyRewardCalendar(), Days, 10, Granter(), () => Now, () => Saves++);

            public DailyQuestService Quests(Func<QuestType, int, bool> available = null) =>
                new DailyQuestService(new DailyQuests(Templates, 3), Granter(), () => Now, available, () => Saves++);
        }

        [Test]
        public void Daily_CoinsDay_GivesMinutesOfIncome_AndSaves()
        {
            var world = new World();
            DailyRewardService daily = world.Daily();

            Assert.IsTrue(daily.TryClaim(out RewardGrant grant));

            Assert.AreEqual(RewardKind.Coins, grant.Kind);
            Assert.AreEqual(1000, grant.Amount);
            Assert.AreEqual(1000, world.Wallet.Coins);
            Assert.AreEqual(1, world.Saves);
            Assert.IsFalse(daily.TryClaim(out _));
        }

        [Test]
        public void Daily_BillsDay_AddsBills()
        {
            var world = new World();
            DailyRewardService daily = world.Daily();
            daily.TryClaim(out _);
            world.Now = Noon.AddDays(1);

            Assert.IsTrue(daily.TryClaim(out RewardGrant grant));
            Assert.AreEqual(RewardKind.Bills, grant.Kind);
            Assert.AreEqual(2, world.Bills.Bills);
        }

        [Test]
        public void Daily_SeventhDay_GivesSkinOnce_ThenBills()
        {
            var world = new World();
            DailyRewardService daily = world.Daily();
            int skinEvents = 0;
            daily.SkinUnlocked += () => skinEvents++;

            for (int i = 0; i < 7; i++)
            {
                world.Now = Noon.AddDays(i);
                daily.TryClaim(out _);
            }

            Assert.IsTrue(daily.HasSkin);
            Assert.AreEqual(1, skinEvents);
            Assert.AreEqual(RewardKind.Bills, daily.GetDay(6).Kind);
            Assert.AreEqual(10, daily.GetDay(6).Amount);
        }

        [Test]
        public void Daily_SaveShiftedBackOneDay_AllowsNextDay()
        {
            var world = new World();
            DailyRewardService daily = world.Daily();
            daily.TryClaim(out _);
            var data = new SaveData();
            daily.Capture(data);

            // Подмена даты в сейве (меню Save/Shift Retention Day −1 d).
            data.Retention.DailyLastClaimDayTicks -= TimeSpan.TicksPerDay;
            DailyRewardService restored = world.Daily();
            restored.Apply(data);

            Assert.IsTrue(restored.CanClaim);
            Assert.IsTrue(restored.TryClaim(out RewardGrant grant));
            Assert.AreEqual(RewardKind.Bills, grant.Kind, "Second day of the calendar.");
        }

        [Test]
        public void Daily_SkinIsSaved()
        {
            var world = new World();
            DailyRewardService daily = world.Daily();
            var data = new SaveData();
            data.Retention.KioskSkinOwned = true;
            daily.Apply(data);

            Assert.IsTrue(daily.HasSkin);
            daily.Capture(data);
            Assert.IsTrue(data.Retention.KioskSkinOwned);
        }

        // ---------- Quests ----------

        [Test]
        public void Quests_ThreeDistinctTypesPerDay()
        {
            var quests = new DailyQuests(Templates, 3);

            Assert.IsTrue(quests.Refresh(Noon, null));
            Assert.AreEqual(3, quests.Active.Count);
            Assert.AreNotEqual(quests.Active[0].Type, quests.Active[1].Type);
            Assert.AreNotEqual(quests.Active[0].Type, quests.Active[2].Type);
            Assert.AreNotEqual(quests.Active[1].Type, quests.Active[2].Type);
        }

        [Test]
        public void Quests_SameDay_KeepsProgress_NextDay_NewSet()
        {
            var quests = new DailyQuests(Templates, 3);
            quests.Refresh(Noon, null);
            quests.Report(quests.Active[0].Type, 1);

            Assert.IsFalse(quests.Refresh(Noon.AddHours(11), null));
            Assert.AreEqual(1, quests.Active[0].Progress);

            Assert.IsTrue(quests.Refresh(Noon.AddHours(12), null));
            Assert.AreEqual(Noon.Date.AddDays(1).Ticks, quests.DayTicks);
            for (int i = 0; i < quests.Active.Count; i++)
                Assert.AreEqual(0, quests.Active[i].Progress);
        }

        [Test]
        public void Quests_UnavailableTypesAreSkipped()
        {
            var quests = new DailyQuests(Templates, 3);
            quests.Refresh(Noon, (type, target) => type != QuestType.HireManager && type != QuestType.BuyUpgrade);

            Assert.AreEqual(3, quests.Active.Count);
            for (int i = 0; i < quests.Active.Count; i++)
            {
                Assert.AreNotEqual(QuestType.HireManager, quests.Active[i].Type);
                Assert.AreNotEqual(QuestType.BuyUpgrade, quests.Active[i].Type);
            }
        }

        [Test]
        public void Quests_ProgressIsCapped_ClaimOnlyOnceWhenComplete()
        {
            var quests = new DailyQuests(new[] { Templates[0] }, 3);
            quests.Refresh(Noon, null);

            quests.Report(QuestType.BuyLevels, 49);
            Assert.IsFalse(quests.Claim(0));
            quests.Report(QuestType.OpenGift, 5);
            quests.Report(QuestType.BuyLevels, 30);

            Assert.AreEqual(50, quests.Active[0].Progress);
            Assert.IsTrue(quests.HasClaimable);
            Assert.IsTrue(quests.Claim(0));
            Assert.IsFalse(quests.Claim(0));
            Assert.IsFalse(quests.HasClaimable);
        }

        [Test]
        public void Quests_ClockMovedBack_KeepsCurrentSet()
        {
            var quests = new DailyQuests(Templates, 3);
            quests.Refresh(Noon, null);
            quests.Report(quests.Active[0].Type, 1);

            Assert.IsFalse(quests.Refresh(Noon.AddDays(-1), null));
            Assert.AreEqual(1, quests.Active[0].Progress);
        }

        [Test]
        public void QuestService_ReputationReward_DoesNotReducePrestigeGain()
        {
            var world = new World();
            var rules = new PrestigeRules(1e6, 0.02);
            DailyQuestService service = world.Quests((type, target) => type == QuestType.WatchBoost);
            service.Refresh();
            world.Prestige.AddEarned(1e8);
            long gainBefore = rules.ReputationGain(world.Prestige.LifetimeEarned, world.Prestige.ReputationFromCities);

            service.Report(QuestType.WatchBoost, 1);
            Assert.IsTrue(service.TryClaim(0, out RewardGrant grant));

            Assert.AreEqual(RewardKind.Reputation, grant.Kind);
            Assert.AreEqual(1, world.Prestige.ReputationEarned);
            Assert.AreEqual(1, world.Prestige.FreeReputation);
            Assert.AreEqual(1, world.Prestige.ReputationBonus);
            Assert.AreEqual(gainBefore, rules.ReputationGain(world.Prestige.LifetimeEarned, world.Prestige.ReputationFromCities));
        }

        [Test]
        public void QuestService_SaveRoundTrip_AndShiftedDayGivesNewQuests()
        {
            var world = new World();
            DailyQuestService service = world.Quests();
            service.Refresh();
            QuestType first = service.Quests.Active[0].Type;
            service.Report(first, 1);
            var data = new SaveData();
            service.Capture(data);

            DailyQuestService restored = world.Quests();
            restored.Apply(data);
            Assert.AreEqual(3, restored.Quests.Active.Count);
            Assert.AreEqual(first, restored.Quests.Active[0].Type);
            Assert.AreEqual(1, restored.Quests.Active[0].Progress);

            data.Retention.QuestsDayTicks -= TimeSpan.TicksPerDay;
            DailyQuestService nextDay = world.Quests();
            nextDay.Apply(data);
            Assert.AreEqual(Noon.Date.Ticks, nextDay.Quests.DayTicks);
            for (int i = 0; i < nextDay.Quests.Active.Count; i++)
                Assert.AreEqual(0, nextDay.Quests.Active[i].Progress);
        }

        [Test]
        public void QuestTracker_CountsLevelsManagersAndUpgrades()
        {
            var world = new World();
            var station = new TestStationDefinition { Id = "a", StartLevel = 1, BaseCost = 1 };
            var second = new TestStationDefinition { Id = "b", StartLevel = 0, BaseCost = 1 };
            var stations = new StationController(new IStationDefinition[] { station, second }, new MilestoneTable(new[] { 25 }, 2), world.Wallet);
            var managers = new ManagerController(stations, world.Wallet, 0);
            var upgrades = new UpgradeService(new IUpgradeDefinition[] { new TestUpgradeDefinition { Cost = 0, TargetStation = station } },
                stations, world.Wallet);
            var templates = new[]
            {
                new QuestTemplate(QuestType.BuyLevels, 50, new RewardDefinition(RewardKind.Coins, 1)),
                new QuestTemplate(QuestType.HireManager, 1, new RewardDefinition(RewardKind.Coins, 1)),
                new QuestTemplate(QuestType.BuyUpgrade, 1, new RewardDefinition(RewardKind.Coins, 1)),
            };
            var service = new DailyQuestService(new DailyQuests(templates, 3), world.Granter(), () => world.Now, null, null);
            service.Refresh();
            using (new QuestTracker(service, stations, managers, upgrades, null, null))
            {
                world.Wallet.Add(1e9);
                stations.TryBuy(0, 10);
                stations.GrantLevels(1, 5);
                managers.TryHire(0);
                upgrades.TryBuy(0);
            }
            stations.TryBuy(0, 10);

            Assert.AreEqual(15, Find(service.Quests, QuestType.BuyLevels).Progress, "Coins and bills levels count, after Dispose — not.");
            Assert.IsTrue(Find(service.Quests, QuestType.HireManager).IsComplete);
            Assert.IsTrue(Find(service.Quests, QuestType.BuyUpgrade).IsComplete);
        }

        [Test]
        public void QuestTracker_IsAvailable_ChecksWhatIsLeft()
        {
            var wallet = new Wallet();
            var station = new TestStationDefinition();
            var stations = new StationController(new IStationDefinition[] { station }, new MilestoneTable(new[] { 25 }, 2), wallet);
            var managers = new ManagerController(stations, wallet, 0);
            var upgrades = new UpgradeService(new IUpgradeDefinition[] { new TestUpgradeDefinition { Cost = 0, TargetStation = station } },
                stations, wallet);

            Assert.IsTrue(QuestTracker.IsAvailable(QuestType.HireManager, 1, managers, upgrades));
            Assert.IsFalse(QuestTracker.IsAvailable(QuestType.BuyUpgrade, 3, managers, upgrades));
            managers.TryHire(0);
            Assert.IsFalse(QuestTracker.IsAvailable(QuestType.HireManager, 1, managers, upgrades));
            Assert.IsTrue(QuestTracker.IsAvailable(QuestType.BuyLevels, 50, managers, upgrades));
        }

        private static DailyQuests.Quest Find(DailyQuests quests, QuestType type)
        {
            for (int i = 0; i < quests.Active.Count; i++)
            {
                if (quests.Active[i].Type == type)
                    return quests.Active[i];
            }
            Assert.Fail("Quest " + type + " not active.");
            return null;
        }

        // ---------- Boost ----------

        [Test]
        public void Boost_AccumulatesUpToMax()
        {
            var boost = new IncomeBoost(2, 240, 4 * 3600);

            Assert.AreEqual(1, boost.Multiplier);
            Assert.IsTrue(boost.Add());
            Assert.AreEqual(2, boost.Multiplier);
            Assert.AreEqual(240, boost.RemainingSeconds);

            for (int i = 0; i < 100; i++)
                boost.Add();
            Assert.AreEqual(4 * 3600, boost.RemainingSeconds);
            Assert.IsFalse(boost.CanAdd);

            boost.Tick(30);
            Assert.IsFalse(boost.CanAdd, "Less than a minute of room — no ad offered.");
            boost.Tick(30);
            Assert.IsTrue(boost.CanAdd);
        }

        [Test]
        public void Boost_TickExpires()
        {
            var boost = new IncomeBoost(2, 240, 4 * 3600);
            boost.Add();

            Assert.IsFalse(boost.Tick(239));
            Assert.IsTrue(boost.Tick(5));
            Assert.AreEqual(0, boost.RemainingSeconds);
            Assert.AreEqual(1, boost.Multiplier);
            Assert.IsFalse(boost.Tick(1));
        }

        [Test]
        public void BoostService_OnlyAfterWatchedAd()
        {
            var world = new World();
            var service = new BoostService(new IncomeBoost(2, 240, 4 * 3600), world.Platform, () => world.Saves++);
            int changed = 0, watched = 0;
            service.MultiplierChanged += () => changed++;
            service.Watched += () => watched++;

            Assert.IsTrue(service.TryWatch(null));
            Assert.IsFalse(service.TryWatch(null), "Ad is already showing.");
            world.Platform.Finish(false);
            Assert.IsFalse(service.Boost.IsActive);

            service.TryWatch(null);
            world.Platform.Finish(true);
            Assert.IsTrue(service.Boost.IsActive);
            Assert.AreEqual(1, changed);
            Assert.AreEqual(1, watched);
            Assert.AreEqual(1, world.Saves);

            service.TryWatch(null);
            world.Platform.Finish(true);
            Assert.AreEqual(1, changed, "Already active — multiplier unchanged.");
            Assert.AreEqual(480, service.Boost.RemainingSeconds);

            service.Tick(480);
            Assert.AreEqual(2, changed);
        }

        [Test]
        public void BoostService_SaveRoundTrip()
        {
            var world = new World();
            var service = new BoostService(new IncomeBoost(2, 240, 4 * 3600), world.Platform, null);
            var data = new SaveData();
            data.Retention.BoostSeconds = 99999;
            service.Apply(data);

            Assert.AreEqual(4 * 3600, service.Boost.RemainingSeconds);
            service.Tick(100);
            service.Capture(data);
            Assert.AreEqual(4 * 3600 - 100, data.Retention.BoostSeconds);
        }

        // ---------- Gift ----------

        private static GiftBoxService Gift(World world)
        {
            return new GiftBoxService(TimeSpan.FromMinutes(10), 5, world.Wallet, () => world.CoinsPerMinute, world.Platform, () => world.Now,
                () => world.Saves++);
        }

        [Test]
        public void Gift_ReadyAtStart_GivesFiveMinutesOfIncome_ThenCooldown()
        {
            var world = new World();
            GiftBoxService gift = Gift(world);
            double opened = 0;
            gift.Opened += coins => opened = coins;

            Assert.IsTrue(gift.IsReady);
            Assert.IsTrue(gift.TryOpen(null));
            world.Platform.Finish(true);

            Assert.AreEqual(500, opened);
            Assert.AreEqual(500, world.Wallet.Coins);
            Assert.AreEqual(1, world.Saves);
            Assert.IsFalse(gift.IsReady);
            Assert.AreEqual(TimeSpan.FromMinutes(10), gift.TimeLeft);

            world.Now = Noon.AddMinutes(10);
            Assert.IsTrue(gift.IsReady);
        }

        [Test]
        public void Gift_AdNotWatched_NothingAndStillReady()
        {
            var world = new World();
            GiftBoxService gift = Gift(world);

            gift.TryOpen(null);
            world.Platform.Finish(false);

            Assert.AreEqual(0, world.Wallet.Coins);
            Assert.IsTrue(gift.IsReady);
        }

        [Test]
        public void Gift_ClockMovedBack_CooldownDoesNotGrow()
        {
            var world = new World();
            GiftBoxService gift = Gift(world);
            gift.TryOpen(null);
            world.Platform.Finish(true);

            world.Now = Noon.AddHours(-5);
            Assert.AreEqual(TimeSpan.FromMinutes(10), gift.TimeLeft);
        }

        // ---------- Math ----------

        [Test]
        public void RewardMath_UsesIncomeOrMinimum()
        {
            Assert.AreEqual(20, RewardMath.CoinsPerMinute(0, 20));
            Assert.AreEqual(600, RewardMath.CoinsPerMinute(10, 20));
            Assert.AreEqual(3000, RewardMath.CoinsForMinutes(600, 5));
        }

        [Test]
        public void Prestige_BonusReputation_IsSavedAndClamped()
        {
            var state = new PrestigeState();
            state.AddBonusReputation(3);
            var data = new SaveData();
            new PrestigeSaveParticipant(state).Capture(data);
            Assert.AreEqual(3, data.Prestige.ReputationBonus);

            var restored = new PrestigeState();
            new PrestigeSaveParticipant(restored).Apply(data);
            Assert.AreEqual(3, restored.ReputationEarned);
            Assert.AreEqual(0, restored.ReputationFromCities);

            data.Prestige.ReputationBonus = 50;
            data = SaveMigrator.Migrate(data);
            Assert.AreEqual(3, data.Prestige.ReputationBonus);
        }

        private sealed class FakePlatform : IPlatformService
        {
            private Action<bool> _pending;

            public bool IsInitialized => true;
            public string Language => "ru";
            public DateTime ServerTimeUtc => Noon;
            public void Init(Action onReady) => onReady?.Invoke();
            public void GameReady() { }
            public void ShowInterstitial(Action onClosed) => onClosed?.Invoke();
            public void Save(string json, Action<bool> onComplete) => onComplete?.Invoke(true);
            public void Load(Action<string> onLoaded) => onLoaded?.Invoke(null);
            public void SetLeaderboardScore(string leaderboardId, double score) { }
            public void Purchase(string productId, Action<bool> onComplete) => onComplete?.Invoke(true);
            public void GetPurchases(Action<string[]> onLoaded) => onLoaded?.Invoke(new string[0]);
            public void ConsumePurchase(string productId) { }

            public void ShowRewarded(Action<bool> onComplete)
            {
                _pending = onComplete;
            }

            public void Finish(bool watched)
            {
                Action<bool> callback = _pending;
                _pending = null;
                callback?.Invoke(watched);
            }
        }
    }
}
