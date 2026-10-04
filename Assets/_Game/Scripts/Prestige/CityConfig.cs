using System.Collections.Generic;
using ShaurmaTycoon.Stations;
using ShaurmaTycoon.Upgrades;
using UnityEngine;

namespace ShaurmaTycoon.Prestige
{
    /// <summary>
    /// Город (GDD §7): станции и улучшения, порог престижа, масштаб значений, цвета ларька и место на карте.
    /// </summary>
    [CreateAssetMenu(menuName = "ShaurmaTycoon/City Config", fileName = "City_")]
    public sealed class CityConfig : ScriptableObject
    {
        [SerializeField] private string _id = string.Empty;
        [Tooltip("Ключ локализации названия города.")]
        [SerializeField] private string _nameKey = string.Empty;
        [SerializeField] private StationConfig[] _stations = new StationConfig[0];
        [SerializeField] private UpgradeConfig[] _upgrades = new UpgradeConfig[0];

        [Header("Prestige")]
        [Tooltip("Сколько нужно заработать в этом городе, чтобы открыть следующий (GDD §7).")]
        [SerializeField] private double _prestigeThreshold = 1e9;
        [Tooltip("Во сколько раз значения города больше базовых (город 2 — ×10). Масштабирует стартовый капитал.")]
        [SerializeField] private double _valueScale = 1;

        [Header("Look")]
        [SerializeField] private Color _skyColor = new Color(1f, 0.87f, 0.68f);
        [SerializeField] private Color _boothColor = new Color(0.85f, 0.30f, 0.20f);

        [Header("Map")]
        [Tooltip("Точка города на карте во вкладке «Город»: доли ширины и высоты карты, (0, 0) — левый нижний угол.")]
        [SerializeField] private Vector2 _mapPosition = new Vector2(0.5f, 0.5f);

        public string Id => _id;
        public string NameKey => _nameKey;
        public IReadOnlyList<StationConfig> Stations => _stations;
        public IReadOnlyList<UpgradeConfig> Upgrades => _upgrades;
        public double PrestigeThreshold => _prestigeThreshold;
        public double ValueScale => _valueScale;
        public Color SkyColor => _skyColor;
        public Color BoothColor => _boothColor;
        public Vector2 MapPosition => _mapPosition;

        private void OnValidate()
        {
            if (_prestigeThreshold < 1)
                _prestigeThreshold = 1;
            if (_valueScale < 0)
                _valueScale = 0;
            _mapPosition = new Vector2(Mathf.Clamp01(_mapPosition.x), Mathf.Clamp01(_mapPosition.y));
        }
    }
}
