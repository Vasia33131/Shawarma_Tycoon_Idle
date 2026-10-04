using System;

namespace ShaurmaTycoon.Platform
{
    /// <summary>
    /// Всё, что зависит от платформы: реклама, сохранения, язык, время, лидерборды, покупки.
    /// Игровой код работает только с этим интерфейсом и не знает про Яндекс.
    /// </summary>
    public interface IPlatformService
    {
        bool IsInitialized { get; }

        /// <summary>Код языка игрока: "ru" или "en".</summary>
        string Language { get; }

        /// <summary>Текущее время UTC (серверное, если платформа его даёт).</summary>
        DateTime ServerTimeUtc { get; }

        void Init(Action onReady);

        /// <summary>Сообщить платформе, что игра загружена и готова к игре.</summary>
        void GameReady();

        /// <param name="onComplete">true — реклама досмотрена, награду выдать.</param>
        void ShowRewarded(Action<bool> onComplete);

        void ShowInterstitial(Action onClosed);

        void Save(string json, Action<bool> onComplete);

        /// <param name="onLoaded">JSON сохранения или null, если сохранения нет.</param>
        void Load(Action<string> onLoaded);

        // TODO(YANDEX): уточнить сигнатуры лидерборда и покупок на этапе 09.
        void SetLeaderboardScore(string leaderboardId, double score);

        void Purchase(string productId, Action<bool> onComplete);
    }
}
