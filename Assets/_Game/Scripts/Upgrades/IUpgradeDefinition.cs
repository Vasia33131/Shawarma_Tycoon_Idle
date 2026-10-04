using ShaurmaTycoon.Stations;

namespace ShaurmaTycoon.Upgrades
{
    /// <summary>
    /// Разовое улучшение (GDD §5): умножает доход одной станции или всех станций города.
    /// </summary>
    public interface IUpgradeDefinition
    {
        string Id { get; }
        double Cost { get; }

        /// <summary>Станция, к которой применяется; null — все станции города.</summary>
        IStationDefinition TargetStation { get; }

        double IncomeMultiplier { get; }
    }
}
