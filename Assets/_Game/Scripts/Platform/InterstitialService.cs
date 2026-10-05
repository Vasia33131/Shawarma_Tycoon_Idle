using System;

namespace ShaurmaTycoon.Platform
{
    /// <summary>
    /// Межстраничная реклама по правилам GDD §8: не чаще раза в 90 сек, не в первые 3 минуты сессии, не при купленном «Без рекламы».
    /// Показывается только в паузе игрового процесса (переход в новый город); после закрытия вызывает продолжение.
    /// </summary>
    public sealed class InterstitialService
    {
        public const double DefaultMinIntervalSeconds = 90;
        public const double DefaultFirstDelaySeconds = 180;

        private readonly IPlatformService _platform;
        private readonly Func<double> _sessionSeconds;
        private readonly double _minInterval;
        private readonly double _firstDelay;
        private double _lastShownAt = double.NegativeInfinity;
        private bool _isShowing;

        /// <param name="sessionSeconds">Секунды с запуска игры (Time.realtimeSinceStartup).</param>
        public InterstitialService(IPlatformService platform, Func<double> sessionSeconds,
            double minIntervalSeconds = DefaultMinIntervalSeconds, double firstDelaySeconds = DefaultFirstDelaySeconds)
        {
            _platform = platform ?? throw new ArgumentNullException(nameof(platform));
            _sessionSeconds = sessionSeconds ?? throw new ArgumentNullException(nameof(sessionSeconds));
            _minInterval = minIntervalSeconds;
            _firstDelay = firstDelaySeconds;
        }

        /// <summary>Реклама отключена покупкой «Без рекламы».</summary>
        public bool AdsDisabled { get; set; }

        public bool CanShow
        {
            get
            {
                if (AdsDisabled || _isShowing)
                    return false;

                double now = _sessionSeconds();
                return now >= _firstDelay && now - _lastShownAt >= _minInterval;
            }
        }

        /// <summary>
        /// Показывает рекламу, если правила позволяют. onContinue вызывается ровно один раз:
        /// сразу (если показывать нельзя) или после закрытия рекламы.
        /// </summary>
        /// <returns>true — реклама запрошена.</returns>
        public bool TryShow(Action onContinue)
        {
            if (!CanShow)
            {
                onContinue?.Invoke();
                return false;
            }

            _isShowing = true;
            _lastShownAt = _sessionSeconds();
            _platform.ShowInterstitial(() =>
            {
                _isShowing = false;
                _lastShownAt = _sessionSeconds();
                onContinue?.Invoke();
            });
            return true;
        }
    }
}
