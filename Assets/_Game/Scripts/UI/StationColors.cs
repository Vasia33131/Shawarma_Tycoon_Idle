using UnityEngine;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Цвета кругов-иконок станций по порядку (пока нет графики, этап 05). Общие для всех вкладок.
    /// </summary>
    public static class StationColors
    {
        private static readonly Color[] Palette =
        {
            new Color(0.91f, 0.45f, 0.18f),
            new Color(0.96f, 0.73f, 0.15f),
            new Color(0.85f, 0.62f, 0.25f),
            new Color(0.30f, 0.65f, 0.80f),
            new Color(0.80f, 0.30f, 0.25f),
            new Color(0.45f, 0.30f, 0.20f),
        };

        /// <summary>Цвет «все станции» (улучшения для всего города).</summary>
        public static Color All { get; } = new Color(0.30f, 0.69f, 0.31f);

        public static Color Get(int stationIndex)
        {
            return stationIndex < 0 ? All : Palette[stationIndex % Palette.Length];
        }
    }
}
