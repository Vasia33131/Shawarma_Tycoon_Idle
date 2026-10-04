using System;
using UnityEngine;

namespace ShaurmaTycoon.Platform
{
    /// <summary>
    /// Заглушка платформы для редактора: реклама сразу «досмотрена», сохранения в PlayerPrefs.
    /// </summary>
    public sealed class EditorPlatformService : IPlatformService
    {
        private const string SaveKey = "shaurma_tycoon_save";

        public bool IsInitialized { get; private set; }

        public string Language => "ru";

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
    }
}
