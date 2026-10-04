using System;
using System.Globalization;

namespace ShaurmaTycoon.Localization
{
    /// <summary>
    /// Сервис локализации: текущий язык и перевод ключей.
    /// Если перевода нет — берётся русский, затем английский, затем сам ключ (видно, что строка забыта).
    /// </summary>
    public sealed class Loc
    {
        public const string Russian = "ru";
        public const string English = "en";

        private readonly LocTable _table;

        public Loc(LocTable table, string platformLanguage)
        {
            _table = table ?? throw new ArgumentNullException(nameof(table));
            Language = NormalizeLanguage(platformLanguage);
        }

        public event Action LanguageChanged;

        /// <summary>"ru" или "en".</summary>
        public string Language { get; private set; }

        public void SetLanguage(string language)
        {
            string normalized = NormalizeLanguage(language);
            if (normalized == Language)
                return;

            Language = normalized;
            LanguageChanged?.Invoke();
        }

        public string Get(string key)
        {
            if (_table.TryGet(key, Language, out string value)
                || _table.TryGet(key, Russian, out value)
                || _table.TryGet(key, English, out value))
                return value;

            return key ?? string.Empty;
        }

        public string Format(string key, object arg0)
        {
            return string.Format(CultureInfo.InvariantCulture, Get(key), arg0);
        }

        public string Format(string key, object arg0, object arg1)
        {
            return string.Format(CultureInfo.InvariantCulture, Get(key), arg0, arg1);
        }

        /// <summary>
        /// Код языка платформы ("ru-RU", "uk", "tr"…) → "ru" или "en".
        /// Языки СНГ показываем на русском, остальные — на английском. Пустой код — русский.
        /// </summary>
        public static string NormalizeLanguage(string code)
        {
            if (string.IsNullOrEmpty(code))
                return Russian;

            string lang = code.Length > 2 ? code.Substring(0, 2) : code;
            switch (lang.ToLowerInvariant())
            {
                case "ru":
                case "be":
                case "kk":
                case "uk":
                case "uz":
                    return Russian;
                default:
                    return English;
            }
        }
    }
}
