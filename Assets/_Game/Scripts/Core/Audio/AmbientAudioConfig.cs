using UnityEngine;

namespace ShaurmaTycoon.Core.Audio
{
    /// <summary>
    /// Звуковая атмосфера (V6): громкости и кроссфейды слоёв эмбиента и общие клипы (гриль, дождь, ветер).
    /// Основа (улица или море) и музыка — свои у каждого города, в CityConfig.
    /// Клипы — имена файлов в Audio/Ambient без расширения: в билд они идут отдельными MP3 (AudioClipStore).
    /// </summary>
    [CreateAssetMenu(menuName = "ShaurmaTycoon/Ambient Audio Config", fileName = "AmbientAudio")]
    public sealed class AmbientAudioConfig : ScriptableObject
    {
        [Header("Base (street / sea)")]
        [Range(0f, 1f)]
        [SerializeField] private float _baseVolume = 0.25f;
        [SerializeField] private float _baseCrossfade = 2f;

        [Header("Grill sizzle")]
        [SerializeField] private string _sizzleName = "";
        [Tooltip("Громкость гриля при пустой и при полной очереди; между ними — плавно.")]
        [SerializeField] private Vector2 _sizzleVolume = new Vector2(0.10f, 0.18f);
        [Tooltip("Секунды, за которые громкость гриля догоняет размер очереди.")]
        [SerializeField] private float _sizzleCrossfade = 4f;

        [Header("Weather")]
        [SerializeField] private string _rainName = "";
        [Tooltip("Для снега.")]
        [SerializeField] private string _windName = "";
        [Range(0f, 1f)]
        [SerializeField] private float _weatherVolume = 0.3f;
        [SerializeField] private float _weatherCrossfade = 3f;

        [Header("Loop")]
        [Tooltip("Секунды перекрытия на стыке петли: записи улицы не бесшовные, стык проходит кроссфейдом.")]
        [SerializeField] private float _loopOverlap = 2f;

        public float BaseVolume => _baseVolume;
        public float BaseCrossfade => _baseCrossfade;
        public string SizzleName => _sizzleName;
        public Vector2 SizzleVolume => _sizzleVolume;
        public float SizzleCrossfade => _sizzleCrossfade;
        public string RainName => _rainName;
        public string WindName => _windName;
        public float WeatherVolume => _weatherVolume;
        public float WeatherCrossfade => _weatherCrossfade;
        public float LoopOverlap => _loopOverlap;

        private void OnValidate()
        {
            _baseCrossfade = Mathf.Max(0.01f, _baseCrossfade);
            _sizzleCrossfade = Mathf.Max(0.01f, _sizzleCrossfade);
            _weatherCrossfade = Mathf.Max(0.01f, _weatherCrossfade);
            _loopOverlap = Mathf.Max(0f, _loopOverlap);
        }
    }
}
