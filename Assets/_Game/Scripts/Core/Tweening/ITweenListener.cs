namespace ShaurmaTycoon.Core.Tweening
{
    /// <summary>
    /// Получатель окончания твина. Интерфейс вместо делегата: подписка без замыканий и аллокаций.
    /// </summary>
    public interface ITweenListener
    {
        void OnTweenCompleted(int tag);
    }
}
