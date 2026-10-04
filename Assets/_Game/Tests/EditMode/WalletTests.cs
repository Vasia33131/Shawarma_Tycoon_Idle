using NUnit.Framework;
using ShaurmaTycoon.Economy;

namespace ShaurmaTycoon.Tests
{
    public class WalletTests
    {
        private Wallet _wallet;
        private int _eventCount;
        private double _lastEventValue;

        [SetUp]
        public void SetUp()
        {
            _wallet = new Wallet();
            _eventCount = 0;
            _wallet.CoinsChanged += value =>
            {
                _eventCount++;
                _lastEventValue = value;
            };
        }

        [Test]
        public void NewWallet_IsEmpty()
        {
            Assert.AreEqual(0, _wallet.Coins);
        }

        [Test]
        public void Add_IncreasesCoins_AndRaisesEvent()
        {
            _wallet.Add(10);

            Assert.AreEqual(10, _wallet.Coins);
            Assert.AreEqual(1, _eventCount);
            Assert.AreEqual(10, _lastEventValue);
        }

        [TestCase(-5)]
        [TestCase(0)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void Add_InvalidAmount_IsIgnored(double amount)
        {
            _wallet.Add(amount);

            Assert.AreEqual(0, _wallet.Coins);
            Assert.AreEqual(0, _eventCount);
        }

        [Test]
        public void Add_Overflow_ClampsToMaxValue()
        {
            _wallet.Add(double.MaxValue);
            _wallet.Add(double.MaxValue);

            Assert.AreEqual(double.MaxValue, _wallet.Coins);
        }

        [Test]
        public void TrySpend_Enough_SpendsAndRaisesEvent()
        {
            _wallet.Add(100);

            Assert.IsTrue(_wallet.TrySpend(40));
            Assert.AreEqual(60, _wallet.Coins);
            Assert.AreEqual(2, _eventCount);
        }

        [Test]
        public void TrySpend_NotEnough_ReturnsFalse_AndKeepsCoins()
        {
            _wallet.Add(10);

            Assert.IsFalse(_wallet.TrySpend(10.5));
            Assert.AreEqual(10, _wallet.Coins);
            Assert.AreEqual(1, _eventCount);
        }

        [Test]
        public void TrySpend_InvalidAmount_ReturnsFalse()
        {
            _wallet.Add(10);

            Assert.IsFalse(_wallet.TrySpend(-1));
            Assert.IsFalse(_wallet.TrySpend(double.NaN));
            Assert.AreEqual(10, _wallet.Coins);
        }

        [Test]
        public void CanAfford_ComparesWithCoins()
        {
            _wallet.Add(50);

            Assert.IsTrue(_wallet.CanAfford(50));
            Assert.IsFalse(_wallet.CanAfford(50.01));
        }

        [TestCase(double.NaN)]
        [TestCase(-1)]
        [TestCase(double.PositiveInfinity)]
        public void SetCoins_InvalidValue_BecomesZero(double value)
        {
            _wallet.Add(10);
            _wallet.SetCoins(value);

            Assert.AreEqual(0, _wallet.Coins);
        }
    }
}
