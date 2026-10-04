using UnityEngine;

namespace ShaurmaTycoon.Core.Audio
{
    /// <summary>Фоновая музыка: трек, громкость и длительность плавного перехода.</summary>
    [CreateAssetMenu(menuName = "ShaurmaTycoon/Music Config", fileName = "Music")]
    public sealed class MusicConfig : ScriptableObject
    {
        [SerializeField] private AudioClip _track = null;
        [Range(0f, 1f)]
        [SerializeField] private float _volume = 0.45f;
        [Tooltip("Секунды на появление и затухание музыки.")]
        [SerializeField] private float _fadeDuration = 1.5f;

        public AudioClip Track => _track;
        public float Volume => _volume;
        public float FadeDuration => _fadeDuration;

        private void OnValidate()
        {
            if (_fadeDuration < 0.01f)
                _fadeDuration = 0.01f;
        }
    }
}
