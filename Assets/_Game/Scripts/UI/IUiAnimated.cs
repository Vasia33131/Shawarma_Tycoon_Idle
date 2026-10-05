using ShaurmaTycoon.Core.Tweening;

namespace ShaurmaTycoon.UI
{
    /// <summary>Постоянная анимация интерфейса (пульс, свечение, вкладки): её тикает PulseDriver, а не свой Update.</summary>
    public interface IUiAnimated
    {
        /// <param name="time">Время с начала тиков PulseDriver, с.</param>
        void Animate(float time, float deltaTime, TweenRunner tweens);
    }
}
