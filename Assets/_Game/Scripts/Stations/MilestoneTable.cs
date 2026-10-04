using System;
using System.Collections.Generic;

namespace ShaurmaTycoon.Stations
{
    /// <summary>
    /// Вехи уровней (GDD §5): на каждой достигнутой вехе цикл ускоряется в SpeedMultiplier раз.
    /// </summary>
    public sealed class MilestoneTable
    {
        private readonly int[] _levels;

        public MilestoneTable(IReadOnlyList<int> levels, double speedMultiplier)
        {
            if (levels == null)
                throw new ArgumentNullException(nameof(levels));
            if (!(speedMultiplier >= 1))
                throw new ArgumentOutOfRangeException(nameof(speedMultiplier), speedMultiplier, "Must be >= 1.");

            _levels = new int[levels.Count];
            for (int i = 0; i < _levels.Length; i++)
                _levels[i] = levels[i];
            Array.Sort(_levels);

            SpeedMultiplier = speedMultiplier;
        }

        public double SpeedMultiplier { get; }

        public int CountReached(int level)
        {
            int count = 0;
            for (int i = 0; i < _levels.Length && _levels[i] <= level; i++)
                count++;
            return count;
        }

        /// <summary>Во сколько раз цикл быстрее базового на этом уровне.</summary>
        public double GetSpeedMultiplier(int level)
        {
            return Math.Pow(SpeedMultiplier, CountReached(level));
        }

        /// <summary>Ближайшая веха выше level или -1, если все пройдены.</summary>
        public int GetNextMilestone(int level)
        {
            for (int i = 0; i < _levels.Length; i++)
            {
                if (_levels[i] > level)
                    return _levels[i];
            }
            return -1;
        }
    }
}
