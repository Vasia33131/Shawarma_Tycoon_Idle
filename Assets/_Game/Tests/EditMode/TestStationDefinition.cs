using ShaurmaTycoon.Stations;

namespace ShaurmaTycoon.Tests
{
    /// <summary>Определение станции для тестов, по умолчанию — станция 1 из GDD.</summary>
    public sealed class TestStationDefinition : IStationDefinition
    {
        public string Id { get; set; } = "test";
        public string NameKey { get; set; } = "station.test";
        public double BaseCost { get; set; } = 4;
        public double CostGrowth { get; set; } = 1.07;
        public double BaseIncome { get; set; } = 1;
        public double CycleTime { get; set; } = 0.6;
        public int StartLevel { get; set; } = 1;
    }
}
