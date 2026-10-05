using System;

namespace ShaurmaTycoon.Shop
{
    /// <summary>
    /// Правила купюр за рекламу (этап 08f): не чаще раза в cooldown и не больше dailyLimit раз за сутки UTC.
    /// Время — серверное UTC от платформы. Если часы ушли назад, «последний просмотр» прижимается к now:
    /// награды сразу нет, но и кулдаун не растягивается навсегда.
    /// </summary>
    public sealed class AdBillReward
    {
        private readonly TimeSpan _cooldown;
        private long _lastClaimTicks;
        private long _dayTicks;

        public AdBillReward(long reward, TimeSpan cooldown, int dailyLimit)
        {
            Reward = Math.Max(1, reward);
            _cooldown = cooldown > TimeSpan.Zero ? cooldown : TimeSpan.Zero;
            DailyLimit = Math.Max(1, dailyLimit);
        }

        public long Reward { get; }
        public int DailyLimit { get; }

        /// <summary>Сколько раз награда получена за сутки, к которым относится последний просмотр.</summary>
        public int ClaimsToday { get; private set; }

        public long LastClaimUtcTicks => _lastClaimTicks;
        public long DayUtcTicks => _dayTicks;

        public int RemainingToday(DateTime nowUtc)
        {
            Normalize(nowUtc);
            return Math.Max(0, DailyLimit - ClaimsToday);
        }

        public bool IsDailyLimitReached(DateTime nowUtc)
        {
            return RemainingToday(nowUtc) == 0;
        }

        public bool CanClaim(DateTime nowUtc)
        {
            return !IsDailyLimitReached(nowUtc) && CooldownLeft(nowUtc) <= TimeSpan.Zero;
        }

        /// <summary>Сколько ждать: до конца кулдауна или, если лимит исчерпан, до полуночи UTC.</summary>
        public TimeSpan TimeLeft(DateTime nowUtc)
        {
            if (IsDailyLimitReached(nowUtc))
                return nowUtc.Date.AddDays(1) - nowUtc;

            TimeSpan left = CooldownLeft(nowUtc);
            return left > TimeSpan.Zero ? left : TimeSpan.Zero;
        }

        /// <summary>Засчитать просмотр. Вызывать только после досмотренной рекламы.</summary>
        public bool Claim(DateTime nowUtc)
        {
            if (!CanClaim(nowUtc))
                return false;

            _lastClaimTicks = nowUtc.Ticks;
            ClaimsToday++;
            return true;
        }

        public void Load(long lastClaimUtcTicks, long dayUtcTicks, int claimsToday)
        {
            _lastClaimTicks = Math.Max(0, lastClaimUtcTicks);
            _dayTicks = Math.Max(0, dayUtcTicks);
            ClaimsToday = Math.Max(0, claimsToday);
        }

        private TimeSpan CooldownLeft(DateTime nowUtc)
        {
            Normalize(nowUtc);
            if (_lastClaimTicks == 0)
                return TimeSpan.Zero;
            return new DateTime(_lastClaimTicks, DateTimeKind.Utc) + _cooldown - nowUtc;
        }

        // Новые сутки — счётчик с нуля; время в прошлом относительно последнего просмотра — прижать к now.
        private void Normalize(DateTime nowUtc)
        {
            long today = nowUtc.Date.Ticks;
            if (_dayTicks != today)
            {
                if (today > _dayTicks)
                    ClaimsToday = 0;
                _dayTicks = today;
            }

            if (_lastClaimTicks > nowUtc.Ticks)
                _lastClaimTicks = nowUtc.Ticks;
        }
    }
}
