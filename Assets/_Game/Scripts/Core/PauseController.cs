using System;

namespace ShaurmaTycoon.Core
{
    /// <summary>
    /// Собирает причины паузы (реклама, платформа попросила паузу, вкладка скрыта, окно потеряло фокус) и сообщает итог:
    /// игра на паузе, пока активна хотя бы одна причина. Меняет TickManager.IsPaused и поднимает GameEvents.GamePaused.
    /// </summary>
    public sealed class PauseController
    {
        private readonly Action<bool> _apply;
        private bool _platform;
        private bool _focusLost;
        private bool _windowBlurred;
        private bool _ad;

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

        /// <summary>Вкладка видна (true) или скрыта/свёрнута (false).</summary>
        public void SetFocus(bool hasFocus)
        {
            _focusLost = !hasFocus;
            Refresh();
        }

        /// <summary>Окно браузера в фокусе (true) или игрок ушёл в другое окно (false).</summary>
        public void SetWindowFocus(bool hasFocus)
        {
            _windowBlurred = !hasFocus;
            Refresh();
        }

        /// <summary>Полноэкранная реклама открыта (вызвана игрой, не дожидаясь сигнала платформы).</summary>
        public void SetAdShowing(bool showing)
        {
            _ad = showing;
            Refresh();
        }

        private void Refresh()
        {
            bool paused = _platform || _focusLost || _windowBlurred || _ad;
            if (paused == IsPaused)
                return;

            IsPaused = paused;
            _apply(paused);
        }
    }
}
