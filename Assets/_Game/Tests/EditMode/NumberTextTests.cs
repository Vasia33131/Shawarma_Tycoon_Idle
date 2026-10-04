using NUnit.Framework;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Localization;

namespace ShaurmaTycoon.Tests
{
    public class NumberTextTests
    {
        [TestCase(0, "0")]
        [TestCase(7, "7")]
        [TestCase(999.99, "999")]
        [TestCase(1000, "1.00K")]
        [TestCase(1005, "1.00K")]
        [TestCase(1050, "1.05K")]
        [TestCase(1234, "1.23K")]
        [TestCase(12345, "12.3K")]
        [TestCase(123456, "123K")]
        [TestCase(999999, "999K")]
        [TestCase(1e6, "1.00M")]
        [TestCase(1e15, "1.00aa")]
        [TestCase(1e93, "1.00ba")]
        [TestCase(-1234, "-1.23K")]
        public void AppendNumber_MatchesExpected(double value, string expected)
        {
            Assert.AreEqual(expected, new NumberText().AppendNumber(value).ToString());
        }

        [Test]
        public void AppendNumber_MaxDouble_StaysWithinLetterSuffixes()
        {
            Assert.AreEqual("179dt", new NumberText().AppendNumber(double.MaxValue).ToString());
        }

        [Test]
        public void Chain_BuildsTextWithPrefixAndSuffix()
        {
            var text = new NumberText();
            text.Append("Тапни! +").AppendNumber(1234).Append('/').Append("с");

            Assert.AreEqual("Тапни! +1.23K/с", text.ToString());
        }

        [Test]
        public void Clear_ResetsLength()
        {
            var text = new NumberText();
            text.Append("abc");
            text.Clear().AppendInteger(42);

            Assert.AreEqual("42", text.ToString());
        }

        [TestCase(0, "0")]
        [TestCase(9, "9")]
        [TestCase(10, "10")]
        [TestCase(1234567890123, "1234567890123")]
        [TestCase(-15, "-15")]
        public void AppendInteger_WritesDigits(long value, string expected)
        {
            Assert.AreEqual(expected, new NumberText().AppendInteger(value).ToString());
        }

        [Test]
        public void SmallCapacity_Grows()
        {
            var text = new NumberText(8);
            text.Append("0123456789").Append("0123456789");

            Assert.AreEqual(20, text.Length);
        }

        [TestCase("{0}/с", "", "/с")]
        [TestCase("Тапни! +{0}", "Тапни! +", "")]
        [TestCase("Открыть\n{0}", "Открыть\n", "")]
        [TestCase("Без подстановки", "Без подстановки", "")]
        [TestCase("", "", "")]
        public void LocTemplate_SplitsAroundPlaceholder(string template, string prefix, string suffix)
        {
            var parsed = new LocTemplate();
            parsed.Set(template);

            Assert.AreEqual(prefix, parsed.Prefix);
            Assert.AreEqual(suffix, parsed.Suffix);
        }
    }
}
