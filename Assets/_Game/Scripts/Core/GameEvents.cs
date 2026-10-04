using System;
using UnityEngine;

namespace ShaurmaTycoon.Core
{
    /// <summary>
    /// Глобальные события игры, на которые подписываются системы.
    /// </summary>
    public static class GameEvents
    {
        /// <summary>true — игра на паузе (реклама, потеря фокуса), false — продолжилась.</summary>
        public static event Action<bool> GamePaused;

        /// <summary>Окно игры получило (true) или потеряло (false) фокус.</summary>
        public static event Action<bool> ApplicationFocusChanged;

        /// <summary>Приложение свёрнуто (true) или развёрнуто (false) — в основном мобильные платформы.</summary>
        public static event Action<bool> ApplicationPaused;

        public static event Action ApplicationQuitting;

        public static void RaiseGamePaused(bool paused)
        {
            GamePaused?.Invoke(paused);
        }

        public static void RaiseApplicationFocusChanged(bool hasFocus)
        {
            ApplicationFocusChanged?.Invoke(hasFocus);
        }

        public static void RaiseApplicationPaused(bool paused)
        {
            ApplicationPaused?.Invoke(paused);
        }

        public static void RaiseApplicationQuitting()
        {
            ApplicationQuitting?.Invoke();
        }

        // Сброс подписчиков при старте Play Mode, если в редакторе отключён Domain Reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            GamePaused = null;
            ApplicationFocusChanged = null;
            ApplicationPaused = null;
            ApplicationQuitting = null;
        }
    }
}
