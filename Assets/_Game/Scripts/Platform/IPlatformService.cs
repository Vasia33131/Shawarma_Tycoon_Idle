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

        /// <summary>Игра встала на паузу (реклама, скрытая вкладка) или продолжилась — разметка геймплея (GameplayAPI start/stop).</summary>
        void SetGameplayPaused(bool paused);

        /// <summary>Каталог товаров с ценами в валюте портала. onLoaded(null) — каталог недоступен.</summary>
        void GetCatalog(Action<ProductPrice[]> onLoaded);

        /// <param name="onComplete">true — реклама досмотрена, награду выдать.</param>
        void ShowRewarded(Action<bool> onComplete);

        /// <summary>Полноэкранная реклама открылась (true) или закрылась (false): игра и звук на это время на паузе.</summary>
        event Action<bool> AdShowing;

        void ShowInterstitial(Action onClosed);

        void Save(string json, Action<bool> onComplete);

        /// <param name="onLoaded">JSON сохранения или null, если сохранения нет.</param>
        void Load(Action<string> onLoaded);

        /// <summary>Отправить результат в лидерборд. Платформа может отказать неавторизованному игроку — это не ошибка.</summary>
        void SetLeaderboardScore(string leaderboardId, double score);

        /// <summary>Оплата товара. onComplete(true) — оплачено; расходуемый товар после выдачи и сохранения надо подтвердить ConsumePurchase.</summary>
        void Purchase(string productId, Action<bool> onComplete);

        /// <summary>Id купленных, но не подтверждённых товаров (расходуемые — не выданные; постоянные — все купленные).</summary>
        void GetPurchases(Action<string[]> onLoaded);

        /// <summary>Подтвердить расходуемую покупку. Вызывать только после выдачи награды и сохранения.</summary>
        void ConsumePurchase(string productId);
    }
}
