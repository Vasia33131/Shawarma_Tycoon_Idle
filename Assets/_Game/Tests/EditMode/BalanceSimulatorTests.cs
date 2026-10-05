using NUnit.Framework;
using ShaurmaTycoon.Balance;
using ShaurmaTycoon.Core;
using ShaurmaTycoon.Prestige;
using ShaurmaTycoon.Stations;
using ShaurmaTycoon.Upgrades;
using UnityEditor;

namespace ShaurmaTycoon.Tests
{
    public class BalanceSimulatorTests
    {
        private static IStationDefinition[] GddStations()
        {
            return new IStationDefinition[]
            {
                new TestStationDefinition { Id = "s1", BaseCost = 4, BaseIncome = 1, CycleTime = 0.6, StartLevel = 1 },
                new TestStationDefinition { Id = "s2", BaseCost = 60, BaseIncome = 60, CycleTime = 3, StartLevel = 0 },
                new TestStationDefinition { Id = "s3", BaseCost = 720, BaseIncome = 540, CycleTime = 6, StartLevel = 0 },
            };
        }

        private static BalanceSimulator Simulator(IStationDefinition[] stations, IUpgradeDefinition[] upgrades)
        {
            return new BalanceSimulator(stations, upgrades, new MilestoneTable(new[] { 25, 50, 100 }, 2), 1000);
        }

        [Test]
        public void Run_OpensStationsInOrderAndReachesThreshold()
        {
            BalanceReport report = Simulator(GddStations(), new IUpgradeDefinition[0])
                .Run(new BalanceSimulationSettings { PrestigeThreshold = 1e6 });

            Assert.AreEqual(0, report.StationUnlockedAt[0]);
            Assert.Greater(report.StationUnlockedAt[1], 0);
            Assert.Greater(report.StationUnlockedAt[2], report.StationUnlockedAt[1]);
            Assert.Greater(report.ThresholdReachedAt, 0);
            Assert.GreaterOrEqual(report.TotalEarned, 1e6);
            Assert.Greater(report.LevelsBought, 0);
        }

        [Test]
        public void Run_IsDeterministic()
        {
            var settings = new BalanceSimulationSettings { PrestigeThreshold = 1e5 };
            BalanceReport a = Simulator(GddStations(), new IUpgradeDefinition[0]).Run(settings);
            BalanceReport b = Simulator(GddStations(), new IUpgradeDefinition[0]).Run(settings);

            Assert.AreEqual(a.ThresholdReachedAt, b.ThresholdReachedAt);
            Assert.AreEqual(a.Purchases.Count, b.Purchases.Count);
        }

        [Test]
        public void Run_BuysCheapUpgradeAndManager()
        {
            IStationDefinition[] stations = GddStations();
            var upgrades = new IUpgradeDefinition[]
            {
                new TestUpgradeDefinition { Id = "u", Cost = 500, TargetStation = stations[0], IncomeMultiplier = 3 },
            };

            BalanceReport report = Simulator(stations, upgrades).Run(new BalanceSimulationSettings { PrestigeThreshold = 1e6 });

            Assert.GreaterOrEqual(report.UpgradeBoughtAt[0], 0);
            Assert.GreaterOrEqual(report.ManagerHiredAt[0], 0);
        }

        [Test]
        public void SlowerPlayer_TakesLonger()
        {
            BalanceReport active = Simulator(GddStations(), new IUpgradeDefinition[0]).Run(BalanceSimulationSettings.Active(1e6));
            BalanceReport typical = Simulator(GddStations(), new IUpgradeDefinition[0]).Run(BalanceSimulationSettings.Typical(1e6));

            Assert.Greater(typical.ThresholdReachedAt, active.ThresholdReachedAt);
        }

        [TestCase(59, "0:59")]
        [TestCase(605, "10:05")]
        [TestCase(3725, "1:02:05")]
        [TestCase(-1, "—")]
        public void FormatTime(double seconds, string expected)
        {
            Assert.AreEqual(expected, BalanceReportFormatter.FormatTime(seconds));
        }

        /// <summary>Цели этапа 11 на настоящих ассетах: вторая станция в первую минуту, город 1 — за 2–3 ч «обычного» игрока.</summary>
        [Test]
        public void City1Assets_MeetStage11Targets()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CityCatalog>("Assets/_Game/Configs/Cities/CityCatalog.asset");
            var balance = AssetDatabase.LoadAssetAtPath<BalanceConfig>("Assets/_Game/Configs/Balance.asset");
            Assert.IsNotNull(catalog);
            Assert.IsNotNull(balance);

            CityConfig city = catalog.Get(0);
            BalanceReport report = new BalanceSimulator(city.Stations, city.Upgrades, balance.CreateMilestoneTable(), balance.ManagerCostMultiplier)
                .Run(BalanceSimulationSettings.Typical(city.PrestigeThreshold));

            Assert.GreaterOrEqual(report.StationUnlockedAt[1], 0, "station 2 opened");
            Assert.LessOrEqual(report.StationUnlockedAt[1], 60, "station 2 within the first minute");
            Assert.GreaterOrEqual(report.PurchasesBefore(60), 5, "purchases in the first minute");
            Assert.GreaterOrEqual(report.ThresholdReachedAt, 1.75 * 3600, "city 1 not faster than ~2 h");
            Assert.LessOrEqual(report.ThresholdReachedAt, 3.25 * 3600, "city 1 not slower than ~3 h");
        }
    }
}
