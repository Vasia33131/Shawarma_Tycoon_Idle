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
        private double _incomeMultiplier = 1;
        private double _globalIncomeMultiplier = 1;
        private double _globalSpeedMultiplier = 1;
        private bool _isAutomated;

        public StationModel(IStationDefinition definition, MilestoneTable milestones)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _milestones = milestones ?? throw new ArgumentNullException(nameof(milestones));

            Level = definition.StartLevel;
            _speedMultiplier = _milestones.GetSpeedMultiplier(Level);
        }

        /// <summary>Аргумент — сама станция.</summary>
        public event Action<StationModel> LevelChanged;

        /// <summary>Изменилось что-то, что видно игроку: уровень, автоматизация или множитель дохода.</summary>
        public event Action<StationModel> Changed;

        /// <summary>Завершился один или несколько циклов за кадр; аргументы — станция и доход за эти циклы.</summary>
        public event Action<StationModel, double> CycleCompleted;

        public IStationDefinition Definition => _definition;
        public int Level { get; private set; }
        public bool IsUnlocked => Level > 0;
        public bool IsRunning { get; private set; }

        /// <summary>Цикл перезапускается сам (нанят менеджер).</summary>
        public bool IsAutomated
        {
            get => _isAutomated;
            set
            {
                if (_isAutomated == value)
                    return;
                _isAutomated = value;
                Changed?.Invoke(this);
            }
        }

        /// <summary>Множитель дохода от апгрейдов (GDD §5, upgradesMult).</summary>
        public double IncomeMultiplier
        {
            get => _incomeMultiplier;
            set
            {
                if (!(value > 0) || double.IsInfinity(value))
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Must be positive and finite.");
                if (_incomeMultiplier == value)
                    return;
                _incomeMultiplier = value;
                Changed?.Invoke(this);
            }
        }

        /// <summary>Множитель дохода, общий для всех станций (престиж, позже — бусты).</summary>
        public double GlobalIncomeMultiplier => _globalIncomeMultiplier;

        /// <summary>Ускорение циклов, общее для всех станций (магазин репутации).</summary>
        public double GlobalSpeedMultiplier => _globalSpeedMultiplier;

        /// <summary>Выставляет общие множители; доля текущего цикла сохраняется.</summary>
        public void SetGlobalModifiers(double incomeMultiplier, double speedMultiplier)
        {
            if (!(incomeMultiplier > 0) || double.IsInfinity(incomeMultiplier))
                throw new ArgumentOutOfRangeException(nameof(incomeMultiplier), incomeMultiplier, "Must be positive and finite.");
            if (!(speedMultiplier > 0) || double.IsInfinity(speedMultiplier))
                throw new ArgumentOutOfRangeException(nameof(speedMultiplier), speedMultiplier, "Must be positive and finite.");
            if (_globalIncomeMultiplier == incomeMultiplier && _globalSpeedMultiplier == speedMultiplier)
                return;

            _globalIncomeMultiplier = incomeMultiplier;
            _globalSpeedMultiplier = speedMultiplier;
            Changed?.Invoke(this);
        }

        /// <summary>Доля текущего цикла, 0..1. Сохраняется при ускорении на вехе.</summary>
        public double Progress { get; private set; }

        public double CycleTime => _definition.CycleTime / (_speedMultiplier * _globalSpeedMultiplier);
        public double IncomePerCycle => _definition.BaseIncome * Level * _incomeMultiplier * _globalIncomeMultiplier;
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

            double earned;
            if (!IsAutomated)
            {
                Progress = 0;
                IsRunning = false;
                earned = IncomePerCycle;
            }
            else
            {
                // На высоких вехах цикл может быть короче кадра — считаем все завершённые циклы.
                double cycles = Math.Floor(Progress);
                Progress -= cycles;
                earned = cycles * IncomePerCycle;
            }

            CycleCompleted?.Invoke(this, earned);
            return earned;
        }

        /// <summary>
        /// Восстановление из сохранения. Некорректные значения исправляются:
        /// уровень не меньше 0, менеджер и цикл только у открытой станции, прогресс в [0, 1).
        /// </summary>
        public void Restore(int level, bool automated, double progress, bool running)
        {
            Level = Math.Max(0, level);
            _speedMultiplier = _milestones.GetSpeedMultiplier(Level);
            _isAutomated = automated && IsUnlocked;
            IsRunning = running && IsUnlocked;
            Progress = IsRunning && progress >= 0 && progress < 1 ? progress : 0;

            Changed?.Invoke(this);
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

            AddLevels(count);
            return true;
        }

        /// <summary>Уровни без оплаты монетами (уже оплачено купюрами).</summary>
        public void AddLevels(int count)
        {
            if (count <= 0)
                return;

            Level += count;
            _speedMultiplier = _milestones.GetSpeedMultiplier(Level);
            LevelChanged?.Invoke(this);
            Changed?.Invoke(this);
        }
    }
}
