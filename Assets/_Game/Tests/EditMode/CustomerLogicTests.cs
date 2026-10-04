using System;
using NUnit.Framework;
using ShaurmaTycoon.Customers;

namespace ShaurmaTycoon.Tests
{
    public class CustomerLogicTests
    {
        private static CustomerDemand CreateDemand()
        {
            return new CustomerDemand(maxQueue: 7, queueGrowth: 1.2, serviceTimeMax: 3, serviceTimeMin: 0.7, serviceSpeedup: 0.6);
        }

        [TestCase(0, 1)]
        [TestCase(1, 1)]
        [TestCase(10, 2)]
        [TestCase(100, 3)]
        [TestCase(1e5, 7)]
        [TestCase(1e30, 7)]
        [TestCase(-5, 1)]
        public void QueueSize_GrowsWithIncome_AndIsCapped(double rate, int expected)
        {
            Assert.AreEqual(expected, CreateDemand().GetQueueSize(rate));
        }

        [Test]
        public void ServiceTime_DecreasesWithIncome_WithinBounds()
        {
            CustomerDemand demand = CreateDemand();

            Assert.AreEqual(3, demand.GetServiceTime(0), 1e-9);
            Assert.Less(demand.GetServiceTime(100), demand.GetServiceTime(1));
            Assert.AreEqual(0.7, demand.GetServiceTime(1e30), 1e-9);
        }

        [Test]
        public void InvalidDemandSettings_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CustomerDemand(0, 1, 3, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CustomerDemand(5, 1, 1, 3, 1));
        }

        [Test]
        public void RateTracker_ConvergesToSteadyIncome()
        {
            var tracker = new IncomeRateTracker(timeConstant: 1);
            for (int i = 0; i < 600; i++)
            {
                tracker.Add(10 * 0.05);
                tracker.Tick(0.05);
            }

            Assert.AreEqual(10, tracker.Rate, 0.01);
        }

        [Test]
        public void RateTracker_DecaysWithoutIncome()
        {
            var tracker = new IncomeRateTracker(timeConstant: 1);
            tracker.Add(100);
            tracker.Tick(1);
            double peak = tracker.Rate;

            for (int i = 0; i < 100; i++)
                tracker.Tick(0.1);

            Assert.Greater(peak, 0);
            Assert.Less(tracker.Rate, peak * 0.01);
        }

        [Test]
        public void RateTracker_IgnoresInvalidInput()
        {
            var tracker = new IncomeRateTracker();
            tracker.Add(-5);
            tracker.Add(double.NaN);
            tracker.Add(double.PositiveInfinity);
            tracker.Tick(1);
            tracker.Tick(0);

            Assert.AreEqual(0, tracker.Rate);
        }
    }
}
