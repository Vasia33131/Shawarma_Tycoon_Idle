using System;
using ShaurmaTycoon.Economy;

namespace ShaurmaTycoon.Stations
{
    /// <summary>
    /// Состояние и логика одной станции: уровень, цикл производства, покупка уровней.
    /// Без менеджера цикл запускается тапом; с менеджером (IsAutomated) — сам, без остановки.
    /// </summary>
    public sealed class StationModel
    {
        private readonly IStationDefinition _definition;
        private readonly MilestoneTable _milestones;
        private double _speedMultiplier;

        public StationModel(IStationDefinition definition, MilestoneTable milestones)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _milestones = milestones ?? throw new ArgumentNullException(nameof(milestones));

            Level = definition.StartLevel;
            _speedMultiplier = _milestones.GetSpeedMultiplier(Level);
        }

        /// <summary>Аргумент — сама станция.</summary>
        public event Action<StationModel> LevelChanged;

        public IStationDefinition Definition => _definition;
        public int Level { get; private set; }
        public bool IsUnlocked => Level > 0;
        public bool IsRunning { get; private set; }

        /// <summary>Цикл перезапускается сам (менеджер, этап 04).</summary>
        public bool IsAutomated { get; set; }

        /// <summary>Доля текущего цикла, 0..1. Сохраняется при ускорении на вехе.</summary>
        public double Progress { get; private set; }

        public double CycleTime => _definition.CycleTime / _speedMultiplier;
        public double IncomePerCycle => _definition.BaseIncome * Level;
        public double IncomePerSecond => IsUnlocked ? IncomePerCycle / CycleTime : 0;
        public int NextMilestone => _milestones.GetNextMilestone(Level);

        /// <summary>Запуск цикла тапом. false — станция закрыта или цикл уже идёт.</summary>
        public bool TryStartCycle()
        {
            if (!IsUnlocked || IsRunning)
                return false;

            IsRunning = true;
            Progress = 0;
            return true;
        }

        /// <summary>Продвигает цикл и возвращает заработанные за deltaTime монеты.</summary>
        public double Advance(double deltaTime)
        {
            if (!IsUnlocked || !(deltaTime > 0))
                return 0;

            if (!IsRunning)
            {
                if (!IsAutomated)
                    return 0;
                IsRunning = true;
                Progress = 0;
            }

            Progress += deltaTime / CycleTime;
            if (Progress < 1)
                return 0;

            if (!IsAutomated)
            {
                Progress = 0;
                IsRunning = false;
                return IncomePerCycle;
            }

            // На высоких вехах цикл может быть короче кадра — считаем все завершённые циклы.
            double cycles = Math.Floor(Progress);
            Progress -= cycles;
            return cycles * IncomePerCycle;
        }

        public double GetCost(int count)
        {
            return EconomyMath.BulkCost(_definition.BaseCost, _definition.CostGrowth, Level, count);
        }

        public int GetMaxAffordable(double money)
        {
            return EconomyMath.MaxAffordable(_definition.BaseCost, _definition.CostGrowth, Level, money);
        }

        public bool TryBuy(Wallet wallet, int count)
        {
            if (wallet == null)
                throw new ArgumentNullException(nameof(wallet));
            if (count <= 0 || !wallet.TrySpend(GetCost(count)))
                return false;

            Level += count;
            _speedMultiplier = _milestones.GetSpeedMultiplier(Level);
            LevelChanged?.Invoke(this);
            return true;
        }
    }
}
