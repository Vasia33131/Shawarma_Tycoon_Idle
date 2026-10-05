using UnityEngine;

namespace ShaurmaTycoon.Ambient
{
    /// <summary>
    /// Живой мир (V5): частоты, скорости, количества и вероятности фоновых анимаций и погоды.
    /// Диапазоны — Vector2 (x — минимум, y — максимум). Размеры пулов Build Game UI берёт отсюда — после правки
    /// количеств пересобрать UI. Все фоновые анимации идут в реальном времени (отладочное «время ×10» их не ускоряет).
    /// </summary>
    [CreateAssetMenu(menuName = "ShaurmaTycoon/Ambient Config", fileName = "Ambient")]
    public sealed class AmbientConfig : ScriptableObject
    {
        [Header("Weather schedule")]
        [Tooltip("Длина UTC-слота, мин: в каждом слоте не больше одних осадков.")]
        [SerializeField] private float _slotMinutes = 10f;
        [Tooltip("Длительность осадков, мин.")]
        [SerializeField] private Vector2 _weatherDuration = new Vector2(2f, 4f);
        [Tooltip("Плавное начало и конец осадков, с.")]
        [SerializeField] private float _weatherTransition = 3f;
        [SerializeField] private float _weatherCheckInterval = 1f;

        [Header("Clouds")]
        [SerializeField] private int _cloudCount = 4;
        [SerializeField] private Vector2 _cloudSpeed = new Vector2(8f, 14f);
        [SerializeField] private Vector2 _cloudAlpha = new Vector2(0.7f, 0.95f);
        [SerializeField] private Vector2 _cloudScale = new Vector2(0.7f, 1.2f);
        [Tooltip("Ширина облака при масштабе 1, px.")]
        [SerializeField] private float _cloudWidth = 230f;
        [Tooltip("Расстояние центра облака от верха сцены, px.")]
        [SerializeField] private Vector2 _cloudTop = new Vector2(50f, 190f);

        [Header("Stars (CityConfig: Show Stars)")]
        [SerializeField] private int _starCount = 25;
        [SerializeField] private Vector2 _starSize = new Vector2(3f, 6f);
        [SerializeField] private Vector2 _starPeriod = new Vector2(2f, 5f);
        [SerializeField] private Vector2 _starAlpha = new Vector2(0.25f, 0.95f);

        [Header("Fireflies / dust")]
        [SerializeField] private int _fireflyCount = 8;
        [Tooltip("В городах с CityConfig: Fireflies (Сочи).")]
        [SerializeField] private int _fireflyCountBoosted = 14;
        [SerializeField] private Vector2 _fireflySize = new Vector2(10f, 16f);
        [SerializeField] private float _fireflyAlpha = 0.55f;
        [SerializeField] private float _fireflyAlphaBoosted = 0.9f;
        [Tooltip("Размах дрейфа, px.")]
        [SerializeField] private Vector2 _fireflyDrift = new Vector2(30f, 80f);
        [Tooltip("Период дрейфа, с.")]
        [SerializeField] private Vector2 _fireflyPeriod = new Vector2(7f, 14f);
        [SerializeField] private Vector2 _fireflyBreathPeriod = new Vector2(2f, 4f);

        [Header("Grill steam")]
        [SerializeField] private int _steamCount = 8;
        [SerializeField] private Vector2 _steamInterval = new Vector2(0.5f, 0.8f);
        [SerializeField] private Vector2 _steamRise = new Vector2(90f, 140f);
        [SerializeField] private float _steamLifetime = 2.5f;
        [SerializeField] private float _steamSize = 30f;
        [SerializeField] private float _steamGrowth = 1.8f;
        [SerializeField] private float _steamAlpha = 0.35f;
        [Tooltip("Покачивание пара по X, px.")]
        [SerializeField] private float _steamSway = 8f;

        [Header("Far birds")]
        [SerializeField] private Vector2 _farBirdInterval = new Vector2(30f, 60f);
        [SerializeField] private Vector2 _farBirdSpeed = new Vector2(60f, 90f);
        [SerializeField] private float _farBirdFlapPeriod = 0.45f;
        [Tooltip("Высота полёта: расстояние от верха сцены, px.")]
        [SerializeField] private Vector2 _farBirdTop = new Vector2(60f, 170f);

