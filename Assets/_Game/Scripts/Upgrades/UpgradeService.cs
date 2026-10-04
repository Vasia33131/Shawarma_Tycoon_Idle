using System;
using System.Collections.Generic;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Stations;

namespace ShaurmaTycoon.Upgrades
{
    /// <summary>
    /// Покупка улучшений города и пересчёт множителей дохода станций (произведение купленных улучшений).
    /// </summary>
    public sealed class UpgradeService
    {
        /// <summary>Индекс цели «все станции».</summary>
        public const int AllStations = -1;

        private readonly IReadOnlyList<IUpgradeDefinition> _upgrades;
        private readonly StationController _stations;
        private readonly Wallet _wallet;
        private readonly bool[] _purchased;
        private readonly int[] _targetIndices;

        public UpgradeService(IReadOnlyList<IUpgradeDefinition> upgrades, StationController stations, Wallet wallet)
        {
            _upgrades = upgrades ?? throw new ArgumentNullException(nameof(upgrades));
            _stations = stations ?? throw new ArgumentNullException(nameof(stations));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));

            _purchased = new bool[upgrades.Count];
            _targetIndices = new int[upgrades.Count];
            for (int i = 0; i < upgrades.Count; i++)
                _targetIndices[i] = FindTargetIndex(upgrades[i]);
        }

        /// <summary>Аргумент — индекс улучшения.</summary>
        public event Action<int> UpgradePurchased;

        public int Count => _upgrades.Count;

        public IUpgradeDefinition GetDefinition(int index)
        {
            return _upgrades[index];
        }

        /// <summary>Индекс целевой станции или AllStations.</summary>
        public int GetTargetStationIndex(int index)
        {
            return _targetIndices[index];
        }

        public bool IsPurchased(int index)
        {
            return _purchased[index];
        }

        public bool TryBuy(int index)
        {
            if (index < 0 || index >= _upgrades.Count || _purchased[index])
                return false;
            if (!_wallet.TrySpend(_upgrades[index].Cost))
                return false;

            _purchased[index] = true;
            ApplyMultipliers();
            UpgradePurchased?.Invoke(index);
            return true;
        }

        private void ApplyMultipliers()
        {
            IReadOnlyList<StationModel> stations = _stations.Stations;
            for (int s = 0; s < stations.Count; s++)
            {
                double multiplier = 1;
                for (int u = 0; u < _upgrades.Count; u++)
                {
                    if (_purchased[u] && (_targetIndices[u] == AllStations || _targetIndices[u] == s))
                        multiplier *= _upgrades[u].IncomeMultiplier;
                }
                stations[s].IncomeMultiplier = multiplier;
            }
        }

        private int FindTargetIndex(IUpgradeDefinition upgrade)
        {
            if (upgrade == null)
                throw new ArgumentException("Upgrade list contains null.");
            if (!(upgrade.IncomeMultiplier >= 1) || double.IsInfinity(upgrade.IncomeMultiplier))
                throw new ArgumentException($"Upgrade '{upgrade.Id}' has invalid multiplier {upgrade.IncomeMultiplier}.");
            if (upgrade.TargetStation == null)
                return AllStations;

            IReadOnlyList<StationModel> stations = _stations.Stations;
            for (int i = 0; i < stations.Count; i++)
            {
                if (ReferenceEquals(stations[i].Definition, upgrade.TargetStation))
                    return i;
            }

            throw new ArgumentException($"Upgrade '{upgrade.Id}' targets a station that is not in this city.");
        }
    }
}
