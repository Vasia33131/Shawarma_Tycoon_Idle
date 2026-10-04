using System;
using UnityEngine;

namespace ShaurmaTycoon.Save
{
    /// <summary>JSON через JsonUtility. Поля, которых нет в строке, остаются со значениями по умолчанию.</summary>
    public sealed class JsonSaveSerializer : ISaveSerializer
    {
        public string Serialize(SaveData data)
        {
            return JsonUtility.ToJson(data);
        }

        public SaveData Deserialize(string json)
        {
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            if (data == null)
                throw new FormatException("Save JSON is empty or not an object.");
            return data;
        }
    }
}
