using System;
using ShaurmaTycoon.Core;
using ShaurmaTycoon.Core.Settings;
using ShaurmaTycoon.Prestige;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace ShaurmaTycoon.Ambient
{
    /// <summary>
    /// Живой мир за ларьком (V5), один ITickable на всё: облака, звёзды, светлячки, пар от гриля, далёкие птицы,
    /// воробей и транспорт за очередью. Все Image — из пулов, собранных Build Game UI; в Tick нет аллокаций.
    /// Анимации идут в реальном времени (TimeScale не ускоряет) и замирают вместе с TickManager (реклама, скрытая вкладка).
    /// «Эффекты фона» выключены — остаются только облака и пар. В дождь и снег звёзды, светлячки и птицы прячутся:
    /// так одновременно на экране не больше ~90 анимированных Image.
    /// </summary>
    public sealed class AmbientWorldView : MonoBehaviour, ITickable
    {
        private enum SparrowState
        {
            Away,
            FlyingIn,
            Resting,
            Hopping,
            FlyingOut,
        }

        // После долгой паузы (вкладка, реклама) — без рывка.
        private const float MaxDeltaTime = 0.1f;
        private const float TwoPi = Mathf.PI * 2f;
        private const float OffscreenMargin = 80f;
        private const float SteamFadeIn = 0.15f;
        private const float SparrowHopStep = 12f;

        [Tooltip("Зона ларька: её размер — границы сцены.")]
        [SerializeField] private RectTransform _area = null;

        [Header("Sky")]
        [SerializeField] private Image[] _clouds = new Image[0];
        [SerializeField] private Sprite[] _cloudSprites = new Sprite[0];
        [SerializeField] private Image[] _stars = new Image[0];
        [Tooltip("Далёкие птицы-«галочки»: корень с двумя крыльями, взмах — scaleY корня.")]
        [SerializeField] private RectTransform[] _farBirds = new RectTransform[0];
        [Tooltip("Смещение каждой птицы стаи от ведущей, px.")]
        [SerializeField] private Vector2[] _farBirdOffsets = new Vector2[0];

        [Header("Street")]
        [Tooltip("Транспорт: якорь — линия тротуара, pivot снизу.")]
        [SerializeField] private Image _vehicle = null;
        [Tooltip("Высота «дороги» над линией тротуара, px.")]
        [SerializeField] private float _roadY = 4f;

        [Header("Front")]
        [Tooltip("Якорь — точка в нижней половине сцены, дрейф — смещение от неё.")]
        [SerializeField] private Image[] _fireflies = new Image[0];
        [SerializeField] private Image[] _steam = new Image[0];
        [Tooltip("Точка над вертелом (координаты от середины линии тротуара).")]
        [SerializeField] private Vector2 _steamOrigin = Vector2.zero;
        [SerializeField] private Image _sparrow = null;
        [Tooltip("Куда садится воробей: навес, тротуар (координаты от середины линии тротуара).")]
        [SerializeField] private Vector2[] _perches = new Vector2[0];

        private AmbientConfig _config;
        private CityConfig _city;
        private GameSettings _settings;
        private WeatherService _weather;
        private TickManager _ticks;
        private Canvas _canvas;
        private float _time;
        private bool _effectsShown;

        private float[] _cloudX;
        private float[] _cloudSpeed;

        private float[] _starPeriod;
        private float[] _starPhase;
        private float _starFactor;

        private Vector4[] _fireflyMotion;
        private Vector4[] _fireflyPhase;
        private int _fireflyCount;
        private float _fireflyAlpha;
        private float _fireflyFactor;

        private float[] _steamAge;
        private float[] _steamRise;
        private float[] _steamPhase;
        private float _steamTimer;

        private bool _birdsFlying;
        private float _birdTimer;
        private float _birdX;
        private float _birdY;
        private float _birdDirection;
        private float _birdSpeed;

        private SparrowState _sparrowState;
        private float _sparrowTimer;
        private float _sparrowProgress;
        private Vector2 _sparrowFrom;
        private Vector2 _sparrowTo;
        private Vector2 _sparrowControl;
        private float _sparrowFacing = 1f;
        private int _sparrowHops;

        private bool _vehicleDriving;
        private bool _vehicleAnnounced;
        private float _vehicleTimer;
        private float _vehicleX;
        private float _vehicleSpeed;
        private float _vehicleWidth;

        /// <summary>Транспорт въехал в кадр (x — центр, от середины сцены). Для звука в V6.</summary>
        public event Action<float> VehiclePassed;

        /// <summary>Транспорт едет: x центра в долях полуширины сцены (−1 левый край … 1 правый). Для панорамы звука.</summary>
        public event Action<float> VehicleMoved;

        public void Bind(AmbientConfig config, CityConfig city, GameSettings settings, WeatherService weather, TickManager ticks)
        {
            _config = config;
            _city = city;
            _settings = settings;
            _weather = weather;
            _ticks = ticks;
            Canvas canvas = GetComponentInParent<Canvas>();
            _canvas = canvas != null ? canvas.rootCanvas : null;
            _effectsShown = EffectsOn;

            SetupClouds();
            SetupStars();
            SetupFireflies();
            SetupSteam();

            SetActive(_farBirds, false);
            _birdTimer = FirstDelay(config.FarBirdInterval);
            if (_sparrow != null)
                _sparrow.gameObject.SetActive(false);
            _sparrowState = SparrowState.Away;
            _sparrowTimer = FirstDelay(config.SparrowInterval);
            if (_vehicle != null)
                _vehicle.gameObject.SetActive(false);
            _vehicleTimer = FirstDelay(config.VehicleInterval);
        }

        /// <summary>Отладка: транспорт проезжает сейчас (если в городе он есть и дорога свободна).</summary>
        public void SpawnVehicleNow()
        {
            if (_config != null && !_vehicleDriving)
                StartVehicle();
        }

        public void Tick(float deltaTime)
        {
            if (_config == null)
                return;

            float scale = _ticks != null && _ticks.TimeScale > 0f ? _ticks.TimeScale : 1f;
            float dt = Mathf.Min(deltaTime / scale, MaxDeltaTime);
            _time += dt;

            Rect area = _area.rect;
            float halfWidth = area.width * 0.5f;
            bool effects = EffectsOn;
            if (_effectsShown && !effects)
                HideExtras();
            _effectsShown = effects;
            bool clear = effects && !IsPrecipitation;

            UpdateClouds(dt, halfWidth);
            UpdateStars(dt, clear && _city != null && _city.ShowStars);
            UpdateFireflies(dt, clear);
            UpdateSteam(dt);
            UpdateFarBirds(dt, halfWidth, clear);
            UpdateSparrow(dt, halfWidth, clear);
            UpdateVehicle(dt, halfWidth, effects);
        }

        private bool EffectsOn => _settings == null || _settings.EffectsOn;

        private bool IsPrecipitation => _weather != null && _weather.Current != WeatherKind.Clear;

        // Первый раз — пораньше, чтобы за первые минуты игрок успел увидеть машину и птиц.
        private static float FirstDelay(Vector2 interval)
        {
            return Random.Range(0.25f, 0.6f) * interval.x;
        }

        private static float Range(Vector2 range)
        {
            return Random.Range(range.x, range.y);
        }

        private float TransitionStep(float dt)
        {
            return dt / _config.WeatherTransition;
        }

        private void HideExtras()
        {
            SetActive(_farBirds, false);
            _birdsFlying = false;
            _birdTimer = Range(_config.FarBirdInterval);
            if (_sparrow != null)
                _sparrow.gameObject.SetActive(false);
            _sparrowState = SparrowState.Away;
            _sparrowTimer = Range(_config.SparrowInterval);
            if (_vehicle != null)
                _vehicle.gameObject.SetActive(false);
            _vehicleDriving = false;
            _vehicleTimer = Range(_config.VehicleInterval);
        }

        // ---------- Clouds ----------

        private void SetupClouds()
        {
            int count = Mathf.Min(_clouds.Length, _config.CloudCount);
            _cloudX = new float[_clouds.Length];
            _cloudSpeed = new float[_clouds.Length];
            float width = _area.rect.width;
            for (int i = 0; i < _clouds.Length; i++)
            {
                bool used = i < count && _cloudSprites.Length > 0;
                _clouds[i].gameObject.SetActive(used);
                if (!used)
                    continue;
                RerollCloud(i);
                // Сразу разбросаны по небу, а не выплывают из-за края.
                _cloudX[i] = (i + Random.Range(0.1f, 0.9f)) / count * width - width * 0.5f;
                PlaceCloud(i);
            }
        }

        private void RerollCloud(int i)
        {
            Image cloud = _clouds[i];
            Sprite sprite = _cloudSprites[Random.Range(0, _cloudSprites.Length)];
            cloud.sprite = sprite;
            float width = _config.CloudWidth * Range(_config.CloudScale);
            float height = sprite != null ? width * sprite.rect.height / sprite.rect.width : width * 0.5f;
            RectTransform rect = cloud.rectTransform;
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -Range(_config.CloudTop));
            cloud.canvasRenderer.SetAlpha(Range(_config.CloudAlpha));
            _cloudSpeed[i] = Range(_config.CloudSpeed);
        }

        private void PlaceCloud(int i)
        {
            RectTransform rect = _clouds[i].rectTransform;
            rect.anchoredPosition = new Vector2(_cloudX[i], rect.anchoredPosition.y);
        }

        private void UpdateClouds(float dt, float halfWidth)
        {
            for (int i = 0; i < _clouds.Length; i++)
            {
                if (!_clouds[i].gameObject.activeSelf)
                    continue;
                _cloudX[i] += _cloudSpeed[i] * dt;
                float halfCloud = _clouds[i].rectTransform.sizeDelta.x * 0.5f;
                if (_cloudX[i] - halfCloud > halfWidth)
                {
                    RerollCloud(i);
                    _cloudX[i] = -halfWidth - _clouds[i].rectTransform.sizeDelta.x * 0.5f;
                }
                PlaceCloud(i);
            }
        }

        // ---------- Stars ----------

        private void SetupStars()
        {
            int count = Mathf.Min(_stars.Length, _config.StarCount);
            _starPeriod = new float[_stars.Length];
            _starPhase = new float[_stars.Length];
            bool show = EffectsOn && !IsPrecipitation && _city != null && _city.ShowStars;
            _starFactor = show ? 1f : 0f;
            for (int i = 0; i < _stars.Length; i++)
            {
                RectTransform rect = _stars[i].rectTransform;
                // Верхняя половина неба; якорем — чтобы не зависеть от ориентации экрана.
                Vector2 anchor = new Vector2(Random.Range(0.03f, 0.97f), Random.Range(0.58f, 0.97f));
                rect.anchorMin = anchor;
                rect.anchorMax = anchor;
                float size = Range(_config.StarSize);
                rect.sizeDelta = new Vector2(size, size);
                _starPeriod[i] = Range(_config.StarPeriod);
                _starPhase[i] = Random.value * TwoPi;
                _stars[i].gameObject.SetActive(show && i < count);
            }
        }

        private void UpdateStars(float dt, bool show)
        {
            if (!Fade(ref _starFactor, show, dt, _stars, _config.StarCount))
                return;
            Vector2 alpha = _config.StarAlpha;
            for (int i = 0; i < _stars.Length; i++)
            {
                if (!_stars[i].gameObject.activeSelf)
                    continue;
                float wave = 0.5f + 0.5f * Mathf.Sin(_time * TwoPi / _starPeriod[i] + _starPhase[i]);
                _stars[i].canvasRenderer.SetAlpha(Mathf.Lerp(alpha.x, alpha.y, wave) * _starFactor);
            }
        }

        /// <summary>Плавно ведёт factor к 0/1 и включает первые count объектов группы. false — группа скрыта, обновлять нечего.</summary>
        private bool Fade(ref float factor, bool show, float dt, Image[] group, int count)
        {
            float target = show ? 1f : 0f;
            if (factor <= 0f && target <= 0f)
                return false;
            if (factor <= 0f && target > 0f)
            {
                for (int i = 0; i < group.Length; i++)
                    group[i].gameObject.SetActive(i < count);
            }
            factor = Mathf.MoveTowards(factor, target, TransitionStep(dt));
            if (factor <= 0f)
            {
                SetActive(group, false);
                return false;
            }
            return true;
        }

        // ---------- Fireflies ----------

        private void SetupFireflies()
        {
            bool boosted = _city != null && _city.Fireflies;
            _fireflyCount = Mathf.Min(_fireflies.Length, boosted ? _config.FireflyCountBoosted : _config.FireflyCount);
            _fireflyAlpha = boosted ? _config.FireflyAlphaBoosted : _config.FireflyAlpha;
            _fireflyMotion = new Vector4[_fireflies.Length];
            _fireflyPhase = new Vector4[_fireflies.Length];
            bool show = EffectsOn && !IsPrecipitation;
            _fireflyFactor = show ? 1f : 0f;
            for (int i = 0; i < _fireflies.Length; i++)
            {
                RectTransform rect = _fireflies[i].rectTransform;
                Vector2 anchor = new Vector2(Random.Range(0.04f, 0.96f), Random.Range(0.1f, 0.5f));
                rect.anchorMin = anchor;
                rect.anchorMax = anchor;
                float size = Range(_config.FireflySize);
                rect.sizeDelta = new Vector2(size, size);
                // x, y — размах; z, w — угловые скорости двух синусов.
                _fireflyMotion[i] = new Vector4(Range(_config.FireflyDrift), Range(_config.FireflyDrift) * 0.5f,
                    TwoPi / Range(_config.FireflyPeriod), TwoPi / Range(_config.FireflyPeriod));
                _fireflyPhase[i] = new Vector4(Random.value * TwoPi, Random.value * TwoPi, Random.value * TwoPi,
                    TwoPi / Range(_config.FireflyBreathPeriod));
                _fireflies[i].gameObject.SetActive(show && i < _fireflyCount);
            }
        }

        private void UpdateFireflies(float dt, bool show)
        {
            if (!Fade(ref _fireflyFactor, show, dt, _fireflies, _fireflyCount))
                return;
            for (int i = 0; i < _fireflies.Length; i++)
            {
                if (!_fireflies[i].gameObject.activeSelf)
                    continue;
                Vector4 motion = _fireflyMotion[i];
                Vector4 phase = _fireflyPhase[i];
                // Сумма двух синусов — плавная «блуждающая» кривая без повторяющегося рисунка.
                float x = motion.x * (0.7f * Mathf.Sin(_time * motion.z + phase.x) + 0.3f * Mathf.Sin(_time * motion.w * 2.3f + phase.y));
                float y = motion.y * (0.6f * Mathf.Sin(_time * motion.w + phase.y) + 0.4f * Mathf.Sin(_time * motion.z * 1.7f + phase.x));
                _fireflies[i].rectTransform.anchoredPosition = new Vector2(x, y);
                float breath = 0.5f + 0.5f * Mathf.Sin(_time * phase.w + phase.z);
                _fireflies[i].canvasRenderer.SetAlpha(_fireflyAlpha * (0.3f + 0.7f * breath) * _fireflyFactor);
            }
        }

        // ---------- Steam ----------

        private void SetupSteam()
        {
            _steamAge = new float[_steam.Length];
            _steamRise = new float[_steam.Length];
            _steamPhase = new float[_steam.Length];
            SetActive(_steam, false);
            _steamTimer = Range(_config.SteamInterval);
        }

        private void UpdateSteam(float dt)
        {
            int count = Mathf.Min(_steam.Length, _config.SteamCount);
            _steamTimer -= dt;
            if (_steamTimer <= 0f)
            {
                _steamTimer = Range(_config.SteamInterval);
                for (int i = 0; i < count; i++)
                {
                    if (_steam[i].gameObject.activeSelf)
                        continue;
                    _steamAge[i] = 0f;
                    _steamRise[i] = Range(_config.SteamRise);
                    _steamPhase[i] = Random.value * TwoPi;
                    _steam[i].canvasRenderer.SetAlpha(0f);
                    _steam[i].gameObject.SetActive(true);
                    break;
                }
            }

            float life = _config.SteamLifetime;
            for (int i = 0; i < _steam.Length; i++)
            {
                Image puff = _steam[i];
                if (!puff.gameObject.activeSelf)
                    continue;
                _steamAge[i] += dt;
                float t = _steamAge[i] / life;
                if (t >= 1f)
                {
                    puff.gameObject.SetActive(false);
                    continue;
                }
                RectTransform rect = puff.rectTransform;
                float sway = Mathf.Sin(_steamAge[i] * 3f + _steamPhase[i]) * _config.SteamSway * t;
                rect.anchoredPosition = _steamOrigin + new Vector2(sway, _steamRise[i] * (1f - (1f - t) * (1f - t)));
                float size = Mathf.Lerp(1f, _config.SteamGrowth, t);
                rect.localScale = new Vector3(size, size, 1f);
                puff.canvasRenderer.SetAlpha(_config.SteamAlpha * Mathf.Min(1f, t / SteamFadeIn) * (1f - t));
            }
        }

        // ---------- Far birds ----------

        private void UpdateFarBirds(float dt, float halfWidth, bool clear)
        {
            if (!_birdsFlying)
            {
                if (!clear)
                    return;
                _birdTimer -= dt;
                if (_birdTimer > 0f)
                    return;
                _birdsFlying = true;
                _birdDirection = Random.value < 0.5f ? -1f : 1f;
                _birdX = -_birdDirection * (halfWidth + OffscreenMargin);
                _birdY = -Range(_config.FarBirdTop);
                _birdSpeed = Range(_config.FarBirdSpeed);
                SetActive(_farBirds, true);
            }

            _birdX += _birdDirection * _birdSpeed * dt;
            float flap = Mathf.PI / Mathf.Max(0.05f, _config.FarBirdFlapPeriod);
            for (int i = 0; i < _farBirds.Length; i++)
            {
                Vector2 offset = i < _farBirdOffsets.Length ? _farBirdOffsets[i] : Vector2.zero;
                // Ведомая чуть позади ведущей по ходу полёта и чуть плавно «плывёт» по высоте.
                _farBirds[i].anchoredPosition = new Vector2(_birdX - offset.x * _birdDirection,
                    _birdY + offset.y + Mathf.Sin(_time * 0.8f + i) * 4f);
                _farBirds[i].localScale = new Vector3(1f, 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(_time * flap + i * 1.3f)), 1f);
            }

            if (Mathf.Abs(_birdX) > halfWidth + OffscreenMargin * 2f)
            {
                _birdsFlying = false;
                _birdTimer = Range(_config.FarBirdInterval);
                SetActive(_farBirds, false);
            }
        }

        // ---------- Sparrow ----------

        private void UpdateSparrow(float dt, float halfWidth, bool clear)
        {
            if (_sparrow == null || _perches.Length == 0)
                return;

            RectTransform rect = _sparrow.rectTransform;
            switch (_sparrowState)
            {
                case SparrowState.Away:
                    if (!clear)
                        return;
                    _sparrowTimer -= dt;
                    if (_sparrowTimer <= 0f)
                        StartSparrowFlight(halfWidth);
                    return;

                case SparrowState.FlyingIn:
                case SparrowState.FlyingOut:
                    float duration = _config.SparrowFlightTime * (_sparrowState == SparrowState.FlyingOut ? 0.8f : 1f);
                    _sparrowProgress += dt / duration;
                    float t = Mathf.Clamp01(_sparrowProgress);
                    float eased = _sparrowState == SparrowState.FlyingIn ? 1f - (1f - t) * (1f - t) : t * t;
                    rect.anchoredPosition = Bezier(_sparrowFrom, _sparrowControl, _sparrowTo, eased);
                    // Машет крыльями — быстро «дрожит» по высоте.
                    rect.localScale = new Vector3(_sparrowFacing, 1f + 0.12f * Mathf.Sin(_time * 45f), 1f);
                    if (t < 1f)
                        return;
                    if (_sparrowState == SparrowState.FlyingOut)
                    {
                        _sparrow.gameObject.SetActive(false);
                        _sparrowState = SparrowState.Away;
                        _sparrowTimer = Range(_config.SparrowInterval);
                        return;
                    }
                    rect.localScale = new Vector3(_sparrowFacing, 1f, 1f);
                    _sparrowState = SparrowState.Resting;
                    _sparrowTimer = Range(_config.SparrowRest);
                    return;

                case SparrowState.Resting:
                    if (ScaredByTap(rect))
                    {
                        StartSparrowLeave(halfWidth);
                        return;
                    }
                    _sparrowTimer -= dt;
                    if (_sparrowTimer > 0f)
                        return;
                    if (_sparrowHops <= 0)
                    {
                        StartSparrowLeave(halfWidth);
                        return;
                    }
                    _sparrowHops--;
                    // Иногда разворачивается перед прыжком.
                    if (Random.value < 0.35f)
                        _sparrowFacing = -_sparrowFacing;
                    _sparrowFrom = rect.anchoredPosition;
                    _sparrowTo = _sparrowFrom + new Vector2(-_sparrowFacing * SparrowHopStep, 0f);
                    _sparrowProgress = 0f;
                    rect.localScale = new Vector3(_sparrowFacing, 1f, 1f);
                    _sparrowState = SparrowState.Hopping;
                    return;

                case SparrowState.Hopping:
                    if (ScaredByTap(rect))
                    {
                        StartSparrowLeave(halfWidth);
                        return;
                    }
                    _sparrowProgress += dt / Mathf.Max(0.05f, _config.SparrowHopTime);
                    float hop = Mathf.Clamp01(_sparrowProgress);
                    rect.anchoredPosition = Vector2.Lerp(_sparrowFrom, _sparrowTo, hop)
                                            + new Vector2(0f, Mathf.Sin(hop * Mathf.PI) * _config.SparrowHopHeight);
                    if (hop < 1f)
                        return;
                    _sparrowState = SparrowState.Resting;
                    _sparrowTimer = Range(_config.SparrowRest);
                    return;
            }
        }

        private void StartSparrowFlight(float halfWidth)
        {
            _sparrowTo = _perches[Random.Range(0, _perches.Length)];
            float side = Random.value < 0.5f ? -1f : 1f;
            _sparrowFrom = new Vector2(side * (halfWidth + OffscreenMargin), _sparrowTo.y + 260f);
            _sparrowControl = new Vector2((_sparrowFrom.x + _sparrowTo.x) * 0.5f, _sparrowFrom.y + 40f);
            // Картинка смотрит влево: летит вправо — отражаем.
            _sparrowFacing = _sparrowTo.x > _sparrowFrom.x ? -1f : 1f;
            _sparrowProgress = 0f;
            _sparrowHops = Random.Range(_config.SparrowHops.x, _config.SparrowHops.y + 1);
            _sparrowState = SparrowState.FlyingIn;
            RectTransform rect = _sparrow.rectTransform;
            float size = _config.SparrowSize;
            Sprite sprite = _sparrow.sprite;
            rect.sizeDelta = new Vector2(size, sprite != null ? size * sprite.rect.height / sprite.rect.width : size);
            rect.anchoredPosition = _sparrowFrom;
            _sparrow.gameObject.SetActive(true);
        }

        private void StartSparrowLeave(float halfWidth)
        {
            Vector2 position = _sparrow.rectTransform.anchoredPosition;
            float side = position.x > 0f ? 1f : -1f;
            if (Random.value < 0.3f)
                side = -side;
            _sparrowFrom = position;
            _sparrowTo = new Vector2(side * (halfWidth + OffscreenMargin), position.y + 280f);
            _sparrowControl = new Vector2(position.x + side * 40f, position.y + 120f);
            _sparrowFacing = side > 0f ? -1f : 1f;
            _sparrowProgress = 0f;
            _sparrowState = SparrowState.FlyingOut;
        }

        // Тап рядом с воробьём (по любому месту экрана): Input не блокируется UI, кнопки работают как обычно.
        private bool ScaredByTap(RectTransform sparrow)
        {
            if (!Input.GetMouseButtonDown(0))
                return false;
            Camera camera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, sparrow.position);
            float scale = _canvas != null ? _canvas.scaleFactor : 1f;
            float radius = _config.SparrowScareRadius * scale;
            return ((Vector2)Input.mousePosition - screen).sqrMagnitude < radius * radius;
        }

        private static Vector2 Bezier(Vector2 a, Vector2 control, Vector2 b, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * control + t * t * b;
        }

        // ---------- Vehicles ----------

        private void UpdateVehicle(float dt, float halfWidth, bool effects)
        {
            if (_vehicle == null)
                return;

            if (!_vehicleDriving)
            {
                if (!effects)
                    return;
                _vehicleTimer -= dt;
                if (_vehicleTimer > 0f)
                    return;
                _vehicleTimer = Range(_config.VehicleInterval);
                StartVehicle();
                return;
            }

            _vehicleX -= _vehicleSpeed * dt;
            float jitter = Mathf.Sin(_time * 23f) * _config.VehicleJitter;
            _vehicle.rectTransform.anchoredPosition = new Vector2(_vehicleX, _roadY + jitter);
            if (halfWidth > 0f)
                VehicleMoved?.Invoke(Mathf.Clamp(_vehicleX / halfWidth, -1f, 1f));

            float half = _vehicleWidth * 0.5f;
            if (!_vehicleAnnounced && _vehicleX - half < halfWidth)
            {
                _vehicleAnnounced = true;
                VehiclePassed?.Invoke(_vehicleX);
            }
            if (_vehicleX + half < -halfWidth - OffscreenMargin)
            {
                _vehicleDriving = false;
                _vehicle.gameObject.SetActive(false);
            }
        }

        private void StartVehicle()
        {
            Sprite sprite = PickVehicle();
            if (sprite == null || _vehicle == null)
                return;

            float height = _config.GetVehicleHeight(sprite);
            _vehicleWidth = height * sprite.rect.width / sprite.rect.height;
            _vehicle.sprite = sprite;
            RectTransform rect = _vehicle.rectTransform;
            rect.sizeDelta = new Vector2(_vehicleWidth, height);
            _vehicleX = _area.rect.width * 0.5f + _vehicleWidth * 0.5f + OffscreenMargin;
            rect.anchoredPosition = new Vector2(_vehicleX, _roadY);
            _vehicleSpeed = Range(_config.VehicleSpeed);
            _vehicleDriving = true;
            _vehicleAnnounced = false;
            _vehicle.gameObject.SetActive(true);
        }

        private Sprite PickVehicle()
        {
            if (_city == null || _city.Vehicles.Count == 0)
                return null;
            int count = _city.Vehicles.Count;
            int start = Random.Range(0, count);
            for (int i = 0; i < count; i++)
            {
                Sprite sprite = _city.Vehicles[(start + i) % count];
                if (sprite != null)
                    return sprite;
            }
            return null;
        }

        private static void SetActive(Image[] group, bool active)
        {
            for (int i = 0; i < group.Length; i++)
            {
                if (group[i].gameObject.activeSelf != active)
                    group[i].gameObject.SetActive(active);
            }
        }

        private static void SetActive(RectTransform[] group, bool active)
        {
            for (int i = 0; i < group.Length; i++)
            {
                if (group[i].gameObject.activeSelf != active)
                    group[i].gameObject.SetActive(active);
            }
        }
    }
}
