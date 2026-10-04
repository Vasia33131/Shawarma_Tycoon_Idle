using System.Collections.Generic;
using UnityEngine;

namespace ShaurmaTycoon.Prestige
{
    /// <summary>Города по порядку открытия: Родной город → Казань → …</summary>
    [CreateAssetMenu(menuName = "ShaurmaTycoon/City Catalog", fileName = "CityCatalog")]
    public sealed class CityCatalog : ScriptableObject
    {
        [SerializeField] private CityConfig[] _cities = new CityConfig[0];

        public IReadOnlyList<CityConfig> Cities => _cities;
        public int Count => _cities.Length;

        /// <summary>Город по индексу; индекс за пределами списка прижимается к краю.</summary>
        public CityConfig Get(int index)
        {
            if (_cities.Length == 0)
                return null;
            return _cities[Mathf.Clamp(index, 0, _cities.Length - 1)];
        }
    }
}
