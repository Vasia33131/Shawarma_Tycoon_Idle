using System;
using ShaurmaTycoon.Save;

namespace ShaurmaTycoon.Core.Settings
{
    /// <summary>Сохраняет GameSettings в SaveData.Settings. Живёт в Boot.</summary>
    public sealed class SettingsSaveParticipant : ISaveParticipant
    {
        private readonly GameSettings _settings;

        public SettingsSaveParticipant(GameSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public void Capture(SaveData data)
        {
            SettingsSave save = data.Settings ?? (data.Settings = new SettingsSave());
            save.SoundOn = _settings.SoundOn;
            save.MusicOn = _settings.MusicOn;
            save.EffectsOn = _settings.EffectsOn;
        }

        public void Apply(SaveData data)
        {
            SettingsSave save = data.Settings ?? new SettingsSave();
            _settings.Load(save.SoundOn, save.MusicOn, save.EffectsOn);
        }
    }
}
