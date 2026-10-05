using UnityEngine;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Цвета кругов-иконок станций по порядку. Общие для всех вкладок.
    /// Вечерняя гамма (Docs/Visual/ART_DIRECTION.md): тёплые, на ~15% приглушённее чистых цветов.
    /// </summary>
    public static class StationColors
    {
        private static readonly Color[] Palette =
        {
            new Color32(0xE0, 0x7B, 0x45, 0xFF),
            new Color32(0xF0, 0xBE, 0x4E, 0xFF),
            new Color32(0xD9, 0xA3, 0x5A, 0xFF),
            new Color32(0x6F, 0xA9, 0xC4, 0xFF),
            new Color32(0xD2, 0x67, 0x5A, 0xFF),
            new Color32(0x9A, 0x6A, 0x4E, 0xFF),
        };

        /// <summary>Цвет «все станции» (улучшения для всего города) — Primary из UiTheme.</summary>
        public static Color All { get; } = new Color32(0x7C, 0xC4, 0x7F, 0xFF);

        public static Color Get(int stationIndex)
        {
            return stationIndex < 0 ? All : Palette[stationIndex % Palette.Length];
        }
    }
}
