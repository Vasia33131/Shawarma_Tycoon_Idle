using System.Collections.Generic;
using ShaurmaTycoon.Stations;
using ShaurmaTycoon.Upgrades;
using UnityEngine;
using UnityEngine.Serialization;

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
        [SerializeField] private double _prestigeThreshold = 1e10;
        [Tooltip("Во сколько раз значения города больше базовых (город 2 — ×10). Масштабирует стартовый капитал.")]
        [SerializeField] private double _valueScale = 1;

        [Header("Look")]
        [Tooltip("Небо у горизонта (раньше — единственный цвет неба).")]
        [FormerlySerializedAs("_skyColor")]
        [SerializeField] private Color _skyHorizon = new Color(1f, 0.87f, 0.68f);
        [Tooltip("Небо вверху и посередине (вечерний градиент, V3). Прозрачный — не задан: всё небо цвета горизонта.")]
        [SerializeField] private Color _skyTop = Color.clear;
        [SerializeField] private Color _skyMiddle = Color.clear;
        [SerializeField] private Color _boothColor = new Color(0.85f, 0.30f, 0.20f);
        [Tooltip("Дальний силуэт города и ближний уличный слой (Sprites/Scene/City0X). Пусто — только небо.")]
        [SerializeField] private Sprite _bgFar = null;
        [SerializeField] private Sprite _bgNear = null;
        [SerializeField] private bool _showMoon = false;
        [SerializeField] private bool _showStars = false;

        [Header("Living world (V5)")]
        [Tooltip("Вероятность осадков в 10-минутном UTC-слоте (0 — всегда ясно). Погода только визуальная.")]
        [Range(0f, 1f)]
        [SerializeField] private float _rainChance = 0.2f;
        [Tooltip("Зимой (декабрь–февраль) вместо дождя идёт снег.")]
        [SerializeField] private bool _snowAllowed = true;
        [Tooltip("Транспорт, который проезжает за очередью (Sprites/Ambient). Назначает Art/Assign Icons.")]
        [SerializeField] private Sprite[] _vehicles = new Sprite[0];
        [Tooltip("Больше и ярче светлячков (южный вечер).")]
        [SerializeField] private bool _fireflies = false;

        [Header("Sound (V6)")]
        [Tooltip("Музыка города: имя файла в Audio/Music без расширения. Пусто — запасной трек из Music.asset. Назначает Art/Assign Icons.")]
        [SerializeField] private string _musicName = "";
        [Tooltip("Основа эмбиента: имя файла в Audio/Ambient без расширения (тихая улица или прибой). Пусто — без эмбиента улицы.")]
        [SerializeField] private string _ambientBaseName = "";

        [Header("Map")]
        [Tooltip("Точка города на карте во вкладке «Город»: доли ширины и высоты карты, (0, 0) — левый нижний угол.")]
        [SerializeField] private Vector2 _mapPosition = new Vector2(0.5f, 0.5f);
        [Tooltip("Миниатюра города на карте (Sprites/Map/map_city_0X). Пусто — кружок с номером. Назначает Art/Assign Icons.")]
        [SerializeField] private Sprite _mapIcon = null;

        public string Id => _id;
        public string NameKey => _nameKey;
        public IReadOnlyList<StationConfig> Stations => _stations;
        public IReadOnlyList<UpgradeConfig> Upgrades => _upgrades;
        public double PrestigeThreshold => _prestigeThreshold;
        public double ValueScale => _valueScale;
        public Color SkyColor => _skyHorizon;
        public Color SkyTop => _skyTop.a > 0f ? _skyTop : _skyHorizon;
        public Color SkyMiddle => _skyMiddle.a > 0f ? _skyMiddle : _skyHorizon;
        public Color BoothColor => _boothColor;
        public Sprite BackgroundFar => _bgFar != null ? _bgFar : null;
        public Sprite BackgroundNear => _bgNear != null ? _bgNear : null;
        public bool ShowMoon => _showMoon;
        public bool ShowStars => _showStars;
        public float RainChance => _rainChance;
        public bool SnowAllowed => _snowAllowed;
        public IReadOnlyList<Sprite> Vehicles => _vehicles;
        public bool Fireflies => _fireflies;
        public string MusicName => _musicName;
        public string AmbientBaseName => _ambientBaseName;
        public Vector2 MapPosition => _mapPosition;
        public Sprite MapIcon => _mapIcon != null ? _mapIcon : null;

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
