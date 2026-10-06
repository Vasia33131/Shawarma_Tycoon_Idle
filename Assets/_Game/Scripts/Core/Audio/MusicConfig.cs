using UnityEngine;

namespace ShaurmaTycoon.Core.Audio
{
    /// <summary>
    /// Фоновая музыка: запасной трек (если у города своего нет), громкость и длительность плавного перехода.
    /// Трек города — CityConfig.MusicName (V6). Треки — имена файлов в Audio/Music: в билд они идут отдельными MP3 (AudioClipStore).
    /// </summary>
    [CreateAssetMenu(menuName = "ShaurmaTycoon/Music Config", fileName = "Music")]
    public sealed class MusicConfig : ScriptableObject
    {
        [Tooltip("Запасной трек: имя файла в Audio/Music без расширения. Играет в городе без своей музыки.")]
        [SerializeField] private string _trackName = "";
        [Range(0f, 1f)]
        [SerializeField] private float _volume = 0.35f;
        [Tooltip("Секунды на появление музыки и кроссфейд при переезде.")]
        [SerializeField] private float _fadeDuration = 2f;

        public string TrackName => _trackName;
        public float Volume => _volume;
        public float FadeDuration => _fadeDuration;

        private void OnValidate()
        {
            if (_fadeDuration < 0.01f)
                _fadeDuration = 0.01f;
        }
    }
}
