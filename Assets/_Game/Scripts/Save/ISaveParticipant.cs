namespace ShaurmaTycoon.Save
{
    /// <summary>Система, состояние которой входит в сохранение.</summary>
    public interface ISaveParticipant
    {
        /// <summary>Записать текущее состояние в data.</summary>
        void Capture(SaveData data);

        /// <summary>Восстановить состояние из data.</summary>
        void Apply(SaveData data);
    }
}
