namespace ShaurmaTycoon.Core
{
    /// <summary>
    /// Объект, который обновляется каждый кадр через TickManager.
    /// </summary>
    public interface ITickable
    {
        /// <param name="deltaTime">Время кадра в секундах с учётом TickManager.TimeScale.</param>
        void Tick(float deltaTime);
    }
}
