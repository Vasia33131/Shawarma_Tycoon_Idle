using ShaurmaTycoon.Stations;
using ShaurmaTycoon.Upgrades;

namespace ShaurmaTycoon.Tests
{
    /// <summary>Определение улучшения для тестов.</summary>
    public sealed class TestUpgradeDefinition : IUpgradeDefinition
    {
        public string Id { get; set; } = "test_upgrade";
        public double Cost { get; set; } = 100;
        public IStationDefinition TargetStation { get; set; }
        public double IncomeMultiplier { get; set; } = 3;
    }
}
