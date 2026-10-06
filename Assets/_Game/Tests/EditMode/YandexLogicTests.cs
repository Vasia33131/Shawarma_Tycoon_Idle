using System;
using NUnit.Framework;
using ShaurmaTycoon.Core;
using ShaurmaTycoon.Platform;
using ShaurmaTycoon.Prestige;
using ShaurmaTycoon.Save;

namespace ShaurmaTycoon.Tests
{
    /// <summary>Платформонезависимая логика этапа 09: выбор сейва, пауза, межстраничная реклама, лидерборд.</summary>
    public class YandexLogicTests
    {
        // ---------- SaveSelector ----------

        private static string SaveJson(long ticks) => "{\"Version\":1,\"Coins\":5,\"SavedAtUtcTicks\":" + ticks + ",\"Stations\":[]}";

        [Test]
        public void SaveSelector_ReadsTicks_WithOrWithoutSpaces()
        {
            Assert.AreEqual(123, SaveSelector.ReadSavedAtTicks(SaveJson(123)));
            Assert.AreEqual(456, SaveSelector.ReadSavedAtTicks("{ \"SavedAtUtcTicks\" : 456 }"));
            Assert.AreEqual(0, SaveSelector.ReadSavedAtTicks("{}"));
            Assert.AreEqual(0, SaveSelector.ReadSavedAtTicks(null));
        }

        [Test]
        public void SaveSelector_Pick_NewerWins()
        {
            string older = SaveJson(100);
            string newer = SaveJson(200);

            Assert.AreEqual(newer, SaveSelector.Pick(newer, older));
            Assert.AreEqual(newer, SaveSelector.Pick(older, newer));
        }

        [Test]
        public void SaveSelector_Pick_EqualPrefersCloud()
        {
            string cloud = SaveJson(100) + " ";
            string local = SaveJson(100);

            Assert.AreEqual(cloud, SaveSelector.Pick(cloud, local));
        }

        [Test]
        public void SaveSelector_Pick_IgnoresEmptyAndHandlesNone()
        {
            string save = SaveJson(1);

            Assert.AreEqual(save, SaveSelector.Pick(null, save));
            Assert.AreEqual(save, SaveSelector.Pick(save, ""));
            Assert.IsNull(SaveSelector.Pick(null, null));
        }

        // ---------- PauseController ----------

        [Test]
        public void Pause_AppliesOnlyOnChange_AndCombinesReasons()
        {
            var applied = new System.Collections.Generic.List<bool>();
            var pause = new PauseController(applied.Add);

            pause.SetPlatformPause(true);
            pause.SetFocus(false);
            pause.SetPlatformPause(false);
            Assert.IsTrue(pause.IsPaused, "Hidden tab still pauses after the ad closes.");
            pause.SetFocus(true);

            CollectionAssert.AreEqual(new[] { true, false }, applied);
            Assert.IsFalse(pause.IsPaused);
        }

        // ---------- InterstitialService ----------

        private sealed class AdPlatform : IPlatformService
        {
            public int Interstitials;
            public Action PendingClose;
            public readonly System.Collections.Generic.List<(string id, double score)> Scores = new System.Collections.Generic.List<(string, double)>();

            public bool IsInitialized => true;
            public string Language => "ru";
            public DateTime ServerTimeUtc => DateTime.UtcNow;
            public void Init(Action onReady) => onReady?.Invoke();
            public void GameReady() { }
            public void SetGameplayPaused(bool paused) { }
            public void GetCatalog(Action<ProductPrice[]> onLoaded) => onLoaded?.Invoke(null);
            public event Action<bool> AdShowing { add { } remove { } }
            public void ShowRewarded(Action<bool> onComplete) => onComplete?.Invoke(true);
            public void Save(string json, Action<bool> onComplete) => onComplete?.Invoke(true);
            public void Load(Action<string> onLoaded) => onLoaded?.Invoke(null);
            public void Purchase(string productId, Action<bool> onComplete) => onComplete?.Invoke(true);
            public void GetPurchases(Action<string[]> onLoaded) => onLoaded?.Invoke(new string[0]);
            public void ConsumePurchase(string productId) { }
            public void SetLeaderboardScore(string leaderboardId, double score) => Scores.Add((leaderboardId, score));

