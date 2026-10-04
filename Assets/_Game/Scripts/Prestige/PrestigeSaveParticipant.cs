using System;
using System.Collections.Generic;
using ShaurmaTycoon.Save;

namespace ShaurmaTycoon.Prestige
{
    /// <summary>Сохраняет PrestigeState в SaveData.Prestige. Живёт в Boot — переживает перезагрузку сцены.</summary>
    public sealed class PrestigeSaveParticipant : ISaveParticipant
    {
        private readonly PrestigeState _state;

        public PrestigeSaveParticipant(PrestigeState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public void Capture(SaveData data)
        {
            PrestigeSave save = data.Prestige ?? (data.Prestige = new PrestigeSave());
            save.CityIndex = _state.CityIndex;
            save.CityEarned = _state.CityEarned;
            save.LifetimeEarned = _state.LifetimeEarned;
            save.ReputationEarned = _state.ReputationEarned;
            save.ReputationSpent = _state.ReputationSpent;

            save.ShopLevels.Clear();
            foreach (var pair in _state.ShopLevels)
                save.ShopLevels.Add(new ShopLevelSave { Id = pair.Key, Level = pair.Value });
        }

        public void Apply(SaveData data)
        {
            PrestigeSave save = data.Prestige ?? new PrestigeSave();
            var levels = new List<KeyValuePair<string, int>>(save.ShopLevels.Count);
            foreach (ShopLevelSave level in save.ShopLevels)
                levels.Add(new KeyValuePair<string, int>(level.Id, level.Level));

            _state.Load(save.CityIndex, save.CityEarned, save.LifetimeEarned, save.ReputationEarned, save.ReputationSpent, levels);
        }
    }
}
