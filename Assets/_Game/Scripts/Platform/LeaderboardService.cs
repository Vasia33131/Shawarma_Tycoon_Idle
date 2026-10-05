using System;
using ShaurmaTycoon.Core;
using ShaurmaTycoon.Prestige;

namespace ShaurmaTycoon.Platform
{
    /// <summary>
    /// Лидерборд «Самая богатая сеть»: суммарный заработок за всё время (PrestigeState.LifetimeEarned).
    /// Отправка не чаще раза в минуту и только когда результат вырос (платформа: не чаще 1 запроса в секунду).
    /// </summary>
    public sealed class LeaderboardService : ITickable
    {
        public const string RichestNetworkId = "richest";
        public const float SendInterval = 60f;

        /// <summary>Безопасный максимум целого числа в JavaScript — выше число теряет точность.</summary>
        public const double MaxScore = 9007199254740991d;

        private readonly IPlatformService _platform;
        private readonly PrestigeState _state;
        private readonly string _leaderboardId;
        private double _lastSent = -1;
        private float _timer;

        public LeaderboardService(IPlatformService platform, PrestigeState state, string leaderboardId = RichestNetworkId)
        {
            _platform = platform ?? throw new ArgumentNullException(nameof(platform));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _leaderboardId = leaderboardId;
        }

        /// <summary>Целый неотрицательный результат; значения выше MaxScore обрезаются.</summary>
        public static double ToScore(double lifetimeEarned)
        {
            if (!(lifetimeEarned >= 1))
                return 0;

            return Math.Min(Math.Floor(lifetimeEarned), MaxScore);
        }

        public void Tick(float deltaTime)
        {
            _timer += deltaTime;
            if (_timer >= SendInterval)
                SendIfGrown();
        }

        /// <summary>Отправить результат сразу (после перехода в новый город), если он вырос.</summary>
        public void SendIfGrown()
        {
            _timer = 0f;
            double score = ToScore(_state.LifetimeEarned);
            if (score <= 0 || score <= _lastSent)
                return;

            _lastSent = score;
            _platform.SetLeaderboardScore(_leaderboardId, score);
        }
    }
}
