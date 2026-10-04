using System.Globalization;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Localization;
using ShaurmaTycoon.Stations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Строка станции: тап по иконке или прогресс-бару запускает цикл, кнопка справа покупает уровни.
    /// Закрытая станция показывает оверлей «Открыть за X».
    /// Тексты обновляются только при изменении данных; каждый кадр меняется лишь ширина прогресс-бара.
    /// </summary>
    public sealed class StationRowView : MonoBehaviour
    {
        // Цикл короче этого показываем полной полосой, иначе она мерцает.
        private const double MinVisibleCycleTime = 0.1;

        [Header("Station")]
        [SerializeField] private Button[] _tapButtons = new Button[0];
        [SerializeField] private Image _icon = null;
        [SerializeField] private TMP_Text _iconLabel = null;
        [SerializeField] private TMP_Text _nameText = null;
        [SerializeField] private TMP_Text _levelText = null;
        [SerializeField] private RectTransform _progressFill = null;
        [SerializeField] private TMP_Text _incomeText = null;
        [SerializeField] private TMP_Text _cycleTimeText = null;
        [SerializeField] private TMP_Text _milestoneText = null;

        [Header("Buy")]
        [SerializeField] private Button _buyButton = null;
        [SerializeField] private TMP_Text _buyCountText = null;
        [SerializeField] private TMP_Text _buyCostText = null;

        [Header("Locked")]
        [SerializeField] private GameObject _lockedRoot = null;
        [SerializeField] private TMP_Text _lockedNameText = null;
        [SerializeField] private Button _unlockButton = null;
        [SerializeField] private TMP_Text _unlockCostText = null;

        private int _index;
        private StationModel _station;
        private StationController _controller;
        private Wallet _wallet;
        private Loc _loc;
        private BuyModeButton _buyMode;

        private int _buyCount;
        private int _shownBuyCount = -1;
        private bool _shownAffordable;
        private bool _shownWaitingForTap;
        private float _shownFill = -1f;

        public void Bind(int index, StationModel station, StationController controller, Wallet wallet, Loc loc,
            BuyModeButton buyMode, Color iconColor)
        {
            _index = index;
            _station = station;
            _controller = controller;
            _wallet = wallet;
            _loc = loc;
            _buyMode = buyMode;

            _icon.color = iconColor;
            _iconLabel.text = (index + 1).ToString(CultureInfo.InvariantCulture);

            for (int i = 0; i < _tapButtons.Length; i++)
                _tapButtons[i].onClick.AddListener(OnTap);
            _buyButton.onClick.AddListener(OnBuy);
            _unlockButton.onClick.AddListener(OnUnlock);

            _station.LevelChanged += OnLevelChanged;
            _wallet.CoinsChanged += OnCoinsChanged;
            _loc.LanguageChanged += RefreshAll;
            _buyMode.ModeChanged += OnBuyModeChanged;

            RefreshAll();
        }

        private void OnDestroy()
        {
            if (_station == null)
                return;

            _station.LevelChanged -= OnLevelChanged;
            _wallet.CoinsChanged -= OnCoinsChanged;
            _loc.LanguageChanged -= RefreshAll;
            _buyMode.ModeChanged -= OnBuyModeChanged;
        }

        /// <summary>Вызывается каждый кадр из StationsPanel.</summary>
        public void TickProgress()
        {
            if (!_station.IsUnlocked)
                return;

            float fill;
            if (_station.IsAutomated && _station.CycleTime < MinVisibleCycleTime)
                fill = 1f;
            else
                fill = _station.IsRunning ? (float)_station.Progress : 0f;

            if (fill != _shownFill)
            {
                _shownFill = fill;
                _progressFill.anchorMax = new Vector2(fill, 1f);
            }

            bool waitingForTap = !_station.IsRunning && !_station.IsAutomated;
            if (waitingForTap != _shownWaitingForTap)
                RefreshIncome();
        }

        private void OnTap()
        {
            _controller.Tap(_index);
        }

        private void OnBuy()
        {
            _controller.TryBuy(_index, _buyCount);
        }

        private void OnUnlock()
        {
            _controller.TryBuy(_index, 1);
        }

        private void OnLevelChanged(StationModel station)
        {
            RefreshAll();
        }

        private void OnCoinsChanged(double coins)
        {
            RefreshBuy(force: false);
        }

        private void OnBuyModeChanged(BuyMode mode)
        {
            RefreshBuy(force: true);
        }

        private void RefreshAll()
        {
            string stationName = _loc.Get(_station.Definition.NameKey);
            bool unlocked = _station.IsUnlocked;

            _lockedRoot.SetActive(!unlocked);
            _nameText.text = stationName;

            if (unlocked)
            {
                _levelText.text = _loc.Format("station.level", _station.Level);
                _cycleTimeText.text = _loc.Format("unit.seconds", FormatSeconds(_station.CycleTime));

                int next = _station.NextMilestone;
                _milestoneText.text = next < 0
                    ? _loc.Get("station.milestone_max")
                    : _loc.Format("station.milestone", next - _station.Level);

                RefreshIncome();
            }
            else
            {
                _lockedNameText.text = stationName;
            }

            RefreshBuy(force: true);
        }

        private void RefreshIncome()
        {
            _shownWaitingForTap = !_station.IsRunning && !_station.IsAutomated;
            string income = BigNumberFormatter.Format(_station.IncomePerCycle);
            _incomeText.text = _shownWaitingForTap ? _loc.Format("station.tap_to_earn", income) : "+" + income;
        }

        private void RefreshBuy(bool force)
        {
            if (!_station.IsUnlocked)
            {
                bool canUnlock = _wallet.CanAfford(_station.GetCost(1));
                if (force || canUnlock != _shownAffordable)
                {
                    _shownAffordable = canUnlock;
                    _unlockButton.interactable = canUnlock;
                    _unlockCostText.text = _loc.Format("station.unlock", BigNumberFormatter.Format(_station.GetCost(1)));
                }
                return;
            }

            _buyCount = GetBuyCount();
            bool affordable = _wallet.CanAfford(_station.GetCost(_buyCount));

            if (force || _buyCount != _shownBuyCount)
            {
                _shownBuyCount = _buyCount;
                _buyCountText.text = _loc.Format("station.buy_count", _buyCount);
                _buyCostText.text = BigNumberFormatter.Format(_station.GetCost(_buyCount));
            }

            if (force || affordable != _shownAffordable)
            {
                _shownAffordable = affordable;
                _buyButton.interactable = affordable;
            }
        }

        private int GetBuyCount()
        {
            switch (_buyMode.Mode)
            {
                case BuyMode.Ten:
                    return 10;
                case BuyMode.Max:
                    return Mathf.Max(1, _station.GetMaxAffordable(_wallet.Coins));
                default:
                    return 1;
            }
        }

        private static string FormatSeconds(double seconds)
        {
            return seconds.ToString(seconds < 0.1 ? "0.###" : "0.##", CultureInfo.InvariantCulture);
        }
    }
}
