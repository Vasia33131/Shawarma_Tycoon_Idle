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
    /// Часто меняющиеся тексты (доход, цена покупки) собираются в NumberText — без аллокаций;
    /// уровень, время цикла и веха — обычными строками, они меняются только при покупке.
    /// По завершении цикла из иконки летит монета, над полосой всплывает «+X».
    /// </summary>
    public sealed class StationRowView : MonoBehaviour, ISceneBinding
    {
        // Цикл короче этого показываем полной полосой, иначе она мерцает.
        private const double MinVisibleCycleTime = 0.1;
        // Эффекты (монета, «+X») от одной станции — не чаще, чем раз в столько секунд.
        private const float MinEffectInterval = 0.25f;

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
        [Tooltip("Пульсирует, пока станция ждёт тапа.")]
        [SerializeField] private PulseEffect _tapPulse = null;

        [Header("Buy")]
        [SerializeField] private Button _buyButton = null;
        [SerializeField] private TMP_Text _buyCountText = null;
        [SerializeField] private TMP_Text _buyCostText = null;

        [Header("Locked")]
        [SerializeField] private GameObject _lockedRoot = null;
        [SerializeField] private TMP_Text _lockedNameText = null;
        [SerializeField] private Button _unlockButton = null;
        [SerializeField] private TMP_Text _unlockCostText = null;

        private readonly NumberText _incomeBuilder = new NumberText();
        private readonly NumberText _countBuilder = new NumberText();
        private readonly NumberText _costBuilder = new NumberText();
        private readonly NumberText _effectBuilder = new NumberText();

        private int _index;
        private StationModel _station;
        private StationController _controller;
        private Wallet _wallet;
        private Loc _loc;
        private BuyModeButton _buyMode;
        private CoinFlyEffect _coins;
        private FloatingTextEffect _floatingText;
        private RectTransform _hiddenEffectSource;

        private LocTemplate _tapTemplate;
        private LocTemplate _countTemplate;
        private LocTemplate _unlockTemplate;

        private int _buyCount;
        private int _shownBuyCount = -1;
        private bool _shownAffordable;
        private bool _shownWaitingForTap;
        private float _shownFill = -1f;
        private float _lastEffectTime = float.NegativeInfinity;

        /// <param name="hiddenEffectSource">Откуда летят монеты, когда строка не видна (окно ларька).</param>
        public void Bind(int index, StationModel station, StationController controller, Wallet wallet, Loc loc,
            BuyModeButton buyMode, Color iconColor, CoinFlyEffect coins, FloatingTextEffect floatingText,
            RectTransform hiddenEffectSource)
        {
            SceneBindings.Track(this);
            _index = index;
            _station = station;
            _controller = controller;
            _wallet = wallet;
            _loc = loc;
            _buyMode = buyMode;
            _coins = coins;
            _floatingText = floatingText;
            _hiddenEffectSource = hiddenEffectSource;

            _tapTemplate = loc.GetTemplate("station.tap_to_earn");
            _countTemplate = loc.GetTemplate("station.buy_count");
            _unlockTemplate = loc.GetTemplate("station.unlock");

            _icon.color = iconColor;
            _iconLabel.text = (index + 1).ToString(CultureInfo.InvariantCulture);

            for (int i = 0; i < _tapButtons.Length; i++)
                _tapButtons[i].onClick.AddListener(OnTap);
            _buyButton.onClick.AddListener(OnBuy);
            _unlockButton.onClick.AddListener(OnUnlock);

            _station.Changed += OnStationChanged;
            _station.CycleCompleted += OnCycleCompleted;
            _wallet.CoinsChanged += OnCoinsChanged;
            _loc.LanguageChanged += RefreshAll;
            _buyMode.ModeChanged += OnBuyModeChanged;

            RefreshAll();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        public void Unbind()
        {
            if (_station == null)
                return;

            _station.Changed -= OnStationChanged;
            _station.CycleCompleted -= OnCycleCompleted;
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

        private void OnStationChanged(StationModel station)
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

        private void OnCycleCompleted(StationModel station, double income)
        {
            float now = Time.unscaledTime;
            if (now - _lastEffectTime < MinEffectInterval)
                return;
            _lastEffectTime = now;

            if (!gameObject.activeInHierarchy)
            {
                if (_hiddenEffectSource != null)
                    _coins.Fly(_hiddenEffectSource.position);
                return;
            }

            _coins.Fly(_icon.rectTransform.position);
            _floatingText.Show(_progressFill.parent.position, _effectBuilder.Clear().Append('+').AppendNumber(income));
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
            if (_tapPulse != null)
                _tapPulse.Active = _shownWaitingForTap;

            _incomeBuilder.Clear();
            if (_shownWaitingForTap)
                _incomeBuilder.Append(_tapTemplate.Prefix).AppendNumber(_station.IncomePerCycle).Append(_tapTemplate.Suffix);
            else
                _incomeBuilder.Append('+').AppendNumber(_station.IncomePerCycle);
            _incomeText.SetNumberText(_incomeBuilder);
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
                    _costBuilder.Clear().Append(_unlockTemplate.Prefix).AppendNumber(_station.GetCost(1)).Append(_unlockTemplate.Suffix);
                    _unlockCostText.SetNumberText(_costBuilder);
                }
                return;
            }

            _buyCount = GetBuyCount();
            bool affordable = _wallet.CanAfford(_station.GetCost(_buyCount));

            if (force || _buyCount != _shownBuyCount)
            {
                _shownBuyCount = _buyCount;
                _countBuilder.Clear().Append(_countTemplate.Prefix).AppendInteger(_buyCount).Append(_countTemplate.Suffix);
                _buyCountText.SetNumberText(_countBuilder);
                _buyCostText.SetNumberText(_costBuilder.Clear().AppendNumber(_station.GetCost(_buyCount)));
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
