using System;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Platform;
using ShaurmaTycoon.Save;

namespace ShaurmaTycoon.Shop
{
    /// <summary>
    /// Купюры за рекламу: по нажатию игрока показывает rewarded и только после досмотра начисляет купюры
    /// и запускает кулдаун. Закрытая раньше времени реклама ничего не даёт и кулдаун не тратит.
    /// Сохраняется сразу (участник сейва — сам сервис).
    /// </summary>
    public sealed class AdBillService : ISaveParticipant
    {
        private readonly AdBillReward _rules;
        private readonly BillWallet _bills;
        private readonly IPlatformService _platform;
        private readonly Func<DateTime> _utcNow;
        private readonly Action _saveNow;

        /// <param name="utcNow">Серверное время UTC (обычно platform.ServerTimeUtc).</param>
        public AdBillService(AdBillReward rules, BillWallet bills, IPlatformService platform, Func<DateTime> utcNow, Action saveNow)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _bills = bills ?? throw new ArgumentNullException(nameof(bills));
            _platform = platform;
            _utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
            _saveNow = saveNow;
        }

        /// <summary>Аргумент — сколько купюр начислено.</summary>
        public event Action<long> Rewarded;

        public AdBillReward Rules => _rules;

        /// <summary>Реклама открыта — повторное нажатие игнорируется.</summary>
        public bool IsShowing { get; private set; }

        public DateTime Now => _utcNow();

        public bool CanWatch => _platform != null && !IsShowing && _rules.CanClaim(_utcNow());

        /// <returns>false — реклама не запускалась (кулдаун, лимит, уже показывается, нет платформы).</returns>
        public bool TryWatch(Action<bool> onDone)
        {
            if (!CanWatch)
                return false;

            IsShowing = true;
            _platform.ShowRewarded(completed =>
            {
                IsShowing = false;
                bool rewarded = completed && _rules.Claim(_utcNow());
                if (rewarded)
                {
                    _bills.Add(_rules.Reward, BillSource.Ad);
                    _saveNow?.Invoke();
                    Rewarded?.Invoke(_rules.Reward);
                }
                onDone?.Invoke(rewarded);
            });
            return true;
        }

        public void Capture(SaveData data)
        {
            AdRewardSave save = data.AdBills ?? (data.AdBills = new AdRewardSave());
            save.LastClaimUtcTicks = _rules.LastClaimUtcTicks;
            save.DayUtcTicks = _rules.DayUtcTicks;
            save.ClaimsToday = _rules.ClaimsToday;
        }

        public void Apply(SaveData data)
        {
            AdRewardSave save = data.AdBills ?? new AdRewardSave();
            _rules.Load(save.LastClaimUtcTicks, save.DayUtcTicks, save.ClaimsToday);
        }
    }
}
