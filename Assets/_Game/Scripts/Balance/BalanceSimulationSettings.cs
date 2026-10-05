namespace ShaurmaTycoon.Balance
{
    /// <summary>
    /// Параметры виртуального игрока прогона баланса. Значения по умолчанию — активный игрок без рекламы:
    /// тапает по клиенту раз в 0.5 с, решает о покупках раз в секунду.
    /// </summary>
    public sealed class BalanceSimulationSettings
    {
        /// <summary>«Обычный» игрок: смотрит на кнопки раз в 10 с, тапает раз в 1.5 с (по нему — цель «город 1 за 2–3 ч»).</summary>
        public static BalanceSimulationSettings Typical(double prestigeThreshold)
        {
            return new BalanceSimulationSettings { DecisionInterval = 10, TapInterval = 1.5, PrestigeThreshold = prestigeThreshold };
        }

        /// <summary>Внимательный игрок: решает каждую секунду, тапает раз в 0.5 с — нижняя граница времени прохождения.</summary>
        public static BalanceSimulationSettings Active(double prestigeThreshold)
        {
            return new BalanceSimulationSettings { PrestigeThreshold = prestigeThreshold };
        }

        /// <summary>Шаг симуляции, с.</summary>
        public double TimeStep { get; set; } = 0.05;

        /// <summary>Как часто игрок тапает по клиенту (запускает простаивающие станции без менеджера), с. 0 — не тапает.</summary>
        public double TapInterval { get; set; } = 0.5;

        /// <summary>Как часто игрок смотрит на кнопки покупок, с.</summary>
        public double DecisionInterval { get; set; } = 1;

        /// <summary>Вес менеджера в оценке окупаемости: 1 — менеджер стоит ровно доход станции.</summary>
        public double ManagerValueFactor { get; set; } = 1;

        /// <summary>Порог переезда (заработано в городе), GDD §7.</summary>
        public double PrestigeThreshold { get; set; } = 1e9;

        /// <summary>Остановить прогон, когда порог достигнут.</summary>
        public bool StopAtThreshold { get; set; } = true;

        /// <summary>Предел прогона, с.</summary>
        public double MaxSeconds { get; set; } = 8 * 3600;

        /// <summary>Как часто записывать снимок дохода, с.</summary>
        public double SampleInterval { get; set; } = 60;
    }
}
