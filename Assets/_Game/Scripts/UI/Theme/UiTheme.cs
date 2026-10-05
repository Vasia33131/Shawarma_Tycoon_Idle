using UnityEngine;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Палитра и тайминги интерфейса «Уютный вечер» (Docs/Visual/ART_DIRECTION.md). Читает Build Game UI:
    /// все цвета и параметры анимаций экрана берутся отсюда. После правки — пересобрать UI;
    /// после правки Button Lip Shade или Card Shade — ещё ShaurmaTycoon/Art/Generate UI Sprites.
    /// </summary>
    [CreateAssetMenu(fileName = "UiTheme", menuName = "ShaurmaTycoon/UI Theme")]
    public sealed class UiTheme : ScriptableObject
    {
        [Header("Text and lines")]
        public Color Outline = new Color32(0x4A, 0x2C, 0x1D, 0xFF);
        public Color TextDark = new Color32(0x3B, 0x24, 0x18, 0xFF);
        public Color TextMuted = new Color32(0x8A, 0x6B, 0x57, 0xFF);
        public Color TextLight = new Color32(0xFF, 0xF6, 0xE8, 0xFF);

        [Header("Surfaces")]
        public Color Panel = new Color32(0xFF, 0xF1, 0xDC, 0xFF);
        public Color Row = new Color32(0xFF, 0xF8, 0xEE, 0xFF);
        public Color RowShade = new Color32(0xEB, 0xD6, 0xBC, 0xFF);
        public Color ScreenBackground = new Color32(0xF6, 0xE6, 0xCF, 0xFF);
        public Color HudBackground = new Color32(0x5B, 0x3A, 0x29, 0xFF);
        [Tooltip("Затемнение под попапами (альфа — итоговая непрозрачность).")]
        public Color Overlay = new Color32(0x2A, 0x1A, 0x2E, 0x99);

        [Header("Buttons")]
        public Color Primary = new Color32(0x7C, 0xC4, 0x7F, 0xFF);
        public Color Secondary = new Color32(0xF4, 0xA2, 0x59, 0xFF);
        public Color Neutral = new Color32(0xA9, 0x84, 0x67, 0xFF);
        public Color Disabled = new Color32(0xD8, 0xC8, 0xB8, 0xFF);
        [Tooltip("Rewarded-кнопки (реклама).")]
        public Color Reward = new Color32(0xFF, 0xC2, 0x4D, 0xFF);

        [Header("Accents")]
        public Color Gold = new Color32(0xFF, 0xCF, 0x5A, 0xFF);
        public Color LightGold = new Color32(0xFF, 0xE8, 0xB0, 0xFF);
        public Color CoinInner = new Color32(0xF2, 0x9E, 0x4C, 0xFF);
        public Color Glow = new Color32(0xFF, 0xE3, 0xA1, 0xFF);
        public Color Reputation = new Color32(0x9B, 0x7F, 0xD4, 0xFF);
        public Color ReputationLight = new Color32(0xD9, 0xCC, 0xF2, 0xFF);
        public Color Bills = new Color32(0x5F, 0xB9, 0x8A, 0xFF);
        public Color BillsLight = new Color32(0xC8, 0xEB, 0xD5, 0xFF);
        public Color Badge = new Color32(0xE5, 0x67, 0x5A, 0xFF);
        public Color FloatingText = new Color32(0xC9, 0x74, 0x2E, 0xFF);

        [Header("Scene (до этапа V3: процедурный ларёк, улица, карта)")]
        public Color Sky = new Color32(0xFF, 0xD7, 0xA0, 0xFF);
        public Color Pavement = new Color32(0xB9, 0x9A, 0x86, 0xFF);
        public Color Curb = new Color32(0x8F, 0x72, 0x62, 0xFF);
        public Color Booth = new Color32(0xD9, 0x60, 0x4A, 0xFF);
        public Color WindowDark = new Color32(0x3E, 0x26, 0x1C, 0xFF);
        public Color Wood = new Color32(0xA8, 0x6E, 0x45, 0xFF);
        public Color Meat = new Color32(0xA8, 0x62, 0x30, 0xFF);
        public Color MeatDark = new Color32(0x7A, 0x42, 0x1E, 0xFF);
        public Color Awning = new Color32(0xFF, 0xF4, 0xE6, 0xFF);
        public Color Steel = new Color32(0xA3, 0x9E, 0xA6, 0xFF);
        public Color Leaf = new Color32(0x7C, 0xB8, 0x6A, 0xFF);
        public Color LeafDark = new Color32(0x4F, 0x8A, 0x4E, 0xFF);
        public Color Land = new Color32(0xE4, 0xE6, 0xC0, 0xFF);
        public Color Hill = new Color32(0xC9, 0xD6, 0xA3, 0xFF);
        public Color Water = new Color32(0xA9, 0xCF, 0xE3, 0xFF);
        public Color Paper = new Color32(0xFF, 0xFB, 0xF2, 0xFF);
        public Color GiftRed = new Color32(0xE0, 0x5D, 0x50, 0xFF);
        public Color LampOff = new Color32(0x3E, 0x26, 0x1C, 0x73);

        [Header("Generated sprites")]
        [Tooltip("Яркость нижнего «бортика» кнопки относительно лица (запекается в button_fill.png).")]
        [Range(0.4f, 1f)] public float ButtonLipShade = 0.72f;
        [Tooltip("Толщина бортика, px текстуры: обычная и нажатая кнопка.")]
        public int ButtonLip = 10;
        public int ButtonLipPressed = 4;
        [Tooltip("Толщина нижней полосы карточек, px.")]
        public int CardShadeHeight = 6;

        [Header("Cards")]
        [Range(0f, 1f)] public float CardOutlineAlpha = 0.35f;

        [Header("Button press")]
        public float PressOffset = 6f;
        public float PressScale = 0.97f;
        public float PressDuration = 0.06f;
        public float ReleaseDuration = 0.18f;

        [Header("Calm motion")]
        public float PulseAmplitude = 0.04f;
        public float PulseSpeed = 2.5f;
        [Range(0f, 1f)] public float GlowMinAlpha = 0.5f;
        [Range(0f, 1f)] public float GlowMaxAlpha = 0.8f;
        public float GlowPeriod = 2f;

        [Header("Popups")]
        public float PopupShowDuration = 0.25f;
        public float PopupHideDuration = 0.12f;
        public float PopupOverlayFade = 0.2f;
        public float PopupStartScale = 0.92f;
        public float PopupStartOffset = 40f;

        [Header("Tabs")]
        public float TabRaise = 10f;
        public float TabTransition = 0.15f;

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
