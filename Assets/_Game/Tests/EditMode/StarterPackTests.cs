using System;
using System.Collections.Generic;
using NUnit.Framework;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Platform;
using ShaurmaTycoon.Save;
using ShaurmaTycoon.Shop;

namespace ShaurmaTycoon.Tests
{
    public class StarterPackTests
    {
        private sealed class FakePlatform : IPlatformService
        {
            public Action<bool> Pending;
            public string LastProduct;
            public readonly List<string> Consumed = new List<string>();

            public bool IsInitialized => true;
            public string Language => "ru";
            public DateTime ServerTimeUtc => DateTime.UtcNow;
            public void Init(Action onReady) => onReady?.Invoke();
            public void GameReady() { }
            public void ShowRewarded(Action<bool> onComplete) => onComplete?.Invoke(true);
            public void ShowInterstitial(Action onClosed) => onClosed?.Invoke();
            public void Save(string json, Action<bool> onComplete) => onComplete?.Invoke(true);
            public void Load(Action<string> onLoaded) => onLoaded?.Invoke(null);
            public void SetLeaderboardScore(string leaderboardId, double score) { }
            public void GetPurchases(Action<string[]> onLoaded) => onLoaded?.Invoke(new string[0]);
            public void ConsumePurchase(string productId) => Consumed.Add(productId);

            public void Purchase(string productId, Action<bool> onComplete)
            {
                LastProduct = productId;
                Pending = onComplete;
            }
        }

        private sealed class World
        {
            public BillWallet Bills = new BillWallet();
            public Wallet Coins = new Wallet();
            public FakePlatform Platform = new FakePlatform();
            public double Income;
            public int Saves;
            public StarterPack Pack;
        }

        private static World Create(double income = 0)
        {
            var world = new World { Income = income };
            // 1 купюра = 360 с дохода, минимум 200 монет.
            var pricing = new BillPricing(() => world.Income, 360, 200);
            world.Pack = new StarterPack(new StarterPackConfig(), world.Bills, world.Coins, pricing, () => world.Income, world.Platform,
                () => world.Saves++);
            return world;
        }

        [Test]
        public void CoinReward_NoIncome_IsFiftyBillsAtMinimumRate()
        {
            World world = Create(income: 0);

            Assert.AreEqual(50 * 200, world.Pack.GetCoinReward());
        }

        [Test]
        public void CoinReward_FollowsIncomeLikeBills()
        {
            World world = Create(income: 100);

            Assert.AreEqual(50 * 100 * 360, world.Pack.GetCoinReward());
        }

        [Test]
        public void CoinReward_NeverBelowMinIncomeMinutes()
        {
            var world = new World { Income = 100 };
            // Курс купюры очень низкий (минимум 1 монета, доход — 1 с): 50 купюр дадут мало, но 10 минут дохода — больше.
            var pricing = new BillPricing(() => 0, 1, 1);
            var pack = new StarterPack(new StarterPackConfig(), world.Bills, world.Coins, pricing, () => world.Income, world.Platform, null);

            Assert.AreEqual(100 * 10 * 60, pack.GetCoinReward());
        }

        [Test]
        public void Buy_GrantsOnlyAfterPaymentConfirmation()
        {
            World world = Create(income: 10);

            Assert.IsTrue(world.Pack.Buy(null));
            Assert.AreEqual("starter_pack", world.Platform.LastProduct);
            Assert.IsTrue(world.Pack.IsPurchasing);
            Assert.AreEqual(0, world.Bills.Bills);

            world.Platform.Pending(true);

            Assert.IsTrue(world.Pack.IsPurchased);
            Assert.IsFalse(world.Pack.IsPurchasing);
            Assert.AreEqual(50, world.Bills.Bills);
            Assert.AreEqual(50 * 10 * 360, world.Coins.Coins);
            Assert.AreEqual(1, world.Saves);
            Assert.IsEmpty(world.Platform.Consumed, "A permanent product is never consumed.");
        }

        [Test]
        public void Buy_Failed_GivesNothingAndAllowsRetry()
        {
            World world = Create();

            world.Pack.Buy(null);
            world.Platform.Pending(false);

            Assert.IsFalse(world.Pack.IsPurchased);
            Assert.AreEqual(0, world.Bills.Bills);
            Assert.IsTrue(world.Pack.CanBuy);
        }

        [Test]
        public void Buy_Twice_IsRejected()
        {
            World world = Create();

            Assert.IsTrue(world.Pack.Buy(null));
            Assert.IsFalse(world.Pack.Buy(null), "Payment window is already open.");
            world.Platform.Pending(true);
            Assert.IsFalse(world.Pack.Buy(null), "Already purchased.");
        }

        [Test]
        public void TryGrantPurchased_GrantsOnceAndIgnoresOtherProducts()
        {
            World world = Create();

            Assert.IsFalse(world.Pack.TryGrantPurchased("bills_small"));
            Assert.IsTrue(world.Pack.TryGrantPurchased("starter_pack"));
            Assert.IsTrue(world.Pack.TryGrantPurchased("starter_pack"));

            Assert.AreEqual(50, world.Bills.Bills, "Granted only once.");
            Assert.AreEqual(1, world.Saves);
        }

        [Test]
        public void SaveRoundTrip_KeepsPurchasedFlag()
        {
            World world = Create();
            world.Pack.TryGrantPurchased("starter_pack");
            var data = new SaveData();
            world.Pack.Capture(data);
            Assert.IsTrue(data.StarterPackBought);

            World restored = Create();
            restored.Pack.Apply(data);

            Assert.IsTrue(restored.Pack.IsPurchased);
            Assert.IsFalse(restored.Pack.CanBuy);
        }
    }
}
