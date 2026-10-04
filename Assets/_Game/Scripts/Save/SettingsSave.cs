using System;

namespace ShaurmaTycoon.Save
{
    /// <summary>Сохранённые настройки. Старые сейвы без этого блока получают значения по умолчанию — всё включено.</summary>
    [Serializable]
    public sealed class SettingsSave
    {
        public bool SoundOn = true;
        public bool MusicOn = true;
    }
}
