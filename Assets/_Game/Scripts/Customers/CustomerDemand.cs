using System;

namespace ShaurmaTycoon.Customers
{
    /// <summary>
    /// Сколько клиентов в очереди и как быстро их обслуживают — от темпа дохода, по логарифму (только визуал).
    /// </summary>
    public sealed class CustomerDemand
    {
        private readonly int _maxQueue;
        private readonly double _queueGrowth;
        private readonly double _serviceTimeMax;
        private readonly double _serviceTimeMin;
        private readonly double _serviceSpeedup;

        /// <param name="queueGrowth">Сколько клиентов добавляется за каждый порядок (×10) дохода.</param>
        /// <param name="serviceSpeedup">Насколько ускоряется обслуживание за каждый порядок дохода.</param>
        public CustomerDemand(int maxQueue, double queueGrowth, double serviceTimeMax, double serviceTimeMin, double serviceSpeedup)
        {
            if (maxQueue < 1)
                throw new ArgumentOutOfRangeException(nameof(maxQueue));
            if (!(serviceTimeMin > 0) || serviceTimeMax < serviceTimeMin)
                throw new ArgumentOutOfRangeException(nameof(serviceTimeMin));

            _maxQueue = maxQueue;
            _queueGrowth = Math.Max(0, queueGrowth);
            _serviceTimeMax = serviceTimeMax;
            _serviceTimeMin = serviceTimeMin;
            _serviceSpeedup = Math.Max(0, serviceSpeedup);
        }

        public int GetQueueSize(double incomePerSecond)
        {
            double orders = Math.Log10(Math.Max(0, incomePerSecond) + 1);
            int size = 1 + (int)Math.Floor(orders * _queueGrowth);
            return Math.Min(Math.Max(size, 1), _maxQueue);
        }

        /// <summary>Сколько секунд клиент стоит у окна.</summary>
        public double GetServiceTime(double incomePerSecond)
        {
            double orders = Math.Log10(Math.Max(0, incomePerSecond) + 1);
            double time = _serviceTimeMax / (1 + orders * _serviceSpeedup);
            return Math.Min(Math.Max(time, _serviceTimeMin), _serviceTimeMax);
        }
    }
}
