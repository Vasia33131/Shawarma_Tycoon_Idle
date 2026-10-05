using System;
using ShaurmaTycoon.Save;

namespace ShaurmaTycoon.Retention
{
    /// <summary>
    /// Ежедневная награда (GDD §9): календарь из 7 дней, день 7 — скин ларька (если уже есть — купюры).
    /// Награда без рекламы, сразу сохраняется. Сам является участником сейва (SaveData.Retention).
    /// </summary>
    public sealed class DailyRewardService : ISaveParticipant
    {
        private readonly DailyRewardCalendar _calendar;
        private readonly RewardDefinition[] _days;
        private readonly int _skinRepeatBills;
        private readonly RewardGranter _granter;
        private readonly Func<DateTime> _utcNow;
        private readonly Action _saveNow;

        public DailyRewardService(DailyRewardCalendar calendar, RewardDefinition[] days, int skinRepeatBills, RewardGranter granter,
            Func<DateTime> utcNow, Action saveNow)
        {
            _calendar = calendar ?? throw new ArgumentNullException(nameof(calendar));
            _days = days ?? new RewardDefinition[0];
            _skinRepeatBills = Math.Max(0, skinRepeatBills);
            _granter = granter ?? throw new ArgumentNullException(nameof(granter));
            _utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
            _saveNow = saveNow;
        }

        /// <summary>Аргументы — индекс выданного дня (0..6) и награда.</summary>
        public event Action<int, RewardGrant> Claimed;

        /// <summary>Скин ларька только что получен.</summary>
        public event Action SkinUnlocked;

        public DailyRewardCalendar Calendar => _calendar;
        public bool HasSkin { get; private set; }
        public DateTime Now => _utcNow();
        public bool CanClaim => _calendar.CanClaim(_utcNow());

        /// <summary>Награда дня с учётом уже полученного скина.</summary>
        public RewardDefinition GetDay(int index)
        {
            RewardDefinition day = index >= 0 && index < _days.Length ? _days[index] : new RewardDefinition(RewardKind.Coins, 0);
            if (day.Kind == RewardKind.KioskSkin && HasSkin)
                return new RewardDefinition(RewardKind.Bills, _skinRepeatBills);
            return day;
        }

        public RewardGrant Preview(int index)
        {
            return _granter.Preview(GetDay(index));
        }

        public bool TryClaim(out RewardGrant grant)
        {
            grant = default;
            int day = _calendar.Claim(_utcNow());
            if (day < 0)
                return false;

            RewardDefinition reward = GetDay(day);
            grant = _granter.Grant(reward);
            bool skin = reward.Kind == RewardKind.KioskSkin;
            if (skin)
                HasSkin = true;

            _saveNow?.Invoke();
            Claimed?.Invoke(day, grant);
            if (skin)
                SkinUnlocked?.Invoke();
            return true;
        }

        public void Capture(SaveData data)
        {
            RetentionSave save = data.Retention ?? (data.Retention = new RetentionSave());
            save.DailyNextDay = _calendar.NextDay;
            save.DailyLastClaimDayTicks = _calendar.LastClaimDayTicks;
            save.KioskSkinOwned = HasSkin;
        }

        public void Apply(SaveData data)
        {
            RetentionSave save = data.Retention ?? new RetentionSave();
            _calendar.Load(save.DailyNextDay, save.DailyLastClaimDayTicks);
            HasSkin = save.KioskSkinOwned;
        }
    }
}
