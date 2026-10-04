using System;
using NUnit.Framework;
using ShaurmaTycoon.Stations;

namespace ShaurmaTycoon.Tests
{
    public class MilestoneTableTests
    {
        private static MilestoneTable CreateGddTable()
        {
            return new MilestoneTable(new[] { 25, 50, 100, 200, 300, 400 }, 2);
        }

        [TestCase(0, 1)]
        [TestCase(24, 1)]
        [TestCase(25, 2)]
        [TestCase(49, 2)]
        [TestCase(50, 4)]
        [TestCase(100, 8)]
        [TestCase(400, 64)]
        [TestCase(1000, 64)]
        public void GetSpeedMultiplier_ReturnsExpected(int level, double expected)
        {
            Assert.AreEqual(expected, CreateGddTable().GetSpeedMultiplier(level));
        }

        [TestCase(1, 25)]
        [TestCase(25, 50)]
        [TestCase(399, 400)]
        [TestCase(400, -1)]
        public void GetNextMilestone_ReturnsExpected(int level, int expected)
        {
            Assert.AreEqual(expected, CreateGddTable().GetNextMilestone(level));
        }

        [Test]
        public void UnsortedLevels_AreSorted()
        {
            var table = new MilestoneTable(new[] { 50, 25 }, 2);

            Assert.AreEqual(25, table.GetNextMilestone(0));
            Assert.AreEqual(2, table.GetSpeedMultiplier(30));
        }

        [Test]
        public void SpeedMultiplierBelowOne_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MilestoneTable(new[] { 25 }, 0.5));
        }
    }
}
