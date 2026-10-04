using System.Collections.Generic;
using NUnit.Framework;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Save;

namespace ShaurmaTycoon.Tests
{
    public class BillWalletTests
    {
        [Test]
        public void NewWallet_IsEmpty()
        {
            Assert.AreEqual(0, new BillWallet().Bills);
        }

        [Test]
        public void Add_IncreasesBills_AndReportsDelta()
        {
            var wallet = new BillWallet();
            var events = new List<(long bills, long delta)>();
            wallet.BillsChanged += (bills, delta) => events.Add((bills, delta));

            wallet.Add(20, BillSource.Purchase);

            Assert.AreEqual(20, wallet.Bills);
            CollectionAssert.AreEqual(new[] { (20L, 20L) }, events);
        }

        [TestCase(0)]
        [TestCase(-5)]
        public void Add_NonPositive_IsIgnored(long amount)
        {
            var wallet = new BillWallet();
            int events = 0;
            wallet.BillsChanged += (_, __) => events++;

            wallet.Add(amount, BillSource.Ad);

            Assert.AreEqual(0, wallet.Bills);
            Assert.AreEqual(0, events);
        }

        [Test]
        public void Add_Overflow_ClampsToMaxValue()
        {
            var wallet = new BillWallet();
            wallet.SetBills(long.MaxValue - 1);

            wallet.Add(10, BillSource.Debug);

            Assert.AreEqual(long.MaxValue, wallet.Bills);
        }

        [Test]
        public void TrySpend_Enough_SpendsAndReportsNegativeDelta()
        {
            var wallet = new BillWallet();
            wallet.Add(10, BillSource.Ad);
            long lastDelta = 0;
            wallet.BillsChanged += (_, delta) => lastDelta = delta;

            Assert.IsTrue(wallet.TrySpend(4, BillSink.Coins));
            Assert.AreEqual(6, wallet.Bills);
            Assert.AreEqual(-4, lastDelta);
        }

        [TestCase(11)]
        [TestCase(-1)]
        public void TrySpend_Invalid_ReturnsFalse_AndKeepsBills(long amount)
        {
            var wallet = new BillWallet();
            wallet.Add(10, BillSource.Ad);

            Assert.IsFalse(wallet.TrySpend(amount, BillSink.Station));
            Assert.AreEqual(10, wallet.Bills);
        }

        [Test]
        public void SetBills_Negative_BecomesZero()
        {
            var wallet = new BillWallet();
            wallet.SetBills(-3);
            Assert.AreEqual(0, wallet.Bills);
        }

        [Test]
        public void Participant_RoundTrip_KeepsBills()
        {
            var source = new BillWallet();
            source.Add(42, BillSource.Purchase);
            var data = new SaveData();
            new BillsSaveParticipant(source).Capture(data);

            var restored = new BillWallet();
            new BillsSaveParticipant(restored).Apply(data);

            Assert.AreEqual(42, restored.Bills);
        }

        [Test]
        public void Migrator_NegativeBills_BecomeZero()
        {
            SaveData data = SaveMigrator.Migrate(new SaveData { Bills = -7 });
            Assert.AreEqual(0, data.Bills);
        }
    }
}
