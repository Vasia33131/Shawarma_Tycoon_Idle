using System;
using ShaurmaTycoon.Core;
using ShaurmaTycoon.Save;
using UnityEngine;

namespace ShaurmaTycoon.Platform
{
    /// <summary>
    /// Платформа Яндекс Игр (этап 09) поверх YandexBridge.
    /// Сейв пишется локально (localStorage) сразу и в облако игрока (player.setData) не чаще раза в CloudSaveInterval;
    /// при загрузке берётся более свежий из двух. Если облако недоступно — работает только локальный.
    /// </summary>
    public sealed class YandexPlatformService : IPlatformService, ITickable
    {
        public const string LocalSaveKey = "shaurma_tycoon_save";

        /// <summary>Лимит платформы на setData/getData — 100 запросов за 5 минут; 8 с (автосейв раз в 10 с).</summary>
        public const float CloudSaveInterval = 8f;

        private const float InitTimeout = 10f;

        private readonly YandexBridge _bridge;
        private Action _onReady;
        private float _initTimer;
        private bool _cloudAvailable;
        private bool _gameReadySent;
        private TimeSpan _serverOffset = TimeSpan.Zero;

        private string _pendingCloudJson;
        private bool _cloudSaveInFlight;
        private float _lastCloudSaveAt = float.NegativeInfinity;

        public YandexPlatformService(YandexBridge bridge)
        {
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
        }

        public bool IsInitialized { get; private set; }
        public string Language { get; private set; } = "ru";
        public DateTime ServerTimeUtc => DateTime.UtcNow + _serverOffset;

        public void Init(Action onReady)
        {
            _onReady = onReady;
            _initTimer = 0f;
            _bridge.Init(status =>
            {
                _cloudAvailable = status == "ok";
                if (status == "fail")
                    Debug.LogWarning("[Yandex] Player is unavailable, cloud saves are off.");
                Complete();
            });
        }

        private void Complete()
        {
            if (IsInitialized)
                return;

            IsInitialized = true;
            Language = _bridge.GetLanguage();

            double serverMs = _bridge.GetServerTimeMs();
            double localMs = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
            _serverOffset = serverMs > 0 ? TimeSpan.FromMilliseconds(serverMs - localMs) : TimeSpan.Zero;

            Action callback = _onReady;
            _onReady = null;
            callback?.Invoke();
        }

        public void GameReady()
        {
            if (_gameReadySent)
                return;

            _gameReadySent = true;
            _bridge.GameReady();
        }

        public void ShowRewarded(Action<bool> onComplete)
        {
            _bridge.ShowRewarded(status => onComplete?.Invoke(status == "ok"));
        }

        public void ShowInterstitial(Action onClosed)
        {
            _bridge.ShowInterstitial(_ => onClosed?.Invoke());
        }

        public void Save(string json, Action<bool> onComplete)
        {
            _bridge.LocalSet(LocalSaveKey, json);
            if (_cloudAvailable)
            {
                _pendingCloudJson = json;
                TrySendCloud();
            }

            onComplete?.Invoke(true);
        }

        public void Load(Action<string> onLoaded)
        {
            string local = _bridge.LocalGet(LocalSaveKey);
            if (!_cloudAvailable)
            {
                onLoaded?.Invoke(SaveSelector.Pick(null, local));
                return;
            }

            _bridge.LoadCloud((status, cloud) => onLoaded?.Invoke(SaveSelector.Pick(status == "ok" ? cloud : null, local)));
        }

        public void SetLeaderboardScore(string leaderboardId, double score)
        {
            _bridge.SetLeaderboardScore(leaderboardId, score);
        }

        public void Purchase(string productId, Action<bool> onComplete)
        {
            _bridge.Purchase(productId, status => onComplete?.Invoke(status == "ok"));
        }

        public void GetPurchases(Action<string[]> onLoaded)
        {
            _bridge.GetPurchases((status, ids) =>
                onLoaded?.Invoke(status == "ok" && !string.IsNullOrEmpty(ids) ? ids.Split(',') : new string[0]));
        }

        public void ConsumePurchase(string productId)
        {
            _bridge.ConsumePurchase(productId);
        }

        /// <summary>Отправить ожидающий сейв в облако сразу (окно теряет фокус, игра закрывается).</summary>
        public void FlushCloud()
        {
            TrySendCloud(force: true);
        }

        public void Tick(float deltaTime)
        {
            if (!IsInitialized)
            {
                _initTimer += deltaTime;
                if (_initTimer >= InitTimeout)
                {
                    Debug.LogWarning("[Yandex] SDK init timed out, starting without cloud.");
                    _cloudAvailable = false;
                    Complete();
                }
                return;
            }

            if (_pendingCloudJson != null)
                TrySendCloud();
        }

        private void TrySendCloud(bool force = false)
        {
            if (_cloudSaveInFlight || _pendingCloudJson == null || (!force && Time.realtimeSinceStartup - _lastCloudSaveAt < CloudSaveInterval))
                return;

            string json = _pendingCloudJson;
            _pendingCloudJson = null;
            _cloudSaveInFlight = true;
            _lastCloudSaveAt = Time.realtimeSinceStartup;
            _bridge.SaveCloud(json, status =>
            {
                _cloudSaveInFlight = false;
                if (status != "ok")
                    Debug.LogWarning("[Yandex] Cloud save failed, local copy is kept.");
            });
        }
    }
}
