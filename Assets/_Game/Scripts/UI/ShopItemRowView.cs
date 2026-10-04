using System.Globalization;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Localization;
using ShaurmaTycoon.Prestige;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Пункт магазина репутации: название, эффект «сейчас → после покупки», уровень и кнопка покупки за репутацию.
    /// Обновляется только при изменении репутации или языка.
    /// </summary>
    public sealed class ShopItemRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameText = null;
        [SerializeField] private TMP_Text _descriptionText = null;
        [SerializeField] private TMP_Text _levelText = null;
        [SerializeField] private Button _buyButton = null;
        [SerializeField] private TMP_Text _buyText = null;

        private int _index;
        private ReputationShop _shop;
        private PrestigeState _state;
        private Loc _loc;
        private double _baseOfflineHours;
        private double _cityValueScale;

        /// <param name="baseOfflineHours">Лимит офлайна без покупок — чтобы показывать итоговый лимит.</param>
        /// <param name="cityValueScale">Масштаб значений текущего города — для стартового капитала.</param>
        public void Bind(int index, ReputationShop shop, PrestigeState state, Loc loc, double baseOfflineHours, double cityValueScale)
        {
            _index = index;
            _shop = shop;
            _state = state;
            _loc = loc;
            _baseOfflineHours = baseOfflineHours;
            _cityValueScale = cityValueScale;

            _buyButton.onClick.AddListener(OnBuy);
            _state.Changed += Refresh;
            _loc.LanguageChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_state == null)
                return;
            _state.Changed -= Refresh;
            _loc.LanguageChanged -= Refresh;
        }

        private void OnBuy()
        {
            _shop.TryBuy(_index);
        }

        private void Refresh()
        {
            ShopItemConfig item = _shop.GetItem(_index);
            int level = _shop.GetLevel(_index);
            bool maxed = _shop.IsMaxed(_index);

            _nameText.text = _loc.Get(item.NameKey);
            string description = _loc.Format(item.DescriptionKey, FormatValue(item.Effect, item.GetValue(level)));
            _descriptionText.text = maxed ? description : description + "  →  " + FormatValue(item.Effect, item.GetValue(level + 1));
            _levelText.text = _loc.Format("shop.level", level, item.MaxLevel);

            _buyText.text = maxed
                ? _loc.Get("shop.max")
                : _loc.Format("shop.buy", _shop.GetNextPrice(_index).ToString(CultureInfo.InvariantCulture));
            _buyButton.interactable = !maxed && _shop.CanBuy(_index);
        }

        private string FormatValue(ShopEffect effect, double value)
        {
            switch (effect)
            {
                case ShopEffect.OfflineLimitHours:
                    return _loc.Format("time.hours", (_baseOfflineHours + value).ToString("0.#", CultureInfo.InvariantCulture));
                case ShopEffect.StartCoins:
                    return BigNumberFormatter.Format(value * _cityValueScale);
                case ShopEffect.FreeManagers:
                    return ((long)value).ToString(CultureInfo.InvariantCulture);
                case ShopEffect.CycleTimeFactor:
                    return ((1 - value) * 100).ToString("0", CultureInfo.InvariantCulture) + "%";
                default:
                    return value.ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}
