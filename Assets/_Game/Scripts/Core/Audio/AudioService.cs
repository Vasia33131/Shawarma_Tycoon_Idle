using System;
using ShaurmaTycoon.Core.Settings;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace ShaurmaTycoon.Core.Audio
{
    /// <summary>
    /// Короткие UI-звуки: пул AudioSource по кругу, случайный клип и питч, ограничение частоты.
    /// На паузе игры (реклама, потеря фокуса) звук выключается.
    /// </summary>
    public sealed class AudioService : IDisposable
    {
        private const int SourceCount = 6;

        private readonly AudioSource[] _sources = new AudioSource[SourceCount];
        private readonly SoundEntry[] _entries;
        private readonly float[] _lastPlayTime;
        private readonly float _masterVolume;
        private readonly GameObject _root;
        private readonly GameSettings _settings;
        private int _nextSource;

        /// <param name="settings">Выключатель звуков; null — звуки всегда включены.</param>
        public AudioService(AudioConfig config, GameSettings settings = null)
        {
            _settings = settings;
            int soundCount = Enum.GetValues(typeof(SoundId)).Length;
            _entries = new SoundEntry[soundCount];
            _lastPlayTime = new float[soundCount];
            for (int i = 0; i < soundCount; i++)
                _lastPlayTime[i] = float.NegativeInfinity;

            if (config != null)
            {
                _masterVolume = config.MasterVolume;
                foreach (SoundEntry entry in config.Sounds)
                {
                    if (entry != null && (int)entry.Id < soundCount)
                        _entries[(int)entry.Id] = entry;
                }
            }
            else
            {
                Debug.LogWarning($"{nameof(AudioService)}: AudioConfig is not assigned, sounds are off.");
            }

            _root = new GameObject(nameof(AudioService));
            for (int i = 0; i < SourceCount; i++)
            {
                AudioSource source = _root.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                _sources[i] = source;
            }

            GameEvents.GamePaused += OnGamePaused;
        }

        public void Play(SoundId id)
        {
            if (_settings != null && !_settings.SoundOn)
                return;

            SoundEntry entry = _entries[(int)id];
            if (entry == null || entry.Clips.Length == 0)
                return;

            float now = Time.unscaledTime;
            if (now - _lastPlayTime[(int)id] < entry.MinInterval)
                return;
            _lastPlayTime[(int)id] = now;

            AudioClip clip = entry.Clips[Random.Range(0, entry.Clips.Length)];
            if (clip == null)
                return;

            AudioSource source = _sources[_nextSource];
            _nextSource = (_nextSource + 1) % SourceCount;
            source.pitch = 1f + Random.Range(-entry.PitchJitter, entry.PitchJitter);
            source.PlayOneShot(clip, entry.Volume * _masterVolume);
        }

        public void Dispose()
        {
            GameEvents.GamePaused -= OnGamePaused;
            if (_root != null)
                Object.Destroy(_root);
        }

        private static void OnGamePaused(bool paused)
        {
            AudioListener.pause = paused;
        }
    }
}
