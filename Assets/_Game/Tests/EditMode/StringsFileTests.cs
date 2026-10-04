using System.IO;
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
            "game.title", "kiosk.placeholder", "hud.income_per_sec",
            "tab.stations", "tab.managers", "tab.upgrades", "tab.city", "tab.coming_soon",
            "buy_mode.label", "buy_mode.max",
            "station.level", "station.tap_to_earn", "station.milestone", "station.milestone_max",
            "station.buy_count", "station.unlock", "unit.seconds",
            "station.shawarma_classic", "station.cheese_lavash", "station.fries",
            "station.drinks_ayran", "station.shawarma_xl", "station.grill_plate",
            "city.home",
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
