using System;
using ShaurmaTycoon.Core.Settings;
using UnityEngine;

namespace ShaurmaTycoon.Core.Audio
{
    /// <summary>
    /// Эмбиент под музыкой (V6), живёт в Boot рядом с MusicService: при переезде основа (улица → море)
    /// меняется кроссфейдом, без тишины. Слои: основа города, шипение гриля (громче при длинной очереди), погода.
    /// Звуки города — часть фона: их выключает и «Звук», и «Эффекты фона». Пауза игры глушит их через AudioListener.pause.
    /// </summary>
    public sealed class AmbientAudioService : ITickable, IDisposable
    {
        private const float MaxDeltaTime = 0.1f;

        private readonly AmbientLayer _base;
        private readonly AmbientLayer _sizzle;
        private readonly AmbientLayer _weather;
        private readonly GameSettings _settings;
        private AmbientAudioConfig _config;
        private Func<float> _queueFullness;
        private float _fullness;

        public AmbientAudioService(GameObject host, GameSettings settings)
        {
            _settings = settings;
            _base = new AmbientLayer(host);
            _sizzle = new AmbientLayer(host);
            _weather = new AmbientLayer(host);
            if (_settings != null)
                _settings.Changed += ApplySettings;
            ApplySettings();
        }

        /// <summary>
        /// Город загрузился: громкости и кроссфейды слоёв. Клипы приходят отдельно (SetBase, SetSizzle, SetWeather) —
        /// по мере загрузки файлов звука; до тех пор играет прежняя основа, и смена идёт кроссфейдом.
        /// </summary>
        public void Apply(AmbientAudioConfig config)
        {
            _config = config;
            if (config == null)
            {
                _base.SetClip(null);
                _sizzle.SetClip(null);
                _weather.SetClip(null);
                return;
            }

            _base.Configure(config.BaseCrossfade, config.LoopOverlap);
            _base.Volume = config.BaseVolume;

            _sizzle.Configure(config.BaseCrossfade, config.LoopOverlap);
            _sizzle.Volume = SizzleVolume();

            _weather.Configure(config.WeatherCrossfade, config.LoopOverlap);
            _weather.Volume = config.WeatherVolume;
        }

        /// <summary>Основа города (улица или прибой); null — слой затихает.</summary>
        public void SetBase(AudioClip clip)
        {
            _base.SetClip(clip);
        }

        /// <summary>Шипение гриля; null — слой затихает.</summary>
        public void SetSizzle(AudioClip clip)
        {
            _sizzle.SetClip(clip);
        }

        /// <summary>Дождь, ветер или null (ясно); переход — за время кроссфейда погоды, вместе с визуалом.</summary>
        public void SetWeather(AudioClip clip)
        {
            _weather.SetClip(clip);
        }

        /// <summary>Заполненность очереди 0–1 (сцена Game); null — гриль на минимуме.</summary>
        public void SetQueueSource(Func<float> queueFullness)
        {
            _queueFullness = queueFullness;
        }

        public void Tick(float deltaTime)
        {
            // Время реальное: ускорение игры из отладочного окна не должно ускорять кроссфейды.
            float dt = Mathf.Min(Time.unscaledDeltaTime, MaxDeltaTime);
            if (_config != null)
            {
                float fullness = _queueFullness != null ? Mathf.Clamp01(_queueFullness()) : 0f;
                _fullness = Mathf.MoveTowards(_fullness, fullness, dt / _config.SizzleCrossfade);
                _sizzle.Volume = SizzleVolume();
            }

            _base.Tick(dt);
            _sizzle.Tick(dt);
            _weather.Tick(dt);
        }

        public void Dispose()
        {
            if (_settings != null)
                _settings.Changed -= ApplySettings;
        }

        private float SizzleVolume()
        {
            Vector2 range = _config.SizzleVolume;
            return Mathf.Lerp(range.x, range.y, _fullness);
        }

        private void ApplySettings()
        {
            bool paused = _settings != null && (!_settings.SoundOn || !_settings.EffectsOn);
            _base.SetPaused(paused);
            _sizzle.SetPaused(paused);
            _weather.SetPaused(paused);
        }
    }
}
