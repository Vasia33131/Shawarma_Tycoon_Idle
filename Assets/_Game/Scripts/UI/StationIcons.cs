using ShaurmaTycoon.Stations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Значок станции в строках: нарисованная еда поверх светлой подложки цвета станции,
    /// а пока картинки нет — как раньше, цветной кружок с номером.
    /// </summary>
    public static class StationIcons
    {
        // Подложка под картинкой — лёгкий тон цвета станции.
        private const float PlateAlpha = 0.3f;

        /// <returns>Картинка станции или настоящий null (без «фейкового null» Unity).</returns>
        public static Sprite Get(IStationDefinition definition)
        {
            Sprite icon = definition is StationConfig config ? config.Icon : null;
            return icon != null ? icon : null;
        }

        /// <param name="label">Номер вместо картинки; null — подпись не трогать.</param>
        public static void Apply(Image plate, Image art, TMP_Text label, Sprite icon, Color color)
        {
            bool hasIcon = icon != null && art != null;
            if (art != null)
            {
                art.sprite = icon;
                art.enabled = hasIcon;
            }
            if (label != null)
                label.enabled = !hasIcon;
            plate.color = hasIcon ? new Color(color.r, color.g, color.b, PlateAlpha) : color;
        }
    }
}
