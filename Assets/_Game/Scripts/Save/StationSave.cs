using System;

namespace ShaurmaTycoon.Save
{
    /// <summary>Сохранённое состояние одной станции (ищется по Id из StationConfig).</summary>
    [Serializable]
    public sealed class StationSave
    {
        public string Id;
        public int Level;
        public bool Automated;
        public double Progress;
        public bool Running;
    }
}
