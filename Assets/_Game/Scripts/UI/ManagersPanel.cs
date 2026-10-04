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

        public void Bind(ManagerController managers, StationController stations, Wallet wallet, Loc loc)
        {
            for (int i = 0; i < stations.Stations.Count; i++)
            {
                ManagerRowView row = Instantiate(_rowPrefab, _content);
                row.name = $"ManagerRow_{i + 1}";
                row.Bind(i, stations.Stations[i], managers, wallet, loc);
            }
        }
    }
}
