using System;
using ShaurmaTycoon.Core;
using ShaurmaTycoon.Platform;
using ShaurmaTycoon.Save;

namespace ShaurmaTycoon.Retention
{
    /// <summary>
    /// Буст «×2 доход» за рекламу (GDD §8). Время добавляется только после досмотра; идёт по тикам игры
    /// (на паузе рекламы и офлайн не тратится). Сам является участником сейва (SaveData.Retention.BoostSeconds).
    /// </summary>
    public sealed class BoostService : ISaveParticipant, ITickable
    {
        private readonly IncomeBoost _boost;
        private readonly IPlatformService _platform;
        private readonly Action _saveNow;

        public BoostService(IncomeBoost boost, IPlatformService platform, Action saveNow)
        {
            _boost = boost ?? throw new ArgumentNullException(nameof(boost));
            _platform = platform;
            _saveNow = saveNow;
        }

        /// <summary>Буст включился или закончился — множитель дохода поменялся.</summary>
        public event Action MultiplierChanged;

        /// <summary>Реклама досмотрена, время добавлено.</summary>
        public event Action Watched;

        public IncomeBoost Boost => _boost;
        public bool IsShowing { get; private set; }
        public bool CanWatch => _platform != null && !IsShowing && _boost.CanAdd;

        /// <returns>false — реклама не запускалась (запас полон, уже показывается, нет платформы).</returns>
        public bool TryWatch(Action<bool> onDone)
        {
            if (!CanWatch)
                return false;

            IsShowing = true;
            _platform.ShowRewarded(completed =>
            {
                IsShowing = false;
                bool wasActive = _boost.IsActive;
                bool added = completed && _boost.Add();
                if (added)
                {
                    _saveNow?.Invoke();
                    if (!wasActive)
                        MultiplierChanged?.Invoke();
                    Watched?.Invoke();
                }
                onDone?.Invoke(added);
            });
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (_boost.Tick(deltaTime))
                MultiplierChanged?.Invoke();
        }

        public void Capture(SaveData data)
        {
            RetentionSave save = data.Retention ?? (data.Retention = new RetentionSave());
            save.BoostSeconds = _boost.RemainingSeconds;
        }

        public void Apply(SaveData data)
        {
            _boost.Load(data.Retention != null ? data.Retention.BoostSeconds : 0);
        }
    }
}