            public void ShowInterstitial(Action onClosed)
            {
                Interstitials++;
                PendingClose = onClosed;
            }
        }

        [Test]
        public void Interstitial_NotInFirstThreeMinutes_ContinuesImmediately()
        {
            var platform = new AdPlatform();
            double now = 100;
            var service = new InterstitialService(platform, () => now);
            int continued = 0;

            bool shown = service.TryShow(() => continued++);

            Assert.IsFalse(shown);
            Assert.AreEqual(1, continued);
            Assert.AreEqual(0, platform.Interstitials);
        }

        [Test]
        public void Interstitial_AfterDelay_ShowsAndContinuesOnClose()
        {
            var platform = new AdPlatform();
            double now = 200;
            var service = new InterstitialService(platform, () => now);
            int continued = 0;

            Assert.IsTrue(service.TryShow(() => continued++));
            Assert.AreEqual(0, continued, "Waits for the ad to close.");
            Assert.IsFalse(service.CanShow, "Second ad cannot start while one is open.");

            platform.PendingClose();

            Assert.AreEqual(1, continued);
        }

        [Test]
        public void Interstitial_RespectsMinInterval()
        {
            var platform = new AdPlatform();
            double now = 200;
            var service = new InterstitialService(platform, () => now);
            service.TryShow(null);
            platform.PendingClose();

            now += 89;
            Assert.IsFalse(service.CanShow);
            now += 2;
            Assert.IsTrue(service.CanShow);
        }

        [Test]
        public void Interstitial_AdsDisabled_NeverShows()
        {
            var platform = new AdPlatform();
            var service = new InterstitialService(platform, () => 10000) { AdsDisabled = true };
            int continued = 0;

            Assert.IsFalse(service.TryShow(() => continued++));
            Assert.AreEqual(1, continued);
            Assert.AreEqual(0, platform.Interstitials);
        }

        // ---------- LeaderboardService ----------

        [Test]
        public void Leaderboard_ToScore_IsIntegerAndClamped()
        {
            Assert.AreEqual(0, LeaderboardService.ToScore(0.5));
            Assert.AreEqual(0, LeaderboardService.ToScore(double.NaN));
            Assert.AreEqual(1234, LeaderboardService.ToScore(1234.9));
            Assert.AreEqual(LeaderboardService.MaxScore, LeaderboardService.ToScore(1e300));
        }

        [Test]
        public void Leaderboard_SendsOnlyWhenGrown()
        {
            var platform = new AdPlatform();
            var state = new PrestigeState();
            var service = new LeaderboardService(platform, state);

            service.SendIfGrown();
            Assert.AreEqual(0, platform.Scores.Count, "Nothing earned yet.");

            state.AddEarned(500);
            service.SendIfGrown();
            service.SendIfGrown();
            Assert.AreEqual(1, platform.Scores.Count);
            Assert.AreEqual(500, platform.Scores[0].score);

            state.AddEarned(100);
            service.SendIfGrown();
            Assert.AreEqual(2, platform.Scores.Count);
            Assert.AreEqual(600, platform.Scores[1].score);
        }

        [Test]
        public void Leaderboard_Tick_SendsAfterInterval()
        {
            var platform = new AdPlatform();
            var state = new PrestigeState();
            state.AddEarned(10);
            var service = new LeaderboardService(platform, state);

            service.Tick(LeaderboardService.SendInterval - 1);
            Assert.AreEqual(0, platform.Scores.Count);
            service.Tick(2);
            Assert.AreEqual(1, platform.Scores.Count);
        }
    }
}
