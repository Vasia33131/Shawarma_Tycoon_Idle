using System.Collections.Generic;
using ShaurmaTycoon.Core;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Localization;
using ShaurmaTycoon.Stations;
using UnityEngine;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Вкладка «Станции»: создаёт строки из префаба и каждый кадр обновляет их прогресс-бары.
    /// </summary>
    public sealed class StationsPanel : MonoBehaviour, ITickable
    {
        [SerializeField] private RectTransform _content = null;
        [SerializeField] private StationRowView _rowPrefab = null;
        [SerializeField] private BuyModeButton _buyModeButton = null;
        [Tooltip("Цвета кругов-иконок станций по порядку (пока нет графики).")]
        [SerializeField] private Color[] _iconColors =
        {
            new Color(0.91f, 0.45f, 0.18f),
            new Color(0.96f, 0.73f, 0.15f),
            new Color(0.85f, 0.62f, 0.25f),
            new Color(0.30f, 0.65f, 0.80f),
            new Color(0.80f, 0.30f, 0.25f),
            new Color(0.45f, 0.30f, 0.20f),
        };

        private readonly List<StationRowView> _rows = new List<StationRowView>(8);

        public void Bind(StationController stations, Wallet wallet, Loc loc)
        {
            _buyModeButton.Bind(loc);

            for (int i = 0; i < stations.Stations.Count; i++)
            {
                StationRowView row = Instantiate(_rowPrefab, _content);
                row.name = $"StationRow_{i + 1}";
                Color color = _iconColors.Length > 0 ? _iconColors[i % _iconColors.Length] : Color.white;
                row.Bind(i, stations.Stations[i], stations, wallet, loc, _buyModeButton, color);
                _rows.Add(row);
            }
        }

        public void Tick(float deltaTime)
        {
            for (int i = 0; i < _rows.Count; i++)
                _rows[i].TickProgress();
        }
    }
}
