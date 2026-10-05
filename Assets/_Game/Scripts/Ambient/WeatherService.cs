using System;
using ShaurmaTycoon.Core;

namespace ShaurmaTycoon.Ambient
{
    /// <summary>
    /// Текущая погода города: раз в checkInterval спрашивает расписание по серверному времени и сообщает о смене.
    /// Отладочное окно может включить погоду принудительно (до перезагрузки сцены). Живёт в сцене Game.
    /// </summary>
    public sealed class WeatherService : ITickable
    {
        private readonly WeatherSchedule _schedule;
        private readonly Func<DateTime> _utcNow;
        private readonly string _cityId;
        private readonly float _rainChance;
        private readonly bool _snowAllowed;
        private readonly float _checkInterval;
        private float _timer;
        private bool _forced;
        private WeatherKind _forcedKind;

        public WeatherService(WeatherSchedule schedule, Func<DateTime> utcNow, string cityId, float rainChance, bool snowAllowed,
            float checkInterval)
        {
            _schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
            _utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
            _cityId = cityId;
            _rainChance = rainChance;
            _snowAllowed = snowAllowed;
            _checkInterval = checkInterval > 0f ? checkInterval : 1f;
            Current = Evaluate();
        }

        public event Action<WeatherKind> WeatherChanged;

        public WeatherKind Current { get; private set; }

        public bool IsForced => _forced;

        /// <summary>Погода из отладочного окна: держится, пока сцена не перезагрузится.</summary>
        public void Force(WeatherKind kind)
        {
            _forced = true;
            _forcedKind = kind;
            Refresh();
        }

        public void Tick(float deltaTime)
        {
            _timer -= deltaTime;
            if (_timer > 0f)
                return;
            _timer = _checkInterval;
            Refresh();
        }

        private void Refresh()
        {
            WeatherKind kind = Evaluate();
            if (kind == Current)
                return;
            Current = kind;
            WeatherChanged?.Invoke(kind);
        }

        private WeatherKind Evaluate()
        {
            return _forced ? _forcedKind : _schedule.Get(_utcNow(), _cityId, _rainChance, _snowAllowed);
        }
    }
}
