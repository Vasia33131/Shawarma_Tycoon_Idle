using System;
using System.Collections.Generic;

namespace ShaurmaTycoon.Retention
{
    /// <summary>
    /// Задания дня (GDD §9): каждые сутки UTC — новые questsPerDay заданий разных типов из пула конфига.
    /// Задание, которое сейчас невыполнимо (все менеджеры наняты, улучшения куплены), не выдаётся.
    /// </summary>
    public sealed class DailyQuests
    {
        public sealed class Quest
        {
            internal Quest(QuestTemplate template, int progress, bool claimed)
            {
                Template = template;
                Progress = Math.Max(0, Math.Min(progress, Target));
                Claimed = claimed;
            }

            public QuestTemplate Template { get; }
            public QuestType Type => Template.Type;
            public int Target => Math.Max(1, Template.Target);
            public int Progress { get; internal set; }
            public bool Claimed { get; internal set; }
            public bool IsComplete => Progress >= Target;
            public bool CanClaim => IsComplete && !Claimed;
        }

        private readonly QuestTemplate[] _templates;
        private readonly int _questsPerDay;
        private readonly List<Quest> _active = new List<Quest>(4);
        private readonly List<int> _candidates = new List<int>(8);

        public DailyQuests(QuestTemplate[] templates, int questsPerDay)
        {
            _templates = templates ?? new QuestTemplate[0];
            _questsPerDay = Math.Max(1, questsPerDay);
        }

        /// <summary>Прогресс, «забрано» или набор заданий изменились.</summary>
        public event Action Changed;

        public IReadOnlyList<Quest> Active => _active;

        /// <summary>Начало суток UTC, для которых выданы текущие задания.</summary>
        public long DayTicks { get; private set; }

        public bool HasClaimable
        {
            get
            {
                for (int i = 0; i < _active.Count; i++)
                {
                    if (_active[i].CanClaim)
                        return true;
                }
                return false;
            }
        }

        /// <summary>Сколько осталось до новых заданий (до полуночи UTC).</summary>
        public static TimeSpan TimeUntilNewDay(DateTime nowUtc)
        {
            return nowUtc.Date.AddDays(1) - nowUtc;
        }

        /// <summary>Новые сутки (или заданий нет) — выдать новые. Вызывать регулярно.</summary>
        /// <param name="isAvailable">Можно ли сейчас выполнить задание (тип, цель).</param>
        /// <returns>true — выданы новые задания.</returns>
        public bool Refresh(DateTime nowUtc, Func<QuestType, int, bool> isAvailable)
        {
            long today = RewardMath.DayTicks(nowUtc);
            if (DayTicks != 0)
                DayTicks = RewardMath.ClampFutureDay(DayTicks, nowUtc);

            bool newDay = DayTicks == 0 || today > DayTicks;
            if (!newDay && _active.Count > 0)
                return false;

            Generate(newDay ? today : DayTicks, isAvailable);
            return true;
        }

        /// <summary>Игрок сделал действие amount раз.</summary>
        public void Report(QuestType type, int amount)
        {
            if (amount <= 0)
                return;

            bool changed = false;
            for (int i = 0; i < _active.Count; i++)
            {
                Quest quest = _active[i];
                if (quest.Type != type || quest.IsComplete)
                    continue;

                quest.Progress = (int)Math.Min(quest.Target, (long)quest.Progress + amount);
                changed = true;
            }

            if (changed)
                Changed?.Invoke();
        }

        /// <returns>false — задание не выполнено или награда уже забрана.</returns>
        public bool Claim(int index)
        {
            if (index < 0 || index >= _active.Count || !_active[index].CanClaim)
                return false;

            _active[index].Claimed = true;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Восстановить из сейва. Тип, которого больше нет в конфиге, и повтор типа пропускаются.</summary>
        public void Load(long dayTicks, IReadOnlyList<QuestState> quests)
        {
            DayTicks = Math.Max(0, dayTicks);
            _active.Clear();
            if (quests != null)
            {
                for (int i = 0; i < quests.Count && _active.Count < _questsPerDay; i++)
                {
                    int template = FindTemplate(quests[i].Type);
                    if (template >= 0 && !HasType(quests[i].Type))
                        _active.Add(new Quest(_templates[template], quests[i].Progress, quests[i].Claimed));
                }
            }
            Changed?.Invoke();
        }

        private void Generate(long dayTicks, Func<QuestType, int, bool> isAvailable)
        {
            DayTicks = dayTicks;
            _active.Clear();

            _candidates.Clear();
            for (int i = 0; i < _templates.Length; i++)
            {
                QuestTemplate template = _templates[i];
                if (template.Target > 0 && (isAvailable == null || isAvailable(template.Type, template.Target)))
                    _candidates.Add(i);
            }

            // Случайно, но одинаково для одних суток.
            var random = new Random((int)(dayTicks / TimeSpan.TicksPerDay));
            for (int i = _candidates.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                int swap = _candidates[i];
                _candidates[i] = _candidates[j];
                _candidates[j] = swap;
            }

            for (int i = 0; i < _candidates.Count && _active.Count < _questsPerDay; i++)
            {
                QuestTemplate template = _templates[_candidates[i]];
                if (!HasType(template.Type))
                    _active.Add(new Quest(template, 0, false));
            }

            Changed?.Invoke();
        }

        private int FindTemplate(QuestType type)
        {
            for (int i = 0; i < _templates.Length; i++)
            {
                if (_templates[i].Type == type && _templates[i].Target > 0)
                    return i;
            }
            return -1;
        }

        private bool HasType(QuestType type)
        {
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i].Type == type)
                    return true;
            }
            return false;
        }
    }
}
