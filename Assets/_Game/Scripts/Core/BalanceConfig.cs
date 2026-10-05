using System;
using ShaurmaTycoon.Offline;
using ShaurmaTycoon.Prestige;
using ShaurmaTycoon.Stations;
using ShaurmaTycoon.Shop;
using UnityEngine;

namespace ShaurmaTycoon.Core
{
    /// <summary>
    /// Глобальный баланс, общий для всех городов.
    /// </summary>
    [CreateAssetMenu(menuName = "ShaurmaTycoon/Balance Config", fileName = "Balance")]
    public sealed class BalanceConfig : ScriptableObject
    {
        [Tooltip("Уровни станции, на которых цикл ускоряется (GDD §5).")]
        [SerializeField] private int[] _milestoneLevels = { 25, 50, 100, 200, 300, 400 };
        [Tooltip("Во сколько раз ускоряется цикл на каждой вехе.")]
        [SerializeField] private double _milestoneSpeedMultiplier = 2;
        [Tooltip("Цена менеджера = base cost станции × это число (GDD §5).")]
        [SerializeField] private double _managerCostMultiplier = 1000;

        [Header("Offline income (GDD §6)")]
        [Tooltip("Доля дохода в секунду, которая начисляется офлайн.")]
        [SerializeField] private double _offlineIncomeRate = 0.5;
        [Tooltip("Сколько часов отсутствия засчитывается (без бонусов репутации).")]
        [SerializeField] private double _offlineLimitHours = 2;
        [Tooltip("Окно офлайн-дохода показывается, только если игрока не было хотя бы столько секунд.")]
        [SerializeField] private double _offlineMinAwaySeconds = 60;
        [Tooltip("Множитель кнопки «Забрать ×2» за рекламу.")]
        [SerializeField] private double _offlineAdMultiplier = 2;

        [Header("Prestige (GDD §7)")]
        [Tooltip("Всего репутации = floor(sqrt(заработано за всё время / это число)).")]
        [SerializeField] private double _reputationDivisor = 1e7;
        [Tooltip("Прибавка к доходу за единицу полученной репутации (0.02 = +2%).")]
        [SerializeField] private double _incomeBonusPerReputation = 0.02;

        [Header("Bills for ads (08f)")]
        [Tooltip("Сколько купюр за досмотренную рекламу.")]
        [SerializeField] private int _adBillsReward = 2;
        [Tooltip("Минут между просмотрами.")]
        [SerializeField] private double _adBillsCooldownMinutes = 10;
        [Tooltip("Просмотров за сутки UTC.")]
        [SerializeField] private int _adBillsDailyLimit = 6;

        public double ManagerCostMultiplier => _managerCostMultiplier;

        public AdBillReward CreateAdBillReward()
        {
            return new AdBillReward(_adBillsReward, TimeSpan.FromMinutes(_adBillsCooldownMinutes), _adBillsDailyLimit);
        }

        public PrestigeRules CreatePrestigeRules()
        {
            return new PrestigeRules(_reputationDivisor, _incomeBonusPerReputation);
        }
        public double OfflineAdMultiplier => _offlineAdMultiplier;
        public double OfflineLimitHours => _offlineLimitHours;

        public MilestoneTable CreateMilestoneTable()
        {
            return new MilestoneTable(_milestoneLevels ?? new int[0], _milestoneSpeedMultiplier);
        }

        public OfflineSettings CreateOfflineSettings()
        {
            return new OfflineSettings(_offlineIncomeRate, _offlineLimitHours * 3600, _offlineMinAwaySeconds);
        }

        private void OnValidate()
        {
            if (_milestoneSpeedMultiplier < 1)
                _milestoneSpeedMultiplier = 1;
            if (_managerCostMultiplier < 0)
                _managerCostMultiplier = 0;
            if (_offlineIncomeRate < 0)
                _offlineIncomeRate = 0;
            if (_offlineLimitHours < 0)
                _offlineLimitHours = 0;
            if (_offlineMinAwaySeconds < 0)
                _offlineMinAwaySeconds = 0;
            if (_reputationDivisor < 1)
                _reputationDivisor = 1;
            if (_incomeBonusPerReputation < 0)
                _incomeBonusPerReputation = 0;
            if (_offlineAdMultiplier < 1)
                _offlineAdMultiplier = 1;
            if (_adBillsReward < 1)
                _adBillsReward = 1;
            if (_adBillsCooldownMinutes < 0)
                _adBillsCooldownMinutes = 0;
            if (_adBillsDailyLimit < 1)
                _adBillsDailyLimit = 1;
        }
    }
}
