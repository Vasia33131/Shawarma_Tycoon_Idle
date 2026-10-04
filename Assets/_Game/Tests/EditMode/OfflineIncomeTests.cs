using System;
using NUnit.Framework;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Localization;
using ShaurmaTycoon.Offline;

namespace ShaurmaTycoon.Tests
{
    public class OfflineIncomeTests
    {
        private static readonly DateTime Now = new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc);
        private static readonly OfflineSettings Gdd = new OfflineSettings(incomeRate: 0.5, limitSeconds: 2 * 3600, minAwaySeconds: 60);

        private static long Ago(TimeSpan span)
        {
            return (Now - span).Ticks;
        }

        [Test]
        public void OneHourAway_GivesHalfIncome()
        {
            OfflineIncome result = OfflineIncomeCalculator.Calculate(10, Ago(TimeSpan.FromHours(1)), Now, Gdd);

            Assert.AreEqual(3600, result.Seconds, 1e-6);
            Assert.AreEqual(10 * 3600 * 0.5, result.Amount, 1e-6);
            Assert.IsFalse(result.IsCapped);
        }

        [Test]
        public void LongAbsence_IsCappedByLimit()
        {
            OfflineIncome result = OfflineIncomeCalculator.Calculate(10, Ago(TimeSpan.FromHours(3)), Now, Gdd);

            Assert.AreEqual(7200, result.Seconds, 1e-6);
            Assert.AreEqual(10 * 7200 * 0.5, result.Amount, 1e-6);
            Assert.IsTrue(result.IsCapped);
        }

        [Test]
        public void ClockMovedBack_GivesNothing()
        {
            OfflineIncome result = OfflineIncomeCalculator.Calculate(10, (Now + TimeSpan.FromHours(1)).Ticks, Now, Gdd);

            Assert.IsFalse(result.HasIncome);
            Assert.AreEqual(0, result.Seconds);
        }

        [Test]
        public void NoSave_GivesNothing()
        {
            Assert.IsFalse(OfflineIncomeCalculator.Calculate(10, 0, Now, Gdd).HasIncome);
        }

        [Test]
        public void NoManagers_GivesNothing()
        {
            Assert.IsFalse(OfflineIncomeCalculator.Calculate(0, Ago(TimeSpan.FromHours(1)), Now, Gdd).HasIncome);
        }

        [Test]
        public void ShortAbsence_BelowMinimum_GivesNothing()
        {
            Assert.IsFalse(OfflineIncomeCalculator.Calculate(10, Ago(TimeSpan.FromSeconds(59)), Now, Gdd).HasIncome);
            Assert.IsTrue(OfflineIncomeCalculator.Calculate(10, Ago(TimeSpan.FromSeconds(61)), Now, Gdd).HasIncome);
        }

        [Test]
        public void InvalidIncome_GivesNothing()
        {
            Assert.IsFalse(OfflineIncomeCalculator.Calculate(double.NaN, Ago(TimeSpan.FromHours(1)), Now, Gdd).HasIncome);
            Assert.IsFalse(OfflineIncomeCalculator.Calculate(double.PositiveInfinity, Ago(TimeSpan.FromHours(1)), Now, Gdd).HasIncome);
        }

        [Test]
        public void Service_Claim_AddsOnce()
        {
            var wallet = new Wallet();
            var service = new OfflineIncomeService(wallet, new OfflineIncome(3600, 500, false), adMultiplier: 2);
            double claimed = 0;
            service.Claimed += amount => claimed = amount;

            Assert.IsTrue(service.HasPending);
            Assert.IsTrue(service.TryClaim(withAdBonus: false));
            Assert.IsFalse(service.TryClaim(withAdBonus: true));

            Assert.AreEqual(500, wallet.Coins);
            Assert.AreEqual(500, claimed);
            Assert.IsFalse(service.HasPending);
        }

        [Test]
        public void Service_ClaimWithAd_Doubles()
        {
            var wallet = new Wallet();
            var service = new OfflineIncomeService(wallet, new OfflineIncome(3600, 500, false), adMultiplier: 2);

            service.TryClaim(withAdBonus: true);

            Assert.AreEqual(1000, wallet.Coins);
        }

        [Test]
        public void Service_NoIncome_HasNothingToClaim()
        {
            var wallet = new Wallet();
            var service = new OfflineIncomeService(wallet, OfflineIncome.None, adMultiplier: 2);

            Assert.IsFalse(service.HasPending);
            Assert.IsFalse(service.TryClaim(withAdBonus: false));
            Assert.AreEqual(0, wallet.Coins);
        }

        [TestCase(0, "1 мин")]
        [TestCase(59, "1 мин")]
        [TestCase(125, "2 мин")]
        [TestCase(3600, "1 ч")]
        [TestCase(5000, "1 ч 23 мин")]
        [TestCase(7200, "2 ч")]
        public void Duration_IsHumanReadable(double seconds, string expected)
        {
            var loc = new Loc(LocTable.Parse(
                "key\tru\ten\n" +
                "time.minutes\t{0} мин\t{0} min\n" +
                "time.hours\t{0} ч\t{0} h\n" +
                "time.hours_minutes\t{0} ч {1} мин\t{0} h {1} min\n"), "ru");

            Assert.AreEqual(expected, DurationFormatter.Format(seconds, loc));
        }
    }
}
