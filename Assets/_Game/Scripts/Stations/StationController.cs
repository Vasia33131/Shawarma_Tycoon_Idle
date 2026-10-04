using System;
using System.Collections.Generic;
using ShaurmaTycoon.Core;
using ShaurmaTycoon.Economy;

namespace ShaurmaTycoon.Stations
{
    /// <summary>
    /// Станции текущего города: продвигает их циклы и зачисляет доход в кошелёк одним событием за тик.
    /// </summary>
    public sealed class StationController : ITickable
    {
        private readonly List<StationModel> _stations;
        private readonly Wallet _wallet;

        public StationController(IReadOnlyList<IStationDefinition> definitions, MilestoneTable milestones, Wallet wallet)
        {
            if (definitions == null)
                throw new ArgumentNullException(nameof(definitions));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));

            _stations = new List<StationModel>(definitions.Count);
            for (int i = 0; i < definitions.Count; i++)
                _stations.Add(new StationModel(definitions[i], milestones));
        }

        public IReadOnlyList<StationModel> Stations => _stations;

        public void Tick(float deltaTime)
        {
            double earned = 0;
            for (int i = 0; i < _stations.Count; i++)
                earned += _stations[i].Advance(deltaTime);

            if (earned > 0)
                _wallet.Add(earned);
        }

        public bool Tap(int index)
        {
            return IsValidIndex(index) && _stations[index].TryStartCycle();
        }

        public bool TryBuy(int index, int count)
        {
            return IsValidIndex(index) && _stations[index].TryBuy(_wallet, count);
        }

        private bool IsValidIndex(int index)
        {
            return index >= 0 && index < _stations.Count;
        }
    }
}
