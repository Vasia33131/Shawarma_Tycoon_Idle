using System;
using System.Collections.Generic;
using NUnit.Framework;
using ShaurmaTycoon.Save;

namespace ShaurmaTycoon.Tests
{
    /// <summary>Реальный JsonUtility — только в Unity Test Runner.</summary>
    public class JsonSaveSerializerTests
    {
        private readonly JsonSaveSerializer _serializer = new JsonSaveSerializer();

        [Test]
        public void RoundTrip_KeepsAllFields()
        {
            var data = new SaveData
            {
                Coins = 123456789.25,
                SavedAtUtcTicks = 638000000000000000,
                Stations = new List<StationSave>
                {
                    new StationSave { Id = "shawarma_classic", Level = 31, Automated = true, Progress = 0.4, Running = true },
                },
                PurchasedUpgrades = new List<string> { "shawarma_classic_x3_1", "all_x2_1" },
            };

            SaveData copy = _serializer.Deserialize(_serializer.Serialize(data));

            Assert.AreEqual(data.Version, copy.Version);
            Assert.AreEqual(data.Coins, copy.Coins);
            Assert.AreEqual(data.SavedAtUtcTicks, copy.SavedAtUtcTicks);
            Assert.AreEqual(31, copy.Stations[0].Level);
            Assert.IsTrue(copy.Stations[0].Automated);
            Assert.AreEqual(0.4, copy.Stations[0].Progress, 1e-12);
            Assert.AreEqual(data.PurchasedUpgrades, copy.PurchasedUpgrades);
        }

        [Test]
        public void LegacyJson_WithoutNewFields_LoadsWithDefaults()
        {
            SaveData data = SaveMigrator.Migrate(_serializer.Deserialize("{\"Coins\":500}"));

            Assert.AreEqual(500, data.Coins);
            Assert.AreEqual(1, data.Version);
            Assert.IsNotNull(data.Stations);
            Assert.IsNotNull(data.PurchasedUpgrades);
        }

        // JsonUtility пишет double примерно с 15 значащими цифрами (не 17), поэтому последние знаки
        // огромных чисел могут слегка измениться. Для монет это незаметно (на экране 3 значащие цифры).
        [Test]
        public void HugeCoins_SurviveRoundTrip_WithinRelativePrecision()
        {
            var data = new SaveData { Coins = 1.2345678901234567e250 };

            double restored = _serializer.Deserialize(_serializer.Serialize(data)).Coins;

            Assert.AreEqual(data.Coins, restored, data.Coins * 1e-14);
        }

        [Test]
        public void Garbage_Throws()
        {
            Assert.Catch<Exception>(() => _serializer.Deserialize("{\"Coins\":500, this is not json"));
        }
    }
}
