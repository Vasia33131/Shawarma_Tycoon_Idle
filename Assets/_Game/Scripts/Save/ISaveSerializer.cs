namespace ShaurmaTycoon.Save
{
    public interface ISaveSerializer
    {
        string Serialize(SaveData data);

        /// <summary>Разбирает строку; при некорректных данных бросает исключение.</summary>
        SaveData Deserialize(string json);
    }
}
