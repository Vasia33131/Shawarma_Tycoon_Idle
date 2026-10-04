using System;
using System.Collections.Generic;

namespace ShaurmaTycoon.Localization
{
    /// <summary>
    /// Таблица строк из TSV: первая строка-заголовок «key, ru, en…», дальше «ключ, перевод, перевод…».
    /// Строки, начинающиеся с #, и пустые строки пропускаются. «\n» в тексте — перенос строки.
    /// </summary>
    public sealed class LocTable
    {
        private readonly Dictionary<string, string[]> _rows;
        private readonly string[] _languages;

        private LocTable(string[] languages, Dictionary<string, string[]> rows)
        {
            _languages = languages;
            _rows = rows;
        }

        public static LocTable Empty { get; } = new LocTable(new string[0], new Dictionary<string, string[]>());

        public IReadOnlyList<string> Languages => _languages;

        public int Count => _rows.Count;

        public static LocTable Parse(string tsv)
        {
            if (string.IsNullOrEmpty(tsv))
                return Empty;

            string[] languages = null;
            var rows = new Dictionary<string, string[]>(256, StringComparer.Ordinal);

            string[] lines = tsv.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimEnd('\r');
                if (line.Length == 0 || line[0] == '#')
                    continue;

                string[] cells = line.Split('\t');
                if (languages == null)
                {
                    languages = new string[cells.Length - 1];
                    for (int c = 1; c < cells.Length; c++)
                        languages[c - 1] = cells[c].Trim().ToLowerInvariant();
                    continue;
                }

                string key = cells[0].Trim();
                if (key.Length == 0)
                    continue;

                var values = new string[languages.Length];
                for (int c = 0; c < values.Length && c + 1 < cells.Length; c++)
                    values[c] = cells[c + 1].Replace("\\n", "\n");
                rows[key] = values;
            }

            return languages == null ? Empty : new LocTable(languages, rows);
        }

        /// <summary>Перевод ключа на язык. Пустой перевод считается отсутствующим.</summary>
        public bool TryGet(string key, string language, out string value)
        {
            value = null;
            if (key == null || !_rows.TryGetValue(key, out string[] values))
                return false;

            int index = Array.IndexOf(_languages, language);
            if (index < 0 || string.IsNullOrEmpty(values[index]))
                return false;

            value = values[index];
            return true;
        }
    }
}
