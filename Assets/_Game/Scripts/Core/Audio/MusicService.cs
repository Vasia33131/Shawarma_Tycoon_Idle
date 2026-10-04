using System;
using UnityEngine;

namespace ShaurmaTycoon.Core.Audio
{
    /// <summary>
    /// Фоновая музыка (живёт в Boot, переживает перезагрузку сцены Game): один зацикленный AudioSource,
    /// плавное появление, смена трека через затухание. Повторный Play того же трека ничего не делает,
    /// поэтому переезд в новый город музыку не обрывает. На паузе игры (реклама, потеря фокуса) — тишина.
    /// </summary>
    public sealed class MusicService : ITickable, IDisposable
    {
        private readonly AudioSource _source;
        private AudioClip _pendingClip;
        private float _volume;
        private float _fadeDuration = 1f;
        private bool _muted;
        private bool _pausedByMute;

        public MusicService(GameObject host)
        {
            _source = host.AddComponent<AudioSource>();
            _source.loop = true;
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _source.volume = 0f;
            GameEvents.GamePaused += OnGamePaused;
        }

        public AudioClip CurrentClip => _pendingClip != null ? _pendingClip : _source.clip;

        /// <summary>Выключатель музыки (настройки): затухает и встаёт на паузу, включение продолжает с того же места.</summary>
        public bool Muted
        {
            get => _muted;
            set
            {
                _muted = value;
                if (_muted || _source.clip == null || _source.isPlaying)
                    return;

                if (_pausedByMute)
                    _source.UnPause();
                else
                    _source.Play();
                _pausedByMute = false;
            }
        }

        public void Play(AudioClip clip, float volume, float fadeDuration)
        {
            if (clip == null)
                return;

            _volume = Mathf.Clamp01(volume);
            _fadeDuration = Mathf.Max(0.01f, fadeDuration);
            if (clip == CurrentClip)
                return;

            if (_source.clip == null || _source.volume <= 0f)
                StartClip(clip);
            else
                _pendingClip = clip;
        }

        public void Tick(float deltaTime)
        {
            float target = _pendingClip != null || _muted ? 0f : _volume;
            float step = _volume > 0f ? _volume / _fadeDuration * deltaTime : 1f;
            _source.volume = Mathf.MoveTowards(_source.volume, target, step);

            if (_source.volume > 0f)
                return;

            if (_pendingClip != null)
            {
                StartClip(_pendingClip);
                _pendingClip = null;
            }
            else if (_muted && _source.isPlaying)
            {
                _source.Pause();
                _pausedByMute = true;
            }
        }

        public void Dispose()
        {
            GameEvents.GamePaused -= OnGamePaused;
        }

        private void StartClip(AudioClip clip)
        {
            _source.clip = clip;
            _source.volume = 0f;
            _pausedByMute = false;
            if (!_muted)
                _source.Play();
        }

        private static void OnGamePaused(bool paused)
        {
            AudioListener.pause = paused;
        }
    }
}
