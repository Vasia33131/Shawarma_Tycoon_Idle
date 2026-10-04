using ShaurmaTycoon.Stations;
using UnityEngine;

namespace ShaurmaTycoon.Upgrades
{
    [CreateAssetMenu(menuName = "ShaurmaTycoon/Upgrade Config", fileName = "Upgrade_")]
    public sealed class UpgradeConfig : ScriptableObject, IUpgradeDefinition
    {
        [SerializeField] private string _id = string.Empty;
        [SerializeField] private double _cost = 1000;
        [Tooltip("Пусто — улучшение для всех станций города.")]
        [SerializeField] private StationConfig _targetStation = null;
        [SerializeField] private double _incomeMultiplier = 3;

        public string Id => _id;
        public double Cost => _cost;
        // Через интерфейс «фейковый null» Unity выглядел бы как объект — возвращаем настоящий null.
        public IStationDefinition TargetStation => _targetStation != null ? _targetStation : null;
        public double IncomeMultiplier => _incomeMultiplier;

        private void OnValidate()
        {
            if (_cost < 0)
                _cost = 0;
            if (_incomeMultiplier < 1)
                _incomeMultiplier = 1;
        }
    }
}
