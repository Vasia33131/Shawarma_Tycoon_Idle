namespace ShaurmaTycoon.Prestige
{
    /// <summary>Что делает пункт магазина репутации. Значение уровня зависит от эффекта.</summary>
    public enum ShopEffect
    {
        /// <summary>+часы к лимиту офлайна.</summary>
        OfflineLimitHours,

        /// <summary>Монеты на старте города (умножаются на масштаб города).</summary>
        StartCoins,

        /// <summary>Сколько первых станций получают бесплатного менеджера.</summary>
        FreeManagers,

        /// <summary>Множитель времени цикла всех станций (0.9 = на 10% быстрее).</summary>
        CycleTimeFactor,
    }
}
