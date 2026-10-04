using System;

namespace ShaurmaTycoon.Offline
{
    /// <summary>Параметры офлайн-дохода (GDD §6). Создаются из BalanceConfig.</summary>
    public readonly struct OfflineSettings
    {
        public OfflineSettings(double incomeRate, double limitSeconds, double minAwaySeconds)
        {
            IncomeRate = Math.Max(0, incomeRate);
            LimitSeconds = Math.Max(0, limitSeconds);
            MinAwaySeconds = Math.Max(0, minAwaySeconds);
        }

        /// <summary>Доля дохода в секунду (0.5 = половина).</summary>
        public double IncomeRate { get; }

        /// <summary>Максимум засчитываемого времени.</summary>
        public double LimitSeconds { get; }

        /// <summary>Меньшее отсутствие не считается (окно не показывается).</summary>
        public double MinAwaySeconds { get; }
    }
}
