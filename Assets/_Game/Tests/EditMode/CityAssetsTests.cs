using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace ShaurmaTycoon.Tests
{
    /// <summary>Проверяет ассеты городов на диске: порядок каталога, пороги, места на карте, уникальные id.</summary>
    public class CityAssetsTests
    {
        private const string ConfigsFolder = "Assets/_Game/Configs";
        private const string CatalogPath = ConfigsFolder + "/Cities/CityCatalog.asset";

        private static readonly Regex GuidRef = new Regex(@"guid: (\w+)");
        private static readonly Regex MapPosition = new Regex(@"^\s*_mapPosition: \{x: ([-\d.eE]+), y: ([-\d.eE]+)\}", RegexOptions.Multiline);

        private static string Read(string path)
        {
            return File.ReadAllText(path).Replace("\r\n", "\n");
        }

        private static string Field(string text, string name)
        {
            Match match = Regex.Match(text, $@"^\s*{name}: (.*)$", RegexOptions.Multiline);
            Assert.IsTrue(match.Success, $"{name} not found");
            return match.Groups[1].Value.Trim();
        }

        private static double Number(string text, string name)
        {
            return double.Parse(Field(text, name), CultureInfo.InvariantCulture);
        }

        private static List<string> CatalogCities()
        {
            var pathByGuid = new Dictionary<string, string>();
            foreach (string meta in Directory.GetFiles(ConfigsFolder + "/Cities", "*.asset.meta"))
                pathByGuid[GuidRef.Match(Read(meta)).Groups[1].Value] = meta.Substring(0, meta.Length - ".meta".Length);

            var cities = new List<string>();
            foreach (Match match in GuidRef.Matches(Read(CatalogPath)))
            {
                if (match.Groups[1].Value == "c1c77c65c0264578a856707533bf70ec")
                    continue; // скрипт CityCatalog
                Assert.IsTrue(pathByGuid.TryGetValue(match.Groups[1].Value, out string path), $"catalog references a missing city {match.Value}");
                cities.Add(Read(path));
            }
            return cities;
        }

        [Test]
        public void Catalog_HasFiveCities_WithGrowingThresholdsAndScales()
        {
            List<string> cities = CatalogCities();

            Assert.AreEqual(5, cities.Count);
            for (int i = 1; i < cities.Count; i++)
            {
                Assert.AreEqual(Number(cities[i - 1], "_prestigeThreshold") * 10, Number(cities[i], "_prestigeThreshold"), 1e-3, $"city {i + 1} threshold");
                Assert.AreEqual(Number(cities[i - 1], "_valueScale") * 10, Number(cities[i], "_valueScale"), 1e-9, $"city {i + 1} scale");
            }
        }

        [Test]
        public void Cities_HaveDistinctMapPositionsInsideTheMap()
        {
            var seen = new HashSet<string>();
            foreach (string city in CatalogCities())
            {
                Match match = MapPosition.Match(city);
                Assert.IsTrue(match.Success, $"{Field(city, "_id")}: no _mapPosition");
                float x = float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                float y = float.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                Assert.That(x, Is.InRange(0f, 1f));
                Assert.That(y, Is.InRange(0f, 1f));
                Assert.IsTrue(seen.Add(match.Value.Trim()), $"{Field(city, "_id")}: same place as another city");
            }
        }

        [Test]
        public void Cities_HaveSixStationsAndTwentyUpgrades()
        {
            foreach (string city in CatalogCities())
            {
                string stations = Regex.Match(city, @"_stations:\n((?:  - .*\n)*)").Groups[1].Value;
                string upgrades = Regex.Match(city, @"_upgrades:\n((?:  - .*\n)*)").Groups[1].Value;
                Assert.AreEqual(6, GuidRef.Matches(stations).Count, $"{Field(city, "_id")} stations");
                Assert.AreEqual(20, GuidRef.Matches(upgrades).Count, $"{Field(city, "_id")} upgrades");
            }
        }

        [TestCase("Stations")]
        [TestCase("Upgrades")]
        [TestCase("Cities")]
        public void Ids_AreUniqueAcrossCities(string folder)
        {
            var ids = new HashSet<string>();
            foreach (string asset in Directory.GetFiles($"{ConfigsFolder}/{folder}", "*.asset", SearchOption.AllDirectories))
            {
                string text = Read(asset);
                if (!Regex.IsMatch(text, @"^\s*_id:", RegexOptions.Multiline))
                    continue;
                Assert.IsTrue(ids.Add(Field(text, "_id")), $"duplicate id in {asset}");
            }

            Assert.Greater(ids.Count, 0);
        }
    }
}
