using System;

namespace ShaurmaTycoon.Core
{
    /// <summary>
    /// Собирает причины паузы (реклама, платформа попросила паузу, окно потеряло фокус) и сообщает итог:
    /// игра на паузе, пока активна хотя бы одна причина. Меняет TickManager.IsPaused и поднимает GameEvents.GamePaused.
    /// </summary>
    public sealed class PauseController
    {
        private readonly Action<bool> _apply;
        private bool _platform;
        private bool _focusLost;

        /// <param name="apply">Применяет итоговое состояние (TickManager, звук); вызывается только при смене.</param>
        public PauseController(Action<bool> apply)
        {
            _apply = apply ?? throw new ArgumentNullException(nameof(apply));
        }

        public bool IsPaused { get; private set; }

        /// <summary>Пауза по сигналу платформы (реклама открыта/закрыта, game_api_pause).</summary>
        public void SetPlatformPause(bool paused)
        {
            _platform = paused;
            Refresh();
        }

        public void SetFocus(bool hasFocus)
        {
            _focusLost = !hasFocus;
            Refresh();
        }

        private void Refresh()
        {
            bool paused = _platform || _focusLost;
            if (paused == IsPaused)
                return;

            IsPaused = paused;
            _apply(paused);
        }
    }
}
