using System.Collections.Generic;
using NUnit.Framework;
using ShaurmaTycoon.Save;

namespace ShaurmaTycoon.Tests
{
    public class SaveMigratorTests
    {
        [Test]
        public void Null_BecomesNewSave()
        {
            SaveData data = SaveMigrator.Migrate(null);

            Assert.AreEqual(SaveData.CurrentVersion, data.Version);
            Assert.AreEqual(0, data.Stations.Count);
        }

        [Test]
        public void MissingLists_AreCreated_VersionZeroBecomesOne()
        {
            var data = new SaveData { Version = 0, Coins = 500, Stations = null, PurchasedUpgrades = null };

            SaveMigrator.Migrate(data);

            Assert.AreEqual(1, data.Version);
            Assert.AreEqual(500, data.Coins);
            Assert.IsNotNull(data.Stations);
            Assert.IsNotNull(data.PurchasedUpgrades);
        }

        [TestCase(double.NaN)]
        [TestCase(-10)]
        [TestCase(double.PositiveInfinity)]
        public void InvalidCoins_BecomeZero(double coins)
        {
            Assert.AreEqual(0, SaveMigrator.Migrate(new SaveData { Coins = coins }).Coins);
        }

        [Test]
        public void InvalidStations_AreFixedOrDropped()
        {
            var data = new SaveData
            {
                Stations = new List<StationSave>
                {
                    null,
                    new StationSave { Id = "" },
                    new StationSave { Id = "a", Level = -3, Progress = double.NaN },
                    new StationSave { Id = "a", Level = 99 },
                    new StationSave { Id = "b", Level = 5, Progress = 1.5 },
                },
            };

            SaveMigrator.Migrate(data);

            Assert.AreEqual(2, data.Stations.Count);
            Assert.AreEqual("a", data.Stations[0].Id);
            Assert.AreEqual(0, data.Stations[0].Level);
            Assert.AreEqual(0, data.Stations[0].Progress);
            Assert.AreEqual(0, data.Stations[1].Progress);
        }

        [Test]
        public void UpgradeIds_AreDeduplicated()
        {
            var data = new SaveData { PurchasedUpgrades = new List<string> { "x", null, "", "x", "y" } };

            SaveMigrator.Migrate(data);

            Assert.AreEqual(2, data.PurchasedUpgrades.Count);
            Assert.AreEqual("y", data.PurchasedUpgrades[1]);
        }
    }
}
