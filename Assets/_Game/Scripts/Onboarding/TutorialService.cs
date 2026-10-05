using System;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Managers;
using ShaurmaTycoon.Save;
using ShaurmaTycoon.Stations;

namespace ShaurmaTycoon.Onboarding
{
    /// <summary>
    /// Онбординг (этап 11, GDD §9): ведёт нового игрока по шагам «тап → уровень → вторая станция → менеджер».
    /// Шаг засчитывается по состоянию игры, а не по нажатию подсказки, поэтому старый сейв с прогрессом
    /// сразу проходит туториал, а действия «не по подсказке» тоже двигают его вперёд.
    /// Шаг хранится в SaveData.TutorialStep и не сбрасывается при переезде.
    /// </summary>
    public sealed class TutorialService : ISaveParticipant, IDisposable
    {
        private const int TapStation = 0;
        private const int SecondStation = 1;

        private readonly StationController _stations;
        private readonly ManagerController _managers;
        private readonly Wallet _wallet;
        private readonly Action _saveNow;
        private bool _tapDone;

        public TutorialService(StationController stations, ManagerController managers, Wallet wallet, Action saveNow)
        {
            _stations = stations ?? throw new ArgumentNullException(nameof(stations));
            _managers = managers ?? throw new ArgumentNullException(nameof(managers));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _saveNow = saveNow;

            if (_stations.Stations.Count > SecondStation)
            {
                _stations.Stations[TapStation].CycleCompleted += OnCycleCompleted;
                _stations.Stations[TapStation].Changed += OnStationChanged;
                _stations.Stations[SecondStation].Changed += OnStationChanged;
            }
            else
            {
                Step = TutorialStep.Done;
            }
            _managers.ManagerHired += OnManagerHired;
        }

        public TutorialStep Step { get; private set; }

        public bool IsActive => Step != TutorialStep.Done;

        /// <summary>Аргумент — новый шаг.</summary>
        public event Action<TutorialStep> StepChanged;

        /// <summary>
        /// Можно ли выполнить текущий шаг прямо сейчас. false — не хватает монет: подсказка показывает на тап,
        /// чтобы заработать.
        /// </summary>
        public bool CanDoCurrentStep
        {
            get
            {
                switch (Step)
                {
                    case TutorialStep.Tap:
                        return true;
                    case TutorialStep.BuyLevel:
                        return _wallet.CanAfford(_stations.Stations[TapStation].GetCost(1));
                    case TutorialStep.UnlockStation:
                        return _wallet.CanAfford(_stations.Stations[SecondStation].GetCost(1));
                    case TutorialStep.HireManager:
                        return _wallet.CanAfford(_managers.GetCost(TapStation));
                    default:
                        return false;
                }
            }
        }

        public void Apply(SaveData data)
        {
            int saved = data != null ? data.TutorialStep : 0;
            Step = saved < 0 ? TutorialStep.Tap : saved > (int)TutorialStep.Done ? TutorialStep.Done : (TutorialStep)saved;
            Refresh();
        }

        public void Capture(SaveData data)
        {
            data.TutorialStep = (int)Step;
        }

        /// <summary>Пропустить туториал (отладочное окно).</summary>
        public void Skip()
        {
            SetStep(TutorialStep.Done);
        }


        public void Dispose()
        {
            if (_stations.Stations.Count > SecondStation)
            {
                _stations.Stations[TapStation].CycleCompleted -= OnCycleCompleted;
                _stations.Stations[TapStation].Changed -= OnStationChanged;
                _stations.Stations[SecondStation].Changed -= OnStationChanged;
            }
            _managers.ManagerHired -= OnManagerHired;
        }

        private void OnCycleCompleted(StationModel station, double earned)
        {
            _tapDone = true;
            Refresh();
        }

        private void OnStationChanged(StationModel station)
        {
            Refresh();
        }

        private void OnManagerHired(int index)
        {
            Refresh();
        }

        /// <summary>Пропускает все шаги, условие которых (или любого следующего) уже выполнено.</summary>
        private void Refresh()
        {
            if (Step == TutorialStep.Done)
                return;

            TutorialStep next = Step;
            for (var candidate = TutorialStep.HireManager; candidate >= next; candidate--)
            {
                if (IsSatisfied(candidate))
                {
                    next = candidate + 1;
                    break;
                }
            }

            if (next != Step)
                SetStep(next);
        }

        private bool IsSatisfied(TutorialStep step)
        {
            StationModel first = _stations.Stations[TapStation];
            switch (step)
            {
                case TutorialStep.Tap:
                    return _tapDone || first.IsAutomated;
                case TutorialStep.BuyLevel:
                    return first.Level > Math.Max(1, first.Definition.StartLevel);
                case TutorialStep.UnlockStation:
                    return _stations.Stations[SecondStation].IsUnlocked;
                case TutorialStep.HireManager:
                    for (int i = 0; i < _managers.Count; i++)
                    {
                        if (_managers.IsHired(i))
                            return true;
                    }
                    return false;
                default:
                    return true;
            }
        }

        private void SetStep(TutorialStep step)
        {
            if (step == Step)
                return;

            Step = step;
            StepChanged?.Invoke(step);
            if (step == TutorialStep.Done)
                _saveNow?.Invoke();
        }
    }
}
