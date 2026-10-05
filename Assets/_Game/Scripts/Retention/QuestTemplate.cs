using System;

namespace ShaurmaTycoon.Retention
{
    /// <summary>Задание из конфига: сделать Target раз действие Type → награда Reward.</summary>
    [Serializable]
    public struct QuestTemplate
    {
        public QuestType Type;
        public int Target;
        public RewardDefinition Reward;

        public QuestTemplate(QuestType type, int target, RewardDefinition reward)
        {
            Type = type;
            Target = target;
            Reward = reward;
        }
    }
}
