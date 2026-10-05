using System;
using System.Collections.Generic;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Managers;
using ShaurmaTycoon.Stations;
using ShaurmaTycoon.Upgrades;

namespace ShaurmaTycoon.Balance
{
    /// <summary>
    /// Прогон баланса города (этап 11): виртуальный игрок играет на настоящих моделях (станции, менеджеры, улучшения)
    /// и жадно покупает то, что быстрее всего окупается. Без рекламы, купюр, офлайна и репутации — «чистая» активная игра.
    /// Результат — время открытия станций, найма менеджеров и достижения порога переезда.
    /// </summary>
    public sealed class BalanceSimulator
    {
        private readonly IReadOnlyList<IStationDefinition> _stationDefinitions;
        private readonly IReadOnlyList<IUpgradeDefinition> _upgradeDefinitions;
        private readonly MilestoneTable _milestones;
        private readonly double _managerCostMultiplier;

        public BalanceSimulator(IReadOnlyList<IStationDefinition> stations, IReadOnlyList<IUpgradeDefinition> upgrades,
            MilestoneTable milestones, double managerCostMultiplier)
        {
            _stationDefinitions = stations ?? throw new ArgumentNullException(nameof(stations));
            _upgradeDefinitions = upgrades ?? throw new ArgumentNullException(nameof(upgrades));
            _milestones = milestones ?? throw new ArgumentNullException(nameof(milestones));
            _managerCostMultiplier = managerCostMultiplier;
        }

        public BalanceReport Run(BalanceSimulationSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            var wallet = new Wallet();
            var stations = new StationController(_stationDefinitions, _milestones, wallet);
            var managers = new ManagerController(stations, wallet, _managerCostMultiplier);
            var upgrades = new UpgradeService(_upgradeDefinitions, stations, wallet);
            var report = new BalanceReport(stations.Stations.Count, upgrades.Count);

            double earned = 0;
            stations.Earned += amount => earned += amount;

            for (int i = 0; i < stations.Stations.Count; i++)
            {
                if (stations.Stations[i].IsUnlocked)
                    report.StationUnlockedAt[i] = 0;
            }

            double time = 0;
            double nextTap = 0;
            double nextDecision = 0;
            double nextSample = 0;
            double dt = settings.TimeStep;

            while (time < settings.MaxSeconds)
            {
                // Тапы по клиенту запускают все простаивающие станции без менеджера.
                if (settings.TapInterval > 0 && time >= nextTap)
                {
                    int started = stations.TapAllIdle();
                    if (started > 0)
                        report.Taps++;
                    nextTap = time + settings.TapInterval;
                }

                stations.Tick((float)dt);
                time += dt;

                if (time >= nextDecision)
                {
                    BuyGreedy(stations, managers, upgrades, wallet, settings, report, time, _milestones.SpeedMultiplier);
                    nextDecision = time + settings.DecisionInterval;
                }

                if (time >= nextSample)
                {
                    report.Samples.Add(new BalanceSample(time, earned, IncomePerSecond(stations)));
                    nextSample = time + settings.SampleInterval;
                }

                if (report.ThresholdReachedAt < 0 && earned >= settings.PrestigeThreshold)
                {
                    report.ThresholdReachedAt = time;
                    if (settings.StopAtThreshold)
                        break;
                }
            }

            report.TotalEarned = earned;
            report.SimulatedSeconds = time;
            for (int i = 0; i < stations.Stations.Count; i++)
                report.FinalLevels[i] = stations.Stations[i].Level;
            return report;
        }

        /// <summary>Доход в секунду всех открытых станций (с менеджером или без — игрок тапает).</summary>
        private static double IncomePerSecond(StationController stations)
        {
            double total = 0;
            for (int i = 0; i < stations.Stations.Count; i++)
                total += stations.Stations[i].IncomePerSecond;
            return total;
        }

