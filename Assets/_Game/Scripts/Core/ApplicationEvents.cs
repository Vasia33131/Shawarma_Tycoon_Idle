using UnityEngine;

namespace ShaurmaTycoon.Core
{
    /// <summary>
    /// Переводит сообщения Unity о фокусе, паузе и выходе в GameEvents. Живёт рядом с TickManager (DontDestroyOnLoad).
    /// </summary>
    public sealed class ApplicationEvents : MonoBehaviour
    {
        private void OnApplicationFocus(bool hasFocus)
        {
            GameEvents.RaiseApplicationFocusChanged(hasFocus);
        }

        private void OnApplicationPause(bool paused)
        {
            GameEvents.RaiseApplicationPaused(paused);
        }

        private void OnApplicationQuit()
        {
            GameEvents.RaiseApplicationQuitting();
        }
    }
}
