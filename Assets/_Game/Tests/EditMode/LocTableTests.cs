using NUnit.Framework;
using ShaurmaTycoon.Localization;

namespace ShaurmaTycoon.Tests
{
    public class LocTableTests
    {
        private const string Tsv =
            "# comment\r\n" +
            "key\tru\ten\r\n" +
            "\r\n" +
            "hello\tПривет\tHello\r\n" +
            "only_ru\tТолько русский\t\r\n" +
            "multiline\tОдин\\nДва\tOne\\nTwo\r\n" +
            "short\tКоротко\n";

        [Test]
        public void Parse_ReadsLanguagesAndRows()
        {
            LocTable table = LocTable.Parse(Tsv);

            Assert.AreEqual(2, table.Languages.Count);
            Assert.AreEqual("ru", table.Languages[0]);
            Assert.AreEqual(4, table.Count);
        }

        [Test]
        public void TryGet_ReturnsTranslation()
        {
            LocTable table = LocTable.Parse(Tsv);

            Assert.IsTrue(table.TryGet("hello", "en", out string value));
            Assert.AreEqual("Hello", value);
        }

        [Test]
        public void TryGet_EmptyOrMissingCell_ReturnsFalse()
        {
            LocTable table = LocTable.Parse(Tsv);

            Assert.IsFalse(table.TryGet("only_ru", "en", out _));
            Assert.IsFalse(table.TryGet("short", "en", out _));
            Assert.IsFalse(table.TryGet("missing", "ru", out _));
            Assert.IsFalse(table.TryGet("hello", "de", out _));
        }

        [Test]
        public void Parse_UnescapesNewLines()
        {
            LocTable.Parse(Tsv).TryGet("multiline", "ru", out string value);

            Assert.AreEqual("Один\nДва", value);
        }

        [Test]
        public void Parse_EmptyText_ReturnsEmptyTable()
        {
            Assert.AreEqual(0, LocTable.Parse("").Count);
            Assert.AreEqual(0, LocTable.Parse(null).Count);
        }
    }
}
