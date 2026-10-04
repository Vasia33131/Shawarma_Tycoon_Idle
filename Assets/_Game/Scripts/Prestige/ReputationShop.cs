using System;
using System.Collections.Generic;

namespace ShaurmaTycoon.Prestige
{
    /// <summary>
    /// Магазин репутации: покупка уровней за свободную репутацию (бонус к доходу при этом не падает)
    /// и значения эффектов для систем игры.
    /// </summary>
    public sealed class ReputationShop
    {
        private readonly IReadOnlyList<ShopItemConfig> _items;
        private readonly PrestigeState _state;

        public ReputationShop(IReadOnlyList<ShopItemConfig> items, PrestigeState state)
        {
            _items = items ?? throw new ArgumentNullException(nameof(items));
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        /// <summary>Аргумент — индекс пункта.</summary>
        public event Action<int> Purchased;

        public int Count => _items.Count;

        public ShopItemConfig GetItem(int index)
        {
            return _items[index];
        }

        public int GetLevel(int index)
        {
            return Math.Min(_state.GetShopLevel(_items[index].Id), _items[index].MaxLevel);
        }

        public bool IsMaxed(int index)
        {
            return GetLevel(index) >= _items[index].MaxLevel;
        }

        /// <summary>Цена следующего уровня или -1, если куплен максимум.</summary>
        public long GetNextPrice(int index)
        {
            return _items[index].GetPrice(GetLevel(index));
        }

        public bool CanBuy(int index)
        {
            long price = GetNextPrice(index);
            return price >= 0 && price <= _state.FreeReputation;
        }

        public bool TryBuy(int index)
        {
            if (index < 0 || index >= _items.Count || !CanBuy(index))
                return false;

            int level = GetLevel(index);
            if (!_state.TrySpend(_items[index].GetPrice(level)))
                return false;

            _state.SetShopLevel(_items[index].Id, level + 1);
            Purchased?.Invoke(index);
            return true;
        }

        /// <summary>Значение эффекта по купленному уровню (без покупок — базовое: 0 или 1 для множителя).</summary>
        public double GetEffect(ShopEffect effect)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].Effect == effect)
                    return _items[i].GetValue(GetLevel(i));
            }
            return ShopItemConfig.BaseValue(effect);
        }
    }
}
