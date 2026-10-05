using UnityEngine;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Иконки интерфейса (пакет A, Docs/Visual/ASSET_PROMPTS.md). Заполняет меню ShaurmaTycoon/Art/Assign Icons
    /// из Sprites/UI/Icons по именам файлов; пустое поле — на его месте остаётся нарисованная кодом заглушка.
    /// </summary>
    [CreateAssetMenu(fileName = "UiIcons", menuName = "ShaurmaTycoon/UI Icons")]
    public sealed class UiIcons : ScriptableObject
    {
        [Header("Currencies")]
        public Sprite Coin;
        public Sprite Bill;
        public Sprite Reputation;
        public Sprite Income;

        [Header("Tabs")]
        public Sprite TabStations;
        public Sprite TabManagers;
        public Sprite TabUpgrades;
        public Sprite TabCity;
        public Sprite TabShop;

        [Header("Buttons")]
        public Sprite Settings;
        public Sprite Gift;
        public Sprite Calendar;
        public Sprite Quests;
        public Sprite Ad;
        public Sprite Boost;
        public Sprite Lock;
        public Sprite Offline;
        public Sprite Check;
        public Sprite Close;
        public Sprite TutorialHand;
        [Tooltip("Режим любования (этап V7).")]
        public Sprite Zen;
    }
}
