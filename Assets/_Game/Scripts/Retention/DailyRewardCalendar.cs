using System;

namespace ShaurmaTycoon.Retention
{
    /// <summary>
    /// 7-дневный календарь (GDD §9): одна награда за сутки UTC. Пропуск дня календарь не сбрасывает —
    /// следующий вход даёт следующий день; после 7-го снова 1-й. Перевод часов назад повторной награды не даёт.
    /// </summary>
    public sealed class DailyRewardCalendar
    {
        public const int DayCount = 7;

        /// <summary>Индекс дня (0..6), который будет выдан следующим.</summary>
        public int NextDay { get; private set; }

        /// <summary>Начало суток UTC последней награды (0 — ещё не забирали).</summary>
        public long LastClaimDayTicks { get; private set; }

        public bool CanClaim(DateTime nowUtc)
        {
            Normalize(nowUtc);
            return LastClaimDayTicks == 0 || RewardMath.DayTicks(nowUtc) > LastClaimDayTicks;
        }

        /// <summary>Сколько ждать следующую награду (0 — можно забрать).</summary>
        public TimeSpan TimeUntilNext(DateTime nowUtc)
        {
            if (CanClaim(nowUtc))
                return TimeSpan.Zero;

            DateTime next = new DateTime(LastClaimDayTicks, DateTimeKind.Utc).AddDays(1);
            TimeSpan left = next - nowUtc;
            return left > TimeSpan.Zero ? left : TimeSpan.Zero;
        }

        /// <returns>Индекс выданного дня или -1, если сегодня награда уже получена.</returns>
        public int Claim(DateTime nowUtc)
        {
            if (!CanClaim(nowUtc))
                return -1;

            int day = NextDay;
            NextDay = (NextDay + 1) % DayCount;
            LastClaimDayTicks = RewardMath.DayTicks(nowUtc);
            return day;
        }

        public void Load(int nextDay, long lastClaimDayTicks)
        {
            NextDay = nextDay >= 0 && nextDay < DayCount ? nextDay : 0;
            LastClaimDayTicks = Math.Max(0, lastClaimDayTicks);
        }

        private void Normalize(DateTime nowUtc)
        {
            if (LastClaimDayTicks != 0)
                LastClaimDayTicks = RewardMath.ClampFutureDay(LastClaimDayTicks, nowUtc);
        }
    }
}
