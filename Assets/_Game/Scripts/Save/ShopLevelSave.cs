using System;

namespace ShaurmaTycoon.Save
{
    /// <summary>Купленный уровень пункта магазина репутации (по Id пункта).</summary>
    [Serializable]
    public sealed class ShopLevelSave
    {
        public string Id;
        public int Level;
    }
}
