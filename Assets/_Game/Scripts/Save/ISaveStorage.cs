using System;

namespace ShaurmaTycoon.Save
{
    /// <summary>Где лежит строка сохранения (PlayerPrefs, облако платформы…).</summary>
    public interface ISaveStorage
    {
        void Save(string json, Action<bool> onComplete);

        /// <param name="onLoaded">JSON или null, если сохранения нет.</param>
        void Load(Action<string> onLoaded);
    }
}
