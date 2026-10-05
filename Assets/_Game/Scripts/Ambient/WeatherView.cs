using ShaurmaTycoon.Core;
using ShaurmaTycoon.Core.Settings;
using ShaurmaTycoon.Customers;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.Ambient
{
    /// <summary>
    /// Погода на экране (V5), один ITickable: дождь (затемнение неба, капли, лужи с кругами, отскоки капель на навесе,
    /// зонтики у клиентов) и снег (снежинки, сугроб на навесе и тротуаре). Начало и конец — плавно.
    /// Чисто визуально: на экономику не влияет. «Эффекты фона» выключены — погода не показывается.
    /// Пулы собирает Build Game UI; в Tick нет аллокаций; время реальное, замирает вместе с TickManager.
    /// </summary>
    public sealed class WeatherView : MonoBehaviour, ITickable
    {
        private const float MaxDeltaTime = 0.1f;
        private const float TwoPi = Mathf.PI * 2f;
        private const float SplashDuration = 0.35f;
        private const float UmbrellaThreshold = 0.3f;

        [Tooltip("Зона ларька: её размер — границы сцены.")]
        [SerializeField] private RectTransform _area = null;

        [Header("Rain")]
        [Tooltip("Затемнение неба и города (за ларьком).")]
        [SerializeField] private Image _skyDarken = null;
        [Tooltip("Капли: якорь — центр сцены.")]
        [SerializeField] private Image[] _drops = new Image[0];
        [Tooltip("Лужи на тротуаре; у каждой первый ребёнок — расходящийся круг.")]
        [SerializeField] private Image[] _puddles = new Image[0];
        [SerializeField] private Image[] _ripples = new Image[0];
        [Tooltip("Отскоки капель на навесе: якорь — линия тротуара.")]
        [SerializeField] private Image[] _splashes = new Image[0];

        [Header("Snow")]
        [Tooltip("Снежинки: якорь — центр сцены.")]
        [SerializeField] private Image[] _flakes = new Image[0];
        [Tooltip("Сугробы: растут по scaleY (pivot снизу).")]
        [SerializeField] private Image[] _drifts = new Image[0];

        [Header("Kiosk")]
        [Tooltip("Середина верхнего края навеса (от середины линии тротуара) и его ширина.")]
        [SerializeField] private Vector2 _awningTop = Vector2.zero;
        [SerializeField] private float _awningWidth = 300f;

        private AmbientConfig _config;
        private GameSettings _settings;
        private WeatherService _weather;
        private CustomerQueue _customers;
        private TickManager _ticks;
        private float _time;

        private float _rain;
        private float _snow;
        private float _drift;
        private Vector2 _dropVelocity;

        private float[] _dropSpeed;
        private float[] _rippleAge;
        private float[] _rippleDelay;
        private int _puddleCount;
        private float[] _splashAge;
        private float[] _splashDelay;
        private Vector2[] _splashFrom;
        private float[] _splashSide;
        private float[] _flakeX;
        private float[] _flakeSpeed;
        private Vector2[] _flakeSway;

        public void Bind(AmbientConfig config, GameSettings settings, WeatherService weather, CustomerQueue customers, TickManager ticks)
        {
            _config = config;
            _settings = settings;
            _weather = weather;
            _customers = customers;
            _ticks = ticks;

            float angle = config.RainAngle * Mathf.Deg2Rad;
            _dropVelocity = new Vector2(-Mathf.Sin(angle), -Mathf.Cos(angle));
            _dropSpeed = new float[_drops.Length];
            for (int i = 0; i < _drops.Length; i++)
            {
                RectTransform rect = _drops[i].rectTransform;
                rect.sizeDelta = config.RainDropSize;
                rect.localRotation = Quaternion.Euler(0f, 0f, -config.RainAngle);
            }

            _puddleCount = Mathf.Min(_puddles.Length, Random.Range(config.PuddleCount.x, config.PuddleCount.y + 1));
            _rippleAge = new float[_puddles.Length];
            _rippleDelay = new float[_puddles.Length];
            for (int i = 0; i < _puddles.Length; i++)
            {
                RectTransform rect = _puddles[i].rectTransform;
                // Тротуар — ниже линии (якорь по Y — доля высоты тротуара от линии вниз).
                float u = (i + Random.Range(0.15f, 0.85f)) / Mathf.Max(1, _puddleCount);
                rect.anchorMin = new Vector2(u, rect.anchorMin.y);
                rect.anchorMax = new Vector2(u, rect.anchorMax.y);
                rect.localScale = new Vector3(Random.Range(0.8f, 1.2f), 1f, 1f);
                _rippleDelay[i] = Range(config.RippleInterval);
            }

            _splashAge = new float[_splashes.Length];
            _splashDelay = new float[_splashes.Length];
            _splashFrom = new Vector2[_splashes.Length];
            _splashSide = new float[_splashes.Length];
            for (int i = 0; i < _splashes.Length; i++)
                _splashDelay[i] = Range(config.SplashInterval);

            _flakeX = new float[_flakes.Length];
            _flakeSpeed = new float[_flakes.Length];
            _flakeSway = new Vector2[_flakes.Length];
            for (int i = 0; i < _flakes.Length; i++)
            {
                float size = Range(config.SnowflakeSize);
                _flakes[i].rectTransform.sizeDelta = new Vector2(size, size);
            }

            HideAll();
        }

        public void Tick(float deltaTime)
        {
            if (_config == null)
                return;

            float scale = _ticks != null && _ticks.TimeScale > 0f ? _ticks.TimeScale : 1f;
            float dt = Mathf.Min(deltaTime / scale, MaxDeltaTime);
            _time += dt;

            WeatherKind shown = _weather != null && (_settings == null || _settings.EffectsOn) ? _weather.Current : WeatherKind.Clear;
            float step = dt / _config.WeatherTransition;
            float rainBefore = _rain;
            float snowBefore = _snow;
            _rain = Mathf.MoveTowards(_rain, shown == WeatherKind.Rain ? 1f : 0f, step);
            _snow = Mathf.MoveTowards(_snow, shown == WeatherKind.Snow ? 1f : 0f, step);
            _drift = shown == WeatherKind.Snow
                ? Mathf.MoveTowards(_drift, 1f, dt / _config.DriftGrowTime)
                : Mathf.MoveTowards(_drift, 0f, dt / _config.DriftMeltTime);

            Rect area = _area.rect;
            Vector2 half = new Vector2(area.width * 0.5f, area.height * 0.5f);

            if (_rain > 0f || rainBefore > 0f)
                UpdateRain(dt, half);
            if (_snow > 0f || snowBefore > 0f)
                UpdateSnow(dt, half);
            UpdateDrifts();

            if (_customers != null)
                _customers.SetRaining(_rain > UmbrellaThreshold, _config.UmbrellaColors);
        }

        private static float Range(Vector2 range)
        {
            return Random.Range(range.x, range.y);
        }

        private void HideAll()
        {
            SetActive(_skyDarken, false);
            SetActive(_drops, false);
            SetActive(_puddles, false);
            SetActive(_splashes, false);
            SetActive(_flakes, false);
            SetActive(_drifts, false);
        }

        // ---------- Rain ----------

        private void UpdateRain(float dt, Vector2 half)
        {
            bool visible = _rain > 0f;
            if (_skyDarken != null)
            {
                SetActive(_skyDarken, visible);
                _skyDarken.canvasRenderer.SetAlpha(_config.RainSkyDarken * _rain);
            }
            if (!visible)
            {
                SetActive(_drops, false);
                SetActive(_puddles, false);
                SetActive(_splashes, false);
                return;
            }

            float dropAlpha = Mathf.Min(1f, _rain * 2f);
            int dropCount = Mathf.Min(_drops.Length, Mathf.RoundToInt(_rain * _config.RainDropCount));
            for (int i = 0; i < _drops.Length; i++)
            {
                Image drop = _drops[i];
                RectTransform rect = drop.rectTransform;
                if (!drop.gameObject.activeSelf)
                {
                    if (i >= dropCount)
                        continue;
                    // Первая волна — по всей высоте, а не стеной сверху.
                    SpawnDrop(i, half, Random.Range(-half.y, half.y));
                    drop.gameObject.SetActive(true);
                }

                Vector2 position = rect.anchoredPosition + _dropVelocity * (_dropSpeed[i] * dt);
                rect.anchoredPosition = position;
                drop.canvasRenderer.SetAlpha(dropAlpha);
                if (position.y >= -half.y - 30f)
                    continue;
                // Долетела: продолжает, только если дождь ещё такой густой.
                if (i < dropCount)
                    SpawnDrop(i, half, half.y + Random.Range(20f, 80f));
                else
                    drop.gameObject.SetActive(false);
            }

            for (int i = 0; i < _puddles.Length; i++)
            {
                bool show = i < _puddleCount;
                SetActive(_puddles[i], show);
                if (!show)
                    continue;
                _puddles[i].canvasRenderer.SetAlpha(_config.PuddleAlpha * _rain);
                UpdateRipple(i, dt);
            }

            for (int i = 0; i < _splashes.Length; i++)
                UpdateSplash(i, dt);
        }

        private void SpawnDrop(int i, Vector2 half, float y)
        {
            // Капли летят влево: сверху их нужно «подсыпать» и правее края.
            float drift = (half.y * 2f) * -_dropVelocity.x / -_dropVelocity.y;
            _drops[i].rectTransform.anchoredPosition = new Vector2(Random.Range(-half.x, half.x + drift), y);
            _dropSpeed[i] = Range(_config.RainSpeed);
        }

        private void UpdateRipple(int i, float dt)
        {
            if (i >= _ripples.Length)
                return;
            Image ripple = _ripples[i];
            if (!ripple.gameObject.activeSelf)
            {
                _rippleDelay[i] -= dt;
                if (_rippleDelay[i] > 0f)
                    return;
                _rippleAge[i] = 0f;
                ripple.gameObject.SetActive(true);
            }

            _rippleAge[i] += dt;
            float t = _rippleAge[i] / _config.RippleDuration;
            if (t >= 1f)
            {
                ripple.gameObject.SetActive(false);
                _rippleDelay[i] = Range(_config.RippleInterval);
                return;
            }
            float size = Mathf.Lerp(0.25f, 1.1f, t);
            ripple.rectTransform.localScale = new Vector3(size, size, 1f);
            ripple.canvasRenderer.SetAlpha(0.7f * (1f - t) * _rain);
        }

        private void UpdateSplash(int i, float dt)
        {
            Image splash = _splashes[i];
            if (!splash.gameObject.activeSelf)
            {
                if (_rain < 0.5f)
                    return;
                _splashDelay[i] -= dt;
                if (_splashDelay[i] > 0f)
                    return;
                _splashAge[i] = 0f;
                _splashFrom[i] = _awningTop + new Vector2(Random.Range(-0.45f, 0.45f) * _awningWidth, 0f);
                _splashSide[i] = Random.value < 0.5f ? -1f : 1f;
                splash.gameObject.SetActive(true);
            }

            _splashAge[i] += dt;
            float t = _splashAge[i] / SplashDuration;
            if (t >= 1f)
            {
                splash.gameObject.SetActive(false);
                _splashDelay[i] = Range(_config.SplashInterval);
                return;
            }
            splash.rectTransform.anchoredPosition = _splashFrom[i] + new Vector2(_splashSide[i] * 14f * t, Mathf.Sin(t * Mathf.PI) * 16f);
            splash.canvasRenderer.SetAlpha((1f - t) * 0.8f * _rain);
        }

        // ---------- Snow ----------

        private void UpdateSnow(float dt, Vector2 half)
        {
            if (_snow <= 0f)
            {
                SetActive(_flakes, false);
                return;
            }

            int count = Mathf.Min(_flakes.Length, Mathf.RoundToInt(_snow * _config.SnowflakeCount));
            for (int i = 0; i < _flakes.Length; i++)
            {
                Image flake = _flakes[i];
                RectTransform rect = flake.rectTransform;
                if (!flake.gameObject.activeSelf)
                {
                    if (i >= count)
                        continue;
                    SpawnFlake(i, half, Random.Range(-half.y, half.y));
                    flake.gameObject.SetActive(true);
                }

                float y = rect.anchoredPosition.y - _flakeSpeed[i] * dt;
                Vector2 sway = _flakeSway[i];
                float x = _flakeX[i] + Mathf.Sin(_time * sway.x + sway.y) * _config.SnowSway;
                rect.anchoredPosition = new Vector2(x, y);
                flake.canvasRenderer.SetAlpha(_config.SnowAlpha * Mathf.Min(1f, _snow * 2f));
                if (y >= -half.y - 20f)
                    continue;
                if (i < count)
                    SpawnFlake(i, half, half.y + Random.Range(10f, 60f));
                else
                    flake.gameObject.SetActive(false);
            }
        }

        private void SpawnFlake(int i, Vector2 half, float y)
        {
            _flakeX[i] = Random.Range(-half.x, half.x);
            _flakeSpeed[i] = Range(_config.SnowSpeed);
            _flakeSway[i] = new Vector2(TwoPi / Range(_config.SnowSwayPeriod), Random.value * TwoPi);
            _flakes[i].rectTransform.anchoredPosition = new Vector2(_flakeX[i], y);
        }

        private void UpdateDrifts()
        {
            bool visible = _drift > 0f;
            float height = Mathf.SmoothStep(0f, 1f, _drift);
            for (int i = 0; i < _drifts.Length; i++)
            {
                Image drift = _drifts[i];
                SetActive(drift, visible);
                if (!visible)
                    continue;
                drift.rectTransform.localScale = new Vector3(1f, Mathf.Max(0.05f, height), 1f);
                drift.canvasRenderer.SetAlpha(_config.DriftAlpha * Mathf.Min(1f, _drift * 3f));
            }
        }

        private static void SetActive(Image image, bool active)
        {
            if (image != null && image.gameObject.activeSelf != active)
                image.gameObject.SetActive(active);
        }

        private static void SetActive(Image[] group, bool active)
        {
            for (int i = 0; i < group.Length; i++)
                SetActive(group[i], active);
        }
    }
}
