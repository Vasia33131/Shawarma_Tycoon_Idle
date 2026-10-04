using System;
using UnityEngine;

namespace ShaurmaTycoon.Core.Audio
{
    /// <summary>Настройки одного звука: варианты клипов, громкость, разброс высоты и минимальный интервал.</summary>
    [Serializable]
    public sealed class SoundEntry
    {
        public SoundId Id;
        [Tooltip("Играется случайный из списка.")]
        public AudioClip[] Clips = new AudioClip[0];
        [Range(0f, 1f)] public float Volume = 1f;
        [Range(0f, 0.3f)] public float PitchJitter = 0.05f;
        [Tooltip("Не чаще одного раза за столько секунд (защита от «каши» на быстрых станциях).")]
        public float MinInterval = 0.05f;
    }
}
