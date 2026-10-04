using System;
using NUnit.Framework;
using ShaurmaTycoon.Economy;

namespace ShaurmaTycoon.Tests
{
    public class EconomyMathTests
    {
        private const double RelativeTolerance = 1e-9;

        [Test]
        public void LevelCost_LevelZero_EqualsBaseCost()
        {
            Assert.AreEqual(4, EconomyMath.LevelCost(4, 1.07, 0));
        }

        [Test]
        public void LevelCost_FollowsFormula()
        {
            AssertRelative(4 * Math.Pow(1.07, 10), EconomyMath.LevelCost(4, 1.07, 10));
        }

        [TestCase(4, 1.07, 0, 1)]
        [TestCase(4, 1.07, 0, 10)]
        [TestCase(60, 1.15, 25, 100)]
        [TestCase(103680, 1.07, 150, 50)]
        [TestCase(1244160, 1.07, 0, 400)]
        public void BulkCost_MatchesLoopSum(double baseCost, double growth, int level, int count)
        {
            double expected = 0;
            for (int i = 0; i < count; i++)
                expected += EconomyMath.LevelCost(baseCost, growth, level + i);

            AssertRelative(expected, EconomyMath.BulkCost(baseCost, growth, level, count));
        }

        [Test]
        public void BulkCost_ZeroCount_IsZero()
        {
            Assert.AreEqual(0, EconomyMath.BulkCost(4, 1.07, 5, 0));
        }

        [Test]
        public void BulkCost_GrowthOne_IsLinear()
        {
            Assert.AreEqual(40, EconomyMath.BulkCost(4, 1, 7, 10));
        }

        [TestCase(4, 1.07, 0, 1)]
        [TestCase(4, 1.07, 0, 7)]
        [TestCase(60, 1.15, 10, 50)]
        [TestCase(8640, 1.07, 99, 120)]
        public void MaxAffordable_ExactMoney_ReturnsCount(double baseCost, double growth, int level, int count)
        {
            double money = EconomyMath.BulkCost(baseCost, growth, level, count);
            Assert.AreEqual(count, EconomyMath.MaxAffordable(baseCost, growth, level, money));
        }

        [TestCase(4, 1.07, 0, 1)]
        [TestCase(4, 1.07, 0, 7)]
        [TestCase(60, 1.15, 10, 50)]
        [TestCase(8640, 1.07, 99, 120)]
        public void MaxAffordable_SlightlyLessThanExact_ReturnsCountMinusOne(double baseCost, double growth, int level, int count)
        {
            double money = EconomyMath.BulkCost(baseCost, growth, level, count) * (1 - 1e-9);
            Assert.AreEqual(count - 1, EconomyMath.MaxAffordable(baseCost, growth, level, money));
        }

        [TestCase(4, 1.07, 0, 1e3)]
        [TestCase(4, 1.07, 37, 123456.789)]
        [TestCase(103680, 1.07, 200, 1e30)]
        [TestCase(60, 1.15, 0, 1e300)]
        public void MaxAffordable_IsTight(double baseCost, double growth, int level, double money)
        {
            int count = EconomyMath.MaxAffordable(baseCost, growth, level, money);

            Assert.LessOrEqual(EconomyMath.BulkCost(baseCost, growth, level, count), money);
            Assert.Greater(EconomyMath.BulkCost(baseCost, growth, level, count + 1), money);
        }

        [TestCase(0)]
        [TestCase(3.99)]
        [TestCase(-100)]
        [TestCase(double.NaN)]
        public void MaxAffordable_NotEnoughOrInvalid_ReturnsZero(double money)
        {
            Assert.AreEqual(0, EconomyMath.MaxAffordable(4, 1.07, 0, money));
        }

        [Test]
        public void GrowthBelowOne_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => EconomyMath.LevelCost(4, 0.9, 1));
        }

        private static void AssertRelative(double expected, double actual)
        {
            Assert.LessOrEqual(Math.Abs(actual - expected), Math.Abs(expected) * RelativeTolerance,
                $"expected {expected:R}, actual {actual:R}");
        }
    }
}
