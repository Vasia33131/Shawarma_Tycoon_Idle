using System;
using ShaurmaTycoon.Core.Settings;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace ShaurmaTycoon.Core.Audio
{
    /// <summary>
    /// Короткие UI-звуки: пул AudioSource по кругу, случайный клип и питч, ограничение частоты и числа одновременных копий.
    /// Отдельный источник с панорамой — для звука, который «едет» по экрану (машина, V6).
    /// На паузе игры (реклама, потеря фокуса) звук выключается.
    /// </summary>
    public sealed class AudioService : IDisposable
    {
        private const int SourceCount = 6;

        private readonly AudioSource[] _sources = new AudioSource[SourceCount];
        private readonly AudioSource _panned;
        private readonly SoundEntry[] _entries;
        private readonly float[] _lastPlayTime;
        // Время окончания звучащих копий, по звуку; null — без ограничения.
        private readonly float[][] _voiceEnds;
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
            _voiceEnds = new float[soundCount][];
            for (int i = 0; i < soundCount; i++)
                _lastPlayTime[i] = float.NegativeInfinity;

            if (config != null)
            {
                _masterVolume = config.MasterVolume;
                foreach (SoundEntry entry in config.Sounds)
                {
                    if (entry == null || (int)entry.Id >= soundCount)
                        continue;
                    _entries[(int)entry.Id] = entry;
                    if (entry.MaxVoices > 0)
                        _voiceEnds[(int)entry.Id] = new float[entry.MaxVoices];
                }
            }
            else
            {
                Debug.LogWarning($"{nameof(AudioService)}: AudioConfig is not assigned, sounds are off.");
            }

            _root = new GameObject(nameof(AudioService));
            for (int i = 0; i < SourceCount; i++)
                _sources[i] = CreateSource();
            _panned = CreateSource();

            GameEvents.GamePaused += OnGamePaused;
        }

        public void Play(SoundId id)
        {
            AudioClip clip = PickClip(id, out SoundEntry entry);
            if (clip == null)
                return;

            AudioSource source = _sources[_nextSource];
            _nextSource = (_nextSource + 1) % SourceCount;
            source.pitch = RandomPitch(entry);
            source.PlayOneShot(clip, entry.Volume * _masterVolume);
        }

        /// <summary>Звук на отдельном источнике с панорамой (−1 слева … 1 справа); прошлый такой звук обрывается.</summary>
        public void PlayPanned(SoundId id, float pan)
        {
            AudioClip clip = PickClip(id, out SoundEntry entry);
            if (clip == null)
                return;

            _panned.clip = clip;
            _panned.pitch = RandomPitch(entry);
            _panned.volume = entry.Volume * _masterVolume;
            _panned.panStereo = Mathf.Clamp(pan, -1f, 1f);
            _panned.Play();
        }

        /// <summary>Сдвинуть панораму звука из <see cref="PlayPanned"/>, пока он играет.</summary>
        public void SetPan(float pan)
        {
            _panned.panStereo = Mathf.Clamp(pan, -1f, 1f);
        }

        public void Dispose()
        {
            GameEvents.GamePaused -= OnGamePaused;
            if (_root != null)
                Object.Destroy(_root);
        }

        private AudioSource CreateSource()
        {
            AudioSource source = _root.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        private AudioClip PickClip(SoundId id, out SoundEntry entry)
        {
            entry = _entries[(int)id];
            if (_settings != null && !_settings.SoundOn)
                return null;
            if (entry == null || entry.Clips.Length == 0)
                return null;

            float now = Time.unscaledTime;
            if (now - _lastPlayTime[(int)id] < entry.MinInterval)
                return null;

            AudioClip clip = entry.Clips[Random.Range(0, entry.Clips.Length)];
            if (clip == null)
                return null;

            float[] voices = _voiceEnds[(int)id];
            if (voices != null && !TakeVoice(voices, now, clip.length))
                return null;

            _lastPlayTime[(int)id] = now;
            return clip;
        }

        // Свободная «копия» — та, что уже доиграла. Нет свободной — звук пропускается (при большом доходе монеты не сливаются в шум).
        private static bool TakeVoice(float[] voiceEnds, float now, float length)
        {
            for (int i = 0; i < voiceEnds.Length; i++)
            {
                if (voiceEnds[i] > now)
                    continue;
                voiceEnds[i] = now + length;
                return true;
            }
            return false;
        }

        private static float RandomPitch(SoundEntry entry)
        {
            return 1f + entry.PitchOffset + Random.Range(-entry.PitchJitter, entry.PitchJitter);
        }

        private static void OnGamePaused(bool paused)
        {
            AudioListener.pause = paused;
        }
    }
}
