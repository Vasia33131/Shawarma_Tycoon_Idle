using System;
using System.Collections.Generic;

namespace ShaurmaTycoon.Save
{
    /// <summary>Удержание: календарь наград, задания дня, буст ×2, подарок-ящик и скин ларька. Время — ticks UTC.</summary>
    [Serializable]
    public sealed class RetentionSave
    {
        /// <summary>Какой день календаря (0..6) будет следующим.</summary>
        public int DailyNextDay;

        /// <summary>Начало суток UTC, в которые награда календаря забрана в последний раз (0 — ни разу).</summary>
        public long DailyLastClaimDayTicks;

        /// <summary>Скин ларька за 7-й день получен.</summary>
        public bool KioskSkinOwned;

        /// <summary>Начало суток UTC, для которых выданы задания.</summary>
        public long QuestsDayTicks;
        public List<QuestSave> Quests = new List<QuestSave>();

        /// <summary>Сколько секунд буста ×2 осталось (идёт только во время игры).</summary>
        public double BoostSeconds;

        /// <summary>Когда последний раз открыт подарок-ящик (0 — ни разу).</summary>
        public long GiftLastClaimUtcTicks;
    }
}
