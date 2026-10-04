using NUnit.Framework;
using ShaurmaTycoon.Localization;

namespace ShaurmaTycoon.Tests
{
    public class LocTests
    {
        private static readonly LocTable Table = LocTable.Parse(
            "key\tru\ten\n" +
            "hello\tПривет\tHello\n" +
            "only_ru\tТолько русский\t\n" +
            "only_en\t\tEnglish only\n" +
            "level\tУр. {0}\tLv. {0}\n");

        [TestCase(null, "ru")]
        [TestCase("", "ru")]
        [TestCase("ru", "ru")]
        [TestCase("ru-RU", "ru")]
        [TestCase("uk", "ru")]
        [TestCase("be", "ru")]
        [TestCase("kk", "ru")]
        [TestCase("uz", "ru")]
        [TestCase("en", "en")]
        [TestCase("tr", "en")]
        [TestCase("EN-us", "en")]
        public void NormalizeLanguage_MapsToRuOrEn(string code, string expected)
        {
            Assert.AreEqual(expected, Loc.NormalizeLanguage(code));
        }

        [Test]
        public void Get_UsesCurrentLanguage()
        {
            Assert.AreEqual("Hello", new Loc(Table, "en").Get("hello"));
            Assert.AreEqual("Привет", new Loc(Table, "ru").Get("hello"));
        }

        [Test]
        public void Get_FallsBackToRussian_ThenEnglish_ThenKey()
        {
            var en = new Loc(Table, "en");
            var ru = new Loc(Table, "ru");

            Assert.AreEqual("Только русский", en.Get("only_ru"));
            Assert.AreEqual("English only", ru.Get("only_en"));
            Assert.AreEqual("missing.key", ru.Get("missing.key"));
        }

        [Test]
        public void Format_SubstitutesArguments()
        {
            Assert.AreEqual("Lv. 12", new Loc(Table, "en").Format("level", 12));
        }

        [Test]
        public void GetTemplate_IsCached_AndUpdatedOnLanguageChange()
        {
            var loc = new Loc(Table, "ru");
            LocTemplate template = loc.GetTemplate("level");

            Assert.AreSame(template, loc.GetTemplate("level"));
            Assert.AreEqual("Ур. ", template.Prefix);

            loc.SetLanguage("en");

            Assert.AreEqual("Lv. ", template.Prefix);
        }

        [Test]
        public void SetLanguage_RaisesEventOnlyOnChange()
        {
            var loc = new Loc(Table, "ru");
            int events = 0;
            loc.LanguageChanged += () => events++;

            loc.SetLanguage("ru-RU");
            loc.SetLanguage("en");

            Assert.AreEqual(1, events);
            Assert.AreEqual("en", loc.Language);
        }
    }
}
