using System;
using ShaurmaTycoon.Platform;

namespace ShaurmaTycoon.Save
{
    /// <summary>Хранилище через платформу: в редакторе — PlayerPrefs, на Яндексе — облако (этап 09).</summary>
    public sealed class PlatformSaveStorage : ISaveStorage
    {
        private readonly IPlatformService _platform;

        public PlatformSaveStorage(IPlatformService platform)
        {
            _platform = platform ?? throw new ArgumentNullException(nameof(platform));
        }

        public void Save(string json, Action<bool> onComplete)
        {
            _platform.Save(json, onComplete);
        }

        public void Load(Action<string> onLoaded)
        {
            _platform.Load(onLoaded);
        }
    }
}
