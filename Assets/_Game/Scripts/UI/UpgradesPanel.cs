using System.Collections.Generic;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Localization;
using ShaurmaTycoon.Stations;
using ShaurmaTycoon.Upgrades;
using UnityEngine;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Вкладка «Улучшения»: некупленные улучшения по возрастанию цены; купленные прячутся.
    /// </summary>
    public sealed class UpgradesPanel : MonoBehaviour
    {
        [SerializeField] private RectTransform _content = null;
        [SerializeField] private UpgradeRowView _rowPrefab = null;
        [Tooltip("Показывается, когда куплено всё.")]
        [SerializeField] private GameObject _allPurchased = null;

        private readonly List<UpgradeRowView> _rows = new List<UpgradeRowView>(32);
        private UpgradeService _upgrades;

        public void Bind(UpgradeService upgrades, StationController stations, Wallet wallet, Loc loc)
        {
            _upgrades = upgrades;

            var order = new List<int>(upgrades.Count);
            for (int i = 0; i < upgrades.Count; i++)
                order.Add(i);
            order.Sort((a, b) => upgrades.GetDefinition(a).Cost.CompareTo(upgrades.GetDefinition(b).Cost));

            for (int i = 0; i < upgrades.Count; i++)
                _rows.Add(null);

            foreach (int index in order)
            {
                UpgradeRowView row = Instantiate(_rowPrefab, _content);
                row.name = $"UpgradeRow_{upgrades.GetDefinition(index).Id}";
                row.Bind(index, upgrades, stations, wallet, loc);
                _rows[index] = row;
            }

            _upgrades.UpgradePurchased += OnUpgradePurchased;
            RefreshVisibility();
        }

        private void OnDestroy()
        {
            if (_upgrades != null)
                _upgrades.UpgradePurchased -= OnUpgradePurchased;
        }

        private void OnUpgradePurchased(int index)
        {
            RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            bool anyLeft = false;
            for (int i = 0; i < _rows.Count; i++)
            {
                bool purchased = _upgrades.IsPurchased(i);
                _rows[i].gameObject.SetActive(!purchased);
                anyLeft |= !purchased;
            }

            if (_allPurchased != null)
                _allPurchased.SetActive(!anyLeft);
        }
    }
}
