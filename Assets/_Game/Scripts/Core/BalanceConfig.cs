using ShaurmaTycoon.Stations;
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

        public double ManagerCostMultiplier => _managerCostMultiplier;

        public MilestoneTable CreateMilestoneTable()
        {
            return new MilestoneTable(_milestoneLevels ?? new int[0], _milestoneSpeedMultiplier);
        }

        private void OnValidate()
        {
            if (_milestoneSpeedMultiplier < 1)
                _milestoneSpeedMultiplier = 1;
            if (_managerCostMultiplier < 0)
                _managerCostMultiplier = 0;
        }
    }
}
