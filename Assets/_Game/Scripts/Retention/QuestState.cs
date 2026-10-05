namespace ShaurmaTycoon.Retention
{
    /// <summary>Сохранённое состояние задания (без шаблона).</summary>
    public readonly struct QuestState
    {
        public QuestState(QuestType type, int progress, bool claimed)
        {
            Type = type;
            Progress = progress;
            Claimed = claimed;
        }

        public QuestType Type { get; }
        public int Progress { get; }
        public bool Claimed { get; }
    }
}
