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

        private readonly List<StationRowView> _rows = new List<StationRowView>(8);

        public void Bind(StationController stations, Wallet wallet, Loc loc, CoinFlyEffect coins,
            FloatingTextEffect floatingText, RectTransform hiddenEffectSource, BillOfferPopup offers)
        {
            _buyModeButton.Bind(loc);

            for (int i = 0; i < stations.Stations.Count; i++)
            {
                StationRowView row = Instantiate(_rowPrefab, _content);
                row.name = $"StationRow_{i + 1}";
                row.Bind(i, stations.Stations[i], stations, wallet, loc, _buyModeButton, StationColors.Get(i),
                    coins, floatingText, hiddenEffectSource, offers);
                _rows.Add(row);
            }
        }

        /// <summary>Строка станции index; null — такой строки нет.</summary>
        public StationRowView GetRow(int index)
        {
            return index >= 0 && index < _rows.Count ? _rows[index] : null;
        }

        public void Tick(float deltaTime)
        {
            for (int i = 0; i < _rows.Count; i++)
                _rows[i].TickProgress();
        }
    }
}
