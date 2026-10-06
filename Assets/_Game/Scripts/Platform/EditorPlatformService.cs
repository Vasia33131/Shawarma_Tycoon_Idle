using System;
using UnityEngine;

namespace ShaurmaTycoon.Platform
{
    /// <summary>
    /// Заглушка платформы для редактора: реклама сразу «досмотрена», сохранения в PlayerPrefs.
    /// Язык — из меню ShaurmaTycoon/Editor Language (в билде язык даёт SDK, см. YandexPlatformService).
    /// </summary>
    public sealed class EditorPlatformService : IPlatformService
    {
        /// <summary>Ключ PlayerPrefs с сохранением (меню ShaurmaTycoon/Save).</summary>
        public const string SaveKey = "shaurma_tycoon_save";
        private const string DefaultLanguage = "ru";

        /// <summary>Ключ EditorPrefs с языком редактора (меню ShaurmaTycoon/Editor Language).</summary>
        public const string EditorLanguagePrefKey = "ShaurmaTycoon.EditorLanguage";

        public bool IsInitialized { get; private set; }

        public string Language
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.EditorPrefs.GetString(EditorLanguagePrefKey, DefaultLanguage);
#else
                return DefaultLanguage;
#endif
            }
        }

        public DateTime ServerTimeUtc => DateTime.UtcNow;

        public void Init(Action onReady)
        {
            IsInitialized = true;
            onReady?.Invoke();
        }

        public void GameReady()
        {
            Debug.Log("[Platform] GameReady");
        }

        public void SetGameplayPaused(bool paused)
        {
        }

        public void GetCatalog(Action<ProductPrice[]> onLoaded)
        {
            onLoaded?.Invoke(null);
        }

        // Реклама в редакторе закрывается мгновенно — паузы нет.
        public event Action<bool> AdShowing
        {
            add { }
            remove { }
        }

        public void ShowRewarded(Action<bool> onComplete)
        {
            Debug.Log("[Platform] Rewarded shown (editor stub)");
            onComplete?.Invoke(true);
        }

        public void ShowInterstitial(Action onClosed)
        {
            Debug.Log("[Platform] Interstitial shown (editor stub)");
            onClosed?.Invoke();
        }

        public void Save(string json, Action<bool> onComplete)
        {
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
            onComplete?.Invoke(true);
        }

        public void Load(Action<string> onLoaded)
        {
            onLoaded?.Invoke(PlayerPrefs.HasKey(SaveKey) ? PlayerPrefs.GetString(SaveKey) : null);
        }

        public void SetLeaderboardScore(string leaderboardId, double score)
        {
            Debug.Log($"[Platform] Leaderboard '{leaderboardId}' = {score}");
        }

        public void Purchase(string productId, Action<bool> onComplete)
        {
            Debug.Log($"[Platform] Purchase '{productId}' (editor stub)");
            onComplete?.Invoke(true);
        }

        public void GetPurchases(Action<string[]> onLoaded)
        {
            onLoaded?.Invoke(new string[0]);
        }

        public void ConsumePurchase(string productId)
        {
            Debug.Log($"[Platform] Consume '{productId}' (editor stub)");
        }
    }
}
