using System;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Stations;

namespace ShaurmaTycoon.Managers
{
    /// <summary>
    /// Найм менеджеров (GDD §5): цена = baseCost × множитель, менеджер включает автоцикл станции.
    /// «Нанят» хранится в самой станции — StationModel.IsAutomated.
    /// </summary>
    public sealed class ManagerController
    {
        private readonly StationController _stations;
        private readonly Wallet _wallet;
        private readonly double _costMultiplier;

        public ManagerController(StationController stations, Wallet wallet, double costMultiplier)
        {
            _stations = stations ?? throw new ArgumentNullException(nameof(stations));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            if (!(costMultiplier >= 0) || double.IsInfinity(costMultiplier))
                throw new ArgumentOutOfRangeException(nameof(costMultiplier), costMultiplier, "Must be >= 0 and finite.");
            _costMultiplier = costMultiplier;
        }

        /// <summary>Аргумент — индекс станции.</summary>
        public event Action<int> ManagerHired;

        public int Count => _stations.Stations.Count;

        public double GetCost(int index)
        {
            return _stations.Stations[index].Definition.BaseCost * _costMultiplier;
        }

        public bool IsHired(int index)
        {
            return IsValidIndex(index) && _stations.Stations[index].IsAutomated;
        }

        /// <summary>Станция открыта и менеджер ещё не нанят (деньги не учитываются).</summary>
        public bool CanHire(int index)
        {
            return IsValidIndex(index) && _stations.Stations[index].IsUnlocked && !_stations.Stations[index].IsAutomated;
        }

        public bool TryHire(int index)
        {
            if (!CanHire(index) || !_wallet.TrySpend(GetCost(index)))
                return false;

            _stations.Stations[index].IsAutomated = true;
            ManagerHired?.Invoke(index);
            return true;
        }

        /// <summary>
        /// Бесплатные менеджеры (магазин репутации): первые count станций получают менеджера,
        /// как только открыты — сразу или позже, при покупке первого уровня.
        /// </summary>
        public void SetFreeManagers(int count)
        {
            FreeManagerCount = Math.Max(0, count);
            for (int i = 0; i < _stations.Stations.Count; i++)
            {
                _stations.Stations[i].Changed -= OnStationChanged;
                if (i < FreeManagerCount)
                {
                    _stations.Stations[i].Changed += OnStationChanged;
                    GrantFree(i);
                }
            }
        }

        public int FreeManagerCount { get; private set; }

        private void OnStationChanged(StationModel station)
        {
            for (int i = 0; i < FreeManagerCount && i < _stations.Stations.Count; i++)
            {
                if (ReferenceEquals(_stations.Stations[i], station))
                    GrantFree(i);
            }
        }

        private void GrantFree(int index)
        {
            if (!CanHire(index))
                return;

            _stations.Stations[index].IsAutomated = true;
            ManagerHired?.Invoke(index);
        }

        private bool IsValidIndex(int index)
        {
            return index >= 0 && index < _stations.Stations.Count;
        }
    }
}
