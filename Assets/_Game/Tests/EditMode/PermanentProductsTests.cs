using System;
using System.Collections.Generic;
using NUnit.Framework;
using ShaurmaTycoon.Platform;
using ShaurmaTycoon.Save;
using ShaurmaTycoon.Shop;

namespace ShaurmaTycoon.Tests
{
    public class PermanentProductsTests
    {
        private sealed class FakePlatform : IPlatformService
        {
            public Action<bool> Pending;
            public string LastProduct;

            public bool IsInitialized => true;
            public string Language => "ru";
            public DateTime ServerTimeUtc => DateTime.UtcNow;
            public void Init(Action onReady) => onReady?.Invoke();
            public void GameReady() { }
            public void SetGameplayPaused(bool paused) { }
            public void GetCatalog(Action<ProductPrice[]> onLoaded) => onLoaded?.Invoke(null);
            public event Action<bool> AdShowing { add { } remove { } }
            public void ShowRewarded(Action<bool> onComplete) => onComplete?.Invoke(true);
            public void ShowInterstitial(Action onClosed) => onClosed?.Invoke();
            public void Save(string json, Action<bool> onComplete) => onComplete?.Invoke(true);
            public void Load(Action<string> onLoaded) => onLoaded?.Invoke(null);
            public void SetLeaderboardScore(string leaderboardId, double score) { }
            public void GetPurchases(Action<string[]> onLoaded) => onLoaded?.Invoke(new string[0]);
            public readonly List<string> Consumed = new List<string>();
            public void ConsumePurchase(string productId) => Consumed.Add(productId);

            public void Purchase(string productId, Action<bool> onComplete)
            {
                LastProduct = productId;
                Pending = onComplete;
            }
        }

        private static PermanentProducts Create(FakePlatform platform, Action save = null)
        {
            return new PermanentProducts(new PermanentOfferConfig { ProductId = "no_ads", DisplayPrice = 149 },
                new PermanentOfferConfig { ProductId = "income_x2", DisplayPrice = 299 }, platform, save);
        }

        [Test]
        public void Buy_GrantsOnlyAfterConfirmation_AndSaves()
        {
            var platform = new FakePlatform();
            int saves = 0;
            PermanentProducts products = Create(platform, () => saves++);
            PermanentProduct? purchased = null;
            products.Purchased += p => purchased = p;

            Assert.IsTrue(products.Buy(PermanentProduct.IncomeX2, null));
            Assert.AreEqual("income_x2", platform.LastProduct);
            Assert.IsTrue(products.IsPurchasing);
            Assert.IsFalse(products.Has(PermanentProduct.IncomeX2));
            Assert.AreEqual(1, products.IncomeMultiplier);

            platform.Pending(true);

            Assert.IsTrue(products.Has(PermanentProduct.IncomeX2));
            Assert.AreEqual(2, products.IncomeMultiplier);
            Assert.AreEqual(PermanentProduct.IncomeX2, purchased);
            Assert.AreEqual(1, saves);
            Assert.IsEmpty(platform.Consumed, "Permanent products are never consumed.");
        }

        [Test]
        public void Buy_Failed_GivesNothing()
        {
            var platform = new FakePlatform();
            PermanentProducts products = Create(platform);

            products.Buy(PermanentProduct.NoAds, null);
            platform.Pending(false);

            Assert.IsFalse(products.Has(PermanentProduct.NoAds));
            Assert.IsTrue(products.CanBuy(PermanentProduct.NoAds));
        }

        [Test]
        public void Buy_WhilePurchasingOrOwned_IsRejected()
        {
            var platform = new FakePlatform();
            PermanentProducts products = Create(platform);

            Assert.IsTrue(products.Buy(PermanentProduct.NoAds, null));
            Assert.IsFalse(products.Buy(PermanentProduct.IncomeX2, null), "One payment window at a time.");
            platform.Pending(true);
            Assert.IsFalse(products.Buy(PermanentProduct.NoAds, null), "Already owned.");
            Assert.AreEqual(1, products.IncomeMultiplier, "No ads does not boost income.");
        }

        [Test]
        public void TryGrantPurchased_GrantsOnce_AndIgnoresUnknown()
        {
            PermanentProducts products = Create(new FakePlatform());
            int events = 0;
            products.Purchased += _ => events++;

            Assert.IsFalse(products.TryGrantPurchased("bills_small"));
            Assert.IsTrue(products.TryGrantPurchased("no_ads"));
            Assert.IsTrue(products.TryGrantPurchased("no_ads"));

            Assert.IsTrue(products.Has(PermanentProduct.NoAds));
            Assert.AreEqual(1, events);
        }

        [Test]
        public void SaveRoundTrip_KeepsBothFlags()
        {
            PermanentProducts products = Create(new FakePlatform());
            products.TryGrantPurchased("no_ads");
            products.TryGrantPurchased("income_x2");
            var data = new SaveData();
            products.Capture(data);

            PermanentProducts restored = Create(new FakePlatform());
            restored.Apply(data);

            Assert.IsTrue(restored.Has(PermanentProduct.NoAds));
            Assert.IsTrue(restored.Has(PermanentProduct.IncomeX2));
            Assert.AreEqual(2, restored.IncomeMultiplier);
        }
    }
}
