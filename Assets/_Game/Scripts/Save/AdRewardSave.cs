using System;

namespace ShaurmaTycoon.Save
{
    /// <summary>Купюры за рекламу: время последнего просмотра и счётчик за сутки UTC (ticks).</summary>
    [Serializable]
    public sealed class AdRewardSave
    {
        public long LastClaimUtcTicks;
        public long DayUtcTicks;
        public int ClaimsToday;
    }
}
