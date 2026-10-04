namespace ShaurmaTycoon.Stations
{
    /// <summary>
    /// Баланс одной станции (GDD §4). Реализуется StationConfig; в тестах — простым классом.
    /// </summary>
    public interface IStationDefinition
    {
        string Id { get; }

        /// <summary>Цена первого уровня; для закрытой станции это цена открытия.</summary>
        double BaseCost { get; }

        /// <summary>Рост цены за уровень: cost(n) = BaseCost × CostGrowth^n.</summary>
        double CostGrowth { get; }

        /// <summary>Доход за цикл на один уровень.</summary>
        double BaseIncome { get; }

        /// <summary>Время цикла в секундах до ускорения вехами.</summary>
        double CycleTime { get; }

        /// <summary>Уровень на старте города: 0 — станция закрыта.</summary>
        int StartLevel { get; }
    }
}