        [Header("Sparrow")]
        [SerializeField] private Vector2 _sparrowInterval = new Vector2(60f, 120f);
        [SerializeField] private float _sparrowSize = 58f;
        [SerializeField] private float _sparrowFlightTime = 1.6f;
        [SerializeField] private Vector2Int _sparrowHops = new Vector2Int(2, 3);
        [SerializeField] private float _sparrowHopHeight = 14f;
        [SerializeField] private float _sparrowHopTime = 0.3f;
        [Tooltip("Пауза между прыжками и перед отлётом, с.")]
        [SerializeField] private Vector2 _sparrowRest = new Vector2(0.7f, 1.6f);
        [Tooltip("Тап ближе этого расстояния (px канваса) — воробей улетает сразу.")]
        [SerializeField] private float _sparrowScareRadius = 170f;

        [Header("Vehicles (CityConfig: Vehicles)")]
        [SerializeField] private Vector2 _vehicleInterval = new Vector2(15f, 40f);
        [SerializeField] private Vector2 _vehicleSpeed = new Vector2(120f, 220f);
        [Tooltip("Подрагивание по Y, px.")]
        [SerializeField] private float _vehicleJitter = 1f;
        [SerializeField] private float _vehicleDefaultHeight = 125f;
        [SerializeField] private VehicleSize[] _vehicleSizes = new VehicleSize[0];
        [SerializeField] private float _headlightAlpha = 0.4f;

        [Header("Rain")]
        [SerializeField] private int _rainDropCount = 50;
        [SerializeField] private Vector2 _rainDropSize = new Vector2(3f, 26f);
        [Tooltip("Наклон капель от вертикали, градусы (капли летят влево вниз).")]
        [SerializeField] private float _rainAngle = 14f;
        [SerializeField] private Vector2 _rainSpeed = new Vector2(900f, 1100f);
        [SerializeField] private float _rainAlpha = 0.45f;
        [SerializeField] private float _rainSkyDarken = 0.18f;
        [SerializeField] private Vector2Int _puddleCount = new Vector2Int(4, 6);
        [SerializeField] private float _puddleAlpha = 0.25f;
        [SerializeField] private Vector2 _rippleInterval = new Vector2(0.3f, 0.8f);
        [SerializeField] private float _rippleDuration = 0.8f;
        [SerializeField] private int _splashCount = 3;
        [SerializeField] private Vector2 _splashInterval = new Vector2(0.4f, 1.2f);
        [Tooltip("Пастельные цвета зонтиков клиентов.")]
        [SerializeField] private Color[] _umbrellaColors =
        {
            new Color32(0xF7, 0xB2, 0xA8, 0xFF), new Color32(0xA8, 0xD8, 0xC8, 0xFF), new Color32(0xB9, 0xC6, 0xF2, 0xFF),
            new Color32(0xFF, 0xDE, 0x95, 0xFF), new Color32(0xD9, 0xB8, 0xEE, 0xFF), new Color32(0xF4, 0xC2, 0x9A, 0xFF),
        };

        [Header("Snow")]
        [SerializeField] private int _snowflakeCount = 40;
        [SerializeField] private Vector2 _snowflakeSize = new Vector2(5f, 10f);
        [SerializeField] private Vector2 _snowSpeed = new Vector2(40f, 80f);
        [SerializeField] private float _snowSway = 18f;
        [SerializeField] private Vector2 _snowSwayPeriod = new Vector2(2f, 4f);
        [SerializeField] private float _snowAlpha = 0.9f;
        [Tooltip("За сколько секунд нарастает сугроб на навесе и тротуаре.")]
        [SerializeField] private float _driftGrowTime = 60f;
        [SerializeField] private float _driftMeltTime = 10f;
        [SerializeField] private float _driftAlpha = 0.85f;

        public float SlotMinutes => _slotMinutes;
        public Vector2 WeatherDuration => _weatherDuration;
        public float WeatherTransition => _weatherTransition;
        public float WeatherCheckInterval => _weatherCheckInterval;

        public int CloudCount => _cloudCount;
        public Vector2 CloudSpeed => _cloudSpeed;
        public Vector2 CloudAlpha => _cloudAlpha;
        public Vector2 CloudScale => _cloudScale;
        public float CloudWidth => _cloudWidth;
        public Vector2 CloudTop => _cloudTop;

