using ShaurmaTycoon.Economy;
using TMPro;

namespace ShaurmaTycoon.UI
{
    public static class TextExtensions
    {
        /// <summary>Выводит NumberText в TMP без создания строки.</summary>
        public static void SetNumberText(this TMP_Text text, NumberText value)
        {
            text.SetCharArray(value.Buffer, 0, value.Length);
        }
    }
}
