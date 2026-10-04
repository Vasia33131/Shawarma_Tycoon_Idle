using System;
using System.Collections.Generic;

namespace ShaurmaTycoon.Prestige
{
    /// <summary>
    /// Прогресс престижа, живущий между городами: заработок, репутация, текущий город, уровни магазина.
    /// Бонус к доходу считается от всей полученной репутации; в магазине тратится «свободная».
    /// </summary>
    public sealed class PrestigeState
    {
        private readonly Dictionary<string, int> _shopLevels = new Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>Изменилась полученная или потраченная репутация, уровни магазина или город.</summary>
        public event Action Changed;

        public int CityIndex { get; private set; }
        public double CityEarned { get; private set; }
        public double LifetimeEarned { get; private set; }
        public long ReputationEarned { get; private set; }
        public long ReputationSpent { get; private set; }
        public long FreeReputation => ReputationEarned - ReputationSpent;
        public IReadOnlyDictionary<string, int> ShopLevels => _shopLevels;

        /// <summary>Учёт заработка (доход станций, офлайн). Вызывается часто — событие не поднимает.</summary>
        public void AddEarned(double amount)
        {
            if (!(amount > 0) || double.IsInfinity(amount))
                return;

            CityEarned += amount;
            LifetimeEarned += amount;
        }

        public int GetShopLevel(string itemId)
        {
            return itemId != null && _shopLevels.TryGetValue(itemId, out int level) ? level : 0;
        }

        public bool TrySpend(long amount)
        {
            if (amount < 0 || amount > FreeReputation)
                return false;

            ReputationSpent += amount;
            Changed?.Invoke();
            return true;
        }

        public void SetShopLevel(string itemId, int level)
        {
            if (string.IsNullOrEmpty(itemId))
                return;

            if (level > 0)
                _shopLevels[itemId] = level;
            else
                _shopLevels.Remove(itemId);
            Changed?.Invoke();
        }

        /// <summary>Переход в город cityIndex с наградой gain: заработок города обнуляется.</summary>
        public void CompletePrestige(long gain, int cityIndex)
        {
            ReputationEarned += Math.Max(0, gain);
            CityIndex = Math.Max(0, cityIndex);
            CityEarned = 0;
            Changed?.Invoke();
        }

        public void Load(int cityIndex, double cityEarned, double lifetimeEarned, long reputationEarned, long reputationSpent,
            IEnumerable<KeyValuePair<string, int>> shopLevels)
        {
            CityIndex = Math.Max(0, cityIndex);
            CityEarned = Math.Max(0, cityEarned);
            LifetimeEarned = Math.Max(CityEarned, lifetimeEarned);
            ReputationEarned = Math.Max(0, reputationEarned);
            ReputationSpent = Math.Min(Math.Max(0, reputationSpent), ReputationEarned);

            _shopLevels.Clear();
            if (shopLevels != null)
            {
                foreach (var pair in shopLevels)
                {
                    if (!string.IsNullOrEmpty(pair.Key) && pair.Value > 0)
                        _shopLevels[pair.Key] = pair.Value;
                }
            }

            Changed?.Invoke();
        }
    }
}
