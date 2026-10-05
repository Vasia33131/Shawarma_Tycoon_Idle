using System;
using System.Collections.Generic;
using ShaurmaTycoon.Core;
using ShaurmaTycoon.Save;

namespace ShaurmaTycoon.Retention
{
    /// <summary>
    /// Задания дня (GDD §9): раз в секунду проверяет смену суток (серверное время UTC) и выдаёт новые задания,
    /// принимает прогресс и выдаёт награды. Сам является участником сейва (SaveData.Retention).
    /// </summary>
    public sealed class DailyQuestService : ISaveParticipant, ITickable
    {
        private const float CheckInterval = 1f;

        private readonly DailyQuests _quests;
        private readonly RewardGranter _granter;
        private readonly Func<DateTime> _utcNow;
        private readonly Func<QuestType, int, bool> _isAvailable;
        private readonly Action _saveNow;
        private readonly List<QuestState> _loadBuffer = new List<QuestState>(4);
        private float _timer;

        /// <param name="isAvailable">Можно ли сейчас выполнить задание (тип, цель) — невыполнимые не выдаются.</param>
        public DailyQuestService(DailyQuests quests, RewardGranter granter, Func<DateTime> utcNow, Func<QuestType, int, bool> isAvailable,
            Action saveNow)
        {
            _quests = quests ?? throw new ArgumentNullException(nameof(quests));
            _granter = granter ?? throw new ArgumentNullException(nameof(granter));
            _utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
            _isAvailable = isAvailable;
            _saveNow = saveNow;
        }

        /// <summary>Аргументы — индекс задания и выданная награда.</summary>
        public event Action<int, RewardGrant> Claimed;

        public DailyQuests Quests => _quests;
        public DateTime Now => _utcNow();

        public void Report(QuestType type, int amount)
        {
            _quests.Report(type, amount);
        }

        public RewardGrant Preview(int index)
        {
            return index >= 0 && index < _quests.Active.Count ? _granter.Preview(_quests.Active[index].Template.Reward) : default;
        }

        public bool TryClaim(int index, out RewardGrant grant)
        {
            grant = default;
            if (!_quests.Claim(index))
                return false;

            grant = _granter.Grant(_quests.Active[index].Template.Reward);
            _saveNow?.Invoke();
            Claimed?.Invoke(index, grant);
            return true;
        }

        /// <summary>Проверить смену суток сейчас (новые задания сразу сохраняются).</summary>
        public void Refresh()
        {
            if (_quests.Refresh(_utcNow(), _isAvailable))
                _saveNow?.Invoke();
        }

        public void Tick(float deltaTime)
        {
            _timer += deltaTime;
            if (_timer < CheckInterval)
                return;

            _timer = 0f;
            Refresh();
        }

        public void Capture(SaveData data)
        {
            RetentionSave save = data.Retention ?? (data.Retention = new RetentionSave());
            save.QuestsDayTicks = _quests.DayTicks;
            save.Quests.Clear();
            for (int i = 0; i < _quests.Active.Count; i++)
            {
                DailyQuests.Quest quest = _quests.Active[i];
                save.Quests.Add(new QuestSave { Type = (int)quest.Type, Progress = quest.Progress, Claimed = quest.Claimed });
            }
        }

        public void Apply(SaveData data)
        {
            RetentionSave save = data.Retention ?? new RetentionSave();
            _loadBuffer.Clear();
            if (save.Quests != null)
            {
                foreach (QuestSave quest in save.Quests)
                {
                    if (quest != null)
                        _loadBuffer.Add(new QuestState((QuestType)quest.Type, quest.Progress, quest.Claimed));
                }
            }
            _quests.Load(save.QuestsDayTicks, _loadBuffer);
            _quests.Refresh(_utcNow(), _isAvailable);
        }
    }
}
