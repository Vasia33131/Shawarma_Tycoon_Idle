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

        public static void RaiseGamePaused(bool paused)
        {
            GamePaused?.Invoke(paused);
        }

        // Сброс подписчиков при старте Play Mode, если в редакторе отключён Domain Reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            GamePaused = null;
        }
    }
}
