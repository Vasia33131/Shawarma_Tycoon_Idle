using System.Globalization;
using NUnit.Framework;
using ShaurmaTycoon.Economy;

namespace ShaurmaTycoon.Tests
{
    public class BigNumberFormatterTests
    {
        [TestCase(0, "0")]
        [TestCase(5, "5")]
        [TestCase(5.99, "5")]
        [TestCase(999, "999")]
        [TestCase(999.99, "999")]
        [TestCase(1000, "1.00K")]
        [TestCase(1234, "1.23K")]
        [TestCase(1239, "1.23K")]
        [TestCase(12345, "12.3K")]
        [TestCase(123456, "123K")]
        [TestCase(999999, "999K")]
        [TestCase(1e6, "1.00M")]
        [TestCase(4.56e6, "4.56M")]
        [TestCase(1e9, "1.00B")]
        [TestCase(1e12, "1.00T")]
        [TestCase(1e15, "1.00aa")]
        [TestCase(1e18, "1.00ab")]
        [TestCase(1e90, "1.00az")]
        [TestCase(1e93, "1.00ba")]
        [TestCase(-1234, "-1.23K")]
        public void Format_ReturnsExpected(double value, string expected)
        {
            Assert.AreEqual(expected, BigNumberFormatter.Format(value));
        }

        [Test]
        public void Format_NaN_ReturnsZero()
        {
            Assert.AreEqual("0", BigNumberFormatter.Format(double.NaN));
        }

        [Test]
        public void Format_MaxDouble_DoesNotThrow()
        {
            Assert.IsNotEmpty(BigNumberFormatter.Format(double.MaxValue));
        }

        [Test]
        public void Format_RussianCulture_UsesDot()
        {
            CultureInfo previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("ru-RU");
                Assert.AreEqual("1.23K", BigNumberFormatter.Format(1234));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }
    }
}
