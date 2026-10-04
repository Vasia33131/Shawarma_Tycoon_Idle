using System.Collections.Generic;
using ShaurmaTycoon.Stations;
using ShaurmaTycoon.Upgrades;
using UnityEngine;

namespace ShaurmaTycoon.Prestige
{
    /// <summary>
    /// Город: станции и улучшения. Фон, множитель и прочее для престижа добавляются на этапе 08.
    /// </summary>
    [CreateAssetMenu(menuName = "ShaurmaTycoon/City Config", fileName = "City_")]
    public sealed class CityConfig : ScriptableObject
    {
        [SerializeField] private string _id = string.Empty;
        [Tooltip("Ключ локализации названия города.")]
        [SerializeField] private string _nameKey = string.Empty;
        [SerializeField] private StationConfig[] _stations = new StationConfig[0];
        [SerializeField] private UpgradeConfig[] _upgrades = new UpgradeConfig[0];

        public string Id => _id;
        public string NameKey => _nameKey;
        public IReadOnlyList<StationConfig> Stations => _stations;
        public IReadOnlyList<UpgradeConfig> Upgrades => _upgrades;
    }
}
