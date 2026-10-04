using System;
using NUnit.Framework;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Platform;
using ShaurmaTycoon.Shop;

namespace ShaurmaTycoon.Tests
{
    public class BillShopTests
    {
        /// <summary>Платформа, где покупку подтверждает тест (Complete), а не сразу.</summary>
        private sealed class FakePlatform : IPlatformService
        {
            public string LastProduct;
            private Action<bool> _pending;

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

            public void Purchase(string productId, Action<bool> onComplete)
            {
                LastProduct = productId;
                _pending = onComplete;
            }

            public void Complete(bool success)
            {
                Action<bool> callback = _pending;
                _pending = null;
                callback?.Invoke(success);
            }
        }

        private static readonly BillPackConfig[] BillPacks =
        {
            new BillPackConfig { Id = "small", ProductId = "bills_small", Bills = 20, DisplayPrice = 19 },
            new BillPackConfig { Id = "large", ProductId = "bills_large", Bills = 350, DisplayPrice = 249, Best = true },
        };

        private static readonly CoinPackConfig[] CoinPacks =
        {
            new CoinPackConfig { Id = "small", PriceBills = 5, IncomeMinutes = 30, MinCoins = 1000 },
            new CoinPackConfig { Id = "large", PriceBills = 60, IncomeMinutes = 720, MinCoins = 50000 },
        };

        private sealed class World
        {
            public BillWallet Bills = new BillWallet();
            public Wallet Coins = new Wallet();
            public FakePlatform Platform = new FakePlatform();
            public double Income = 10;
            public int Saves;
            public BillShop Shop;
        }

        private static World Create(double cityScale = 1)
        {
            var world = new World();
            world.Shop = new BillShop(BillPacks, CoinPacks, world.Bills, world.Coins, () => world.Income, cityScale, world.Platform,
                () => world.Saves++);
            return world;
        }

        [Test]
        public void CoinReward_IsMinutesOfIncome()
        {
            World world = Create();
            world.Income = 100;

            Assert.AreEqual(100 * 30 * 60, world.Shop.GetCoinReward(0));
        }

        [Test]
        public void CoinReward_NoIncome_UsesMinimumTimesCityScale()
        {
            World world = Create(cityScale: 10);
            world.Income = 0;

            Assert.AreEqual(10000, world.Shop.GetCoinReward(0));
        }

        [Test]
        public void TryBuyCoins_Enough_SpendsBillsAddsCoinsAndSaves()
        {
            World world = Create();
            world.Bills.Add(7, BillSource.Debug);
            double reported = 0;
            world.Shop.CoinsPurchased += (_, coins) => reported = coins;

            Assert.IsTrue(world.Shop.TryBuyCoins(0));

            Assert.AreEqual(2, world.Bills.Bills);
            Assert.AreEqual(10 * 30 * 60, world.Coins.Coins);
            Assert.AreEqual(world.Coins.Coins, reported);
            Assert.AreEqual(1, world.Saves);
        }

        [Test]
        public void TryBuyCoins_NotEnoughBills_ChangesNothing()
        {
            World world = Create();
            world.Bills.Add(4, BillSource.Debug);

            Assert.IsFalse(world.Shop.CanBuyCoins(0));
            Assert.IsFalse(world.Shop.TryBuyCoins(0));
            Assert.AreEqual(4, world.Bills.Bills);
            Assert.AreEqual(0, world.Coins.Coins);
            Assert.AreEqual(0, world.Saves);
        }

        [Test]
        public void BuyBillPack_Success_AddsBillsOnlyAfterConfirmation()
        {
            World world = Create();
            bool? result = null;

            Assert.IsTrue(world.Shop.BuyBillPack(1, ok => result = ok));
            Assert.AreEqual("bills_large", world.Platform.LastProduct);
            Assert.IsTrue(world.Shop.IsPurchasing);
            Assert.AreEqual(0, world.Bills.Bills);

            world.Platform.Complete(true);

            Assert.AreEqual(true, result);
            Assert.IsFalse(world.Shop.IsPurchasing);
            Assert.AreEqual(350, world.Bills.Bills);
            Assert.AreEqual(1, world.Saves);
        }

        [Test]
        public void BuyBillPack_Failed_GivesNothing()
        {
            World world = Create();
            bool? result = null;
            world.Shop.BuyBillPack(0, ok => result = ok);

            world.Platform.Complete(false);

            Assert.AreEqual(false, result);
            Assert.AreEqual(0, world.Bills.Bills);
            Assert.IsFalse(world.Shop.IsPurchasing);
            Assert.AreEqual(0, world.Saves);
        }

        [Test]
        public void BuyBillPack_WhilePurchasing_IsRejected()
        {
            World world = Create();
            world.Shop.BuyBillPack(0, null);

            Assert.IsFalse(world.Shop.BuyBillPack(1, null));
            Assert.AreEqual("bills_small", world.Platform.LastProduct);
        }

        [TestCase(-1)]
        [TestCase(2)]
        public void InvalidIndex_IsRejected(int index)
        {
            World world = Create();
            world.Bills.Add(100, BillSource.Debug);

            Assert.IsFalse(world.Shop.TryBuyCoins(index));
            Assert.IsFalse(world.Shop.BuyBillPack(index, null));
        }
    }
}
