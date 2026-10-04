using System.Globalization;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Localization;
using ShaurmaTycoon.Stations;
using ShaurmaTycoon.Upgrades;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Строка улучшения: «Станция: доход ×3» и кнопка покупки. Купленное улучшение прячется.
    /// </summary>
    public sealed class UpgradeRowView : MonoBehaviour, ISceneBinding
    {
        [SerializeField] private Image _icon = null;
        [SerializeField] private TMP_Text _iconLabel = null;
        [SerializeField] private TMP_Text _descriptionText = null;
        [SerializeField] private Button _buyButton = null;
        [SerializeField] private TMP_Text _buyText = null;

        private int _index;
        private UpgradeService _upgrades;
        private StationController _stations;
        private Wallet _wallet;
        private Loc _loc;
        private bool _shownAffordable;

        public void Bind(int index, UpgradeService upgrades, StationController stations, Wallet wallet, Loc loc)
        {
            SceneBindings.Track(this);
            _index = index;
            _upgrades = upgrades;
            _stations = stations;
            _wallet = wallet;
            _loc = loc;

            int target = upgrades.GetTargetStationIndex(index);
            _icon.color = StationColors.Get(target);
            _iconLabel.text = "×" + FormatMultiplier(upgrades.GetDefinition(index).IncomeMultiplier);
            _buyButton.onClick.AddListener(OnBuy);

            _wallet.CoinsChanged += OnCoinsChanged;
            _loc.LanguageChanged += ForceRefresh;

            ForceRefresh();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        public void Unbind()
        {
            if (_wallet == null)
                return;

            _wallet.CoinsChanged -= OnCoinsChanged;
            _loc.LanguageChanged -= ForceRefresh;
        }

        private void OnBuy()
        {
            _upgrades.TryBuy(_index);
        }

        private void OnCoinsChanged(double coins)
        {
            RefreshAffordable(force: false);
        }

        private void ForceRefresh()
        {
            IUpgradeDefinition definition = _upgrades.GetDefinition(_index);
            int target = _upgrades.GetTargetStationIndex(_index);
            string multiplier = FormatMultiplier(definition.IncomeMultiplier);

            _descriptionText.text = target == UpgradeService.AllStations
                ? _loc.Format("upgrade.all", multiplier)
                : _loc.Format("upgrade.station", _loc.Get(_stations.Stations[target].Definition.NameKey), multiplier);
            _buyText.text = _loc.Format("upgrade.buy", BigNumberFormatter.Format(definition.Cost));

            RefreshAffordable(force: true);
        }

        private void RefreshAffordable(bool force)
        {
            if (_upgrades.IsPurchased(_index))
                return;

            bool affordable = _wallet.CanAfford(_upgrades.GetDefinition(_index).Cost);
            if (!force && affordable == _shownAffordable)
                return;

            _shownAffordable = affordable;
            _buyButton.interactable = affordable;
        }

        private static string FormatMultiplier(double multiplier)
        {
            return multiplier.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
