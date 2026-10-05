using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ShaurmaTycoon.Localization;

namespace ShaurmaTycoon.Tests
{
    /// <summary>Проверяет реальную таблицу строк игры.</summary>
    public class StringsFileTests
    {
        private const string Path = "Assets/_Game/Configs/Localization/strings.txt";

        private static readonly string[] RequiredKeys =
        {
            "game.title", "hud.income_per_sec",
            "tab.stations", "tab.managers", "tab.upgrades", "tab.city", "tab.coming_soon",
            "buy_mode.label", "buy_mode.max",
            "station.level", "station.tap_to_earn", "station.milestone", "station.milestone_max",
            "station.buy_count", "station.unlock", "unit.seconds",
            "station.shawarma_classic", "station.cheese_lavash", "station.fries",
            "station.drinks_ayran", "station.shawarma_xl", "station.grill_plate",
            "city.home",
            "manager.description", "manager.hire", "manager.hired", "manager.locked",
            "upgrade.station", "upgrade.all", "upgrade.buy", "upgrade.none",
            "customer.phrase_01", "customer.phrase_02", "customer.phrase_03", "customer.phrase_04", "customer.phrase_05",
            "customer.phrase_06", "customer.phrase_07", "customer.phrase_08", "customer.phrase_09", "customer.phrase_10",
            "offline.title", "offline.subtitle", "offline.claim", "offline.claim_ad", "offline.limit",
            "time.minutes", "time.hours", "time.hours_minutes",
            "city.kazan", "station.kazan_shawarma", "station.echpochmak_wrap", "station.chak_chak", "station.katyk_tea",
            "station.kazan_xl", "station.plov_plate",
            "prestige.earned_progress", "prestige.gain", "prestige.bonus", "prestige.reputation",
            "prestige.go_next", "prestige.go_same", "prestige.confirm_title", "prestige.confirm_title_same",
            "prestige.confirm_body", "prestige.confirm_gain", "prestige.confirm_yes", "prestige.confirm_no",
            "shop.title", "shop.level", "shop.buy", "shop.max",
            "shop.offline_limit.name", "shop.offline_limit.desc", "shop.start_coins.name", "shop.start_coins.desc",
            "shop.free_managers.name", "shop.free_managers.desc", "shop.fast_customers.name", "shop.fast_customers.desc",
            "map.title", "map.here", "map.status_done", "map.status_current", "map.status_next", "map.status_locked", "map.details",
            "kiosk.upgraded", "city.sochi", "city.piter", "city.moscow",
            "settings.title", "settings.sound", "settings.music", "settings.on", "settings.off", "settings.close",
            "bills.name",
            "tab.shop", "shop.bills_title", "shop.coins_title", "shop.bills_small", "shop.bills_medium", "shop.bills_large",
            "shop.coins_small", "shop.coins_medium", "shop.coins_large", "shop.bills_amount", "shop.coins_reward",
            "shop.price_yan", "shop.price_bills", "shop.best",
            "bills.offer_title", "bills.offer_body", "bills.offer_poor", "bills.offer_buy", "bills.offer_shop", "bills.offer_cancel",
            "bills.item_unlock", "bills.item_levels", "bills.item_upgrade", "bills.item_manager",
        };

        private static LocTable LoadTable()
        {
            return LocTable.Parse(File.ReadAllText(Path));
        }

        [Test]
        public void HasRussianAndEnglishColumns()
        {
            LocTable table = LoadTable();

            Assert.AreEqual("ru", table.Languages[0]);
            Assert.AreEqual("en", table.Languages[1]);
        }

        [Test]
        public void ConfigNameKeys_HaveBothTranslations()
        {
            LocTable table = LoadTable();
            var keyPattern = new Regex(@"^\s*_nameKey:\s*(\S+)\s*$", RegexOptions.Multiline);
            int checkedKeys = 0;

            foreach (string folder in new[] { "Assets/_Game/Configs/Stations", "Assets/_Game/Configs/Cities" })
            {
                foreach (string asset in Directory.GetFiles(folder, "*.asset", SearchOption.AllDirectories))
                {
                    foreach (Match match in keyPattern.Matches(File.ReadAllText(asset)))
                    {
                        string key = match.Groups[1].Value;
                        Assert.IsTrue(table.TryGet(key, "ru", out _), $"ru missing: {key} ({asset})");
                        Assert.IsTrue(table.TryGet(key, "en", out _), $"en missing: {key} ({asset})");
                        checkedKeys++;
                    }
                }
            }

            Assert.Greater(checkedKeys, 0);
        }

        [Test]
        public void RequiredKeys_HaveBothTranslations()
        {
            LocTable table = LoadTable();

            foreach (string key in RequiredKeys)
            {
                Assert.IsTrue(table.TryGet(key, "ru", out _), "ru missing: " + key);
                Assert.IsTrue(table.TryGet(key, "en", out _), "en missing: " + key);
            }
        }
    }
}
