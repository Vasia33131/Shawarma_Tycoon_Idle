using System;
using ShaurmaTycoon.Save;

namespace ShaurmaTycoon.Prestige
{
    /// <summary>
    /// Переход в новый город (GDD §7). Доступен, когда в текущем городе заработан порог и есть что получить.
    /// Переход: начислить репутацию → сбросить данные города в сейве (монеты = стартовый капитал) → сохранить
    /// → перезагрузить сцену, где системы города создадутся заново. В последнем городе — перезапуск того же.
    /// </summary>
    public sealed class PrestigeController
    {
        private readonly PrestigeState _state;
        private readonly PrestigeRules _rules;
        private readonly int _cityIndex;
        private readonly int _cityCount;
        private readonly SaveService _save;
        private readonly ISaveParticipant _cityParticipant;
        private readonly Func<int, double> _startCoinsForCity;
        private readonly Action _reloadScene;

        /// <param name="startCoinsForCity">Стартовый капитал для города с данным индексом.</param>
        /// <param name="reloadScene">Перезагрузка сцены Game после перехода.</param>
        public PrestigeController(PrestigeState state, PrestigeRules rules, double cityThreshold, int cityIndex, int cityCount,
            SaveService save, ISaveParticipant cityParticipant, Func<int, double> startCoinsForCity, Action reloadScene)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _rules = rules;
            if (!(cityThreshold > 0))
                throw new ArgumentOutOfRangeException(nameof(cityThreshold), cityThreshold, "Must be positive.");
            if (cityCount < 1)
                throw new ArgumentOutOfRangeException(nameof(cityCount));

            Threshold = cityThreshold;
            _cityIndex = Math.Min(Math.Max(0, cityIndex), cityCount - 1);
            _cityCount = cityCount;
            _save = save;
            _cityParticipant = cityParticipant;
            _startCoinsForCity = startCoinsForCity ?? (_ => 0);
            _reloadScene = reloadScene ?? throw new ArgumentNullException(nameof(reloadScene));
        }

        public double Threshold { get; }
        public bool IsLastCity => _cityIndex >= _cityCount - 1;
        public int NextCityIndex => IsLastCity ? _cityIndex : _cityIndex + 1;

        /// <summary>Доля пути к порогу, 0..1.</summary>
        public double Progress => Math.Min(1, _state.CityEarned / Threshold);

        public bool IsThresholdReached => _state.CityEarned >= Threshold;
        public long ForecastGain => _rules.ReputationGain(_state.LifetimeEarned, _state.ReputationEarned);
        public bool CanPrestige => IsThresholdReached && ForecastGain >= 1;

        public double CurrentIncomeMultiplier => _rules.IncomeMultiplier(_state.ReputationEarned);
        public double IncomeMultiplierAfterPrestige => _rules.IncomeMultiplier(_state.ReputationEarned + ForecastGain);

        public bool TryPrestige()
        {
            if (!CanPrestige)
                return false;

            long gain = ForecastGain;
            int nextCity = NextCityIndex;

            // Сначала отключаем город от сейва: иначе он записал бы своё состояние поверх сброса.
            if (_save != null && _cityParticipant != null)
                _save.Unregister(_cityParticipant);

            _state.CompletePrestige(gain, nextCity);

            if (_save != null)
            {
                SaveData data = _save.Data;
                data.Stations.Clear();
                data.PurchasedUpgrades.Clear();
                data.Coins = Math.Max(0, _startCoinsForCity(nextCity));
                _save.SaveNow();
            }

            _reloadScene();
            return true;
        }
    }
}
