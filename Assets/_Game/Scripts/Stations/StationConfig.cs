using UnityEngine;

namespace ShaurmaTycoon.Stations
{
    [CreateAssetMenu(menuName = "ShaurmaTycoon/Station Config", fileName = "Station_")]
    public sealed class StationConfig : ScriptableObject, IStationDefinition
    {
        private const double MinCycleTime = 0.01;

        [SerializeField] private string _id = string.Empty;
        [Tooltip("Ключ локализации названия станции.")]
        [SerializeField] private string _nameKey = string.Empty;
        [SerializeField] private Sprite _icon = null;

        [Header("Balance")]
        [SerializeField] private double _baseCost = 4;
        [SerializeField] private double _costGrowth = 1.07;
        [SerializeField] private double _baseIncome = 1;
        [SerializeField] private double _cycleTime = 1;
        [Tooltip("0 — станция закрыта на старте города.")]
        [SerializeField] private int _startLevel;

        public string Id => _id;
        public string NameKey => _nameKey;
        public Sprite Icon => _icon;
        public double BaseCost => _baseCost;
        public double CostGrowth => _costGrowth;
        public double BaseIncome => _baseIncome;
        public double CycleTime => _cycleTime;
        public int StartLevel => _startLevel;

        private void OnValidate()
        {
            if (_baseCost < 0)
                _baseCost = 0;
            if (_costGrowth < 1)
                _costGrowth = 1;
            if (_baseIncome < 0)
                _baseIncome = 0;
            if (_cycleTime < MinCycleTime)
                _cycleTime = MinCycleTime;
            if (_startLevel < 0)
                _startLevel = 0;
        }
    }
}
