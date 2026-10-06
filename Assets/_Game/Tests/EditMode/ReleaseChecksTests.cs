using System.Collections.Generic;
using NUnit.Framework;
using ShaurmaTycoon.Core;
using ShaurmaTycoon.Localization;
using ShaurmaTycoon.Platform;
using ShaurmaTycoon.Shop;

namespace ShaurmaTycoon.Tests
{
    /// <summary>Этап 12 (требования модерации): цены из каталога платформы и пауза по фокусу окна и рекламе.</summary>
    public class ReleaseChecksTests
    {
        private static Loc CreateLoc(string language)
        {
            return new Loc(LocTable.Parse("key\tru\ten\nshop.price_yan\t{0} ян\t{0} YAN\n"), language);
        }

        // ---------- PriceCatalog ----------

        [Test]
        public void Parse_ReadsIdPriceAndCurrency_SkipsBroken()
        {
            ProductPrice[] items = PriceCatalog.Parse("bills_small=19=YAN;broken;no_ads=149=YAN;=5=YAN;x=7");

            Assert.AreEqual(3, items.Length);
            Assert.AreEqual("bills_small", items[0].Id);
            Assert.AreEqual("19", items[0].Value);
            Assert.AreEqual("YAN", items[0].CurrencyCode);
            Assert.AreEqual("no_ads", items[1].Id);
            Assert.AreEqual("x", items[2].Id);
            Assert.AreEqual(string.Empty, items[2].CurrencyCode);
            Assert.AreEqual(0, PriceCatalog.Parse("").Length);
        }

        [Test]
        public void Format_UsesCatalogPrice_AndFallsBackToConfig()
        {
            var catalog = new PriceCatalog();
            Loc ru = CreateLoc("ru");

            Assert.AreEqual("99 ян", catalog.Format(ru, "starter_pack", 99), "Before the catalog arrives the config price is shown.");

            catalog.Apply(PriceCatalog.Parse("starter_pack=79=YAN;other=5=RUB"));

            Assert.AreEqual("79 ян", catalog.Format(ru, "starter_pack", 99));
            Assert.AreEqual("79 YAN", catalog.Format(CreateLoc("en"), "starter_pack", 99));
            Assert.AreEqual("5 RUB", catalog.Format(ru, "other", 1), "Another portal currency is shown by its code.");
            Assert.AreEqual("149 ян", catalog.Format(ru, "no_ads", 149));
        }

        [Test]
        public void IsAvailable_HidesProductsMissingFromLoadedCatalog()
        {
            var catalog = new PriceCatalog();
            int changed = 0;
            catalog.Changed += () => changed++;

            Assert.IsTrue(catalog.IsAvailable("no_ads"), "Not loaded yet: everything is shown.");

            catalog.Apply(null);
            Assert.IsTrue(catalog.IsAvailable("no_ads"), "Catalog failed to load: everything is shown.");
            Assert.AreEqual(0, changed);

            catalog.Apply(PriceCatalog.Parse("bills_small=19=YAN"));
            Assert.AreEqual(1, changed);
            Assert.IsTrue(catalog.IsAvailable("bills_small"));
            Assert.IsFalse(catalog.IsAvailable("no_ads"));
            Assert.IsFalse(catalog.IsAvailable(null));
        }

        // ---------- Wallet.Spent (сейв после покупки) ----------

        [Test]
        public void Wallet_Spent_RaisedOnlyForRealSpending()
        {
            var wallet = new Economy.Wallet();
            var spent = new List<double>();
            wallet.Spent += spent.Add;
            wallet.Add(100);

            wallet.TrySpend(30);
            wallet.TrySpend(0);
            wallet.TrySpend(1000);

            CollectionAssert.AreEqual(new[] { 30.0 }, spent);
        }

        // ---------- PauseController ----------

        [Test]
        public void Pause_WindowBlurAndAd_ArePauseReasons()
        {
            var applied = new List<bool>();
            var pause = new PauseController(applied.Add);

            pause.SetWindowFocus(false);
            Assert.IsTrue(pause.IsPaused, "Alt+Tab mutes the game.");
            pause.SetAdShowing(true);
            pause.SetWindowFocus(true);
            Assert.IsTrue(pause.IsPaused, "The ad is still open.");
            pause.SetAdShowing(false);

            CollectionAssert.AreEqual(new[] { true, false }, applied);
            Assert.IsFalse(pause.IsPaused);
        }
    }
}