        public int StarCount => _starCount;
        public Vector2 StarSize => _starSize;
        public Vector2 StarPeriod => _starPeriod;
        public Vector2 StarAlpha => _starAlpha;

        public int FireflyCount => _fireflyCount;
        public int FireflyCountBoosted => _fireflyCountBoosted;
        public Vector2 FireflySize => _fireflySize;
        public float FireflyAlpha => _fireflyAlpha;
        public float FireflyAlphaBoosted => _fireflyAlphaBoosted;
        public Vector2 FireflyDrift => _fireflyDrift;
        public Vector2 FireflyPeriod => _fireflyPeriod;
        public Vector2 FireflyBreathPeriod => _fireflyBreathPeriod;

        public int SteamCount => _steamCount;
        public Vector2 SteamInterval => _steamInterval;
        public Vector2 SteamRise => _steamRise;
        public float SteamLifetime => _steamLifetime;
        public float SteamSize => _steamSize;
        public float SteamGrowth => _steamGrowth;
        public float SteamAlpha => _steamAlpha;
        public float SteamSway => _steamSway;

        public Vector2 FarBirdInterval => _farBirdInterval;
        public Vector2 FarBirdSpeed => _farBirdSpeed;
        public float FarBirdFlapPeriod => _farBirdFlapPeriod;
        public Vector2 FarBirdTop => _farBirdTop;

        public Vector2 SparrowInterval => _sparrowInterval;
        public float SparrowSize => _sparrowSize;
        public float SparrowFlightTime => _sparrowFlightTime;
        public Vector2Int SparrowHops => _sparrowHops;
        public float SparrowHopHeight => _sparrowHopHeight;
        public float SparrowHopTime => _sparrowHopTime;
        public Vector2 SparrowRest => _sparrowRest;
        public float SparrowScareRadius => _sparrowScareRadius;

        public Vector2 VehicleInterval => _vehicleInterval;
        public Vector2 VehicleSpeed => _vehicleSpeed;
        public float VehicleJitter => _vehicleJitter;
        public float HeadlightAlpha => _headlightAlpha;

        public int RainDropCount => _rainDropCount;
        public Vector2 RainDropSize => _rainDropSize;
        public float RainAngle => _rainAngle;
        public Vector2 RainSpeed => _rainSpeed;
        public float RainAlpha => _rainAlpha;
        public float RainSkyDarken => _rainSkyDarken;
        public Vector2Int PuddleCount => _puddleCount;
        public float PuddleAlpha => _puddleAlpha;
        public Vector2 RippleInterval => _rippleInterval;
        public float RippleDuration => _rippleDuration;
        public int SplashCount => _splashCount;
        public Vector2 SplashInterval => _splashInterval;
        public Color[] UmbrellaColors => _umbrellaColors;

        public int SnowflakeCount => _snowflakeCount;
        public Vector2 SnowflakeSize => _snowflakeSize;
        public Vector2 SnowSpeed => _snowSpeed;
        public float SnowSway => _snowSway;
        public Vector2 SnowSwayPeriod => _snowSwayPeriod;
        public float SnowAlpha => _snowAlpha;
        public float DriftGrowTime => _driftGrowTime;
        public float DriftMeltTime => _driftMeltTime;
        public float DriftAlpha => _driftAlpha;

        /// <summary>Высота транспорта на экране: из таблицы размеров или по умолчанию.</summary>
        public float GetVehicleHeight(Sprite sprite)
        {
            for (int i = 0; i < _vehicleSizes.Length; i++)
            {
                if (_vehicleSizes[i].Sprite == sprite && _vehicleSizes[i].Height > 0f)
                    return _vehicleSizes[i].Height;
            }
            return _vehicleDefaultHeight;
        }

        public WeatherSchedule CreateSchedule()
        {
            return new WeatherSchedule(_slotMinutes, _weatherDuration.x, _weatherDuration.y);
        }

        private void OnValidate()
        {
            _slotMinutes = Mathf.Max(1f, _slotMinutes);
            _weatherTransition = Mathf.Max(0.1f, _weatherTransition);
            _steamLifetime = Mathf.Max(0.1f, _steamLifetime);
            _rippleDuration = Mathf.Max(0.05f, _rippleDuration);
            _driftGrowTime = Mathf.Max(0.1f, _driftGrowTime);
            _driftMeltTime = Mathf.Max(0.1f, _driftMeltTime);
        }
    }
}
