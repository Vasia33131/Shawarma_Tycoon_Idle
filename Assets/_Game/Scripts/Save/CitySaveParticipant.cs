using System;
using System.Collections.Generic;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Stations;
using ShaurmaTycoon.Upgrades;

namespace ShaurmaTycoon.Save
{
    /// <summary>
    /// Сохраняет и восстанавливает монеты, станции (уровень, менеджер, цикл) и купленные улучшения города.
    /// Станции сопоставляются по Id; станции без записи в сейве остаются как в конфиге.
    /// </summary>
    public sealed class CitySaveParticipant : ISaveParticipant
    {
        private readonly Wallet _wallet;
        private readonly StationController _stations;
        private readonly UpgradeService _upgrades;

        public CitySaveParticipant(Wallet wallet, StationController stations, UpgradeService upgrades)
        {
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _stations = stations ?? throw new ArgumentNullException(nameof(stations));
            _upgrades = upgrades ?? throw new ArgumentNullException(nameof(upgrades));
        }

        public void Capture(SaveData data)
        {
            data.Coins = _wallet.Coins;

            data.Stations.Clear();
            IReadOnlyList<StationModel> stations = _stations.Stations;
            for (int i = 0; i < stations.Count; i++)
            {
                StationModel station = stations[i];
                data.Stations.Add(new StationSave
                {
                    Id = station.Definition.Id,
                    Level = station.Level,
                    Automated = station.IsAutomated,
                    Progress = station.Progress,
                    Running = station.IsRunning,
                });
            }

            data.PurchasedUpgrades.Clear();
            _upgrades.CollectPurchasedIds(data.PurchasedUpgrades);
        }

        public void Apply(SaveData data)
        {
            _wallet.SetCoins(data.Coins);

            IReadOnlyList<StationModel> stations = _stations.Stations;
            for (int i = 0; i < stations.Count; i++)
            {
                StationSave saved = Find(data.Stations, stations[i].Definition.Id);
                if (saved != null)
                    stations[i].Restore(saved.Level, saved.Automated, saved.Progress, saved.Running);
            }

            _upgrades.Restore(data.PurchasedUpgrades);
        }

        private static StationSave Find(List<StationSave> stations, string id)
        {
            for (int i = 0; i < stations.Count; i++)
            {
                if (string.Equals(stations[i].Id, id, StringComparison.Ordinal))
                    return stations[i];
            }
            return null;
        }
    }
}