        /// <summary>
        /// Покупает лучшие по окупаемости покупки, пока хватает денег. Если лучшая покупка не по карману —
        /// копит на неё (не тратит на худшие), как делает внимательный игрок.
        /// </summary>
        private static void BuyGreedy(StationController stations, ManagerController managers, UpgradeService upgrades, Wallet wallet,
            BalanceSimulationSettings settings, BalanceReport report, double time, double milestoneSpeed)
        {
            for (int guard = 0; guard < 1000; guard++)
            {
                Candidate best = FindBest(stations, managers, upgrades, settings, milestoneSpeed);
                if (best.Kind == BalancePurchaseKind.None || !wallet.CanAfford(best.Cost))
                    return;

                switch (best.Kind)
                {
                    case BalancePurchaseKind.Level:
                        bool wasLocked = !stations.Stations[best.Index].IsUnlocked;
                        if (!stations.TryBuy(best.Index, 1))
                            return;
                        report.LevelsBought++;
                        if (wasLocked)
                            report.StationUnlockedAt[best.Index] = time;
                        break;
                    case BalancePurchaseKind.Manager:
                        if (!managers.TryHire(best.Index))
                            return;
                        report.ManagerHiredAt[best.Index] = time;
                        break;
                    case BalancePurchaseKind.Upgrade:
                        if (!upgrades.TryBuy(best.Index))
                            return;
                        report.UpgradeBoughtAt[best.Index] = time;
                        break;
                }

                report.Purchases.Add(new BalancePurchase(time, best.Kind, best.Index));
            }
        }

        private static Candidate FindBest(StationController stations, ManagerController managers, UpgradeService upgrades,
            BalanceSimulationSettings settings, double milestoneSpeed)
        {
            Candidate best = default;
            double bestPayback = double.MaxValue;
            IReadOnlyList<StationModel> list = stations.Stations;

            for (int i = 0; i < list.Count; i++)
            {
                StationModel station = list[i];
                double cost = station.GetCost(1);
                double gain = LevelGain(station, milestoneSpeed);
                Consider(ref best, ref bestPayback, BalancePurchaseKind.Level, i, cost, gain);

                if (managers.CanHire(i))
                {
                    // Без менеджера станция ждёт тапа: игрок тапает не всегда, поэтому менеджер ценится как весь доход станции,
                    // умноженный на вес (≥ 1 — нанимать охотнее, чем считает чистая окупаемость).
                    double managerGain = station.IncomePerSecond * settings.ManagerValueFactor;
                    Consider(ref best, ref bestPayback, BalancePurchaseKind.Manager, i, managers.GetCost(i), managerGain);
                }
            }

            for (int u = 0; u < upgrades.Count; u++)
            {
                if (upgrades.IsPurchased(u))
                    continue;

                IUpgradeDefinition definition = upgrades.GetDefinition(u);
                int target = upgrades.GetTargetStationIndex(u);
                double affected = 0;
                for (int i = 0; i < list.Count; i++)
                {
                    if (target == UpgradeService.AllStations || target == i)
                        affected += list[i].IncomePerSecond;
                }
                Consider(ref best, ref bestPayback, BalancePurchaseKind.Upgrade, u, definition.Cost, affected * (definition.IncomeMultiplier - 1));
            }

            return best;
        }

        /// <summary>Прибавка дохода/с от одного уровня, включая ускорение на вехе.</summary>
        private static double LevelGain(StationModel station, double milestoneSpeed)
        {
            IStationDefinition d = station.Definition;
            double perLevelMultiplier = station.IncomeMultiplier * station.GlobalIncomeMultiplier;
            double speedNow = d.CycleTime / station.CycleTime;
            double now = station.IsUnlocked ? d.BaseIncome * station.Level * perLevelMultiplier / station.CycleTime : 0;
            double speedNext = station.NextMilestone == station.Level + 1 ? speedNow * milestoneSpeed : speedNow;
            double next = d.BaseIncome * (station.Level + 1) * perLevelMultiplier * speedNext / d.CycleTime;
            return next - now;
        }


        private static void Consider(ref Candidate best, ref double bestPayback, BalancePurchaseKind kind, int index, double cost, double gain)
        {
            if (!(gain > 0) || double.IsInfinity(cost))
                return;

            double payback = cost / gain;
            if (payback < bestPayback)
            {
                bestPayback = payback;
                best = new Candidate(kind, index, cost);
            }
        }

        private readonly struct Candidate
        {
            public Candidate(BalancePurchaseKind kind, int index, double cost)
            {
                Kind = kind;
                Index = index;
                Cost = cost;
            }

            public BalancePurchaseKind Kind { get; }
            public int Index { get; }
            public double Cost { get; }
        }
    }
}
