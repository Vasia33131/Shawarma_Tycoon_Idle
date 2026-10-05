using System.Globalization;
using System.Text.RegularExpressions;

namespace ShaurmaTycoon.Save
{
    /// <summary>Выбор между облачным и локальным сейвом: побеждает записанный позже (по SavedAtUtcTicks).</summary>
    public static class SaveSelector
    {
        private static readonly Regex TicksPattern = new Regex("\"SavedAtUtcTicks\"\\s*:\\s*(-?\\d+)", RegexOptions.Compiled);

        /// <summary>Время записи из JSON сейва; 0 — пусто или поле не найдено.</summary>
        public static long ReadSavedAtTicks(string json)
        {
            if (string.IsNullOrEmpty(json))
                return 0;

            Match match = TicksPattern.Match(json);
            return match.Success && long.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long ticks)
                ? ticks
                : 0;
        }

        /// <returns>Более свежий из двух сейвов; при равенстве — облачный; пустые игнорируются; оба пустые — null.</returns>
        public static string Pick(string cloudJson, string localJson)
        {
            bool hasCloud = !string.IsNullOrEmpty(cloudJson);
            bool hasLocal = !string.IsNullOrEmpty(localJson);
            if (!hasCloud && !hasLocal)
                return null;
            if (!hasLocal)
                return cloudJson;
            if (!hasCloud)
                return localJson;

            return ReadSavedAtTicks(localJson) > ReadSavedAtTicks(cloudJson) ? localJson : cloudJson;
        }
    }
}
