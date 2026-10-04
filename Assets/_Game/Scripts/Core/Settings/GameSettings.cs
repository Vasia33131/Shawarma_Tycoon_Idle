using System;

namespace ShaurmaTycoon.Core.Settings
{
    /// <summary>Настройки игрока (общие для всех городов): звуки и музыка. По умолчанию всё включено.</summary>
    public sealed class GameSettings
    {
        /// <summary>Изменилась любая настройка.</summary>
        public event Action Changed;

        public bool SoundOn { get; private set; } = true;
        public bool MusicOn { get; private set; } = true;

        public void SetSound(bool on)
        {
            if (SoundOn == on)
                return;
            SoundOn = on;
            Changed?.Invoke();
        }

        public void SetMusic(bool on)
        {
            if (MusicOn == on)
                return;
            MusicOn = on;
            Changed?.Invoke();
        }

        public void Load(bool soundOn, bool musicOn)
        {
            SoundOn = soundOn;
            MusicOn = musicOn;
            Changed?.Invoke();
        }
    }
}
