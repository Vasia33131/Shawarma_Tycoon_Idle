using System;
using ShaurmaTycoon.Managers;
using ShaurmaTycoon.Stations;
using ShaurmaTycoon.Upgrades;

namespace ShaurmaTycoon.Retention
{
    /// <summary>
    /// Засчитывает действия игрока в задания дня: купленные уровни станций (за монеты и купюры), найм менеджеров,
    /// улучшения, просмотры буста и открытые подарки. Уровни считаются по разнице с последним известным.
    /// </summary>
    public sealed class QuestTracker : IDisposable
    {
        private readonly DailyQuestService _quests;
        private readonly StationController _stations;
        private readonly ManagerController _managers;
        private readonly UpgradeService _upgrades;
        private readonly BoostService _boost;
        private readonly GiftBoxService _gift;
        private readonly int[] _levels;

        public QuestTracker(DailyQuestService quests, StationController stations, ManagerController managers, UpgradeService upgrades,
            BoostService boost, GiftBoxService gift)
        {
            _quests = quests ?? throw new ArgumentNullException(nameof(quests));
            _stations = stations ?? throw new ArgumentNullException(nameof(stations));
            _managers = managers;
            _upgrades = upgrades;
            _boost = boost;
            _gift = gift;

            _levels = new int[_stations.Stations.Count];
            for (int i = 0; i < _levels.Length; i++)
            {
                _levels[i] = _stations.Stations[i].Level;
                _stations.Stations[i].LevelChanged += OnLevelChanged;
            }

            if (_managers != null)
                _managers.ManagerHired += OnManagerHired;
            if (_upgrades != null)
                _upgrades.UpgradePurchased += OnUpgradePurchased;
            if (_boost != null)
                _boost.Watched += OnBoostWatched;
            if (_gift != null)
                _gift.Opened += OnGiftOpened;
        }

        /// <summary>Можно ли сейчас выполнить задание: менеджеров и улучшений может уже не остаться.</summary>
        public static bool IsAvailable(QuestType type, int target, ManagerController managers, UpgradeService upgrades)
        {
            switch (type)
            {
                case QuestType.HireManager:
                    return managers != null && CountNotHired(managers) >= target;
                case QuestType.BuyUpgrade:
                    return upgrades != null && CountNotPurchased(upgrades) >= target;
                default:
                    return true;
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < _levels.Length; i++)
                _stations.Stations[i].LevelChanged -= OnLevelChanged;
            if (_managers != null)
                _managers.ManagerHired -= OnManagerHired;
            if (_upgrades != null)
                _upgrades.UpgradePurchased -= OnUpgradePurchased;
            if (_boost != null)
                _boost.Watched -= OnBoostWatched;
            if (_gift != null)
                _gift.Opened -= OnGiftOpened;
        }

        private void OnLevelChanged(StationModel station)
        {
            for (int i = 0; i < _levels.Length; i++)
            {
                if (!ReferenceEquals(_stations.Stations[i], station))
                    continue;

                int delta = station.Level - _levels[i];
                _levels[i] = station.Level;
                if (delta > 0)
                    _quests.Report(QuestType.BuyLevels, delta);
                return;
            }
        }

        private void OnManagerHired(int index)
        {
            _quests.Report(QuestType.HireManager, 1);
        }

        private void OnUpgradePurchased(int index)
        {
            _quests.Report(QuestType.BuyUpgrade, 1);
        }

        private void OnBoostWatched()
        {
            _quests.Report(QuestType.WatchBoost, 1);
        }

        private void OnGiftOpened(double coins)
        {
            _quests.Report(QuestType.OpenGift, 1);
        }

        private static int CountNotHired(ManagerController managers)
        {
            int count = 0;
            for (int i = 0; i < managers.Count; i++)
            {
                if (!managers.IsHired(i))
                    count++;
            }
            return count;
        }

        private static int CountNotPurchased(UpgradeService upgrades)
        {
            int count = 0;
            for (int i = 0; i < upgrades.Count; i++)
            {
                if (!upgrades.IsPurchased(i))
                    count++;
            }
            return count;
        }
    }
}
