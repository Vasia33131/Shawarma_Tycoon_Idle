namespace ShaurmaTycoon.Localization
{
    /// <summary>
    /// Шаблон строки с одним «{0}», разобранный на префикс и суффикс.
    /// Позволяет собирать «Тапни! +1.23K» в NumberText без string.Format и аллокаций.
    /// </summary>
    public sealed class LocTemplate
    {
        private const string Placeholder = "{0}";

        public string Prefix { get; private set; } = string.Empty;
        public string Suffix { get; private set; } = string.Empty;

        /// <summary>Разбирает шаблон; вызывать при смене языка.</summary>
        public void Set(string template)
        {
            if (string.IsNullOrEmpty(template))
            {
                Prefix = string.Empty;
                Suffix = string.Empty;
                return;
            }

            int index = template.IndexOf(Placeholder, System.StringComparison.Ordinal);
            if (index < 0)
            {
                Prefix = template;
                Suffix = string.Empty;
                return;
            }

            Prefix = template.Substring(0, index);
            Suffix = template.Substring(index + Placeholder.Length);
        }
    }
}
