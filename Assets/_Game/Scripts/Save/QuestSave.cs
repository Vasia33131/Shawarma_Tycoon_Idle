using System;

namespace ShaurmaTycoon.Save
{
    /// <summary>Задание дня: тип (int значения QuestType), прогресс, награда забрана.</summary>
    [Serializable]
    public sealed class QuestSave
    {
        public int Type;
        public int Progress;
        public bool Claimed;
    }
}
