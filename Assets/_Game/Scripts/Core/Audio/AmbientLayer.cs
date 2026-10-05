using UnityEngine;

namespace ShaurmaTycoon.Core.Audio
{
    /// <summary>
    /// Один слой эмбиента (V6): бесконечная петля на двух AudioSource. Записи с улицы не бывают бесшовными,
    /// поэтому за overlap секунд до конца клип запускается заново на втором источнике и стык проходит кроссфейдом.
    /// Смена клипа — тоже кроссфейд; громкость слоя плавно идёт к цели. Выключенный слой встаёт на паузу.
    /// </summary>
    public sealed class AmbientLayer
    {
        private readonly AudioSource[] _sources = new AudioSource[2];
        private readonly float[] _envelope = new float[2];
        private int _current;
        private AudioClip _clip;
        private float _crossfade = 1f;
        private float _envelopeSpeed = 1f;
        private float _overlap;
        private float _level;
        private float _target;
        private bool _paused;

        public AmbientLayer(GameObject host)
        {
            for (int i = 0; i < _sources.Length; i++)
            {
                AudioSource source = host.AddComponent<AudioSource>();
                source.loop = false;
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.volume = 0f;
                _sources[i] = source;
            }
        }

        public AudioClip Clip => _clip;

        /// <summary>Громкость слоя (0–1), к которой он плавно придёт за время кроссфейда.</summary>
        public float Volume
        {
            get => _target;
            set => _target = Mathf.Clamp01(value);
        }

        /// <param name="crossfade">Секунды на смену клипа и на изменение громкости.</param>
        /// <param name="overlap">Секунды перекрытия на стыке петли.</param>
        public void Configure(float crossfade, float overlap)
        {
            _crossfade = Mathf.Max(0.01f, crossfade);
            _overlap = Mathf.Max(0f, overlap);
        }

        /// <summary>Сменить клип кроссфейдом; null — слой затихает.</summary>
        public void SetClip(AudioClip clip)
        {
            if (clip == _clip)
                return;
            _clip = clip;
            _envelopeSpeed = 1f / _crossfade;
            if (clip != null)
                StartOnOther(clip);
        }

        /// <summary>Звук выключен в настройках: слой затихает и встаёт на паузу; включение продолжает с того же места.</summary>
        public void SetPaused(bool paused)
        {
            if (_paused == paused)
                return;
            _paused = paused;
            if (paused)
                return;
            for (int i = 0; i < _sources.Length; i++)
            {
                if (_sources[i].clip != null)
                    _sources[i].UnPause();
            }
        }

        public void Tick(float deltaTime)
        {
            float levelTarget = _paused ? 0f : _target;
            _level = Mathf.MoveTowards(_level, levelTarget, deltaTime / _crossfade);

            AudioSource current = _sources[_current];
            if (_clip != null && current.clip == _clip && !_paused && NeedsSeam(current))
            {
                _envelopeSpeed = 1f / _overlap;
                StartOnOther(_clip);
                current = _sources[_current];
            }

            for (int i = 0; i < _sources.Length; i++)
            {
                AudioSource source = _sources[i];
                if (source.clip == null)
                    continue;

                float envelopeTarget = i == _current && _clip != null ? 1f : 0f;
                _envelope[i] = Mathf.MoveTowards(_envelope[i], envelopeTarget, _envelopeSpeed * deltaTime);
                source.volume = _envelope[i] * _level;

                bool finished = !source.isPlaying && !_paused && !AudioListener.pause;
                if ((_envelope[i] <= 0f && envelopeTarget <= 0f) || finished)
                {
                    source.Stop();
                    source.clip = null;
                    _envelope[i] = 0f;
                }
            }

            if (_paused && _level <= 0f)
            {
                for (int i = 0; i < _sources.Length; i++)
                {
                    if (_sources[i].isPlaying)
                        _sources[i].Pause();
                }
            }
        }

        // Короткий клип (меньше трёх перекрытий) просто зациклен средствами Unity.
        private bool NeedsSeam(AudioSource source)
        {
            return !source.loop && source.isPlaying && source.time >= _clip.length - _overlap;
        }

        private void StartOnOther(AudioClip clip)
        {
            _current = 1 - _current;
            AudioSource source = _sources[_current];
            source.Stop();
            source.clip = clip;
            source.loop = _overlap <= 0f || clip.length < _overlap * 3f;
            source.time = 0f;
            source.volume = 0f;
            _envelope[_current] = 0f;
            source.Play();
            if (_paused)
                source.Pause();
        }
    }
}
