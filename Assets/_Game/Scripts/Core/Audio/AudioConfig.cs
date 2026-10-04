using System.Collections.Generic;
using UnityEngine;

namespace ShaurmaTycoon.Core.Audio
{
    [CreateAssetMenu(menuName = "ShaurmaTycoon/Audio Config", fileName = "Audio")]
    public sealed class AudioConfig : ScriptableObject
    {
        [Range(0f, 1f)]
        [SerializeField] private float _masterVolume = 0.8f;
        [SerializeField] private SoundEntry[] _sounds = new SoundEntry[0];

        public float MasterVolume => _masterVolume;
        public IReadOnlyList<SoundEntry> Sounds => _sounds;
    }
}
