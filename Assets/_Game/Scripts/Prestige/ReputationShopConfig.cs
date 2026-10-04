using System.Collections.Generic;
using UnityEngine;

namespace ShaurmaTycoon.Prestige
{
    [CreateAssetMenu(menuName = "ShaurmaTycoon/Reputation Shop Config", fileName = "ReputationShop")]
    public sealed class ReputationShopConfig : ScriptableObject
    {
        [SerializeField] private ShopItemConfig[] _items = new ShopItemConfig[0];

        public IReadOnlyList<ShopItemConfig> Items => _items;
    }
}
