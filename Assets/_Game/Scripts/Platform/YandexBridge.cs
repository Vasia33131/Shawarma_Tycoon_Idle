using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ShaurmaTycoon.Platform
{
    /// <summary>
    /// Связь с Yandex Games SDK через YandexBridge.jslib. Объект называется "YandexBridge" — туда JS шлёт SendMessage.
    /// Асинхронные вызовы получают id, ответ приходит строкой «id|статус|данные»; id 0 — события платформы.
    /// </summary>
    public sealed class YandexBridge : MonoBehaviour
    {
        public const string ObjectName = "YandexBridge";

        private readonly Dictionary<int, Action<string, string>> _requests = new Dictionary<int, Action<string, string>>(8);
        private int _nextId = 1;

        /// <summary>Платформа просит поставить игру на паузу (true) или продолжить (false): реклама, свёрнутая вкладка.</summary>
        public event Action<bool> PauseRequested;

        /// <summary>Вкладка скрыта (false) или снова видима (true).</summary>
        public event Action<bool> VisibilityChanged;

        public static YandexBridge Create()
        {
            var go = new GameObject(ObjectName);
            DontDestroyOnLoad(go);
            return go.AddComponent<YandexBridge>();
        }

        // ---------- Вызовы ----------

        public void Init(Action<string> onResult) => Call(id => Native.Init(id), (status, _) => onResult(status));
        public void LoadCloud(Action<string, string> onResult) => Call(id => Native.LoadCloud(id), onResult);
        public void SaveCloud(string json, Action<string> onResult) => Call(id => Native.SaveCloud(id, json), (status, _) => onResult(status));
        public void ShowRewarded(Action<string> onResult) => Call(id => Native.ShowRewarded(id), (status, _) => onResult(status));
        public void ShowInterstitial(Action<string> onResult) => Call(id => Native.ShowInterstitial(id), (status, _) => onResult(status));
        public void Purchase(string productId, Action<string> onResult) => Call(id => Native.Purchase(id, productId), (status, _) => onResult(status));
        public void GetPurchases(Action<string, string> onResult) => Call(id => Native.GetPurchases(id), onResult);

        public string GetLanguage() => Native.GetLanguage();
        public double GetServerTimeMs() => Native.GetServerTimeMs();
        public void GameReady() => Native.GameReady();
        public void SetLeaderboardScore(string leaderboardId, double score) => Native.SetLeaderboardScore(leaderboardId, score);
        public void ConsumePurchase(string productId) => Native.ConsumePurchase(productId);
        public void LocalSet(string key, string value) => Native.LocalSet(key, value);
        public string LocalGet(string key) => Native.LocalGet(key);

        private void Call(Action<int> native, Action<string, string> onResult)
        {
            int id = _nextId++;
            _requests[id] = onResult;
            native(id);
        }

        /// <summary>Вызывается из JS (SendMessage).</summary>
        public void OnResult(string message)
        {
            int first = message.IndexOf('|');
            int second = first < 0 ? -1 : message.IndexOf('|', first + 1);
            if (second < 0 || !int.TryParse(message.Substring(0, first), out int id))
            {
                Debug.LogWarning($"[Yandex] Unexpected bridge message: {message}");
                return;
            }

            string status = message.Substring(first + 1, second - first - 1);
            string payload = message.Substring(second + 1);

            if (id == 0)
            {
                if (status == "pause")
                    PauseRequested?.Invoke(true);
                else if (status == "resume")
                    PauseRequested?.Invoke(false);
                else if (status == "blur")
                    VisibilityChanged?.Invoke(false);
                else if (status == "focus")
                    VisibilityChanged?.Invoke(true);
                return;
            }

            if (!_requests.TryGetValue(id, out Action<string, string> callback))
                return;

            _requests.Remove(id);
            try
            {
                callback(status, payload);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static class Native
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            [DllImport("__Internal")] private static extern void YB_Init(int id);
            [DllImport("__Internal")] private static extern IntPtr YB_GetLanguage();
            [DllImport("__Internal")] private static extern double YB_GetServerTimeMs();
            [DllImport("__Internal")] private static extern void YB_GameReady();
            [DllImport("__Internal")] private static extern void YB_LocalSet(string key, string value);
            [DllImport("__Internal")] private static extern IntPtr YB_LocalGet(string key);
            [DllImport("__Internal")] private static extern void YB_LoadCloud(int id);
            [DllImport("__Internal")] private static extern void YB_SaveCloud(int id, string json);
            [DllImport("__Internal")] private static extern void YB_ShowRewarded(int id);
            [DllImport("__Internal")] private static extern void YB_ShowInterstitial(int id);
            [DllImport("__Internal")] private static extern void YB_SetLeaderboardScore(string name, double score);
            [DllImport("__Internal")] private static extern void YB_Purchase(int id, string productId);
            [DllImport("__Internal")] private static extern void YB_GetPurchases(int id);
            [DllImport("__Internal")] private static extern void YB_ConsumePurchase(string productId);

            public static void Init(int id) => YB_Init(id);
            public static string GetLanguage() => Marshal.PtrToStringUTF8(YB_GetLanguage());
            public static double GetServerTimeMs() => YB_GetServerTimeMs();
            public static void GameReady() => YB_GameReady();
            public static void LocalSet(string key, string value) => YB_LocalSet(key, value);
            public static string LocalGet(string key) => Marshal.PtrToStringUTF8(YB_LocalGet(key));
            public static void LoadCloud(int id) => YB_LoadCloud(id);
            public static void SaveCloud(int id, string json) => YB_SaveCloud(id, json);
            public static void ShowRewarded(int id) => YB_ShowRewarded(id);
            public static void ShowInterstitial(int id) => YB_ShowInterstitial(id);
            public static void SetLeaderboardScore(string name, double score) => YB_SetLeaderboardScore(name, score);
            public static void Purchase(int id, string productId) => YB_Purchase(id, productId);
            public static void GetPurchases(int id) => YB_GetPurchases(id);
            public static void ConsumePurchase(string productId) => YB_ConsumePurchase(productId);
#else
            // Вне WebGL-билда моста нет: платформа в редакторе — EditorPlatformService.
            public static void Init(int id) { }
            public static string GetLanguage() => "ru";
            public static double GetServerTimeMs() => 0;
            public static void GameReady() { }
            public static void LocalSet(string key, string value) { }
            public static string LocalGet(string key) => string.Empty;
            public static void LoadCloud(int id) { }
            public static void SaveCloud(int id, string json) { }
            public static void ShowRewarded(int id) { }
            public static void ShowInterstitial(int id) { }
            public static void SetLeaderboardScore(string name, double score) { }
            public static void Purchase(int id, string productId) { }
            public static void GetPurchases(int id) { }
            public static void ConsumePurchase(string productId) { }
#endif
        }
    }
}
