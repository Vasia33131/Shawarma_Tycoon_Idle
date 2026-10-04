using ShaurmaTycoon.Core;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Localization;
using ShaurmaTycoon.Stations;
using TMPro;
using UnityEngine;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Верхняя панель: монеты и доход/сек. Тексты обновляются не чаще 10 раз в секунду и только при изменении.
    /// </summary>
    public sealed class HudView : MonoBehaviour, ITickable
    {
        private const float RefreshInterval = 0.1f;

        [SerializeField] private TMP_Text _coinsText = null;
        [SerializeField] private TMP_Text _incomeText = null;

        private Wallet _wallet;
        private StationController _stations;
        private Loc _loc;
        private double _shownCoins = double.NaN;
        private double _shownIncome = double.NaN;
        private float _timer;

        public void Bind(Wallet wallet, StationController stations, Loc loc)
        {
            _wallet = wallet;
            _stations = stations;
            _loc = loc;
            _loc.LanguageChanged += ForceRefresh;
            ForceRefresh();
        }

        private void OnDestroy()
        {
            if (_loc != null)
                _loc.LanguageChanged -= ForceRefresh;
        }

        public void Tick(float deltaTime)
        {
            _timer += deltaTime;
            if (_timer < RefreshInterval)
                return;

            _timer = 0f;
            Refresh(force: false);
        }

        private void ForceRefresh()
        {
            Refresh(force: true);
        }

        private void Refresh(bool force)
        {
            double coins = _wallet.Coins;
            if (force || coins != _shownCoins)
            {
                _shownCoins = coins;
                _coinsText.text = BigNumberFormatter.Format(coins);
            }

            double income = _stations.IncomePerSecond;
            if (force || income != _shownIncome)
            {
                _shownIncome = income;
                bool hasIncome = income > 0;
                _incomeText.gameObject.SetActive(hasIncome);
                if (hasIncome)
                    _incomeText.text = _loc.Format("hud.income_per_sec", BigNumberFormatter.Format(income));
            }
        }
    }
}
