using UnityEngine;

namespace ShaurmaTycoon.Retention
{
    /// <summary>
    /// Баланс удержания (GDD §8–9): календарь, задания дня, буст ×2, подарок-ящик, скин ларька.
    /// Монеты в наградах — в минутах дохода, но не меньше «минимума за минуту» × масштаб города.
    /// </summary>
    [CreateAssetMenu(menuName = "ShaurmaTycoon/Retention Config", fileName = "Retention")]
    public sealed class RetentionConfig : ScriptableObject
    {
        [Header("Daily reward calendar (GDD §9)")]
        [Tooltip("7 дней. Монеты — минуты дохода. Награды — только монеты и купюры (скин ларька убран из календаря).")]
        [SerializeField] private RewardDefinition[] _days =
        {
            new RewardDefinition(RewardKind.Coins, 10),
            new RewardDefinition(RewardKind.Bills, 2),
            new RewardDefinition(RewardKind.Coins, 30),
            new RewardDefinition(RewardKind.Bills, 3),
            new RewardDefinition(RewardKind.Coins, 60),
            new RewardDefinition(RewardKind.Bills, 5),
            new RewardDefinition(RewardKind.Bills, 10),
        };
        [Tooltip("Купюры вместо скина, когда скин уже получен.")]
        [SerializeField] private int _skinRepeatBills = 10;
        [Tooltip("Цвет ларька со скином «Золотой ларёк» (вместо цвета города).")]
        [SerializeField] private Color _skinBoothColor = new Color(0.93f, 0.68f, 0.12f);

        [Header("Coin rewards")]
        [Tooltip("Минимум монет за «минуту дохода» в городе 1 (× масштаб города): в начале игры дохода ещё нет.")]
        [SerializeField] private double _minCoinsPerMinute = 20;

        [Header("Daily quests (GDD §9)")]
        [SerializeField] private int _questsPerDay = 3;
        [Tooltip("Пул заданий: в день выдаются разные типы. Монеты — минуты дохода.")]
        [SerializeField] private QuestTemplate[] _quests =
        {
            new QuestTemplate(QuestType.BuyLevels, 50, new RewardDefinition(RewardKind.Coins, 20)),
            new QuestTemplate(QuestType.HireManager, 1, new RewardDefinition(RewardKind.Reputation, 2)),
            new QuestTemplate(QuestType.BuyUpgrade, 3, new RewardDefinition(RewardKind.Coins, 30)),
            new QuestTemplate(QuestType.WatchBoost, 1, new RewardDefinition(RewardKind.Reputation, 1)),
            new QuestTemplate(QuestType.OpenGift, 2, new RewardDefinition(RewardKind.Coins, 15)),
        };

        [Header("Income boost (GDD §8)")]
        [SerializeField] private double _boostMultiplier = 2;
        [Tooltip("Минут буста за одну рекламу.")]
        [SerializeField] private double _boostMinutesPerAd = 4;
        [Tooltip("Сколько часов буста можно накопить.")]
        [SerializeField] private double _boostMaxHours = 4;

        [Header("Gift box (GDD §8)")]
        [SerializeField] private double _giftCooldownMinutes = 10;
        [Tooltip("Подарок = столько минут дохода.")]
        [SerializeField] private double _giftIncomeMinutes = 5;

        public RewardDefinition[] Days => _days;
        public int SkinRepeatBills => _skinRepeatBills;
        public Color SkinBoothColor => _skinBoothColor;
        public double MinCoinsPerMinute => _minCoinsPerMinute;
        public int QuestsPerDay => _questsPerDay;
        public QuestTemplate[] Quests => _quests;
        public double GiftCooldownMinutes => _giftCooldownMinutes;
        public double GiftIncomeMinutes => _giftIncomeMinutes;

        public IncomeBoost CreateBoost()
        {
            return new IncomeBoost(_boostMultiplier, _boostMinutesPerAd * 60, _boostMaxHours * 3600);
        }

        private void OnValidate()
        {
            if (_skinRepeatBills < 0)
                _skinRepeatBills = 0;
            if (_minCoinsPerMinute < 0)
                _minCoinsPerMinute = 0;
            if (_questsPerDay < 1)
                _questsPerDay = 1;
            if (_boostMultiplier < 1)
                _boostMultiplier = 1;
            if (_boostMinutesPerAd < 1)
                _boostMinutesPerAd = 1;
            if (_boostMaxHours < 0)
                _boostMaxHours = 0;
            if (_giftCooldownMinutes < 0)
                _giftCooldownMinutes = 0;
            if (_giftIncomeMinutes < 0)
                _giftIncomeMinutes = 0;
        }
    }
}
