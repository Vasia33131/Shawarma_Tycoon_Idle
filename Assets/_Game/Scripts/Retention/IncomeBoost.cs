using System;

namespace ShaurmaTycoon.Retention
{
    /// <summary>
    /// Буст «×2 доход» (GDD §8): каждая реклама добавляет secondsPerAd, всего не больше maxSeconds.
    /// Время идёт только во время игры (Tick): на паузе и офлайн не тратится.
    /// </summary>
    public sealed class IncomeBoost
    {
        // Рекламу предлагаем, только если в запас влезает хотя бы минута — иначе она почти ничего не даст.
        private const double MinUsefulSeconds = 60;

        private readonly double _multiplier;

        public IncomeBoost(double multiplier, double secondsPerAd, double maxSeconds)
        {
            _multiplier = Math.Max(1, multiplier);
            SecondsPerAd = Math.Max(1, secondsPerAd);
            MaxSeconds = Math.Max(SecondsPerAd, maxSeconds);
        }

        public double SecondsPerAd { get; }
        public double MaxSeconds { get; }
        public double RemainingSeconds { get; private set; }
        public bool IsActive => RemainingSeconds > 0;

        /// <summary>Текущий множитель дохода: ×2 во время буста, иначе 1.</summary>
        public double Multiplier => IsActive ? _multiplier : 1;

        /// <summary>Множитель буста (для текста кнопки), независимо от того, идёт ли он.</summary>
        public double BoostMultiplier => _multiplier;

        public bool CanAdd => MaxSeconds - RemainingSeconds >= Math.Min(MinUsefulSeconds, SecondsPerAd);

        /// <returns>false — запас полон.</returns>
        public bool Add()
        {
            if (!CanAdd)
                return false;

            RemainingSeconds = Math.Min(MaxSeconds, RemainingSeconds + SecondsPerAd);
            return true;
        }

        /// <returns>true — буст закончился на этом тике.</returns>
        public bool Tick(double deltaTime)
        {
            if (!IsActive || !(deltaTime > 0))
                return false;

            RemainingSeconds = Math.Max(0, RemainingSeconds - deltaTime);
            return !IsActive;
        }

        public void Load(double remainingSeconds)
        {
            RemainingSeconds = remainingSeconds > 0 && !double.IsInfinity(remainingSeconds) ? Math.Min(MaxSeconds, remainingSeconds) : 0;
        }
    }
}
