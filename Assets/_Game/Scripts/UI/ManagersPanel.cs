using System.Collections.Generic;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Localization;
using ShaurmaTycoon.Managers;
using ShaurmaTycoon.Stations;
using UnityEngine;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Вкладка «Менеджеры»: по строке на каждую станцию города.
    /// </summary>
    public sealed class ManagersPanel : MonoBehaviour
    {
        [SerializeField] private RectTransform _content = null;
        [SerializeField] private ManagerRowView _rowPrefab = null;

        private readonly List<ManagerRowView> _rows = new List<ManagerRowView>(8);

        public void Bind(ManagerController managers, StationController stations, Wallet wallet, Loc loc, BillOfferPopup offers)
        {
            for (int i = 0; i < stations.Stations.Count; i++)
            {
                ManagerRowView row = Instantiate(_rowPrefab, _content);
                row.name = $"ManagerRow_{i + 1}";
                row.Bind(i, stations.Stations[i], managers, wallet, loc, offers);
                _rows.Add(row);
            }
        }

        /// <summary>Строка менеджера станции index; null — такой строки нет.</summary>
        public ManagerRowView GetRow(int index)
        {
            return index >= 0 && index < _rows.Count ? _rows[index] : null;
        }
    }
}
