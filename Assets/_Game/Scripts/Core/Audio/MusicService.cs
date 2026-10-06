using System;
using UnityEngine;

namespace ShaurmaTycoon.Core.Audio
{
    /// <summary>
    /// Фоновая музыка (живёт в Boot, переживает перезагрузку сцены Game): два зацикленных AudioSource.
    /// Новый трек (переезд в другой город, V6) звучит поверх старого — кроссфейд без тишины между ними.
    /// Повторный Play того же трека ничего не делает. На паузе игры (реклама, потеря фокуса) — тишина.
    /// </summary>
    public sealed class MusicService : ITickable, IDisposable
    {
        private readonly AudioSource[] _sources = new AudioSource[2];
        private readonly Action<AudioClip> _releaseClip;
        private int _current;
        private float _volume;
        private float _fadeDuration = 1f;
        private bool _muted;
        private bool _pausedByMute;
        private float _volumeScale = 1f;

        /// <param name="releaseClip">Трек отыграл и затих (переезд): освободить память. null — только выгрузить данные клипа.</param>
        public MusicService(GameObject host, Action<AudioClip> releaseClip = null)
        {
            _releaseClip = releaseClip;
            for (int i = 0; i < _sources.Length; i++)
            {
                AudioSource source = host.AddComponent<AudioSource>();
                source.loop = true;
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.volume = 0f;
                _sources[i] = source;
            }
            GameEvents.GamePaused += OnGamePaused;
        }

        public AudioClip CurrentClip => Current.clip;

        private AudioSource Current => _sources[_current];

        private AudioSource Previous => _sources[1 - _current];

        /// <summary>Выключатель музыки (настройки): затухает и встаёт на паузу, включение продолжает с того же места.</summary>
        public bool Muted
        {
            get => _muted;
            set
            {
                _muted = value;
                if (_muted || Current.clip == null || Current.isPlaying)
                    return;

                if (_pausedByMute)
                    Current.UnPause();
                else
                    Current.Play();
                _pausedByMute = false;
            }
        }

        /// <summary>Множитель громкости (режим любования — тише на 15%); меняется плавно, за время перехода.</summary>
        public float VolumeScale
        {
            get => _volumeScale;
            set => _volumeScale = Mathf.Clamp01(value);
        }

        /// <param name="fadeDuration">Секунды на появление нового трека (и затухание старого).</param>
        public void Play(AudioClip clip, float volume, float fadeDuration)
        {
            if (clip == null)
                return;

            _volume = Mathf.Clamp01(volume);
            _fadeDuration = Mathf.Max(0.01f, fadeDuration);
            if (clip == CurrentClip)
                return;

            // Старый трек доигрывает на втором источнике и затухает; если там ещё затухал третий — он обрывается.
            _current = 1 - _current;
            AudioSource source = Current;
            source.Stop();
            source.clip = clip;
            source.volume = 0f;
            _pausedByMute = false;
            if (!_muted)
                source.Play();
        }

        public void Tick(float deltaTime)
        {
            float step = _volume > 0f ? _volume / _fadeDuration * deltaTime : 1f;

            AudioSource current = Current;
            current.volume = Mathf.MoveTowards(current.volume, _muted ? 0f : _volume * _volumeScale, step);
            if (_muted && current.volume <= 0f && current.isPlaying)
            {
                current.Pause();
                _pausedByMute = true;
            }

            AudioSource previous = Previous;
            if (previous.clip == null)
                return;
            previous.volume = Mathf.MoveTowards(previous.volume, 0f, step);
            if (previous.volume > 0f)
                return;
            previous.Stop();
            AudioClip old = previous.clip;
            previous.clip = null;
            // В WebGL трек распакован браузером целиком (~50 МБ) — после затухания освобождаем, при возврате загрузится снова.
            if (old != CurrentClip)
            {
                if (_releaseClip != null)
                    _releaseClip(old);
                else
                    old.UnloadAudioData();
            }
        }

        public void Dispose()
        {
            GameEvents.GamePaused -= OnGamePaused;
        }

        private static void OnGamePaused(bool paused)
        {
            AudioListener.pause = paused;
        }
    }
}
